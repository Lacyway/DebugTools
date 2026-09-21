using System.Reflection;
using EFT;
using JsonType;
using SPT.Reflection.Patching;

namespace DebugTools.Patches;

internal sealed class ClientBackendSession_SetBotSettings_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientBackendSession)
            .GetMethod(nameof(ClientBackendSession.SetBotSettings));
    }

    [PatchPrefix]
    public static void Prefix(GlobalConfigurationResponse backEndSettings)
    {
        backEndSettings.Config.TimeBeforeDeployLocal = 3;
    }
}
