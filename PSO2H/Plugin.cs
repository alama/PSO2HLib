using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace PSO2H
{
    //A single plugin object
    //This class handles loading configurations and updating when provided a download handler
    public class Plugin
    {
        #region Class Variables
        public Dictionary<string, Configuration> PluginConfiguration;

        private string _pluginName;
        private string _pluginDescription;
        private string _pluginSource;
        private string _pluginConfigSource;
        private string _pluginFile;
        private string _pluginConfig;
        private Func<string, string, DownloadStatus> _downloadPlugin = null; //In = URL, In = Local File location, Out = Download Status

        #endregion

        #region Enums

        public enum DownloadStatus
        {
            Success,
            Fail,
            InProgress,
            UpToDate,
            Unknown
        }

        #endregion

        #region Constructors

        //Expect a JSON in the format: { "PluginName":"<Name>", "PluginDescription":"<Description>", "PluginSource":"<SourceURL>" "PluginConfigSource":"<ConfigSourceURL>" }
        //localPath is the folder for the plugins (E.G. G:\Games\PSO2\Plugins)
        public Plugin(string jsonPluginConfig, string localPath, Func<string, string, DownloadStatus> downloadPlugin = null)
        {
            JObject info = JObject.Parse(jsonPluginConfig);
            string pluginName = info["PluginName"].Value<string>();
            string pluginDescription = info["PluginDescription"].Value<string>();
            string pluginSource = info["PluginSource"].Value<string>();
            string pluginConfigSource = info["PluginConfigSource"].Value<string>();

            if (downloadPlugin == null)
                downloadPlugin = DefaultDownload;

            _pluginName = pluginName;
            _pluginDescription = pluginDescription;

            _pluginFile = Path.Combine(localPath, String.Concat(pluginName, ".dll"));
            _pluginConfig = Path.Combine(localPath, String.Concat(pluginName, ".cfg"));

            _pluginSource = pluginSource;
            _pluginConfigSource = pluginConfigSource;

            if (File.Exists(_pluginFile))
                PluginConfiguration = Configuration.ParseConfigurationFile(_pluginConfig);

            UpdatePlugin();
            WriteConfigurationToFile();
        }

        #endregion

        #region Properties

        public string PluginName
        {
            get
            {
                return _pluginName;
            }
        }

        public string PluginDescription
        {
            get
            {
                return _pluginDescription;
            }
        }

        public string PluginSource
        {
            get
            {
                return _pluginSource;
            }
            set
            {
                _pluginSource = value;
            }
        }

        public string PluginFile
        {
            get
            {
                return _pluginFile;
            }
            set
            {
                _pluginFile = value;
            }
        }

        public string PluginConfig
        {
            get
            {
                return _pluginConfig;
            }
            set
            {
                _pluginConfig = value;
            }
        }



        #endregion

        #region Public Methods

        //Default method just checks remote date, compares it to local and downloads 
        //E.g. url = http://vxyz.me/files/PSO2DamageDump.dll, local = G:\Games\PSO2\Plugins\PSO2DamageDump.dll
        //E.g. url = http://vxyz.me/files/PSO2DamageDump.cfg, local = G:\Games\PSO2\Plugins\PSO2DamageDump.cfg
        public static DownloadStatus DefaultDownload(string url, string local)
        {
            DateTime localTime, remoteTime;

            //Get local last updated datetime
            if (File.Exists(local))
                localTime = File.GetLastWriteTimeUtc(local);
            else
                localTime = DateTime.MinValue.ToUniversalTime();

            //Get remote last updated datetime
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            var response = (HttpWebResponse)request.GetResponse();
            response.Close();

            remoteTime = response.LastModified.ToUniversalTime();

            if (localTime > remoteTime)
                return DownloadStatus.UpToDate;

            WebClient w = new WebClient();
            w.DownloadFile(url, String.Format("{0}.tmp", local));

            if (File.Exists(local))
                File.Delete(local);

            File.Move(String.Format("{0}.tmp", local), local);

            return DownloadStatus.Success;
        }

        //Update plugin based on defined plugin source and update function
        //Will throw exceptions if not setup correctly
        public void UpdatePlugin()
        {
            if (_pluginSource == null || _pluginSource.Length == 0)
                throw new Exception("Plugin source not configured.");

            if (_downloadPlugin == null)
                throw new Exception("Download function isn't configured.");

            try
            {
                _downloadPlugin(_pluginSource, _pluginFile);
                //No problem redownloading this because we should already have the config results saved
                _downloadPlugin(_pluginConfigSource, _pluginConfig); 
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void WriteConfigurationToFile(string configPath = null)
        {
            if (configPath == null)
                configPath = _pluginConfig;

            FileStream config = File.OpenWrite(configPath);
            StreamWriter sw = new StreamWriter(config);
            sw.Write(Configuration.ConfigurationsToString(PluginConfiguration));
            sw.Close();
        }

        #endregion

        #region Helper Functions

        #endregion
    }
}
