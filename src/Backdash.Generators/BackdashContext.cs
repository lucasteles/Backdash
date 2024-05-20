using Microsoft.CodeAnalysis;

namespace Backdash.Generators;

record BackdashContext(
    string Name,
    string NameSpace,
    string StateType,
    ParentClass? Parent,
    ClassMember[] Members);

record ParentClass(string Name, ParentClass? Parent);

record ClassMember(string Name, ITypeSymbol Type);
