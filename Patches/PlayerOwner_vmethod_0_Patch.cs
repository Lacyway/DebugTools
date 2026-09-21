using System.Reflection;
using Comfort.Common;
using DebugTools.FreeCamera;
using EFT;
using SPT.Reflection.Patching;

namespace DebugTools.Patches;

internal sealed class PlayerOwner_vmethod_0_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerOwner)
            .GetMethod(nameof(PlayerOwner.vmethod_0));
    }

    [PatchPrefix]
    public static void Prefix()
    {
        if (Singleton<GameWorld>.Instance is not HideoutGameWorld)
        {
            Singleton<GameWorld>.Instance.gameObject.AddComponent<FreeCameraController>();
        }
    }
}
