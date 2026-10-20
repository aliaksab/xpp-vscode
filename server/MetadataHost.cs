using System.Collections.Generic;
using System.Linq;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using Microsoft.Dynamics.AX.Metadata.Providers;
using Microsoft.Dynamics.AX.Metadata.Service;
using Microsoft.Dynamics.AX.Metadata.Storage;
using Microsoft.Dynamics.AX.Metadata.Storage.DiskProvider;

namespace XppVsCode.Server
{
    /// <summary>Headless replacement for the metadata services Visual Studio hosts.</summary>
    internal sealed class MetadataHost
    {
        public IMetadataProvider Provider { get; }
        public IMetaModelService MetaService { get; }

        public MetadataHost(XppConfig config)
        {
            var disk = new DiskProviderConfiguration();
            foreach (string path in config.ReferencePackagesPaths.Concat(new[] { config.ModelStoreFolder })
                         .Where(p => !string.IsNullOrEmpty(p))
                         .Distinct(System.StringComparer.OrdinalIgnoreCase))
            {
                disk.AddMetadataPath(path);
            }

            Provider = new MetadataProviderFactory().CreateDiskProvider(disk);
            MetaService = new MetaModelServiceFactory().Create(Provider);
        }

        public IEnumerable<ModelInfo> GetModels() => MetaService.GetModels();
    }
}
