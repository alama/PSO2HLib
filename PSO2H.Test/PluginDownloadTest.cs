using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PSO2H;
using System.IO;

namespace PSO2H.Test
{
    [TestClass]
    public class PluginDownloadTest
    {
        [TestMethod]
        //0. Delete Plugins directory if exists
        //1. Instantiate instance of Plugin, forcibly trigger update (version difference)
        //2. Check if files are downloaded and exist
        //3. Change configuration
        //4. Save configuration
        //5. Instantiate new instance of Plugin, forcibly trigger update (version difference)
        //6. Check if configuration value is what we updated to earlier 
        //7. Make configuration empty again
        //8. Instantiate another new instance of Plugin, do not trigger update
        //8. Check configuration
        public void DownloadAndConfigureTest()
        {
            string TestJSON = @"{""Name"":""PSO2DamageDump"", ""CurrentVersion"": 1.0, ""Description"": ""A damage dumper for PSO2, for use with ACT and other analysis tools"", ""Plugin"": ""http://vxyz.me/files/Plugins/PSO2DamageDump/PSO2DamageDump.dll"", ""Configuration"": ""http://vxyz.me/files/Plugins/PSO2DamageDump/PSO2DamageDump.cfg""}";
            double CurrentVersion = 0.9;
            double ActualVersion = 1.0;

            if (Directory.Exists("./Plugins"))
                Directory.Delete("./Plugins", true);

            string errMsg;
            Plugin pl1 = new Plugin(TestJSON, "./Plugins", out errMsg, CurrentVersion);

            Assert.AreEqual(pl1.CurrentVersion, 1.0);
            Assert.AreEqual(pl1.PluginName, "PSO2DamageDump");
            Assert.IsTrue(Directory.Exists("./Plugins"));
            Assert.IsTrue(File.Exists("./Plugins/PSO2DamageDump.dll"));
            Assert.IsTrue(File.Exists("./Plugins/PSO2DamageDump.cfg"));

            pl1.PluginConfiguration["directory"].Value = @"G:\Games\";
            pl1.WriteConfigurationToFile();

            Plugin pl2 = new Plugin(TestJSON, "./Plugins", out errMsg, CurrentVersion);
            Assert.AreEqual(pl2.PluginConfiguration["directory"].Value, @"G:\Games\");
            pl2.PluginConfiguration["directory"].Value = @"";
            pl2.WriteConfigurationToFile();

            Plugin pl3 = new Plugin(TestJSON, "./Plugins", out errMsg, ActualVersion);
            Assert.AreEqual(pl3.PluginConfiguration["directory"].Value, @"");
        }
    }
}
