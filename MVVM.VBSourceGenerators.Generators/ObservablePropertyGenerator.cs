using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MVVM.VBSourceGenerators.Generators.DiagnosticDescriptors;
using static MVVM.VBSourceGenerators.Generators.SyntaxUtilities;

namespace MVVM.VBSourceGenerators.Generators;

[Generator(LanguageNames.VisualBasic)]
public class ObservablePropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all VB.NET fields with <ObservableProperty>
        var fieldDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is FieldDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    var field = (FieldDeclarationSyntax)ctx.Node;

                    if (GetFirstAttributeByName(field, "ObservableProperty") is not null)
                        return (field, ctx.SemanticModel);
                
                    return default;
                })
            .Where(static t => t != default);

        var collected = fieldDeclarations.Collect();


        context.RegisterSourceOutput(collected, (spc, fields) =>
        {

            var fieldsByClass = fields
                .Where(f => f.Item1.Parent is ClassBlockSyntax)
                .GroupBy(f => (ClassBlockSyntax)f.Item1.Parent);

            foreach (var classGroup in fieldsByClass)
            {
                var classNode = classGroup.Key;
                var className = classNode.ClassStatement.Identifier.Text;
                var ns = GetNamespace(classNode);

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"Namespace {ns}");

                sb.AppendLine($"Partial Class {className}");

                foreach (var (field, semanticModel) in classGroup)
                {
                    var dependentProperties = GetDependentProperties(field);
                    var canExecuteChangedForProperties = GetCanExecuteChangedForProperties(field);

                    var attachableAttributes = GetAttachableAttributes(classNode, field, semanticModel, spc);

                    bool broadcastPropertyChangedToRecipients = GetCanBroadcastToRecipients(classNode, field, semanticModel, spc);

                    foreach (var declarator in field.Declarators)
                    {
                        foreach (var nameSyntax in declarator.Names)
                        {
                            var fieldName = nameSyntax.Identifier.Text.TrimStart('_');
                            var propertyName = ToPascalCase(fieldName.TrimStart('_'));
                            string typeName = GetPropertyTypeName(semanticModel, declarator);

                            foreach (var attachableAttribute in attachableAttributes)
                            {
                            sb.AppendLine($"    <{attachableAttribute}>");
                            }

                            sb.AppendLine($"    Public Property {propertyName} As {typeName}");
                            sb.AppendLine($"        Get");
                            sb.AppendLine($"            Return _{fieldName}");
                            sb.AppendLine($"        End Get");
                            sb.AppendLine($"        Set(value As {typeName})");
                            sb.AppendLine($"            If Global.System.Collections.Generic.EqualityComparer(Of {typeName}).Default.Equals(_{fieldName}, value) Then Return");
                            
                            if (broadcastPropertyChangedToRecipients)
                            {
                            sb.AppendLine($"            Dim _oldValue As {typeName} = _{fieldName}");
                            }

                            sb.AppendLine($"            On{propertyName}Changing(value)");
                            sb.AppendLine($"            On{propertyName}Changing(Nothing, value)");
                            //TODO: Add support for static reusable OnPropertyChangingEventArgs/OnPropertyChangedEventArgs similar to the C# implementation to minimise allocations
                            sb.AppendLine($"            OnPropertyChanging(NameOf({propertyName}))");
                            sb.AppendLine($"            _{fieldName} = value");
                            sb.AppendLine($"            On{propertyName}Changed(value)");
                            sb.AppendLine($"            On{propertyName}Changed(Nothing, value)");
                            sb.AppendLine($"            OnPropertyChanged(NameOf({propertyName}))");

                            foreach (var depProp in dependentProperties)
                            {
                                sb.AppendLine($"            OnPropertyChanged(NameOf({depProp}))");
                            }
                            foreach (var depProp in canExecuteChangedForProperties)
                            {
                                sb.AppendLine($"            {depProp}.NotifyCanExecuteChanged()");
                            }

                            if (broadcastPropertyChangedToRecipients)
                            {
                                sb.AppendLine($"            Broadcast(_oldValue, value, \"{propertyName}\")");
                            }

                            sb.AppendLine($"        End Set");
                            sb.AppendLine($"    End Property");

                            sb.AppendLine();

                            sb.AppendLine($"    Partial Private Sub On{propertyName}Changing(value As {typeName}): End Sub");
                            sb.AppendLine($"    Partial Private Sub On{propertyName}Changing(oldValue As {typeName}, newValue As {typeName}): End Sub");
                            sb.AppendLine($"    Partial Private Sub On{propertyName}Changed(value As {typeName}): End Sub");
                            sb.AppendLine($"    Partial Private Sub On{propertyName}Changed(oldValue As {typeName}, newValue As {typeName}): End Sub");

                            sb.AppendLine();

                        }
                    }
                }

                sb.AppendLine("End Class");
                if (!string.IsNullOrEmpty(ns))
                {
                    sb.AppendLine("End Namespace");
                }

                spc.AddSource($"{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{className}.g.vb", sb.ToString());
            }

        });


    }

    private bool GetCanBroadcastToRecipients(ClassBlockSyntax classNode, FieldDeclarationSyntax field, SemanticModel semanticModel, SourceProductionContext spc)
    {
        // 1. Check for <NotifyPropertyChangedRecipients> on the field
        bool hasNotifyRecipients = GetAttributesByName(field, "NotifyPropertyChangedRecipients").Any();

        if (!hasNotifyRecipients) return false;

        // 2. Check if the class inherits from ObservableRecipient

        var classSymbol = semanticModel.GetDeclaredSymbol(classNode) as INamedTypeSymbol;
        if (classSymbol == null)
            return false;

        // Check base types for ObservableRecipient
        var inheritsObservableRecipient = InheritsFrom(classSymbol, "ObservableRecipient");

        if (!inheritsObservableRecipient)
        {
            var diagnostic = Diagnostic.Create(MissingObservableRecipientInheritance, field.GetLocation(), classSymbol.Name);
            spc.ReportDiagnostic(diagnostic);
        }

        return inheritsObservableRecipient;
    }

    private static string GetPropertyTypeName(SemanticModel semanticModel, VariableDeclaratorSyntax declarator)
    {
        var asClause = declarator.AsClause as SimpleAsClauseSyntax;
        ITypeSymbol typeSymbol = null;

        if (asClause?.Type != null)
        {   // explicit type
            typeSymbol = semanticModel.GetTypeInfo(asClause.Type).Type;
        }
        else if (declarator.Initializer?.Value is ExpressionSyntax initExpr)
        {   // Try to infer type from initializer
            typeSymbol = semanticModel.GetTypeInfo(initExpr).Type;
        }

        // Fallback: get declared symbol for the field and use its type
        if (typeSymbol == null || typeSymbol.SpecialType == SpecialType.System_Object)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(declarator.Names.First()) as IFieldSymbol;
            if (declaredSymbol != null && declaredSymbol.Type != null)
                typeSymbol = declaredSymbol.Type;
        }

        var typeName = typeSymbol?.ToDisplayString() ?? "Object";
        return typeName;
    }

    private static List<string> GetCanExecuteChangedForProperties(FieldDeclarationSyntax field)
    {
        var canExecuteChangedProperties = new List<string>();
        foreach (var attribute in GetAttributesByName(field, "NotifyCanExecuteChangedFor"))
        {

            // Handle multiple arguments if needed
            foreach (var arg in attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>())
            {
                // VB: NameOf(FullName)
                if (arg.GetExpression() is NameOfExpressionSyntax nameofExpr)
                {
                    var propName = nameofExpr.Argument.ToString().Trim('"');
                    canExecuteChangedProperties.Add(propName);
                }
            }
              
        }
       
        return canExecuteChangedProperties;
    }

    private static List<string> GetDependentProperties(FieldDeclarationSyntax field)
    {
        // Collect dependent property names from NotifyPropertyChangedFor
        var dependentProperties = new List<string>();
   
        foreach (var attribute in GetAttributesByName(field, "NotifyPropertyChangedFor"))
        {
            
            // Handle multiple arguments if needed
            foreach (var arg in attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>())
            {
                // VB: NameOf(FullName)
                if (arg.GetExpression() is NameOfExpressionSyntax nameofExpr)
                {
                    var propName = nameofExpr.Argument.ToString().Trim('"');
                    dependentProperties.Add(propName);
                }
            }
         
        }
       
        return dependentProperties;
    }

  

}
