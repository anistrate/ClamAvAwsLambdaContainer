using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using ClamAvAwsLambdaContainer.Models;
using ClamAvAwsLambdaContainer.Services.ClamAvAwsLambdaContainer.Services;
using nClam;
using System;
using System.Linq;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ClamAvAwsLambdaContainer
{
    public class Function
    {

        private readonly AmazonS3Client _s3Client = new AmazonS3Client(RegionEndpoint.EUWest1);

        public async Task FunctionHandler(S3Event fileUploadedEvent, ILambdaContext context)
        {
            try
            {
                var uploadedFileInfo = new UploadedFileInfo(fileUploadedEvent);
                var clamServer = new ClamServer();

                if (!clamServer.IsRunning()) clamServer.Start();
                if (clamServer.IsReadyToScan())
                {
                    var s3ObjectRequest = new GetObjectRequest()
                    {
                        BucketName = uploadedFileInfo.BucketName,
                        Key = uploadedFileInfo.FilePath
                    };

                    var s3ObjectToScan = await _s3Client.GetObjectAsync(s3ObjectRequest);
                    var scanResult = await clamServer.ScanFile(s3ObjectToScan.ResponseStream);
                    switch (scanResult.Result)
                    {
                        case ClamScanResults.Clean:
                            LambdaLogger.Log($"File {uploadedFileInfo.FileName} is clean");
                            break;
                        case ClamScanResults.VirusDetected:
                            LambdaLogger.Log($"Virus detected: {scanResult.InfectedFiles?.Aggregate(" ", (x, virus) => x + (" " + virus.VirusName))}");
                            break;
                        case ClamScanResults.Unknown:
                            LambdaLogger.Log($"Unknown result for file : {uploadedFileInfo.FileName}");
                            break;
                        case ClamScanResults.Error:
                            LambdaLogger.Log($"An error occured for file : {uploadedFileInfo.FileName}");
                            break;
                    }

                }
                else
                {
                    LambdaLogger.Log($"Timeout while waiting for port server to accept connections.");
                    clamServer.LogCurrentlyRunningProcesses();
                }
            }
            catch (Exception exc)
            {
                LambdaLogger.Log($"Exception: {exc.Message}");
            }

        }
    }
}