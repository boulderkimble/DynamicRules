import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';
import * as os from 'os';

export function activate(context: vscode.ExtensionContext) {
    const diagnosticCollection = vscode.languages.createDiagnosticCollection('linqRules');

    context.subscriptions.push(
        vscode.workspace.onDidSaveTextDocument(document => {
            const editor = vscode.window.activeTextEditor;
            const fileNamePattern = getFileNamePattern();
            if (
                editor &&
                isRuleFile(document) &&
                matchesPattern(path.basename(document.fileName), fileNamePattern)
            ) {
                validateRuleFile(editor.document, diagnosticCollection, context);
            }
        }),
        vscode.commands.registerCommand('rulevalidator.validate', () => {
            const editor = vscode.window.activeTextEditor;
            const fileNamePattern = getFileNamePattern();
            if (
                editor &&
                isRuleFile(editor.document) &&
                matchesPattern(path.basename(editor.document.fileName), fileNamePattern)
            ) {
                validateRuleFile(editor.document, diagnosticCollection, context);
            } else {
                vscode.window.showInformationMessage('File does not match the Rule Validator file name pattern.');
            }
        }),
        vscode.commands.registerCommand('rulevalidator.setAssemblies', async () => {
            let allPaths: string[] = [];
            let keepGoing = true;
            while (keepGoing) {
                const uris = await vscode.window.showOpenDialog({
                    canSelectMany: true,
                    filters: { 'Assemblies': ['dll'] },
                    openLabel: 'Select Assemblies'
                });
                if (uris && uris.length > 0) {
                    allPaths.push(...uris.map(uri => uri.fsPath));
                    const answer = await vscode.window.showQuickPick(['Yes', 'No'], { placeHolder: 'Add more assemblies from another directory?' });
                    keepGoing = answer === 'Yes';
                } else {
                    keepGoing = false;
                }
            }
            if (allPaths.length > 0) {
                await vscode.workspace.getConfiguration().update(
                    'rulevalidator.assemblyPaths',
                    allPaths.join(','),
                    vscode.ConfigurationTarget.Global
                );
            vscode.window.showInformationMessage('Assembly paths updated.');
            }
        })
    );

    function getFileNamePattern(): string {
        return vscode.workspace.getConfiguration().get<string>('rulevalidator.fileNamePattern') || 'rules_*';
    }

    function matchesPattern(fileName: string, pattern: string): boolean {
        const regex = new RegExp(pattern);
        return regex.test(fileName);
    }

    function isRuleFile(document: vscode.TextDocument): boolean {
        const ext = path.extname(document.fileName).toLowerCase();
        return (
            document.languageId === 'yaml' ||
            document.languageId === 'json' ||
            ext === '.yaml' ||
            ext === '.yml' ||
            ext === '.json'
        );
    }
}

function getCliPath(context: vscode.ExtensionContext): string {
    return path.join(context.extensionPath, 'cli', 'Release', 'net8.0', 'RuleValidator.exe');
}

function getAssemblyPaths(): string | undefined {
    return vscode.workspace.getConfiguration().get<string>('rulevalidator.assemblyPaths');
}

function validateRuleFile(document: vscode.TextDocument, diagnostics: vscode.DiagnosticCollection, context: vscode.ExtensionContext) {
    const assemblyPaths = getAssemblyPaths();
    if (!assemblyPaths) {
        vscode.window.showErrorMessage('Please set rulevalidator.assemblyPaths in the extension settings.');
        return;
    }
    const cliPath = getCliPath(context);
    execFile(cliPath, [document.fileName, assemblyPaths], (error, stdout, stderr) => {
        const diags: vscode.Diagnostic[] = [];
        const output = vscode.window.createOutputChannel('Rule Validator');
        if (error && (error as any).code !== 3) {
            vscode.window.showErrorMessage(`Validation failed: ${stderr || error.message}`);
            diagnostics.set(document.uri, []);
            output.appendLine(`Validation failed: ${stderr || error.message}`);
            output.show(true);
            return;
        }

        const lines = stdout.split('\n');
        const text = document.getText();
        for (const line of lines) {
            // Global parameters
            const globalParamMatch = line.match(/Parameter '(.+?)' error: (.+)/);
            // Rule Condition/Then errors
            const ruleMatch = line.match(/Rule '(.+?)' (Condition|Then) error: (.+)/);
            // Rule parameter errors: Rule 'ruleId' Parameter 'paramName' error: ...
            const paramMatch = line.match(/Rule '(.+?)' Parameter '(.+?)' error: (.+)/);
            if (globalParamMatch) {
                const [, paramName, message] = globalParamMatch;
                let idx = text.indexOf(paramName);
                let lineNum = 0;
                if (idx !== -1) {
                    lineNum = document.positionAt(idx).line;
                }
                diags.push(new vscode.Diagnostic(
                    new vscode.Range(lineNum, 0, lineNum, 100),
                    `[Parameter: ${paramName}] ${message}`,
                    vscode.DiagnosticSeverity.Error
                ));
                output.appendLine(`[Parameter: ${paramName}] ${message}`);
            } else if (ruleMatch) {
                const [, ruleId, field, message] = ruleMatch;
                const idx = text.indexOf(ruleId);
                let lineNum = 0;
                if (idx !== -1) {
                    lineNum = document.positionAt(idx).line;
                }
                diags.push(new vscode.Diagnostic(
                    new vscode.Range(lineNum, 0, lineNum, 100),
                    `[${field}] ${message}`,
                    vscode.DiagnosticSeverity.Error
                ));
                output.appendLine(`[${field}] ${message}`);
            } else if (paramMatch) {
                const [, ruleId, paramName, message] = paramMatch;
                let idx = text.indexOf(paramName);
                let lineNum = 0;
                if (idx !== -1) {
                    lineNum = document.positionAt(idx).line;
                }
                diags.push(new vscode.Diagnostic(
                    new vscode.Range(lineNum, 0, lineNum, 100),
                    `[Parameter: ${paramName}] ${message}`,
                    vscode.DiagnosticSeverity.Error
                ));
                output.appendLine(`[Parameter: ${paramName}] ${message}`);
            }
        }
        diagnostics.set(document.uri, diags);
        if (diags.length === 0) {
            output.appendLine('Rule validation successful.');
            output.show(true);
            vscode.window.showInformationMessage('Rule validation successful.');
        }
    });
}

export function deactivate() { }