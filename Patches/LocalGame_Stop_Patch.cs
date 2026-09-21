using System.Reflection;
using Comfort.Common;
using DebugTools.FreeCamera;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DebugTools.Patches;

internal sealed class LocalGame_Stop_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalGame)
            .GetMethod(nameof(LocalGame.Stop));
    }

    [PatchPrefix]
    public static void Prefix()
    {
        if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent<FreeCameraController>(out var controller))
        {
            Object.Destroy(controller);
        }
    }
}
