using System.Diagnostics;

namespace MassSensorWithSignal;

internal static class MassSensorWithSignalLog
{
    private const string Prefix = "[MassSensorWithSignal] ";

    /// <summary>Verbose trace output. Compiled out of Release builds.</summary>
    [Conditional("DEBUG")]
    internal static void Trace(string message)
    {
        UnityEngine.Debug.LogWarning(Prefix + message);
    }

    internal static void Warn(string message)
    {
        UnityEngine.Debug.LogWarning(Prefix + message);
    }

    internal static void Error(string message)
    {
        UnityEngine.Debug.LogError(Prefix + message);
    }
}
