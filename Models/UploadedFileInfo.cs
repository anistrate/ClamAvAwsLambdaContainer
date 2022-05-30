using Amazon.Lambda.S3Events;
using System.Linq;

namespace ClamAvAwsLambdaContainer.Models
{
    public class UploadedFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public string BucketName { get; set; }

        public UploadedFileInfo(S3Event s3Event)
        {
            BucketName = s3Event.Records[0].S3.Bucket.Name;
            FilePath = s3Event.Records[0].S3.Object.Key;
            FileName = s3Event.Records[0].S3.Object.Key.Split("/").Last();
            FileExtension = s3Event.Records[0].S3.Object.Key.Split("/").Last().Split(".").Last();
        }
    }
}