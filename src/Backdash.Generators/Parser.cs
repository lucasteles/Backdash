using System.Collections.Generic;
using System.Linq;
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


    public static (TypeDeclarationSyntax, ITypeSymbol)? GetStructSemanticTargetForGeneration(
        GeneratorSyntaxContext context)
    {
        var declarationSyntax = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in declarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax)
                        .Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (!fullName.StartsWith(BackdashSerializerAttribute)
                    || !attributeContainingTypeSymbol.IsGenericType
                    || attributeContainingTypeSymbol.TypeArguments.FirstOrDefault() is not { } typeArg
                   )
                    continue;

                return (declarationSyntax, typeArg);
            }
        }

        return null;
    }

    public static BackdashContext? GetGenerationContext(SemanticModel model,
        TypeDeclarationSyntax? syntax,
        ITypeSymbol? typeParam,
        CancellationToken ct)
    {
        if (syntax is null || typeParam is null)
            return null;

        ct.ThrowIfCancellationRequested();

        if (model.GetDeclaredSymbol(syntax, ct) is not { } nameSymbol)
            return null;

        List<ClassMember> members = [];

        foreach (var member in typeParam.GetMembers())
        {
            if (member.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
                continue;

            if (member.Kind is not (SymbolKind.Property or SymbolKind.Field))
                continue;

            switch (member)
            {
                case IPropertySymbol property:
                    members.Add(new(property.Name, property.Type));
                    break;

                case IFieldSymbol field:
                    members.Add(new(field.Name, field.Type));
                    break;
            }
        }

        return new(
            Name: nameSymbol.Name,
            NameSpace: GetNameSpace(syntax),
            StateType: typeParam.ToDisplayString(),
            Parent: GetParentClasses(syntax),
            Members: [.. members]
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
