﻿#region USING
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
    public class LogWriterSingleton
    {

        /// <summary>
        /// Single instance of logwriter
        /// </summary>
        private static LogWriterSingleton Instance;

        /// <summary>
        /// Queue used to store logs
        /// </summary>
        private static Queue<Log> LogQueue;

        /// <summary>
        /// Path to save log files
        /// </summary>
        public static string LogPath = "C:/tmp/";

        /// <summary>
        /// Lof file name
        /// </summary>
        public static string LogFile = "logfile.log";

        /// <summary>
        /// Flush log when time reached
        /// </summary>
        private static int FlushAtAge = 2;

        /// <summary>
        /// Flush log when quantity reached
        /// </summary>
        private static int FlushAtQty = 10;

        /// <summary>
        /// Timestamp of last flush
        /// </summary>
        private static DateTime FlushedAt;

        /// <summary>
        /// Private constructor -> prevent instantiation
        /// </summary>
        private LogWriterSingleton() { }

        /// <summary>
        /// Returns static instance of writer
        /// </summary>
        public static LogWriterSingleton GetInstance
        {
            get
            {
                if (Instance == null)
                {
                    Instance = new LogWriterSingleton();
                    LogQueue = new Queue<Log>();
                    FlushedAt = DateTime.Now;
                }
                return Instance;
            }
        }

        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void WriteToLog(string message)
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
        public static void WriteToLog(Exception e)
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
        public static void ForceFlush()
        {
            FlushLogToFile();
        }

        /// <summary>
        /// Check if time to flush to file
        /// </summary>
        /// <returns></returns>
        private static bool CheckTimeToFlush()
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
        private static void FlushLogToFile()
        {
            while (LogQueue.Count > 0)
            {

                // Get entry to log
                Log entry = LogQueue.Dequeue();
                string path = LogPath + entry.GetDate() + "_" + LogFile;

                // Crete filestream
                FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write);
                using (var writer = new StreamWriter(stream))
                {
                    // Log to file
                    writer.WriteLine(String.Format(@"{0} {1}", entry.GetTime(), entry.GetMessage()));
                    //stream.Close();
                }
                stream.Close();
            }
        }

    }

}