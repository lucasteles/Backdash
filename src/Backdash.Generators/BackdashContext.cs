namespace Backdash.Generators;

record BackdashContext(
    string Name,
    string NameSpace,
    ParentClass? Parent
);

record ParentClass(string Name, ParentClass? Parent);
