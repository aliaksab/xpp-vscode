using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace XppVsCode.Server
{
    /// <summary>MCP over stdio: one JSON-RPC message per line.</summary>
    internal sealed class McpServer
    {
        private const string ProtocolVersion = "2025-06-18";
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private readonly ToolRouter _router;
        private readonly string _version;

        public McpServer(ToolRouter router, string version)
        {
            _router = router;
            _version = version;
        }

        public void Run()
        {
            var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

            string line;
            while ((line = input.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                string response = Handle(line);
                if (response != null) output.WriteLine(response);
            }
        }

        private string Handle(string line)
        {
            Dictionary<string, object> msg;
            try { msg = (Dictionary<string, object>)_json.DeserializeObject(line); }
            catch (Exception ex) { return Error(null, -32700, "Parse error: " + ex.Message); }

            msg.TryGetValue("id", out object id);
            string method = msg.TryGetValue("method", out object m) ? m as string : null;
            var args = msg.TryGetValue("params", out object p) ? p as Dictionary<string, object> : null;

            // Notifications carry no id and get no response.
            if (id == null) return null;

            try
            {
                switch (method)
                {
                    case "initialize":
                        string requested = args != null && args.TryGetValue("protocolVersion", out object v) ? v as string : null;
                        return Result(id, new Dictionary<string, object>
                        {
                            ["protocolVersion"] = requested ?? ProtocolVersion,
                            ["capabilities"] = new Dictionary<string, object> { ["tools"] = new Dictionary<string, object>() },
                            ["serverInfo"] = new Dictionary<string, object> { ["name"] = "xpp-vscode", ["version"] = _version },
                        });
                    case "ping":
                        return Result(id, new Dictionary<string, object>());
                    case "tools/list":
                        return Result(id, new Dictionary<string, object> { ["tools"] = _router.ListTools() });
                    case "tools/call":
                        string name = args?["name"] as string;
                        var toolArgs = args != null && args.TryGetValue("arguments", out object a) ? a as Dictionary<string, object> : null;
                        return Result(id, ToolResult(_router.Call(name, toolArgs ?? new Dictionary<string, object>())));
                    default:
                        return Error(id, -32601, "Method not found: " + method);
                }
            }
            catch (ToolException ex)
            {
                return Result(id, ToolResult(ex.Message, isError: true));
            }
            catch (Exception ex)
            {
                Log.Write("Unhandled error in " + method + ": " + ex);
                return Result(id, ToolResult("Internal error: " + ex.Message, isError: true));
            }
        }

        private static Dictionary<string, object> ToolResult(string text, bool isError = false) => new Dictionary<string, object>
        {
            ["content"] = new object[] { new Dictionary<string, object> { ["type"] = "text", ["text"] = text } },
            ["isError"] = isError,
        };

        private string Result(object id, object result) =>
            _json.Serialize(new Dictionary<string, object> { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result });

        private string Error(object id, int code, string message) =>
            _json.Serialize(new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["error"] = new Dictionary<string, object> { ["code"] = code, ["message"] = message },
            });
    }

    internal sealed class ToolException : Exception
    {
        public ToolException(string message) : base(message) { }
    }

    internal static class Log
    {
        // stdout is reserved for protocol messages.
        public static void Write(string message) => Console.Error.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
    }
}
