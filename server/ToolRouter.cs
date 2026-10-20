using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Dynamics.AX.Metadata.MetaModel;

namespace XppVsCode.Server
{
    internal sealed class ToolRouter
    {
        private readonly XppConfig _config;
        private readonly Lazy<MetadataHost> _host;

        public ToolRouter(XppConfig config)
        {
            _config = config;
            // Loading metadata takes seconds; defer it so the MCP handshake answers immediately.
            _host = new Lazy<MetadataHost>(() => new MetadataHost(config));
        }

        public object[] ListTools() => new object[]
        {
            Tool("xpp_get_environment", "Shows the active D365FO configuration: metadata folders, custom model store, cross-reference database.",
                Schema()),
            Tool("xpp_list_models", "Lists all models. Custom models (editable) are marked.",
                Schema()),
            Tool("xpp_read_object", "Reads a class or table: declaration, methods with full source, fields.",
                Schema(new[] { "objectType", "objectName" },
                    ("objectType", "string", "AxClass or AxTable"),
                    ("objectName", "string", "Exact object name"))),
        };

        public string Call(string name, Dictionary<string, object> args)
        {
            switch (name)
            {
                case "xpp_get_environment": return GetEnvironment();
                case "xpp_list_models": return ListModels();
                case "xpp_read_object": return ReadObject(Required(args, "objectType"), Required(args, "objectName"));
                default: throw new ToolException("Unknown tool: " + name);
            }
        }

        private string GetEnvironment()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Configuration: " + _config.ConfigFile);
            sb.AppendLine("Framework directory: " + _config.FrameworkDirectory);
            sb.AppendLine("Reference packages: " + string.Join("; ", _config.ReferencePackagesPaths));
            sb.AppendLine("Custom model store: " + _config.ModelStoreFolder);
            sb.AppendLine("Cross-reference DB: " + _config.CrossReferencesDbServerName + " / " + _config.CrossReferencesDatabaseName);
            return sb.ToString();
        }

        private string ListModels()
        {
            var models = _host.Value.GetModels().OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var sb = new StringBuilder();
            sb.AppendLine(models.Count + " models:");
            foreach (var m in models)
                sb.AppendLine("- " + m.Name + " (" + m.Layer + (IsCustom(m) ? ", custom" : "") + ")");
            return sb.ToString();
        }

        private bool IsCustom(ModelInfo model)
        {
            string store = _config.ModelStoreFolder;
            return !string.IsNullOrEmpty(store)
                && System.IO.Directory.Exists(System.IO.Path.Combine(store, model.Module ?? model.Name));
        }

        private string ReadObject(string objectType, string objectName)
        {
            var service = _host.Value.MetaService;
            switch (objectType)
            {
                case "AxClass":
                    var cls = service.GetClass(objectName) ?? throw new ToolException("Class '" + objectName + "' not found.");
                    return FormatSource(cls.Name, cls.Declaration, cls.Methods.Select(m => m.Source));
                case "AxTable":
                    var table = service.GetTable(objectName) ?? throw new ToolException("Table '" + objectName + "' not found.");
                    var sb = new StringBuilder();
                    sb.AppendLine("Table " + table.Name + " (label " + table.Label + ")");
                    sb.AppendLine("Fields:");
                    foreach (var f in table.Fields)
                        sb.AppendLine("- " + f.Name + " : " + f.GetType().Name.Replace("AxTableField", "") + EdtSuffix(f));
                    sb.Append(FormatSource(null, table.SourceCode?.Declaration, table.Methods.Select(m => m.Source)));
                    return sb.ToString();
                default:
                    throw new ToolException("Unsupported objectType '" + objectType + "'. This preview supports AxClass and AxTable.");
            }
        }

        private static string EdtSuffix(AxTableField field) =>
            string.IsNullOrEmpty(field.ExtendedDataType) ? "" : " (EDT " + field.ExtendedDataType + ")";

        private static string FormatSource(string title, string declaration, IEnumerable<string> methods)
        {
            var sb = new StringBuilder();
            if (title != null) sb.AppendLine("Class " + title);
            if (!string.IsNullOrWhiteSpace(declaration))
            {
                sb.AppendLine("Declaration:");
                sb.AppendLine(declaration);
            }
            foreach (string source in methods)
            {
                sb.AppendLine();
                sb.AppendLine(source);
            }
            return sb.ToString();
        }

        private static string Required(Dictionary<string, object> args, string name)
        {
            if (args.TryGetValue(name, out object value) && value is string s && s.Length > 0) return s;
            throw new ToolException("Missing required argument: " + name);
        }

        private static Dictionary<string, object> Tool(string name, string description, Dictionary<string, object> schema) =>
            new Dictionary<string, object> { ["name"] = name, ["description"] = description, ["inputSchema"] = schema };

        private static Dictionary<string, object> Schema(string[] required = null, params (string name, string type, string description)[] props) =>
            new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = props.ToDictionary(p => p.name, p => (object)new Dictionary<string, object>
                {
                    ["type"] = p.type,
                    ["description"] = p.description,
                }),
                ["required"] = required ?? new string[0],
            };
    }
}
