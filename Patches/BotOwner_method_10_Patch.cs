using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DebugTools.Patches;

public sealed class BotOwner_method_10_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotOwner)
            .GetMethod(nameof(BotOwner.method_10));
    }

    [PatchPrefix]
    public static bool Prefix(BotOwner __instance)
    {
        if (!DT_Plugin.CatchBotErrors.Value)
        {
            return true;
        }

        try
        {
            __instance.VoxelesPersonalData.Activate(__instance.BotsGroup.BotGame.BotsController.CoversData);
            __instance.LookSensor.Activate();
            __instance.Settings.Activate();
            __instance.ExternalItemsController.Activate();
            __instance.ItemTaker.Activate();
            __instance.BewarePlantedMine.Activate();
            __instance.EnemyChooser.Activate();
            __instance.PlanDropItem.Activate();
            __instance.MinesData.Activate();
            __instance.ItemDropper.Activate();
            __instance.SuppressStationary.Activate();
            __instance.NavMeshCutterController.Activate();
            __instance.BotFollower.Activate();
            __instance.FriendlyTilt.Activate();
            __instance.RandomPlanItemDropper.Activate();
            __instance.Tactic.Activate();
            __instance.EnemiesController.Activate(__instance.BotsGroup.BotGame.BotsController.OnlineDependenceSettings.CanPersueAxeman);
            __instance.HearingSensor.Init();
            __instance.LeaveData.Activate(__instance.BotsGroup.BotZone.Modifier.LeaveDist);
            __instance.Receiver.Init();
            __instance.Mover.Activate();
            __instance.BotTalk.Activate();
            __instance.LoyaltyData.Activate();
            __instance.AssaultDangerArea.Activate();
            __instance.DangerArea.Activate();
            __instance.BotPersonalStats.Init(__instance, __instance.BotsGroup.BotZone.name);
            __instance.StandBy.InitPoints(__instance.BotsGroup.BotZone.Modifier.DistToActivate, __instance.BotsGroup.BotZone.Modifier.DistToSleep);
            __instance.AfterActivationSubscribe();
            __instance.FlashGrenade.Activate();
            __instance.PeaceHardAim.Activate();
            __instance.ShootData.Activate();
            __instance.PeaceLook.Activate();
            __instance.NearDoorData.Activate();
            __instance.AIData.Activate();
            __instance.UnityEditorRunChecker.Activate();
            __instance.NightVision.Activate();
            __instance.SearchData.Activate();
            __instance.Medecine.Activate();
            __instance.BotState = EBotState.Active;
            __instance.Memory.Activate();
            __instance.SuppressShoot.Activate();
            __instance.EatDrinkData.Activate();
            __instance.SecondWeaponData.Activate();
            __instance.BotLay.Activate();
            __instance.SuppressGrenade.Activate();
            __instance.method_11();
            __instance.Brain.Activate();
            __instance.PatrollingData.Activate();
            __instance.WeaponManager.Activate();
            __instance.BotFollower.TryFindBoss();

            Traverse.Create(__instance)
                .Field("_activateTime")
                .SetValue(Time.time);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[BotActivation Exception]: {ex}");
            __instance.BotState = EBotState.ActiveFail;
        }

        return false;
    }
}
