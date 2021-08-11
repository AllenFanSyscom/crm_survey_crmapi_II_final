using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common
{



    public class Log
    {
        public static object LockingTarget = new object();
        public static System.Threading.Mutex FileMutex = new System.Threading.Mutex(false, "CampaignSearch");


        public static void Info(string info)
        {
            LogToFile(Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,"Logs"), "Info", info);
        }

        public static void Error(string error)
        {
            LogToFile(Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,"Logs"), "Error", error);
        }

        private static void LogToFile(string logFilePath, string fileName, string message)
        {
            while (!FileMutex.WaitOne(TimeSpan.FromSeconds(1), false))
            {
                // wait until file is free
            }

            try
            {
                lock (LockingTarget)
                {
                    // Determine whether the directory exists. If not, Try to create one.
                    //Neil 2017/08/31 新增 for Path Manipulation
                    if (!Directory.Exists(logFilePath))
                    {
                        DirectoryInfo dirInfo = Directory.CreateDirectory(logFilePath);
                    }

                    // Create file name with date.
                    DateTime dt = DateTime.Now;
                    string strFileName = fileName + " - " + dt.ToString("yyyy-MM-dd", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".txt";

                    //while (IsFileLocked(new FileInfo(logFilePath + strFileName)) && File.Exists(logFilePath + strFileName))
                    //{
                    //    // just wait for unlock
                    //    Thread.Sleep(1000); // Or TimeSpan.FromSeconds(1)
                    //}

                    StreamWriter sw = null;
                    try
                    {
             
                        sw = File.AppendText(logFilePath + strFileName);
                        string logLine = String.Format("{0:G}: {1}", DateTime.Now, message);
                        sw.WriteLine(logLine);
                    }
                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        if (sw != null)
                            sw.Close();
                    }
                }
            }
            finally { FileMutex.ReleaseMutex(); }
        }
       
    }


    /// <summary>
    /// 利用log4net產生log
    /// </summary>
    //    public class Log
    //    {
    //        private static readonly ILog log = LogManager.GetLogger("SurveyWebAPIV2Log", typeof(Common.Log));
    //        /// <summary>
    //        /// Debug訊息
    //        /// </summary>
    //        /// <param name="msg"></param>
    //        /// <param name="obj"></param>
    //        public static void Debug(string msg, object obj = null)
    //        {
    //            if (log.IsDebugEnabled && !string.IsNullOrWhiteSpace(msg))
    //            {
    //                if (obj == null)
    //                {
    //                    log.Debug(msg);
    //                }
    //                else
    //                {
    //                    log.DebugFormat(msg, obj);
    //                }
    //            }
    //        }
    //        /// <summary>
    //        /// 一般訊息
    //        /// </summary>
    //        /// <param name="msg"></param>
    //        /// <param name="obj"></param>
    //        public static void Info(string msg, object obj = null)
    //        {
    //            if (log.IsInfoEnabled && !string.IsNullOrEmpty(msg))
    //            {
    //                if (obj == null)
    //                {
    //                    log.Info(msg);
    //                }
    //                else
    //                {
    //                    log.InfoFormat(msg, obj);
    //                }
    //            }
    //        }
    //        /// <summary>
    //        /// 錯誤訊息
    //        /// </summary>
    //        /// <param name="msg"></param>
    //        /// <param name="obj"></param>
    //        public static void Error(string msg, object obj = null)
    //        {
    //            if (log.IsErrorEnabled && !string.IsNullOrEmpty(msg))
    //            {
    //                if (obj == null)
    //                {
    //                    log.Error(msg);
    //                }
    //                else
    //                {
    //                    log.ErrorFormat(msg, obj);
    //                }
    //            }
    //        }
    //        /// <summary>
    //        /// 重要訊息
    //        /// </summary>
    //        /// <param name="msg"></param>
    //        /// <param name="obj"></param>
    //        public static void Fatal(string msg, object obj = null)
    //        {
    //            if (log.IsFatalEnabled && !string.IsNullOrEmpty(msg))
    //            {
    //                if (obj == null)
    //                {
    //                    log.Fatal(msg);
    //                }
    //                else
    //                {
    //                    log.FatalFormat(msg, obj);
    //                }
    //            }
    //        }
    //    }
}
