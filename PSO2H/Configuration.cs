using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace PSO2H
{
    public class Configuration
    {
        public readonly string Name;
        public string Value;
        public readonly string Type;
        public readonly string Comment;

        public Configuration(string configName, string configType, string comment, string configValue)
        {
            Name = configName;
            Value = configValue;
#warning TODO: Add a config type enum
            Type = configType;
            Comment = comment;
        }

        public static Dictionary<string, Configuration> ParseConfigurationFile(string fullFilePath)
        {
            Dictionary<string, Configuration> retVal = new Dictionary<string, Configuration>();

            //Key[type;comment]=Value
            try
            {
                Regex format = new Regex(@"(^[^[]*)\[([a-z]*);([^\]]*)]=([^\n\r]*)");
                foreach (string line in File.ReadLines(fullFilePath))
                {
                    Match m = format.Match(line);
                    if (m.Groups.Count != 4)
                        throw new Exception(String.Format("ParseConfigurationFile: Error parsing {0}", fullFilePath));

                    Configuration cfg = new Configuration(m.Groups[0].Value, m.Groups[1].Value, m.Groups[3].Value, m.Groups[4].Value);
                }
            }
            catch (FileNotFoundException)
            {
                return retVal;
            }

            return retVal;
        }

        public static string ConfigurationsToString(Dictionary<string, Configuration> configs)
        {
            StringWriter sw = new StringWriter();

            //Key[type;comment]=Value
            foreach (KeyValuePair<string, Configuration> config in configs)
                sw.WriteLine(String.Format("{0}[{1};{2}]={3}", config.Key, config.Value.Type, config.Value.Comment, config.Value.Value));
            return sw.ToString();
        }
    }
}
