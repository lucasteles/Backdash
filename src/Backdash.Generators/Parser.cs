using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Backdash.Generators;

static class Parser
{
    const string BackdashSerializerAttribute = "Backdash.Serialization.StateSerializer";

    public static bool IsTypeTargetForGeneration(SyntaxNode node)
        => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } t
           && t.Modifiers.Any(SyntaxKind.PartialKeyword)
           &&
           (node.IsKind(SyntaxKind.StructDeclaration) ||
            node.IsKind(SyntaxKind.ClassDeclaration) ||
            node.IsKind(SyntaxKind.RecordDeclaration) ||
            node.IsKind(SyntaxKind.RecordStructDeclaration));


    public static TypeDeclarationSyntax? GetStructSemanticTargetForGeneration(
        GeneratorSyntaxContext context)
    {
        var structDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in structDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax)
                        .Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.StartsWith(BackdashSerializerAttribute))
                    return structDeclarationSyntax;
            }
        }

        return null;
    }

    public static BackdashContext? GetGenerationContext(
        SemanticModel model,
        TypeDeclarationSyntax? syntax,
        CancellationToken ct
    )
    {
        if (syntax is null)
            return null;

        ct.ThrowIfCancellationRequested();

        if (model.GetDeclaredSymbol(syntax, ct) is not { } nameSymbol)
            return null;

        return new(
            Name: nameSymbol.Name,
            NameSpace: GetNameSpace(syntax),
            Parent: GetParentClasses(syntax)
        );
    }

    static ParentClass? GetParentClasses(SyntaxNode structSymbol)
    {
        var parentIdClass = structSymbol.Parent as TypeDeclarationSyntax;
        ParentClass? parentClass = null;

        while (parentIdClass is not null && IsAllowedKind(parentIdClass.Kind()))
        {
            parentClass = new(
                Name: parentIdClass.Identifier.ToString() + parentIdClass.TypeParameterList,
                Parent: parentClass);

            parentIdClass = parentIdClass.Parent as TypeDeclarationSyntax;
        }

        return parentClass;

        static bool IsAllowedKind(SyntaxKind kind) => kind
            is SyntaxKind.ClassDeclaration
            or SyntaxKind.StructDeclaration
            or SyntaxKind.RecordStructDeclaration
            or SyntaxKind.RecordDeclaration;
    }

    static string GetNameSpace(SyntaxNode structSymbol)
    {
        var potentialNamespaceParent = structSymbol.Parent;
        while (potentialNamespaceParent is not (
               null
               or NamespaceDeclarationSyntax
               or FileScopedNamespaceDeclarationSyntax))
            potentialNamespaceParent = potentialNamespaceParent.Parent;

        if (potentialNamespaceParent is not BaseNamespaceDeclarationSyntax namespaceParent)
            return string.Empty;

        var nameSpace = namespaceParent.Name.ToString();
        while (true)
        {
            if (namespaceParent.Parent is not NamespaceDeclarationSyntax namespaceParentParent)
                break;

            namespaceParent = namespaceParentParent;
            nameSpace = $"{namespaceParent.Name}.{nameSpace}";
        }

        return nameSpace;
    }
}
