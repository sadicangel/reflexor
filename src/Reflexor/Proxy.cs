using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Reflexor;

internal readonly record struct Proxy(
    string Name,
    string? Namespace,
    Accessibility Accessibility,
    string TargetType,
    string DisplayTargetType,
    bool IsStatic,
    bool IsRefLike,
    ImmutableArray<GenericType> GenericTypes,
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods);
