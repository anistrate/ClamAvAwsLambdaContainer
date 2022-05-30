using Amazon.Lambda.Core;
using nClam;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ClamAvAwsLambdaContainer.Services
{
    namespace ClamAvAwsLambdaContainer.Services
    {
        public class ClamServer
        {
            private const string LocalhostAddress = "127.0.0.1";
            private const int Port = 3310;
            private const string ProcessName = "clamd";
            private const string DaemonPath = "/usr/sbin/clamd";
            private const int MaxTimeout = 120;

            public void Start()
            {
                var processStartInfo = new ProcessStartInfo(DaemonPath);
                Process.Start(processStartInfo);
            }

            public bool IsRunning()
            {
                return Process.GetProcesses().Any(x => x.ProcessName == ProcessName);
            }

            public bool IsReadyToScan()
            {
                var iteration = 1;
                var clamdServerIsListeningOnPort = false;
                do
                {
                    clamdServerIsListeningOnPort = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == Port);
                    iteration++;
                }
                while (!clamdServerIsListeningOnPort && iteration < MaxTimeout);
                return clamdServerIsListeningOnPort;
            }

            public async Task<ClamScanResult> ScanFile(Stream stream)
            {
                var clamClient = new ClamClient(IPAddress.Parse(LocalhostAddress), Port);
                return await clamClient.SendAndScanFileAsync(stream);
            }

            public void LogCurrentlyRunningProcesses()
            {
                LambdaLogger.Log("Running processes:");
                var processesRunning = Process.GetProcesses();
                foreach (var process in processesRunning)
                {
                    LambdaLogger.Log($"Process: {process.ProcessName} ID: {process.Id} Handle: {process.Handle} StartTime: {process.StartTime}");
                }

            }


        }
    }

}