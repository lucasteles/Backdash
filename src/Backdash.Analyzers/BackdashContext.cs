using Microsoft.CodeAnalysis;

namespace Backdash.Analyzers;

record BackdashContext(
    string Name,
    string NameSpace,
    string StateType,
    ParentClass? Parent,
    ClassMember[] Members
);

record ParentClass(string Name, ParentClass? Parent);

record struct ClassMember(string Name, bool IsProperty, ITypeSymbol Type);
