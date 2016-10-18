using NLog;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using sensu_client.Command;
using sensu_client.Update;

namespace sensu_client.Update
{
    class UpdateScheduler
    {
        protected CommandConfiguration _commandConfiguration { get; set; }
        private const int DaysNextUpdate = 1;
        private DateTime DayTimeToday = DateTime.Today;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool ExecuteUpdateCheck(CommandConfiguration configuration, JObject check)
        {
            UpdateFile file = new Update.UpdateFile(configuration.Plugins, check);

            if (File.Exists(file.filepath))
            {
                try
                {
                    file = ReadUpdateFile(file);
                    bool performUpdate = CompareTimeForUpdate(file);

                    if (performUpdate)
                    {
                        file.FileTimestamp = DateTime.Today;
                        DownloadUpdate(file, check);
                        WriteUpdateFile(file);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e is IOException)
                    {
                        throw new Exception("IOException: " + e.StackTrace);
                    } 
                    else if (e is UnauthorizedAccessException)
                    { 
                        FileAttributes attr = (new FileInfo(file.filepath)).Attributes;
                        string exception = "UnAuthorizedAccesException: Unable to access file. ";
                        if ((attr & FileAttributes.ReadOnly) > 0)
                        {
                            exception = exception + "File is readonly";
                        }
                        throw new Exception(exception + " Stacktrace: " + e.StackTrace);
                    }
               }
            }
            else if (!File.Exists(file.filepath) || !File.Exists(file.checkFilePath))
            {
                file.FileTimestamp = DateTime.Today;
                DownloadUpdate(file, check);
                createUpdateFile(file);
                return true;
            }
            return false;
        }

        private bool CompareTimeForUpdate(UpdateFile file)
        {
            DateTime nextUpdateDate = file.FileTimestamp.AddDays(DaysNextUpdate);
            if ((DateTime.Compare(nextUpdateDate, DayTimeToday)) <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DownloadUpdate(UpdateFile file, JObject check)
        {
            
            UpdateDownloader UpdateDownloader = new UpdateDownloader(file);
            UpdateDownloader.downloadUpdate(check);
        }

        public UpdateFile ReadUpdateFile(UpdateFile file)
        {

            using (StreamReader reader = new StreamReader(file.filepath))
            {
                string line = reader.ReadToEnd();
                file.FileTimestamp = Convert.ToDateTime(line);
                reader.Close();
            }
            return file;
        }

        public void WriteUpdateFile(UpdateFile file)
        {
            try
            {
                File.Delete(file.filepath);
            }
            catch (Exception e)
            {
                throw new Exception(e.StackTrace);
            }

            DirectoryInfo dInfo = new DirectoryInfo(file.filepath);
            using (StreamWriter writer = new StreamWriter(file.filepath, true))
            {
                writer.Write(file.FileTimestamp);
                writer.Close();
            }
        }

        public void createUpdateFile(UpdateFile file)
        {
            using (FileStream fs = File.Create(file.filepath))
            {
                fs.Close();
            }

            using (StreamWriter sw = new StreamWriter(file.filepath))
            {
                DateTime timeStamp = new DateTime(file.FileTimestamp.Ticks);
                sw.Write(timeStamp);
                DirectoryInfo dInfo = new DirectoryInfo(file.filepath);
                File.SetAttributes(file.filepath, FileAttributes.Hidden);
                sw.Close();
           }
        }
    }
}
