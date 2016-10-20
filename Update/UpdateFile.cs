using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace sensu_client.Update
{
    class UpdateFile
    {
        public UpdateFile(string pluginFilePath, JObject check)
        {
            setFileNameAndPath(pluginFilePath, check);
        }

        private const string _timeArgument = "timestamp update = ";
        private const string _fileExtension = "txt";
        public string filepath { get; set; }
        public string checkFilePath{ get; set; }
        public string filename{ get; set; }
        public string checkname { get; set; }
        public string command { get; set; }
        private DateTime _lastUpdateDatetime;

        public DateTime FileTimestamp
        {
            get
            {
                return _lastUpdateDatetime;
            }

            set
            {
                _lastUpdateDatetime = value;
            }
        }

        public string FileExtension
        {
            get
            {
                return _fileExtension;
            }
        }

        public string TimeArgument
        {
            get
            {
                return _timeArgument;
            }
        }

        private void setFileNameAndPath(string pluginsPath, JObject check)
        {
            this.command = (string)check["command"];
            var index1 = command.LastIndexOf("/") + 1;
            this.checkname = command.Substring(index1);
            var index2 = this.checkname.IndexOf(Command.RemoteCommand.PSEXTENSION);
            index2 = index2 + (Command.RemoteCommand.PSEXTENSION.Length);
            if (checkname.Length > index2)
            {
                this.checkname = checkname.Remove(index2);
            }
            this.filename = "." + (checkname.Remove(this.checkname.IndexOf(Command.RemoteCommand.PSEXTENSION)));
            this.filename = this.filename + FileExtension;
            this.filepath = String.Format(@"{0}\{1}", pluginsPath, filename);
            this.checkFilePath = String.Format(@"{0}\{1}", pluginsPath, checkname);
       }
    }
}
