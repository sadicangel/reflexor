using System.Runtime.CompilerServices;

namespace Reflexor.Tests.Helpers;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DerivePathInfo((_, projectDirectory, type, method) => new PathInfo(
            directory: projectDirectory,
            typeName: type.Name,
            methodName: method.Name));
    }
}
