using Microsoft.Build.Locator;

namespace AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;

internal static class MsBuildRegistration
{
    private static bool _registered;
    private static readonly object Gate = new();

    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (_registered || MSBuildLocator.IsRegistered)
            {
                _registered = true;
                return;
            }

            MSBuildLocator.RegisterDefaults();
            _registered = true;
        }
    }
}

