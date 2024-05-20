using System.Text;
using Microsoft.CodeAnalysis;

namespace Backdash.Generators;

static class SourceGenerationHelper
{
    public static void CreateSerializer(BackdashContext item, StringBuilder sb)
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
        foreach (var member in item.Members)
        {
            if (member.Type.TypeKind is TypeKind.Array)
                continue;

            writes.Append(tab);
            writes.AppendLine($"binaryWriter.Write(in data.{member.Name});");

            reads.Append(tab);


            if (member.Type.IsUnmanagedType)
            {
                reads.AppendLine($"result.{member.Name} = binaryReader.Read{member.Type.Name}();");
            }
            else
            {
                reads.AppendLine($"binaryReader.Read{member.Type.Name}(ref result.{member.Name});");
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
