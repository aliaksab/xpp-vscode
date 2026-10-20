using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace XppVsCode.Server
{
    internal sealed class XppConfig
    {
        public string ConfigFile { get; private set; }
        public string FrameworkDirectory { get; private set; }
        public string BinDirectory => Path.Combine(FrameworkDirectory, "bin");
        public List<string> ReferencePackagesPaths { get; private set; } = new List<string>();
        public string ModelStoreFolder { get; private set; }
        public string CrossReferencesDbServerName { get; private set; }
        public string CrossReferencesDatabaseName { get; private set; }
        public string DefaultModelForNewProjects { get; private set; }

        public static XppConfig Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                throw new FileNotFoundException("D365FO configuration file not found: " + configFile);

            var json = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(File.ReadAllText(configFile));
            var cfg = new XppConfig
            {
                ConfigFile = configFile,
                FrameworkDirectory = Get(json, "FrameworkDirectory"),
                ModelStoreFolder = Get(json, "ModelStoreFolder"),
                CrossReferencesDbServerName = Get(json, "CrossReferencesDbServerName"),
                CrossReferencesDatabaseName = Get(json, "CrossReferencesDatabaseName"),
                DefaultModelForNewProjects = Get(json, "DefaultModelForNewProjects"),
            };
            if (json.TryGetValue("ReferencePackagesPaths", out object paths) && paths is IEnumerable list)
                cfg.ReferencePackagesPaths = list.OfType<string>().ToList();
            if (string.IsNullOrEmpty(cfg.FrameworkDirectory))
                throw new InvalidOperationException("FrameworkDirectory is missing in " + configFile);
            return cfg;
        }

        private static string Get(Dictionary<string, object> json, string name) =>
            json.TryGetValue(name, out object value) ? value as string : null;
    }
}
