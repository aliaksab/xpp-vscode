import * as vscode from 'vscode';
import { readActiveEnvironment } from './environment';

const ServerVersion = '0.0.1';

export function registerMcpProvider(context: vscode.ExtensionContext, log: vscode.OutputChannel): void {
    const changed = new vscode.EventEmitter<void>();
    context.subscriptions.push(changed);

    context.subscriptions.push(vscode.lm.registerMcpServerDefinitionProvider('xpp.mcp', {
        onDidChangeMcpServerDefinitions: changed.event,
        provideMcpServerDefinitions: async () => {
            try {
                const env = await readActiveEnvironment();
                const exe = context.asAbsolutePath('server/bin/XppVsCode.Server.exe');
                return [new vscode.McpStdioServerDefinition('X++ (D365FO)', exe, ['--config', env.configFile], {}, ServerVersion)];
            } catch (err) {
                log.appendLine(`MCP server not started: ${(err as Error).message}`);
                return [];
            }
        },
    }));
}
