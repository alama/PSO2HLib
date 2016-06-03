using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace PSO2H
{
    //TODO: Change exceptions to instead log errors, and return false to caller instead
    public enum ConfigurationType
    {
        STRING, //Free Text
        SELECT, //Single Select
        MSELECT, //Multi Select
        TOGGLE, //Dual value switch
        RANGE, //Range of values
        //If any new types are added here, ValidateParameters must also be updated
    }

    public class Configuration
    {
        public readonly string Name;
        public string Value;
        public ConfigurationType Type;
        public IEnumerable<string> Parameters; //Parameters are parsed out as a CSV

        public Configuration(string configName, string configType, string parameters, string configValue)
        {
            Name = configName;
            Value = configValue ?? "";
            if (!Enum.TryParse<ConfigurationType>(configType, out Type))
                throw new Exception($"Configuration Type {configType} not recognized.");
            Parameters = parameters.Split(',');
        }

        //For Parameter Validation. Returns false if the parameters don't match the expected format for the configType
        //Returns true if the parameter is valid, false otherwise
        //If false, a message is stored in errMsg to specify the issue
        public static bool ValidateParameters(ConfigurationType configType, IEnumerable<string> parameters, out string errMsg)
        {
            errMsg = "";
            switch (configType)
            {
                case ConfigurationType.STRING: //String expects no parameters, but can have something like a pre-determined field, always true
                    return true;
                case ConfigurationType.SELECT:
                    if (parameters.Count() == 0)
                    {
                        errMsg = "SELECT must have 1 parameter";
                        return false;
                    }
                    break;
                case ConfigurationType.MSELECT:
                    if (parameters.Count() == 0)
                    {
                        errMsg = "MSELECT must have 1 parameter";
                        return false;
                    }
                    break;
                case ConfigurationType.TOGGLE:
                    if (parameters.Count() != 2)
                    {
                        errMsg = "TOGGLE must have exactly 2 parameters.";
                        return false;
                    }
                    break;
                case ConfigurationType.RANGE:
                    if (parameters.Count() != 2)
                    {
                        errMsg = "RANGE must have exactly 2 32-bit INTEGER parameters (MIN and MAX).";
                        return false;
                    }
                    foreach (string s in parameters)
                    {
                        int throwaway;
                        if (!Int32.TryParse(s, out throwaway))
                        {
                            errMsg = "RANGE parameters must be 32-bit integers.";
                            return false;
                        }
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        public static Regex configFormat = new Regex(@"(^[^[]*)\[([a-zA-Z]*);([^\]]*)]=([^\n\r]*)");

        public static Dictionary<string, Configuration> ParseConfigurationFile(string fullFilePath)
        {
            Dictionary<string, Configuration> retVal = new Dictionary<string, Configuration>();

            //Key[type;param1,param2,param3...]=Value
            try
            {
                foreach (string line in File.ReadLines(fullFilePath))
                {
                    Match m = configFormat.Match(line);
                    if (m.Groups.Count != 5)
                        throw new Exception($"ParseConfigurationFile: Error parsing {fullFilePath}");

                    Configuration cfg = new Configuration(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);

                    retVal.Add(cfg.Name, cfg);
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

            //Key[type;param1,param2,param3...]=Value
            foreach (KeyValuePair<string, Configuration> config in configs)
                sw.WriteLine($"{config.Key}[{Enum.GetName(typeof(ConfigurationType), config.Value.Type)};{String.Join(",", config.Value.Parameters)}]={config.Value.Value}");

            return sw.ToString();
        }
    }
}