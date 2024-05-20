using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Backdash.Generators;

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
            if (member.Type is IArrayTypeSymbol { ElementType: { } itemType })
            {
                var arrayIndex = ++arrays;
                var path = $"data.{member.Name}";

                writes.AppendLine(
                    $$"""
                              binaryWriter.Write({{path}}.Length);
                              for(var i = 0; i < {{path}}.Length; i++)
                              {
                      """);

                reads.AppendLine(
                    $$"""
                              var size{{arrayIndex}} = binaryReader.ReadInt32();
                              for(var i = 0; i < size{{arrayIndex}}; i++)
                              {
                      """);

                if (itemType.IsUnmanagedType)
                {
                    writes.Append(tab);
                    writes.AppendLine($"binaryWriter.Write(in data.{member.Name}[i]);");

                    reads.Append(tab);
                    reads.AppendLine($"result.{member.Name}[i] = binaryReader.Read{itemType.Name}();");
                }
                else if (serializerMap.TryGetValue(itemType.ToDisplayString(), out var memberSerializer))
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


                writes.Append(tab);
                writes.AppendLine("}");
                reads.Append(tab);
                reads.AppendLine("}");
            }
            else
            {
                if (member.Type.IsUnmanagedType)
                {
                    writes.Append(tab);
                    writes.AppendLine($"binaryWriter.Write(in data.{member.Name});");

                    reads.Append(tab);
                    reads.AppendLine($"result.{member.Name} = binaryReader.Read{member.Type.Name}();");
                }
                else if (serializerMap.TryGetValue(member.Type.ToDisplayString(), out var memberSerializer))
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
            }
        }

        sb.Replace("[[WRITES]]", writes.ToString());
        sb.Replace("[[READS]]", reads.ToString());

        if (hasNamespace) sb.Append('}');
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
