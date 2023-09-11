using System.Reflection;
using JetBrains.Annotations;

namespace Helpers;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new() { Value = nameof(Debug) };
    public static Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}

public record Sdk(string Version, string RollForward);

public record GlobalJson(Sdk Sdk);

[PublicAPI, UsedImplicitly(ImplicitUseKindFlags.Assign)]
public class GlobalJsonAttribute : ParameterAttribute
{
    readonly AbsolutePath filePath;

    public GlobalJsonAttribute()
    {
        filePath = NukeBuild.RootDirectory / "global.json";
        Assert.FileExists(filePath);
    }

    public override bool List { get; set; }

    public override object GetValue(MemberInfo member, object instance)
        => filePath.ReadJson<GlobalJson>();
}
