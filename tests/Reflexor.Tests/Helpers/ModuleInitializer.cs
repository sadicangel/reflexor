using System.Runtime.CompilerServices;

namespace Reflexor.Tests.Helpers;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DerivePathInfo((sourceFile, projectDirectory, type, method) => new(
            directory: projectDirectory,
            typeName: type.Name,
            methodName: method.Name));
    }
}
