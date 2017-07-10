using DeepdreamGui.ExtensionMethods;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// Types of Events that can be raised by the image Processing. The enum values equal the output names of the Python and Lua image processing scripts.
    /// See DeepdreamEventArgs Status for Values.
    /// </summary>
    public enum DeepDreamEvents
    {
        /// <summary>
        /// Total progress of the processing.
        /// </summary>
        TotalProgress,

        /// <summary>
        /// Progress of the current image processing.
        /// </summary>
        Progress,
        
        /// <summary>
        /// Processing step was started (Image x of n)
        /// </summary>
        StepStarted,
        
        /// <summary>
        /// Processing step was completed
        /// </summary>
        StepComplete,
        
        /// <summary>
        /// All Processing steps have been completed.
        /// </summary>
        AllComplete,
        
        /// <summary>
        /// Processed image was saved to temporary path.
        /// </summary>
        SavedImage,
        
        /// <summary>
        /// Error happened.
        /// </summary>
        Error,
        
        /// <summary>
        /// Image progressing process has started.
        /// </summary>
        ProcessStarted,
        
        /// <summary>
        /// Image processing process has ended.
        /// </summary>
        ProcessDone,

        /// <summary>
        /// Process may crash here. Deliver Error report in advance
        /// </summary>
        PossibleError
    }

    /// <summary>
    /// Class for DeepDreamEvent data. Combines EventNumber with Status string.
    /// GUI Relevant Information mus begin with a "*" and include the eventname seperated from further information by ":". See Processing Scripts.
    /// </summary>
    public class DeepDreamEventArgs : EventArgs
    {
        /// <summary>
        /// Type of Event
        /// </summary>
        public DeepDreamEvents Event { get; set; }
        
        /// <summary>
        /// Status string. This is used for various information like the progress percentage or error codes.
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Logic to prepare, start, stop and control image processing. Temporary data is generated here. This is a Singleton, user Worker.Insance.
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Set true, to output more debug data
        /// </summary>
        bool debug = false;

        private bool ignoreNextExitCode = false;
        private static Worker _instance = null;
        private Process current;
        private string currentImage = "";
        private FixedSizedQueue<string> _messageQueue = new FixedSizedQueue<string>() { Limit = 32 };
        private int possibleError = 0;

        /// <summary>
        /// Relays received DeepDreamEvents
        /// </summary>
        public EventHandler<DeepDreamEventArgs> ProcessEvent;
        private string styleImage;

        /// <summary>
        /// Path to temporary folder. Creates "dreamdata" Folder in /User/AppData/Temp
        /// </summary>
        internal static string DataPath
        {
            get
            {
                var tmploc = Path.GetTempPath();
                var location = Path.Combine(tmploc, "dreamdata");
                if (!Directory.Exists(location))
                    try
                    {
                        Directory.CreateDirectory(location);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Worker.DataPath; "+e);
                        throw;
                    }
                return location;
            }
        }

        private Worker()
        {
        }

        /// <summary>
        /// Get the current instance of the Worker class. Worker object is created lazy.
        /// </summary>
        public static Worker Instance => _instance ?? (_instance = new Worker());

        /// <summary>
        /// Force image processing to stop.
        /// </summary>
        /// <returns>Task Object containig the running cancellation. Can be awaited.</returns>
        public Task Kill()
        {
            return Task.Factory.StartNew(() =>
            {
                //Ignore next exit code, if process is running and killed.
                ignoreNextExitCode = current != null;
                DeleteFile(currentImage);
                currentImage = String.Empty;
                DeleteFile(styleImage);
                styleImage = String.Empty;

                DockerModel.KillAndDeleteDockerContainer();

                if (current == null) return;
                current = null;
            });
        }

        /// <summary>
        /// Saves Image to path with globally unique id as name.
        /// </summary>
        /// <param name="img">Input image</param>
        /// <param name="path">Path to save the temporary file</param>
        /// <returns>Path to saved file</returns>
        private string SaveTmpImage(BitmapImage img, string path)
        {
            if (img == null) return string.Empty;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder {QualityLevel = 100};
            String name = System.Guid.NewGuid().ToString() + ".jpg"; //file name 

            encoder.Frames.Add(BitmapFrame.Create(img));

            var saveDir = Path.Combine(path, name);
            try
            {
                Trace.Write("Worker.SaveTmpImage; Try to save tmp image.");
                using (var filestream = new FileStream(saveDir, FileMode.Create))
                    encoder.Save(filestream);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Worker.SaveTmpImage; "+ex);
                DeleteFile(path);
            }
            return saveDir;
        }

        /// <summary>
        /// Delete File. No Exception is thrown on error.
        /// </summary>
        /// <param name="path">Path to file</param>
        public static void DeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                Trace.WriteLine("Worker.DeleteFile; Try to delete file="+path);
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Trace.Write("Worker.DeleteFile; "+ex);
            }
        }

        /// <summary>
        /// Recursively delete Directories. No Exception is thrown on error.
        /// </summary>
        /// <param name="targetDir">Target Directory</param>
        public static void DeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return;

            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                DeleteFile(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            try
            {
                Trace.WriteLine("Wroker.DeleteDirectory; Try to delete directory="+targetDir);
                Directory.Delete(targetDir,false);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Worker.DeleteDirectory; "+e);
                return;
            }
        }

        /// <summary>
        /// Read the Process output and raise Events.
        /// Process Output mus begin with a *, followed by the EventName and the status, seperated by ":"
        /// Example: *savedimage:/data/img5.jpg
        /// </summary>
        /// <param name="data"></param>
        private void InvokeEvent(string data)
        {
            //Check if GUI relevant information.
            if (data[0] != '*' || !data.Contains(':')) return;

            var splitted = data.Split(':');
            var value = String.Join(":", splitted.Skip(1));
            var id = splitted.First();
            id = id.Substring(1, id.Length - 1);

           
            DeepDreamEvents?ev = null;

            switch (id.ToLower())
            {
                case "totalprogress":
                    ev = DeepDreamEvents.TotalProgress;
                    break;
                case "progress":
                    ev = DeepDreamEvents.Progress;
                    break;
                case "stepstarted":
                    ev = DeepDreamEvents.StepStarted;
                    Trace.WriteLine("Worker.InvokeEvent; New event=" + id + ":" + value);
                    break;
                case "stepcomplete":
                    ev = DeepDreamEvents.StepComplete;
                    Trace.WriteLine("Worker.InvokeEvent; New event=" + id + ":" + value);
                    break;
                case "allcomplete":
                    ev = DeepDreamEvents.AllComplete;
                    Trace.WriteLine("Worker.InvokeEvent; New event=" + id + ":" + value);
                    possibleError = 0;
                    break;
                case "savedimage":
                    ev = DeepDreamEvents.SavedImage;
                    var filename = Path.GetFileName(value);
                    value = Path.Combine(DataPath,"output",filename);
                    Trace.WriteLine("Worker.InvokeEvent; New event=" + id + ":" + value);
                    break;
                case "error":
                    ev = DeepDreamEvents.Error;
                    Trace.WriteLine("Worker.InvokeEvent; New event=" + id + ":" + value);
                    break;
                case "possibleerror":
                    ev = DeepDreamEvents.PossibleError;
                    Trace.WriteLine("Worker.InvokeEvent; Possible Error=" + id + ":" + value);
                    int.TryParse(value, out possibleError);
                    break;
                default:
                    break;
            }

            if(ev != null) OnDeepDreamEvent(ev.Value,value);
        }

        private void OnDeepDreamEvent(DeepDreamEvents ev, string status)
        {
            ProcessEvent?.Invoke(this, new DeepDreamEventArgs { Event = ev, Status = status });
        }

        /// <summary>
        /// Prepare image processing
        /// </summary>
        /// <param name="image">Image to process</param>
        /// <param name="style">Optional: Style image (only for neural art processing)</param>
        /// <returns>Task Object containing the Preparation. Can be awaited.</returns>
        private Task PrepareTask(BitmapImage image, BitmapImage style = null)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (this)
                {
                    Kill().Wait();
                    DeleteFile(currentImage);
                    currentImage = SaveTmpImage(image, DataPath);

                    if (style != null)
                    {
                        DeleteFile(styleImage);
                        styleImage = SaveTmpImage(style, DataPath);
                    }
                }
            });
        }

        /// <summary>
        /// Run Task. Starts process with argString, hooks reading of output and evaluates return values. 
        /// </summary>
        /// <param name="argString"></param>
        /// <returns></returns>
        private Task RunTask(string argString)
        {
            return Task.Factory.StartNew(() =>
            {
                OnDeepDreamEvent(DeepDreamEvents.ProcessStarted, string.Empty);
                lock (this)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo("powershell")
                    {
                        Arguments = argString,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true                        
                    };

                    Trace.WriteLine("Worker.RunTask; "+argString);
                    Process p = new Process { StartInfo = startInfo };
                    current = p;

                    p.OutputDataReceived += (sender, args) =>
                    {
                        _messageQueue.Enqueue(args.Data);
                        //GUI Relevant Ouptut Data of ImageProcessing begins with *
                        if (args.Data == null || args.Data.Length < 2 || (args.Data[0] != '*' && !debug)) return; //
                        InvokeEvent(args.Data);
                        Trace.WriteLine(args.Data);
                    };

                    //p.ErrorDataReceived += (sender, args) => Trace.WriteLine("Err " + args.Data);
                    possibleError = 0;
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Trace.WriteLine($"Worker.RunTask; Task Exited with code: {p.ExitCode}");

                    var exitCode = p.ExitCode;
                    if(possibleError > 0 && exitCode == 1)
                    {
                       exitCode = possibleError;
                    }

                    if (!ignoreNextExitCode)
                    {
                        switch (exitCode)
                        {
                            case 0: //Success
                                break;
                            case 137:
                                OnDeepDreamEvent(DeepDreamEvents.Error,
                                    Properties.Resources.info_error_ram);
                                break;
                            case 125:
                                OnDeepDreamEvent(DeepDreamEvents.Error,
                                     Properties.Resources.info_error_workingdirectory);
                                break;
                            default:
                                OnDeepDreamEvent(DeepDreamEvents.Error,
                                    $"{Properties.Resources.info_error_undefined} {p.ExitCode}");
                                break;
                        }
                        ignoreNextExitCode = false;
                    }

                    if(exitCode != 0)
                    {
                        Trace.WriteLine("Error: print last " + _messageQueue.Count + " lines");
                        while (_messageQueue.Count > 0)
                        {
                            Trace.WriteLine("DEBUG:\t" + _messageQueue.Dequeue());
                        }
                    }


                    DeleteFile(currentImage);
                    currentImage = String.Empty;

                    DeleteFile(styleImage);
                    styleImage = String.Empty;

                    OnDeepDreamEvent(DeepDreamEvents.ProcessDone, string.Empty);
                    current = null;
                }
            });
        }   
       
        /// <summary>
        /// Run DeepDream processing. This will run the docker container and execute the processing for DeepDream images.
        /// </summary>
        /// <param name="image">Image to process</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="scale">Normalized Scale Factor. Calculate by normalizedScale = 1 - (100/ScaleFact) where ScaleFact is in % (100% Current Size, 200% Double, ...)</param>
        /// <param name="intensity">Intensity</param>
        /// <param name="octave">Octave</param>
        /// <param name="model">Processing Model</param>
        /// <param name="rotate">Rotation in degrees</param>
        public void DeepDream(BitmapImage image, int iterations, double scale,double intensity, int octave, string model, double rotate)
        {
            Task.Factory.StartNew(() =>
            {
                PrepareTask(image).ContinueWith(task =>
                {
                    var argString =
                        $"/c {DockerModel.DockerStartAddition} docker run -v {DockerModel.PathToDockerPath(DataPath)}:/data -e INPUT={Path.GetFileName(currentImage)} -e ITER={iterations} -e OCTAVE={octave} -e INTENSITY={intensity.ToString(CultureInfo.InvariantCulture)} -e SCALE={scale.ToString(CultureInfo.InvariantCulture)} -e MODEL={model} -e ROTATE={rotate.ToString(CultureInfo.InvariantCulture)} muelmx/neuralart_exec python -u /deepdream/deepdream.py ";

                    RunTask(argString);
                });
            });
        }

        /// <summary>
        /// Run NeuralArt processing. This will run the docker container and execute the processing for NeuralArt images.
        /// Enable reduceSize and set a small maxsidelength so speed up processing.
        /// </summary>
        /// <param name="image">Image to process</param>
        /// <param name="style">Image style to apply</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="reduceSize">Enable changing (reducing) of image size.</param>
        /// <param name="maxsidelength">Reduce long edge of Image to this value. Proportionally reduces small edge.</param>
        public void NeuralArt(BitmapImage image, BitmapImage style, int iterations, bool reduceSize, int maxsidelength)
        {
            Task.Factory.StartNew(() =>
            {
                PrepareTask(image, style).ContinueWith(task =>
                {
                    int size = reduceSize ? maxsidelength : 0;
                    var argString =
                        $"/c {DockerModel.DockerStartAddition} docker run -v {DockerModel.PathToDockerPath(DataPath)}:/data muelmx/neuralart_exec /home/torch/install/bin/qlua /neuralart/main.lua --style {"/data/" + Path.GetFileName(styleImage)} --display_interval 0 --output_dir /data/output --content {"/data/" + Path.GetFileName(currentImage)} --num_iters {iterations} --size {size}  --cpu";
                       
                    RunTask(argString);
                });
            });
        }
    }
}
