using System.Collections.Immutable;

namespace Reflexor;

public readonly record struct GenericType(
    string Name,
    ImmutableArray<string> Constraints);
