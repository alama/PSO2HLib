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
        public void DownloadTest1()
        {
            string TestJSON = @"{""Name"":""PSO2DamageDump"", ""CurrentVersion"": 1.0, ""Description"": ""A damage dumper for PSO2, for use with ACT and other analysis tools"", ""Plugin"": ""http://vxyz.me/files/Plugins/PSO2DamageDump/PSO2DamageDump.dll"", ""Configuration"": ""http://vxyz.me/files/Plugins/PSO2DamageDump/PSO2DamageDump.cfg""}";
            double CurrentVersion = 0.9;

            string errMsg;
            Plugin pl = new Plugin(TestJSON, "./Plugins", out errMsg, CurrentVersion);

            Assert.AreEqual(pl.CurrentVersion, 1.0);
            Assert.AreEqual(pl.PluginName, "PSO2DamageDump");
            Assert.IsTrue(Directory.Exists("./Plugins"));
            Assert.IsTrue(File.Exists("./Plugins/PSO2DamageDump.dll"));
            Assert.IsTrue(File.Exists("./Plugins/PSO2DamageDump.cfg"));
        }
    }
}
