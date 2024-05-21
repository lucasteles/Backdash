using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Backdash.Analysers;

static class SourceGenerationHelper
{
    public static void CreateSerializer(
        BackdashContext item,
        ImmutableDictionary<string, BackdashContext> serializerMap,
        StringBuilder sb
    )
    {
        var hasNamespace = !string.IsNullOrEmpty(item.NameSpace);
        if (hasNamespace)
            sb
                .Append("namespace ")
                .Append(item.NameSpace)
                .AppendLine(@"
{");

        sb.Append(Templates.BaseSerializer);
        sb.Replace("[[NAME]]", item.Name);

        var type = item.StateType;
        sb.Replace("[[TYPE]]", type);

        StringBuilder writes = new();
        StringBuilder reads = new();
        const string tab = "\t\t";
        var arrays = 0;

        foreach (var member in item.Members)
        {
            var paramModifier = member.IsProperty ? string.Empty : "in ";
            const string sizeProp = "Length";

            if (IsArrayLike(member.Type, out var itemType))
            {
                var arrayIndex = ++arrays;
                var path = $"data.{member.Name}";

                writes.AppendLine(
                    $$"""
                              binaryWriter.Write({{path}}.{{sizeProp}});
                              for(var i = 0; i < {{path}}.{{sizeProp}}; i++)
                              {
                      """);

                reads.AppendLine(
                    $$"""
                              var size{{arrayIndex}} = binaryReader.ReadInt32();
                              for(var i = 0; i < size{{arrayIndex}}; i++)
                              {
                      """);

                if (serializerMap.TryGetValue(itemType.ToDisplayString(), out var memberSerializer))
                {
                    var serializerName = $"{memberSerializer.NameSpace}.{memberSerializer.Name}.Shared";

                    writes.AppendLine(
                        $"""
                         {tab}var byteCount{arrayIndex} = {serializerName}.Serialize(in data.{member.Name}[i], binaryWriter.CurrentBuffer);
                         {tab}binaryWriter.Advance(byteCount{arrayIndex});
                         """);

                    reads.AppendLine(
                        $"""
                         {tab}var byteCount{arrayIndex} = {serializerName}.Deserialize(binaryReader.CurrentBuffer, ref result.{member.Name}[i]);
                         {tab}binaryReader.Advance(byteCount{arrayIndex});
                         """);
                }
                else if (itemType.IsUnmanagedType)
                {
                    writes.Append(tab);
                    writes.AppendLine($"binaryWriter.Write({paramModifier}data.{member.Name}[i]);");

                    reads.Append(tab);
                    reads.AppendLine($"result.{member.Name}[i] = binaryReader.Read{itemType.Name}();");
                }

                writes.Append(tab);
                writes.AppendLine("}");
                reads.Append(tab);
                reads.AppendLine("}");
            }
            else
            {
                if (serializerMap.TryGetValue(member.Type.ToDisplayString(), out var memberSerializer))
                {
                    var serializerName = $"{memberSerializer.NameSpace}.{memberSerializer.Name}.Shared";

                    writes.AppendLine(
                        $"""
                         {tab}var byteCount = {serializerName}.Serialize(in data.{member.Name}, binaryWriter.CurrentBuffer);
                         {tab}binaryWriter.Advance(byteCount);
                         """);

                    reads.AppendLine(
                        $"""
                         {tab}var byteCount = {serializerName}.Deserialize(binaryReader.CurrentBuffer, ref result.{member.Name});
                         {tab}binaryReader.Advance(byteCount);
                         """);
                }
                else if (member.Type.IsUnmanagedType)
                {
                    writes.Append(tab);
                    writes.AppendLine($"binaryWriter.Write({paramModifier}data.{member.Name});");

                    reads.Append(tab);
                    reads.AppendLine($"result.{member.Name} = binaryReader.Read{member.Type.Name}();");
                }
            }
        }

        sb.Replace("[[WRITES]]", writes.ToString());
        sb.Replace("[[READS]]", reads.ToString());

        if (hasNamespace) sb.Append('}');
    }

    static bool IsArrayLike(ITypeSymbol memberType, out ITypeSymbol elementType)
    {
        elementType = memberType;

        if (memberType is IArrayTypeSymbol { ElementType: { } itemType })
        {
            elementType = itemType;
            return true;
        }

        if (memberType is INamedTypeSymbol { TypeArguments.Length: 1 } named &&
            named.ToDisplayString().StartsWith("Backdash.Data.Array"))
        {
            elementType = named.TypeArguments.First();
            return true;
        }

        return false;
    }

    internal static string CreateSourceName(string nameSpace, ParentClass? parent, string name)
    {
        var sb = new StringBuilder(nameSpace).Append('.');
        while (parent != null)
        {
            var s = parent.Name
                .Replace(" ", "")
                .Replace(",", "")
                .Replace("<", "__")
                .Replace(">", "");
            sb.Append(s).Append('.');
            parent = parent.Parent;
        }

        return sb.Append(name).Append(".g.cs").ToString();
    }
}
