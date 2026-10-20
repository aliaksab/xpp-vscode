using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace XppVsCode.Server
{
    internal static class Program
    {
        public const string Version = "0.0.1";

        private static int Main(string[] args)
        {
            string configFile = ArgValue(args, "--config");
            XppConfig config;
            try
            {
                config = XppConfig.Load(configFile);
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                return 1;
            }

            string bin = config.BinDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string path = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };

            Log.Write("xpp-vscode server " + Version + " using " + config.ConfigFile);
            Run(config);
            return 0;
        }

        // Kept separate so D365FO types are only JIT-resolved after AssemblyResolve is registered.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run(XppConfig config) => new McpServer(new ToolRouter(config), Version).Run();

        private static string ArgValue(string[] args, string name)
        {
            int i = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
