using System.Text;

namespace Backdash.Generators;

static class SourceGenerationHelper
{
    public static void CreateSerializer(BackdashContext item, StringBuilder sb)
    {
        sb.Append(Templates.BaseSerializer);
        var type = $"{item.NameSpace}.{item.Name}";
        sb.Replace("[[NAME]]", item.Name);
        sb.Replace("[[TYPE]]", type);
        sb.Replace("[[WRITES]]", string.Empty);
        sb.Replace("[[READS]]", string.Empty);
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
