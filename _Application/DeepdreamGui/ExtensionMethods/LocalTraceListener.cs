using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// Trace Listener for debug messages. Writes Messages to File.
    /// </summary>
    public class LocalTraceListener:TraceListener
    {
        private readonly string _localLogFilePath;

        /// <summary>
        /// Constructor for Local Trace Listener
        /// </summary>
        public LocalTraceListener()
        {
            //http://stackoverflow.com/questions/4765789/getting-files-by-creation-date-in-net
            DirectoryInfo info = new DirectoryInfo(Environment.CurrentDirectory);
            //Order logfiles ascending
            FileInfo[] files = info.GetFiles("*.log").OrderBy(f => f.CreationTime).ToArray();
            if (files.Length > 10)
            {
                //if more than 10 delete the oldest 10
                for (int i = 0; i < 10; i++)
                {
                    File.Delete(files[i].FullName);
                }
            }

            string fileName = "DeepDreamGui_" + DateTime.Now.ToString("yyyy_MM_dd-hh_mm") + ".log";
            _localLogFilePath = Path.Combine(Environment.CurrentDirectory, fileName);
            if (!File.Exists(_localLogFilePath))
            {
                File.Create(_localLogFilePath).Close();
            }
        }

        /// <summary>
        /// Write Message to Trace Listener
        /// </summary>
        /// <param name="message"></param>
        public override void Write(string message)
        {
            lock (this)
            {
                File.AppendAllText(_localLogFilePath, DateTime.Now + @":" + message);
            }
        }

        /// <summary>
        /// Write Line to Trace Listener
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
            this.Write(message + Environment.NewLine);
        }
    }
}
