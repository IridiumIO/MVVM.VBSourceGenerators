using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static MVVM.VBSourceGenerators.Generators.SyntaxUtilities;

namespace MVVM.VBSourceGenerators.Generators;

[Generator(LanguageNames.VisualBasic)]
public class CommonAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        var targetDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider<(SyntaxNode, SemanticModel)>(
                predicate: static (node, _) => node is FieldDeclarationSyntax || node is MethodBlockSyntax,
                transform: static (ctx, _) =>
                {
                    switch (ctx.Node)
                    {
                        case MethodBlockSyntax methodBlock:
                            
                            if (GetFirstAttributeByName(methodBlock, "RelayCommand") is not null)
                                return (methodBlock, ctx.SemanticModel);
                            break;
                        case FieldDeclarationSyntax field:
                            
                            if (GetFirstAttributeByName(field, "ObservableProperty") is not null)
                                return (field, ctx.SemanticModel);
                            break;
                    }


                    return default;
                })
            .Where(static t => t != default);

        var collected = targetDeclarations.Collect();


        context.RegisterPostInitializationOutput(RegisterSharedAttributes);
    }

    public static void RegisterSharedAttributes(IncrementalGeneratorPostInitializationContext ctx)
    {
        //We can't use this because it literally breaks WPF's own code generation. WTF?
        //ctx.AddEmbeddedAttributeDefinition();

        var sourceCode = """
                        Imports System

                        ''' <summary>
                        ''' Indicates that the specified attribute(s) should be attached to the generated property
                        ''' corresponding to the decorated field or method. Use this to propagate attributes such as
                        ''' <c>JsonIgnore</c>, <c>Browsable</c>, or any other attribute to the generated property when used in 
                        ''' conjunction with ObservableProperty or RelayCommand.
                        ''' </summary>
                        ''' <remarks>
                        ''' <para>
                        ''' <b>Warning:</b> When using the string-based overload, you <b>must</b> specify the fully qualified attribute path,
                        ''' including any required constructor arguments, as it will be emitted verbatim in the generated code.
                        ''' The generator does not resolve or import namespaces for string-based attributes.
                        ''' </para>
                        ''' <example> Example usage (property, type-based):
                        ''' <code>
                        ''' &lt;ObservableProperty&gt;       
                        ''' &lt;AttachAttribute(GetType(JsonIgnoreAttribute))&gt;
                        ''' Private _myField As String
                        ''' </code>
                        ''' </example>
                        ''' <example> Example usage (property, string-based):
                        ''' <code>
                        ''' &lt;ObservableProperty&gt;
                        ''' &lt;AttachAttribute("System.Text.Json.Serialization.JsonPropertyName(""MyFieldName"")")&gt;
                        ''' Private _myField As String
                        ''' </code>
                        ''' </example>
                        ''' <example> Example usage (relaycommand):
                        ''' <code>
                        ''' &lt;RelayCommand&gt;       
                        ''' &lt;AttachAttribute(GetType(JsonIgnoreAttribute))&gt;
                        ''' Private Sub MyCommand()
                        '''     ' Command logic here
                        ''' End Sub
                        ''' </code>
                        ''' </example>
                        ''' <para>
                        ''' For more information, see:
                        ''' <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty#adding-custom-attributes"> Microsoft documentation</see>
                        ''' </para>
                        ''' </remarks>
                        <AttributeUsage(AttributeTargets.Field Or AttributeTargets.Method, AllowMultiple:=True, Inherited:=False)>
                        Friend NotInheritable Class AttachAttributeAttribute : Inherits Attribute

                            Public ReadOnly TargetAttributes As Type()
                            Public ReadOnly TargetAttributeStrings As String()
                            ''' <summary>
                            ''' Initializes a new instance of the <see cref="AttachAttributeAttribute"/> class.
                            ''' </summary>
                            ''' <param name="attributeStrings">The fully qualified attribute names to attach to the generated property, which can include parameters.</param>
                            Public Sub New(ParamArray attributeStrings As String())
                                TargetAttributeStrings = attributeStrings
                            End Sub

                            ''' <summary>
                            ''' Initializes a new instance of the <see cref="AttachAttributeAttribute"/> class.
                            ''' </summary>
                            ''' <param name="attributes">The attribute types to attach to the generated property.</param>
                            Public Sub New(ParamArray attributes As Type())
                                TargetAttributes = attributes
                            End Sub

                        End Class

                        """;
        ctx.AddSource("AttachAttribute_Attribute.g.vb", sourceCode);
    }


}