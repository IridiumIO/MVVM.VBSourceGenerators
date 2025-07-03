using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MVVM.VBSourceGenerators.Generators;

[Generator(LanguageNames.VisualBasic)]
public class RelayCommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all VB.NET methods with <RelayCommand>
        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodBlockSyntax,
                transform: static (ctx, _) =>
                {
                    var method = (MethodBlockSyntax)ctx.Node;
                    foreach (var attributeList in method.BlockStatement.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            var name = attribute.Name.ToString();
                            if (name.Contains("RelayCommand"))
                            {
                                return (method, ctx.SemanticModel);
                            }
                        }
                    }
                    return default;
                })
            .Where(static t => t != default);

        var collected = methodDeclarations.Collect();


        context.RegisterSourceOutput(collected, (spc, methods) =>
        {

            var methodsByClass = methods
                .Where(f => f.Item1.Parent is ClassBlockSyntax)
                .GroupBy(f => (ClassBlockSyntax)f.Item1.Parent);

            foreach (var classGroup in methodsByClass)
            {
                var classNode = classGroup.Key;
                var className = classNode.ClassStatement.Identifier.Text;
                var ns = GetNamespace(classNode);

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"Namespace {ns}");
                sb.AppendLine($"Partial Class {className}");

                foreach (var (method, semanticModel) in classGroup)
                {
                    
                    var methodName = method.SubOrFunctionStatement.Identifier.Text;
                    var methodReturnType = method.SubOrFunctionStatement.AsClause?.Type.ToString() ?? "Void";

                    // Check if the method is async
                    var isAsync = method.SubOrFunctionStatement.Modifiers.Any(m => m.Text == "Async");
                    var isFunction = method.SubOrFunctionStatement.Kind() == Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FunctionStatement;
                    var isAsyncSub = isAsync && !isFunction;
                    var isAsyncFunction = isAsync && isFunction && methodReturnType.Contains("Task");

                    // Report diagnostic if Async Sub
                    if (isAsyncSub)
                        spc.ReportDiagnostic(Diagnostic.Create(AsyncSubWarning, method.SubOrFunctionStatement.Identifier.GetLocation(), methodName));

                    // Report diagnostic if non-async Function
                    if (isFunction && !isAsync && !methodReturnType.Contains("Task"))
                        spc.ReportDiagnostic(Diagnostic.Create(SyncFunctionWarning, method.SubOrFunctionStatement.Identifier.GetLocation(), methodName));


                    var relayCommandInterfaceType = isAsyncFunction ? "CommunityToolkit.MVVM.Input.IAsyncRelayCommand" : "CommunityToolkit.MVVM.Input.IRelayCommand";
                    var relayedCommandType = isAsyncFunction ? "CommunityToolkit.MVVM.Input.AsyncRelayCommand" : "CommunityToolkit.MVVM.Input.RelayCommand";


                    //Check if the method has parameters
                    var parameters = method.SubOrFunctionStatement.ParameterList.Parameters;

                    // Report diagnostic if too many parameters (I think you can only have one?)
                    if (parameters.Count > 1)
                        spc.ReportDiagnostic(Diagnostic.Create(TooManyParametersError, method.SubOrFunctionStatement.Identifier.GetLocation(), methodName));

                    // If the method has parameters, we need to generate the command with the type of the first parameter
                    var ofType = parameters.Count > 0 ? $"(Of {parameters[0].AsClause.Type})" : string.Empty;

                    //Check if a Function exists called "Can{MethodName} to act as the CanExecute() callback"
                    var canExecuteMethodName = $"Can{methodName}";
                    var canExecuteMethod = classNode.Members
                        .OfType<MethodBlockSyntax>()
                        .FirstOrDefault(m => m.SubOrFunctionStatement.Identifier.Text == canExecuteMethodName);

                    var canExecuteReturnType = canExecuteMethod?.SubOrFunctionStatement.AsClause?.Type.ToString() ?? "Boolean";

                    if (canExecuteMethod is not null && canExecuteReturnType == "Boolean")
                    {
                        sb.AppendLine($"    Public ReadOnly Property {methodName}Command As {relayCommandInterfaceType} = New {relayedCommandType}{ofType}(AddressOf {methodName}, AddressOf {canExecuteMethodName})");
                    }else
                    {
                        sb.AppendLine($"    Public ReadOnly Property {methodName}Command As {relayCommandInterfaceType} = New {relayedCommandType}{ofType}(AddressOf {methodName})");
                    }


                }

                sb.AppendLine("End Class");
                if (!string.IsNullOrEmpty(ns))
                {
                    sb.AppendLine("End Namespace");
                }

                spc.AddSource($"{className}.g_RelayCommands.vb", sb.ToString());
            }

        });


    }

    private static readonly DiagnosticDescriptor AsyncSubWarning = new DiagnosticDescriptor(
      id: "IRI001",
      title: "Async Sub is not supported for RelayCommand",
      messageFormat: "Method '{0}' is an Async Sub. Use Async Function returning Task for async commands.",
      category: "Usage",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor SyncFunctionWarning = new DiagnosticDescriptor(
      id: "IRI002",
      title: "Synchronous Functions should not be used as RelayCommands",
      messageFormat: "Method '{0}' is a synchronous function. Use a Sub instead as returns will be discarded.",
      category: "Usage",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true);


    private static readonly DiagnosticDescriptor TooManyParametersError = new DiagnosticDescriptor(
     id: "IRI003",
     title: "Multiple parameters are not support for RelayCommands",
     messageFormat: "Method '{0}' contains multiple parameters. Only one parameter can be used with a RelayCommand",
     category: "Usage",
     DiagnosticSeverity.Error,
     isEnabledByDefault: true);



    private static string GetNamespace(SyntaxNode node)
    {
        while (node != null)
        {
            if (node is NamespaceBlockSyntax ns)
                return ns.NamespaceStatement.Name.ToString();
            node = node.Parent;
        }
        return string.Empty;
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToUpper();
        return char.ToUpper(name[0]) + name.Substring(1);
    }


}
