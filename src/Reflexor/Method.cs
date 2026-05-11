using System.Collections.Immutable;

namespace Reflexor;

public readonly record struct Method(
    string Name,
    string ReturnType,
    bool IsStatic,
    bool IsOverride,
    bool IsReadOnly,
    bool IsUnsafe,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly,
    ImmutableArray<GenericType> GenericTypes,
    ImmutableArray<Parameter> Parameters);
