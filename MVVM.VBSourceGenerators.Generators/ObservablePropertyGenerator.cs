using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                    foreach (var attributeList in field.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            var name = attribute.Name.ToString();
                            if (name.Contains("ObservableProperty"))
                            {
                                return (field, ctx.SemanticModel);
                            }
                        }
                    }
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
                sb.AppendLine(@"<Global.System.CodeDom.Compiler.GeneratedCode(""IridiumIO.MVVM.VBSourceGenerators"", ""0.3.0"")>");
                sb.AppendLine($"Partial Class {className}");

                foreach (var (field, semanticModel) in classGroup)
                {
                    var dependentProperties = GetDependentProperties(field);
                    var canExecuteChangedForProperties = GetCanExecuteChangedForProperties(field);

                    foreach (var declarator in field.Declarators)
                    {
                        foreach (var nameSyntax in declarator.Names)
                        {
                            var fieldName = nameSyntax.Identifier.Text.TrimStart('_');
                            var propertyName = ToPascalCase(fieldName.TrimStart('_'));
                            string typeName = GetPropertyTypeName(semanticModel, declarator);

                            sb.AppendLine($"    Public Property {propertyName} As {typeName}");
                            sb.AppendLine($"        Get");
                            sb.AppendLine($"            Return _{fieldName}");
                            sb.AppendLine($"        End Get");
                            sb.AppendLine($"        Set(value As {typeName})");
                            sb.AppendLine($"            If Global.System.Collections.Generic.EqualityComparer(Of {typeName}).Default.Equals(_{fieldName}, value) Then Return");
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

                spc.AddSource($"{className}.g_ObservableProperties.vb", sb.ToString());
            }

        });


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
        foreach (var attributeList in field.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName.Contains("NotifyCanExecuteChangedFor"))
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
            }
        }
        return canExecuteChangedProperties;
    }

    private static List<string> GetDependentProperties(FieldDeclarationSyntax field)
    {
        // Collect dependent property names from NotifyPropertyChangedFor
        var dependentProperties = new List<string>();
        foreach (var attributeList in field.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName.Contains("NotifyPropertyChangedFor"))
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
            }
        }
        return dependentProperties;
    }

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
