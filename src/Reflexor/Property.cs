namespace Reflexor;

public readonly record struct Property(
    string Name,
    string Type,
    bool IsStatic,
    bool IsReadOnly,
    bool IsUnsafe);
