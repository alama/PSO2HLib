using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace PSO2H
{
    //A single plugin object
    //This class handles loading configurations and updating when provided a download handler
    public class Plugin
    {
        #region Class Variables
        public Dictionary<string, Configuration> PluginConfiguration;

        private string _pluginSource;
        private readonly string _pluginConfigSource;
        private string _pluginFile;
        private string _pluginConfig;
        private double _currentVersion;
        private readonly Func<string, string, DownloadStatus> _downloadPlugin; //In = URL, In = Local File location, Out = Download Status, will run asynchronously

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

        //Expect a JSON in the format: { "Name":"<Plugin Name>", "CurrentVersion":<Floating Point Number for Plugin Version>, "Plugin":"<Link to DLL>", "Configuration":"<Link to Configuration>", "Description":"<A description of the plugin>" }
        //localPath is the folder for the plugins (E.G. G:\Games\PSO2\Plugins)
        //currVersion is the current local version. It's expected that the client application handles storing this somehow.
        public Plugin(string jsonPluginConfig, string localPath, out string errMsg, double currVersion = 0.0, Func<string, string, DownloadStatus> downloadPlugin = null)
        {
            errMsg = "";
            JObject info = JObject.Parse(jsonPluginConfig);
            string pluginName = info["Name"].Value<string>();
            string pluginDescription = info["Description"].Value<string>();
            string pluginSource = info["Plugin"].Value<string>();
            string pluginConfigSource = info["Configuration"].Value<string>() ?? "";

            localPath = Path.GetFullPath(localPath);

            if (downloadPlugin == null)
                downloadPlugin = DefaultDownload;

            _downloadPlugin = downloadPlugin;

            PluginName = pluginName;
            PluginDescription = pluginDescription;

            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
            _pluginFile = Path.Combine(localPath, String.Concat(pluginName, ".dll"));
            _pluginConfig = Path.Combine(localPath, String.Concat(pluginName, ".cfg"));

            _pluginSource = pluginSource;
            _pluginConfigSource = pluginConfigSource;

            //Maintain configuration settings
            if (File.Exists(_pluginFile))
                PluginConfiguration = Configuration.ParseConfigurationFile(_pluginConfig);
            else
                PluginConfiguration = new Dictionary<string, Configuration>();

            _currentVersion = info["CurrentVersion"].Value<double>();

            if (currVersion != _currentVersion)
            {
                UpdatePlugin();

                //Get any new parameters that weren't available before, and add new configurations
                Dictionary<string, Configuration> newPluginConfiguration = Configuration.ParseConfigurationFile(_pluginConfig);
                foreach (string key in newPluginConfiguration.Keys)
                {
                    if (PluginConfiguration.ContainsKey(key))
                    {
                        PluginConfiguration[key].Type = newPluginConfiguration[key].Type;
                        PluginConfiguration[key].Parameters = PluginConfiguration[key].Parameters.Union(newPluginConfiguration[key].Parameters);
                        if (PluginConfiguration[key].Type != ConfigurationType.STRING && !PluginConfiguration[key].Parameters.Contains(PluginConfiguration[key].Value))
                            errMsg += $"Warning: The current value of the parameter for {key} is not in the list of parameters\n";
                    }
                    else
                    {
                        PluginConfiguration.Add(key, newPluginConfiguration[key]);
                    }
                }

                WriteConfigurationToFile();
            }
        }

        #endregion

        #region Properties

        public string PluginName { get; }

        public double CurrentVersion { get { return _currentVersion; } }

        public string PluginDescription { get; }

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
            string ext = Path.GetExtension(local) ?? "";
            string tmpFileName = Path.ChangeExtension(local, ext + "tmp");
            using (WebClient w = new WebClient())
            {
                w.DownloadFile(url, tmpFileName);
            }

            if (File.Exists(local))
                File.Delete(local);

            File.Move(tmpFileName, local);

            return DownloadStatus.Success;
        }

        //Update plugin based on defined plugin source and update function
        //Will throw exceptions if not setup correctly
        public void UpdatePlugin()
        {
            if (string.IsNullOrEmpty(_pluginSource) || !Uri.IsWellFormedUriString(_pluginSource, UriKind.Absolute))
                throw new Exception("Plugin source not configured.");

            if (_downloadPlugin == null)
                throw new Exception("Download function isn't configured.");

            //Yes this will not handle well when somehow your configuration and file are the same file
            Task[] downloadTasks = {
                Task.Run(() => _downloadPlugin(_pluginSource, _pluginFile)),
                Task.Run(() => _pluginConfigSource.Length > 0 ? _downloadPlugin(_pluginConfigSource, _pluginConfig) : DownloadStatus.UpToDate ) //No problem redownloading this because we should already have the config results saved
	        };

            Task.WaitAll(downloadTasks);
        }

        public void WriteConfigurationToFile(string configPath = null)
        {
            if (PluginConfiguration.Count == 0) //Don't write configuration files for things that don't have any
                return;

            if (configPath == null)
                configPath = _pluginConfig;

            FileStream config = File.Create(configPath);
            StreamWriter sw = new StreamWriter(config);
            sw.Write(Configuration.ConfigurationsToString(PluginConfiguration));
            sw.Close();
        }

        #endregion

        #region Helper Functions

        #endregion
    }
}