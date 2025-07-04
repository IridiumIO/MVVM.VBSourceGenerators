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


                    var attributeParams = GetRelayCommandAttributeParameters(method, semanticModel);
                    
                    if (attributeParams.HasCanExecute)
                    {
                        // Find the RelayCommand attribute node
                        var relayCommandAttribute = method.BlockStatement.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .FirstOrDefault(attr => attr.Name.ToString().Contains("RelayCommand"));

                        if (relayCommandAttribute != null)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                CanExecuteParameterDefined,
                                relayCommandAttribute.GetLocation(),
                                methodName));
                        }
                    }

                    //Check if the method has parameters
                    var parameters = method.SubOrFunctionStatement.ParameterList.Parameters;

                    // Only allow one parameter, or two if the second is a CancellationToken in an async function
                    if (
                        (isAsyncFunction && parameters.Count > 2) ||
                        (!isAsyncFunction && parameters.Count > 1) ||
                        (isAsyncFunction && parameters.Count == 2 && parameters[1].AsClause?.Type?.ToString() != "CancellationToken")
                    )
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(TooManyParametersError, method.SubOrFunctionStatement.Identifier.GetLocation(), methodName));
                    }

                    // If the method has parameters, we need to generate the command with the type of the first parameter
                    var ofType = parameters.Count > 0 ? $"(Of {parameters[0].AsClause.Type})" : string.Empty;

                    //Check if a Function exists called "Can{MethodName} to act as the CanExecute() callback"
                    var canExecuteMethodName = $"Can{methodName}";
                    var canExecuteMethod = classNode.Members
                        .OfType<MethodBlockSyntax>()
                        .FirstOrDefault(m => m.SubOrFunctionStatement.Identifier.Text == canExecuteMethodName);

                    var canExecuteReturnType = canExecuteMethod?.SubOrFunctionStatement.AsClause?.Type.ToString() ?? "Boolean";


                    var hasAsyncRelayCommandOptions = attributeParams.AllowConcurrentExecutions || attributeParams.FlowExceptionsToTaskScheduler;
                    var asyncRelayCommandOptions = "";
                    if (hasAsyncRelayCommandOptions)
                    {
                        asyncRelayCommandOptions = String.Join(" Or ", new List<string> { 
                            attributeParams.FlowExceptionsToTaskScheduler ? "CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler" : "",
                            attributeParams.AllowConcurrentExecutions ? "CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions.AllowConcurrentExecutions" : ""
                        }.Where(s => !string.IsNullOrEmpty(s)));
                    }


                    if (canExecuteMethod is not null && canExecuteReturnType == "Boolean")
                    {
                        sb.AppendLine($"    Public ReadOnly Property {methodName}Command As {relayCommandInterfaceType} = New {relayedCommandType}{ofType}(AddressOf {methodName}, AddressOf {canExecuteMethodName} {(hasAsyncRelayCommandOptions ? ", options:= " + asyncRelayCommandOptions : String.Empty)})");
                    }else
                    {
                        sb.AppendLine($"    Public ReadOnly Property {methodName}Command As {relayCommandInterfaceType} = New {relayedCommandType}{ofType}(AddressOf {methodName} {(hasAsyncRelayCommandOptions ? ", options:= " + asyncRelayCommandOptions : String.Empty)})");
                    }

                    if (attributeParams.IncludeCancellationCommand && isAsync)
                    {
                        sb.AppendLine($"    Public ReadOnly Property {methodName}CancelCommand As System.Windows.Input.ICommand = CommunityToolkit.Mvvm.Input.IAsyncRelayCommandExtensions.CreateCancelCommand({methodName}Command)");
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




    private static (
        bool IncludeCancellationCommand, 
        bool AllowConcurrentExecutions, 
        bool FlowExceptionsToTaskScheduler, 
        bool HasCanExecute)
        GetRelayCommandAttributeParameters(MethodBlockSyntax method, SemanticModel sem)
    {
        bool includeCancelCommand = false;
        bool allowConcurrentExecutions = false;
        bool flowExceptionsToTaskScheduler = false;
        bool canExecuteDefined = false;

        foreach (var attribute in method.BlockStatement.AttributeLists.SelectMany(al => al.Attributes))
        {
            if (!attribute.Name.ToString().Contains("RelayCommand")) continue;

            if (attribute.ArgumentList == null) continue;

            foreach (var arg in attribute.ArgumentList.Arguments)
            {
                if (arg is not SimpleArgumentSyntax simpleArg || simpleArg.NameColonEquals == null) continue;

                var argName = simpleArg.NameColonEquals.Name.Identifier.Text;
                var valueOpt = sem.GetConstantValue(simpleArg.Expression);

                if (!valueOpt.HasValue) continue;

                switch (argName)
                {
                    case "IncludeCancelCommand":
                        if (valueOpt.Value is bool b1) includeCancelCommand = b1;
                        break;
                    case "AllowConcurrentExecutions":
                        if (valueOpt.Value is bool b2) allowConcurrentExecutions = b2;
                        break;
                    case "FlowExceptionsToTaskScheduler":
                        if (valueOpt.Value is bool b3) flowExceptionsToTaskScheduler = b3;
                        break;
                    case "CanExecute":
                        canExecuteDefined = true;
                        break;
                }
            }
        }

        return (includeCancelCommand, allowConcurrentExecutions, flowExceptionsToTaskScheduler, canExecuteDefined);
    }



    private static readonly DiagnosticDescriptor AsyncSubWarning = new DiagnosticDescriptor(
      id: "IRI001",
      title: "Async Sub is not supported for RelayCommand",
      messageFormat: "Method '{0}' is an Async Sub. Use Async Function returning Task for async RelayCommands.",
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
     messageFormat: "Method '{0}' contains multiple parameters. Only one parameter can be used with a RelayCommand, except when the second parameter is a CancellationToken in an async method",
     category: "Usage",
     DiagnosticSeverity.Error,
     isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CanExecuteParameterDefined = new DiagnosticDescriptor(
     id: "IRI004",
     title: "CanExecute property not implemented for RelayCommand attribute",
     messageFormat: "Method '{0}' describes the 'CanExecute' property which is not supported and will be ignored. You should instead create a method called Can{0}.",
     category: "Usage",
     DiagnosticSeverity.Warning,
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
