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


}
