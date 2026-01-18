using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MVVM.VBSourceGenerators.Generators;

public static  class SyntaxUtilities
{


    public static string GetNamespace(SyntaxNode node)
    {
        while (node != null)
        {
            if (node is NamespaceBlockSyntax ns)
                return ns.NamespaceStatement.Name.ToString();
            node = node.Parent;
        }
        return string.Empty;
    }

    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToUpper();
        return char.ToUpper(name[0]) + name.Substring(1);
    }




    public static IEnumerable<AttributeSyntax> GetAttributesByName(SyntaxNode node, string attributeName)
    {
        var attributeLists = node switch
        {
            MethodBlockSyntax m => m.BlockStatement.AttributeLists,
            FieldDeclarationSyntax f => f.AttributeLists,
            _ => Enumerable.Empty<AttributeListSyntax>()
        };

        return attributeLists
            .SelectMany(al => al.Attributes)
            .Where(attr => attr.Name.ToString().Contains(attributeName));
    }

    public static AttributeSyntax? GetFirstAttributeByName(SyntaxNode node, string attributeName)
    {
        return GetAttributesByName(node, attributeName).FirstOrDefault();
    }


    public static ITypeSymbol? GetTypeSymbol(SemanticModel semanticModel, TypeSyntax? typeSyntax)
    {
        return typeSyntax != null ? semanticModel.GetTypeInfo(typeSyntax).Type : null;
    }

    public static ITypeSymbol? GetTypeSymbolFromInitializer(SemanticModel semanticModel, ExpressionSyntax? initializer)
    {
        return initializer != null ? semanticModel.GetTypeInfo(initializer).Type : null;
    }


    public static bool InheritsFrom(INamedTypeSymbol? classSymbol, string baseTypeName)
    {
        if (classSymbol == null) return false;
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == baseTypeName)
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }



    public static string GetTypeName(SemanticModel semanticModel, VariableDeclaratorSyntax declarator)
    {
        var asClause = declarator.AsClause as SimpleAsClauseSyntax;
        ITypeSymbol typeSymbol = null;

        if (asClause?.Type != null)
            typeSymbol = semanticModel.GetTypeInfo(asClause.Type).Type;
        else if (declarator.Initializer?.Value is ExpressionSyntax initExpr)
            typeSymbol = semanticModel.GetTypeInfo(initExpr).Type;

        if (typeSymbol == null || typeSymbol.SpecialType == SpecialType.System_Object)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(declarator.Names.First()) as IFieldSymbol;
            if (declaredSymbol != null && declaredSymbol.Type != null)
                typeSymbol = declaredSymbol.Type;
        }

        return typeSymbol?.ToDisplayString() ?? "Object";
    }


    public static string[] GetAttachableAttributes(ClassBlockSyntax classNode, SyntaxNode node, SemanticModel semanticModel, SourceProductionContext spc)
    {

        var attributes = GetAttributesByName(node, "AttachAttribute").ToList();
        var result = new List<string>();

        foreach (var attr in attributes)
        {
            // For each argument in the attribute (should be TypeSyntax, e.g., GetType(JsonIgnore))
            foreach (var arg in attr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>())
            {

                var expr = arg.GetExpression();

                if (expr is GetTypeExpressionSyntax getTypeExpr)
                {
                    // Get the type symbol for the type in GetType(...)
                    var typeSymbol = semanticModel.GetTypeInfo(getTypeExpr.Type).Type;
                    if (typeSymbol != null)
                    {
                        // Use the unqualified name for the attribute (e.g., JsonIgnore)
                        // Optionally, you could use typeSymbol.Name + "Attribute" if you want to support both forms
                        var attrName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        result.Add(attrName);
                    }
                }
                else if (expr is LiteralExpressionSyntax literal && literal.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.StringLiteralExpression))
                {
                 
                    result.Add(literal.Token.ValueText);
                }
            }
        }

        return result.ToArray();
    }

    public static void CollectInterfaceTypeNamesFromVariable(IFieldSymbol variableSymbol, HashSet<string> result)
    {
        if (variableSymbol == null || result == null) return;

        foreach (var attr in variableSymbol.GetAttributes())
        {
            var name = attr.AttributeClass?.Name;
            if (name == null) continue;
            if (!name.Equals("ImplementsPropertyAttribute", StringComparison.Ordinal) &&
                !name.Equals("ImplementsProperty", StringComparison.Ordinal)) continue;

            foreach (var ctorArg in attr.ConstructorArguments)
            {
                if (ctorArg.Kind == TypedConstantKind.Type && ctorArg.Value is ITypeSymbol tSym)
                {
                    var ifaceName = tSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    if (!string.IsNullOrEmpty(ifaceName)) result.Add(ifaceName);
                }
                else if (ctorArg.Kind == TypedConstantKind.Primitive && ctorArg.Value is string s && !string.IsNullOrEmpty(s))
                {
                    if (s.Contains('.'))
                    {
                        var lastDot = s.LastIndexOf('.');
                        if (lastDot > 0)
                        {
                            result.Add(s.Substring(0, lastDot));
                        }
                    }
                }
            }

            // Named arguments may carry interface type
            foreach (var named in attr.NamedArguments)
            {
                var val = named.Value;
                if (val.Kind == TypedConstantKind.Type && val.Value is ITypeSymbol tSym2)
                {
                    var ifaceName = tSym2.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    if (!string.IsNullOrEmpty(ifaceName)) result.Add(ifaceName);
                }
                else if (val.Kind == TypedConstantKind.Primitive && val.Value is string ss && !string.IsNullOrEmpty(ss))
                {
                    if (named.Key.IndexOf("interface", StringComparison.OrdinalIgnoreCase) >= 0 && ss.Contains('.'))
                    {
                        result.Add(ss);
                    }
                }
            }
        }
    }

    public static List<string> GetImplementsEntriesFromVariable(IFieldSymbol variableSymbol)
    {
        var implEntries = new List<string>();
        if (variableSymbol == null) return implEntries;

        var implAttrs = variableSymbol.GetAttributes()
            .Where(ad =>
            {
                var n = ad.AttributeClass?.Name;
                return n != null && (n.Equals("ImplementsPropertyAttribute", StringComparison.Ordinal) || n.Equals("ImplementsProperty", StringComparison.Ordinal));
            });

        foreach (var implAttrData in implAttrs)
        {
            string impltypeName = null;
            string memberName = null;

            // constructor args (prefer Type constants)
            foreach (var ctorArg in implAttrData.ConstructorArguments)
            {
                if (ctorArg.Kind == TypedConstantKind.Type && ctorArg.Value is ITypeSymbol tSym)
                {
                    impltypeName = tSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                }
                else if (ctorArg.Kind == TypedConstantKind.Primitive && ctorArg.Value is string s && !string.IsNullOrEmpty(s))
                {
                    if (s.Contains('.'))
                    {
                        var lastDot = s.LastIndexOf('.');
                        memberName ??= s.Substring(lastDot + 1);
                        impltypeName ??= s.Substring(0, lastDot);
                    }
                    else
                    {
                        memberName ??= s;
                    }
                }
            }

            // named args fallback
            foreach (var named in implAttrData.NamedArguments)
            {
                var key = named.Key;
                var val = named.Value;
                if (val.Kind == TypedConstantKind.Type && val.Value is ITypeSymbol tSym)
                {
                    impltypeName ??= tSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                }
                else if (val.Kind == TypedConstantKind.Primitive && val.Value is string ss && !string.IsNullOrEmpty(ss))
                {
                    if (key.Equals("MemberName", StringComparison.OrdinalIgnoreCase) || key.Equals("Member", StringComparison.OrdinalIgnoreCase))
                        memberName ??= ss;
                    else if (key.IndexOf("interface", StringComparison.OrdinalIgnoreCase) >= 0)
                        impltypeName ??= ss;
                }
            }

            if (!string.IsNullOrEmpty(impltypeName) && !string.IsNullOrEmpty(memberName))
            {
                implEntries.Add($"{impltypeName}.{memberName}");
            }
        }

        return implEntries;
    }


}
