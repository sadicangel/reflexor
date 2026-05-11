using System.Collections.Immutable;

namespace Reflexor;

public readonly record struct Proxy(
    string Name,
    string? Namespace,
    string TargetType,
    string DisplayTargetType,
    bool IsStatic,
    bool IsRefLike,
    ImmutableArray<GenericType> GenericTypes,
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods);
