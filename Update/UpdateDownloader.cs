using Newtonsoft.Json.Linq;
using System;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace sensu_client.Update
{
    class UpdateDownloader
    {
        private UpdateFile _updateFile { get; set; }
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public UpdateDownloader(UpdateFile file)
        {
            _updateFile = file;
        }

        public void downloadUpdate(JObject check)
        {
            try
            {
                log.Debug("About to download update for: " + _updateFile.checkFilePath);
                string downloadURL = check["command"].ToString().Replace(Command.RemoteCommand.PREFIX, "");
                string finalDownloadURL = downloadURL.Remove(downloadURL.IndexOf(Command.RemoteCommand.PSEXTENSION) + (Command.RemoteCommand.PSEXTENSION.Length));
                log.Debug("Preparing download on URL: " + finalDownloadURL);
                HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(finalDownloadURL);
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

                try
                {
                    using (Stream input = Response.GetResponseStream())
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        using (Stream output = File.OpenWrite(_updateFile.checkFilePath))
                        {
                            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                output.Write(buffer, 0, bytesRead);
                            }
                        }
                        Response.Close();
                    }
                }
                catch (IOException ioE)
                {
                    log.Warn("Error writing new updatefile: " + ioE.StackTrace);
                }
            }
            catch (Exception e)
            {
                log.Warn("Error downloading update: " + e.StackTrace);
            }
      }
    }
}
