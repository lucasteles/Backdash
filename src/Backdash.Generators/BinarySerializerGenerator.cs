using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Backdash.Generators;

/// <inheritdoc />
[Generator]
public class BinarySerializerGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var serializerDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => Parser.IsTypeTargetForGeneration(s),
                static (ct, _) => (
                    Target: Parser.GetStructSemanticTargetForGeneration(ct),
                    ct.SemanticModel
                )
            )
            .Where(static m => m.Target is not null)
            .Select(static (arg, ct) =>
            {
                if (arg.Target is null) return null;
                var (target, typeParam) = arg.Target.Value;

                return Parser.GetGenerationContext(arg.SemanticModel, target, typeParam, ct);
            })
            .Where(static m => m is not null)
            .Select(static (arg, _) => arg!)
            .Collect();

        context.RegisterSourceOutput(serializerDeclarations, static (spc, source) =>
            Execute(source, spc));
    }

    static void Execute(
        ImmutableArray<BackdashContext> valuesToGenerate,
        SourceProductionContext context
    )
    {
        if (valuesToGenerate.IsDefaultOrEmpty) return;
        StringBuilder sb = new();

        var serializerMap = valuesToGenerate
            .ToImmutableDictionary(x => x.StateType, x => x);

        foreach (var item in valuesToGenerate)
        {
            sb.Clear();
            SourceGenerationHelper.CreateSerializer(item, serializerMap, sb);
            var fileName = SourceGenerationHelper.CreateSourceName(
                item.NameSpace,
                item.Parent,
                item.Name);

            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
