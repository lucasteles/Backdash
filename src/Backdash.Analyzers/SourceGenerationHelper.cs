using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Backdash.Analyzers;

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
        const string tab1 = "    ";
        const string tab2 = $"{tab1}{tab1}";
        const string tab3 = $"{tab2}{tab1}";
        var arrays = 0;
        const string sizeProp = "Length";
        string paramModifier;

        foreach (var member in item.Members.OrderBy(x => x.Name, StringComparer.InvariantCulture))
        {
            paramModifier = member.IsProperty ? string.Empty : "in ";

            if (IsArrayLike(member.Type, out var itemType))
            {
                BuildArrayMember(itemType, member);
            }
            else
            {
                BuildValueMember(member);
            }
        }

        sb.Replace("[[WRITES]]", writes.ToString());
        sb.Replace("[[READS]]", reads.ToString());

        if (hasNamespace) sb.Append('}');

        return;

        void BuildValueMember(ClassMember member)
        {
            if (serializerMap.TryGetValue(member.Type.ToDisplayString(), out var memberSerializer))
            {
                var serializerName = $"{memberSerializer.NameSpace}.{memberSerializer.Name}.Shared";

                writes.AppendLine(
                    $"""
                     {tab2}{serializerName}.Serialize(in binaryWriter, in data.{member.Name});
                     """);

                reads.AppendLine(
                    $"""
                     {tab2}{serializerName}.Deserialize(in binaryReader, ref result.{member.Name});
                     """);
            }
            else if (member.Type.IsUnmanagedType)
            {
                writes.Append(tab2);
                writes.AppendLine(
                    member.Type is INamedTypeSymbol { EnumUnderlyingType: { } underTypeWrite }
                        ? $"binaryWriter.Write(({underTypeWrite.Name})data.{member.Name});"
                        : $"binaryWriter.Write({paramModifier}data.{member.Name});"
                );

                reads.Append(tab2);
                reads.AppendLine(
                    member.Type is INamedTypeSymbol { EnumUnderlyingType: { } underTypeRead }
                        ? $"result.{member.Name} = ({member.Type.Name})binaryReader.Read{underTypeRead.Name}();"
                        : $"result.{member.Name} = binaryReader.Read{member.Type.Name}();"
                );
            }
        }

        void BuildArrayMember(ITypeSymbol itemType, ClassMember member)
        {
            if (IsTypeArrayCopiable(itemType))
            {
                writes.Append(tab2);
                writes.AppendLine($"binaryWriter.Write(data.{member.Name});");

                reads.Append(tab2);
                reads.AppendLine($"binaryReader.Read{itemType.Name}(result.{member.Name});");
            }
            else
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
                         {tab3}{serializerName}.Serialize(in binaryWriter, in data.{member.Name}[i]);
                         """);

                    reads.AppendLine(
                        $"""
                         {tab3}{serializerName}.Deserialize(in binaryReader, ref result.{member.Name}[i]);
                         """);
                }
                else if (itemType.IsUnmanagedType)
                {
                    writes.Append(tab3);
                    writes.AppendLine(
                        itemType is INamedTypeSymbol { EnumUnderlyingType: { } underTypeWrite }
                            ? $"binaryWriter.Write(({underTypeWrite.Name})data.{member.Name}[i]);"
                            : $"binaryWriter.Write({paramModifier}data.{member.Name}[i]);"
                    );

                    reads.Append(tab3);
                    reads.AppendLine(
                        itemType is INamedTypeSymbol { EnumUnderlyingType: { } underTypeRead }
                            ? $"result.{member.Name}[i] = ({itemType.Name})binaryReader.Read{underTypeRead.Name}();"
                            : $"result.{member.Name}[i] = binaryReader.Read{itemType.Name}();"
                    );
                }

                writes.Append(tab2);
                writes.AppendLine("}");
                reads.Append(tab2);
                reads.AppendLine("}");
            }
        }
    }

    static bool IsArrayLike(ITypeSymbol memberType, out ITypeSymbol elementType)
    {
        elementType = memberType;
        if (memberType is not IArrayTypeSymbol { ElementType: { } itemType }) return false;
        elementType = itemType;
        return true;
    }

    internal static string CreateSourceName(string nameSpace, ParentClass? parent, string name)
    {
        var sb = new StringBuilder(nameSpace).Append('.');
        while (parent is not null)
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

    static bool IsTypeArrayCopiable(ITypeSymbol type)
    {
        Debug.Assert(type != null);

        if (!type.IsUnmanagedType || type is INamedTypeSymbol { EnumUnderlyingType: not null })
            return false;

        return type.SpecialType switch
        {
            SpecialType.System_SByte or SpecialType.System_Byte
                or SpecialType.System_Char or SpecialType.System_Boolean
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64 => true,
            _ => false,
        };
    }
}
