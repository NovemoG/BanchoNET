using System.Runtime.CompilerServices;

namespace BanchoNET.Tests;

internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SetIfMissing("DOMAIN", "test.local");
        SetIfMissing("COMMAND_PREFIX", "!");
        SetIfMissing("JWT_SECRET", "test-only-signing-key-at-least-32-chars");
    }

    private static void SetIfMissing(
        string name,
        string value
    ) {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            Environment.SetEnvironmentVariable(name, value);
    }
}