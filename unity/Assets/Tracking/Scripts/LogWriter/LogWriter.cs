#region USING
using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
#endregion

namespace LogWriter
{

    /// <summary>
    /// Simple thread safe logging helper
    /// </summary>
    public class LogWriter
    {

        /// <summary>
        /// Queue used to store logs
        /// </summary>
        private Queue<Log> LogQueue;

        /// <summary>
        /// Path to save log files
        /// </summary>
        public string LogPath = "C:/tmp/";

        /// <summary>
        /// Log file prefix
        /// </summary>
        public string LogFilePrefix = "log_";

        /// <summary>
        /// Log file extension
        /// </summary>
        public string LogFileExtension = ".log";

        /// <summary>
        /// Log file extension
        /// </summary>
        public string LogFileName = ".log";

        /// <summary>
        /// Flush log when time reached
        /// </summary>
        public int FlushAtAge = 2;

        /// <summary>
        /// Flush log when quantity reached
        /// </summary>
        public int FlushAtQty = 10;

        /// <summary>
        /// Timestamp of last flush
        /// </summary>
        private DateTime FlushedAt;

        /// <summary>
        /// Constructor
        /// </summary>
        public LogWriter(string _LogFilePath = "", string _LogFilePrefix = "", string _LogFileExtension = ".log")
        {
            LogPath = _LogFilePath;
            LogFilePrefix = _LogFilePrefix;
            LogFileExtension = _LogFileExtension;

            LogQueue = new Queue<Log>();
            FlushedAt = DateTime.Now;
            LogFileName = LogPath + LogFilePrefix + TimeToFileName() + LogFileExtension;
        }

        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void WriteToLog(string message)
        {
            lock (LogQueue)
            {

                // Create log
                Log log = new Log(message);
                LogQueue.Enqueue(log);

                // Check if should flush
                if (LogQueue.Count >= FlushAtQty || CheckTimeToFlush())
                {
                    FlushLogToFile();
                }

            }
        }

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="e">Exception to log</param>
        public void WriteToLog(Exception e)
        {
            lock (LogQueue)
            {

                // Create log
                Log msg = new Log(e.Source.ToString().Trim() + " " + e.Message.ToString().Trim());
                Log stack = new Log("Stack: " + e.StackTrace.ToString().Trim());
                LogQueue.Enqueue(msg);
                LogQueue.Enqueue(stack);

                // Check if should flush
                if (LogQueue.Count >= FlushAtQty || CheckTimeToFlush())
                {
                    FlushLogToFile();
                }

            }
        }

        /// <summary>
        /// Force flush of log queue
        /// </summary>
        public void ForceFlush()
        {
            FlushLogToFile();
        }

        /// <summary>
        /// Check if time to flush to file
        /// </summary>
        /// <returns></returns>
        private bool CheckTimeToFlush()
        {
            TimeSpan time = DateTime.Now - FlushedAt;
            if (time.TotalSeconds >= FlushAtAge)
            {
                FlushedAt = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Flush log queue to file
        /// </summary>
        private void FlushLogToFile()
        {
            while (LogQueue.Count > 0)
            {

                // Get entry to log
                Log entry = LogQueue.Dequeue();
                //string path = LogPath + entry.GetDate() + "_" + LogFile;
                //string path = LogPath + LogFilePrefix + "_" + TimeToFileName() + LogFileExtension;

                // Crete filestream
                FileStream stream = new FileStream(LogFileName, FileMode.Append, FileAccess.Write);
                using (var writer = new StreamWriter(stream))
                {
                    // Log to file
                    writer.WriteLine(String.Format(@"{0} {1}", entry.GetTime(), entry.GetMessage()));
                    //stream.Close();
                }
                stream.Close();
            }
        }

        private string TimeToFileName()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

}