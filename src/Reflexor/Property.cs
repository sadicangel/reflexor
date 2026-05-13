namespace Reflexor;

public readonly record struct Property(
    string Name,
    string MetadataName,
    string Type,
    string AccessorTargetType,
    string AccessorDisplayTargetType,
    bool IsStatic,
    bool IsReadOnly,
    bool IsUnsafe);
