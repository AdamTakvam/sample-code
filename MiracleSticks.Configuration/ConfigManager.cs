using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace MiracleSticks.Configuration
{
    /// <summary>Secure config manager.</summary>
    /// <remarks>app.settings won't cut it for this.</remarks>
    public class ConfigManager
    {
        private const string ConfigFile = "MiracleSticks.config";
        private const int DefaultPort = 5900;

        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ConfigData));
        private static ConfigData configData = null;

        public static ConfigData Data
        {
            get
            {
                if (configData == null)
                    configData = InitializeConfigData();
                return configData;
            }
        }

        public static void Save()
        {
            Data.Signature = ComputeSignature(Data);

            using(FileStream stream = File.Open(ConfigFile, FileMode.Create))
            {
                serializer.Serialize(stream, configData);
            }
        }

        private static ConfigData InitializeConfigData()
        {
            if (File.Exists(ConfigFile))
            {
                using (StreamReader reader = File.OpenText(ConfigFile))
                {
                    return serializer.Deserialize(reader) as ConfigData;
                }
            }
            else
            {
                ConfigData configData = new ConfigData { Port = DefaultPort };
                return configData;
            }
        }

        /// <summary>Compute signature for data integrity.</summary>
        public static string ComputeSignature(ConfigData configData)
        {
            // Yeah, it's just a hash. But I don't want to deal with portability issues around private keys.
            // This should be more than enough to thwart a novice.
            string protectedContents = String.Join("|", new string[] { configData.StickId, configData.GroupId });
            SHA1CryptoServiceProvider hasher = new SHA1CryptoServiceProvider();
            byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(protectedContents));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
