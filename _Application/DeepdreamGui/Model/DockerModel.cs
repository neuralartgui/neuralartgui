using DeepdreamGui.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// Class to test and control Docker
    /// </summary>
    public static class DockerModel
    {
        private static bool _dockerToolboxMode = true;
        


        /// <summary>
        /// Test if Docker is running in Toolbox Mode
        /// </summary>
        public static bool DockerToolboxMode
        {
            private set { _dockerToolboxMode = value; }
            get
            {
                DockerInstalled();
                return _dockerToolboxMode;
            }
        }

        /// <summary>
        /// Check if Hyper-V is enabled and at least 1 virtual Machine is installed.
        /// </summary>
        /// <returns>HyperV Enabled</returns>
        private static bool TestHyperV()
        {
            try
            {
                ManagementScope manScope = new ManagementScope(@"\\.\root\virtualization\v2");

                // define the information we want to query - in this case, just grab all properties of the object
                ObjectQuery queryObj = new ObjectQuery("SELECT * FROM Msvm_ComputerSystem");

                // connect and set up our search
                ManagementObjectSearcher vmSearcher = new ManagementObjectSearcher(manScope, queryObj);
                ManagementObjectCollection vmCollection = vmSearcher.Get();
                if (vmCollection.Count > 0) return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Return Path of Docker Installation
        /// </summary>
        /// <returns>Path to Docker Executable</returns>
        public static string GetDockerPath()
        {
            string startPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string dockerPath = Path.Combine(startPath, "Docker", "Docker", "Docker for Windows.exe");

            if (File.Exists(dockerPath) && TestHyperV())
            {
                DockerToolboxMode = false;
                return dockerPath;
            }
            return string.Empty;
        }

        /// <summary>
        /// Test if Docker is installed and set DockerToolBoxMode.
        /// </summary>
        /// <returns>Docker is installed</returns>
        public static bool DockerInstalled()
        {

            var argString = $"/c docker-machine inspect";
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = argString
            };
            Process p = new Process { StartInfo = startInfo };
            p.Start();
            p.WaitForExit(5000);
            var output = p.StandardError.ReadToEnd();
            if (output.Contains("Error: No machine"))
            {
                argString = $"/c docker version";
                startInfo.Arguments = argString;

                p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit(5000);
                output = p.StandardOutput.ReadToEnd();
                if (!output.Contains("Version")) return false;
                DockerToolboxMode = false;
                return true;
            }
            DockerToolboxMode = true;
            return true;
        }

        /// <summary>
        /// Return name of DockerToolbox start batch File if neccessary.
        /// </summary>
        public static string DockerStartAddition => DockerToolboxMode ? ".\\start_toolbox.bat" : string.Empty;

        private const int MaxValue = 2400;
        /// <summary>
        /// Start Docker virtual Machine if not running
        /// </summary>
        /// <returns>Task that starts Docker. Can be awaited.</returns>
        public static async Task<bool> StartDockerAsync()
        {
            return await Task<bool>.Factory.StartNew(() =>
            {

                if (!DockerToolboxMode)
                {
                    var path = GetDockerPath();
                    if (String.IsNullOrEmpty(path) || !File.Exists(path)) return false;

                    ProcessStartInfo startInfo = new ProcessStartInfo(path)
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process p = new Process { StartInfo = startInfo };
                    try
                    {
                        p.Start();

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);
                        //Console.WriteLine(ex);
                        return false;
                    }
                }
                else
                {
                    var argString = "/c docker-machine start default";
                    var startInfo = new ProcessStartInfo("cmd.exe")
                    {
                        Arguments = argString,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var p = new Process { StartInfo = startInfo };
                    p.Start();
                    p.WaitForExit(30000);
                }

                int abortVar = 0;
                while (true)
                {
                    var test = TestDockerRunningAsync();
                    test.Wait();

                    if (test.Result || abortVar > MaxValue)
                    {
                        return test.Result;
                    }
                    abortVar++;
                    Thread.Sleep(100);
                }
            });
        }

        /// <summary>
        /// Check if required Docker image is installed
        /// </summary>
        /// <returns>Task that checks docker images. Can be awaited.</returns>
        public static async Task<bool> TestDockerImagesAsync()
        {
            return await Task<bool>.Factory.StartNew(() =>
            {
                var argString = $"/c {DockerStartAddition} docker images";
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
                {
                    Arguments = argString,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit(5000);
                var output = p.StandardOutput.ReadToEnd();
                return output.Contains("muelmx/neuralart_exec");
            });
        }

        /// <summary>
        /// Check if Docker Virtual Machine is Running
        /// </summary>
        /// <returns>Task that checks if Docker is running. Can be awaited.</returns>
        public static async Task<bool> TestDockerRunningAsync()
        {
            return await Task<bool>.Factory.StartNew(() =>
            {
                var argString = $"/c {DockerStartAddition} docker images";
                var startInfo = new ProcessStartInfo("cmd.exe")
                {
                    Arguments = argString,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit(2000);
                var output = p.StandardOutput.ReadToEnd();
                return !output.Contains("error occurred") && !string.IsNullOrEmpty(output); // && output.Contains("neuralart_exec");
            });
        }

        /// <summary>
        /// Kill all running docker container and delete them. Blocking call.
        /// </summary>
        public static void KillAndDeleteDockerContainer()
        {
            var startInfoKill = new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c {DockerStartAddition} purge_container.bat",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Trace.WriteLine(startInfoKill.Arguments);
            Process.Start(startInfoKill)?.WaitForExit(60000);
        }

        /// <summary>
        /// Convert Windows Style Path to Docker Style Path to properly mound Shared Drives.
        /// Makes something like C:\\Users\\Shared to //c/Users/Shared
        /// </summary>
        /// <param name="path">Windows Style Path</param>
        /// <returns>Docker Style Path</returns>
        public static string PathToDockerPath(string path)
        {
            var splitted = path.Split(':');
            splitted[0] = splitted[0].ToLower();
            path = String.Join("", splitted);
            path = path.Replace('\\', '/');
            path = path.Replace(":","");
            path = "//" + path;

            return path;
        }
    }
}
