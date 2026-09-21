using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using Diz.Jobs;
using Diz.Utils;
using EFT;
using EFT.Console.Core;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace DebugTools.Utils;

public sealed class DebugCommands
{
    [ConsoleCommand("RecreateBackend", "", "", "Recreates the backend")]
    public sealed class RecreateBackend : AsyncCommand
    {
        public override object[] ArgumentsValue => [];

        public override async Task Execute()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                LogError("Cannot recreate backend in raid.");
                return;
            }

            if (!TarkovApplication.Exist(out var tarkovApp))
            {
                LogError("TarkovApp missing");
                return;
            }

            await tarkovApp.RecreateCurrentBackend();
        }
    }

    [ConsoleCommand("bring", description: "Teleports all AI to yourself as the host")]
    public static void Bring()
    {
        if (!CheckForGame())
        {
            return;
        }

        var gameWorld = Utilities.GameWorld;
        var count = 0;
        var targetPosition = gameWorld.MainPlayer.Transform;
        foreach (var player in gameWorld.AllAlivePlayersList)
        {
            if (player.IsAI && player.HealthController.IsAlive)
            {
                count++;
                player.Teleport(targetPosition.Original.position + (targetPosition.Original.forward * 2));
            }
        }

        LogInfo($"Teleported {count} AI to requester.");
    }

    [ConsoleCommand("heal", description: "Heals local player fully")]
    public static void Heal()
    {
        if (!CheckForGame())
        {
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        localPlayer.ActiveHealthController.RestoreFullHealth();
    }

    [ConsoleCommand("killMe", description: "Kills the local player")]
    public static void KillMe()
    {
        if (!CheckForGame())
        {
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        localPlayer.KillMe(EBodyPartColliderType.HeadCommon, 10_000f);
    }

    [ConsoleCommand("destroyLimb", description: "Destroys a limb")]
    public static void DestroyLimb([ConsoleArgument("Stomach", "the limb to destroy")] string bodyPart)
    {
        if (!CheckForGame())
        {
            return;
        }

        if (!Enum.TryParse(bodyPart, true, out EBodyPart selectedBodypart))
        {
            LogInfo($"Invalid BodyPartType: {bodyPart}");
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        if (localPlayer.ActiveHealthController.BodyState.TryGetValue(selectedBodypart, out var partState))
        {
            localPlayer.ActiveHealthController.ChangeHealth(selectedBodypart, -partState.Health.Current, DamageHelper.UndefinedDamage);
            localPlayer.ActiveHealthController.DestroyBodyPart(selectedBodypart, EDamageType.Undefined);
        }
    }

    [ConsoleCommand("god", description: "Set god mode on/off")]
    public static void God([ConsoleArgument(false, "true or false to toggle god mode")] bool state)
    {
        if (!CheckForGame())
        {
            return;
        }

        var value = state ? 0 : 1;
        Utilities.GameWorld.MainPlayer.ActiveHealthController.SetDamageCoeff(value);
        if (value == 0)
        {
            LogInfo("God mode on");
        }
        else
        {
            LogInfo("God mode off");
        }
    }

    [ConsoleCommand("despawnAllAi", description: "Despawns all AI bots")]
    public static void DespawnAllAI()
    {
        if (!CheckForGame())
        {
            return;
        }

        if (!Singleton<AbstractGame>.Instantiated)
        {
            return;
        }

        if (Singleton<AbstractGame>.Instance is LocalGame localGame)
        {
            var gameWorld = Utilities.GameWorld;
            var botsController = (BotsController)Utilities.BotsControllerField.GetValue(localGame);
            if (botsController != null)
            {
                foreach (var bot in botsController.Bots.BotOwners.ToArray())
                {
                    LogInfo($"Despawning: {bot.Profile.Nickname}");
                    localGame.BotDespawn(bot);
                }
            }

        }
    }

    [ConsoleCommand("stoptimer", description: "Stops the game timer")]
    public static void StopTimer()
    {
        var game = Singleton<AbstractGame>.Instance;
        if (game == null || game is not LocalGame localGame)
        {
            LogError("Game was null or not a LocalGame");
            return;
        }

        if (localGame != null)
        {
            if (localGame.GameTimer.Status == GameTimer.EGameTimerStatus.Stopped)
            {
                LogError("GameTimer is already stopped at: " + localGame.GameTimer.PastTime.ToString());
                return;
            }
            localGame.GameTimer.TryStop();
            if (localGame.GameTimer.Status == GameTimer.EGameTimerStatus.Stopped)
            {
                LogInfo("GameTimer stopped at: " + localGame.GameTimer.PastTime.ToString());
            }
        }
    }

    [ConsoleCommand("goToBTR", description: "Teleports you to the BTR if active")]
    public static void GoToBTR()
    {
        var game = Singleton<AbstractGame>.Instance;
        if (game == null || game is not LocalGame localGame)
        {
            LogError("Game was null or not a LocalGame");
            return;
        }

        if (localGame != null)
        {
            var gameWorld = localGame.GameWorld;
            if (gameWorld != null)
            {
                if (gameWorld.BtrController != null)
                {
                    var btrTransform = Traverse.Create(gameWorld.BtrController.BtrView)
                        .Field<Transform>("_cachedTransform").Value;
                    if (btrTransform != null)
                    {
                        var myPlayer = gameWorld.MainPlayer;
                        if (myPlayer != null)
                        {
                            myPlayer.Teleport(btrTransform.position + (Vector3.forward * 3));
                        }
                    }
                }
                else
                {
                    LogWarning("There is no BTRController active!");
                }
            }
        }
    }

    [ConsoleCommand("spawnItem", description: "Spawns an item from a templateId")]
    public static void SpawnItem([ConsoleArgument("", "The templateId to spawn an item from")] string templateId,
        [ConsoleArgument(1, "The amount to spawn if the item can stack")] int amount = 1)
    {
        if (!CheckForGame())
        {
            return;
        }

        /*if (!Singleton<GameWorld>.Instantiated)
        {
            SpawnItemInMainMenu(templateId, amount);
        }*/

        var gameWorld = Utilities.GameWorld;
        var player = gameWorld.MainPlayer;
        if (!player.HealthController.IsAlive)
        {
            LogError("You cannot spawn an item while dead!");
            return;
        }

        var itemFactory = Singleton<ItemFactory>.Instance;
        if (itemFactory == null)
        {
            LogError("ItemFactory was null!");
            return;
        }

        var item = itemFactory.GetPresetItem(templateId);
        if (amount > 1 && item.StackMaxSize > 1)
        {
            item.StackObjectsCount = Mathf.Clamp(amount, 1, item.StackMaxSize);
        }
        else
        {
            item.StackObjectsCount = 1;
        }
        _ = SpawnItemInWorld(item, player, gameWorld);
    }

    private static async Task SpawnItemInWorld(Item item, Player player, GameWorld gameWorld)
    {
        List<ResourceKey> collection = [];
        foreach (var subItem in item.GetAllItems())
        {
            collection.AddRange(subItem.Template.AllResources);
        }
        await Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Online,
            [.. collection], JobYieldPriority.Immediate, null, default);

        gameWorld.SetupItem(item, player,
            player.Transform.Original.position + player.Transform.Original.forward + (player.Transform.Original.up / 2), Quaternion.identity);

        LogInfo("Spawned item: " + item.ShortName.Localized());
    }

    [ConsoleCommand("spawnItemInInventory", description: "Spawns an item from a templateId in your inventory")]
    public static void SpawnItemInInventory([ConsoleArgument("", "The templateId to spawn an item from")] string templateId,
        [ConsoleArgument(1, "The amount to spawn if the item can stack")] int amount = 1)
    {
        if (!CheckForGame())
        {
            return;
        }

        /*if (!Singleton<GameWorld>.Instantiated)
        {
            SpawnItemInMainMenu(templateId, amount);
        }*/

        var gameWorld = Utilities.GameWorld;
        var player = gameWorld.MainPlayer;
        if (!player.HealthController.IsAlive)
        {
            LogError("You cannot spawn an item while dead!");
            return;
        }

        var itemFactory = Singleton<ItemFactory>.Instance;
        if (itemFactory == null)
        {
            LogError("ItemFactory was null!");
            return;
        }

        var item = itemFactory.GetPresetItem(templateId);
        LogInfo($"Created item {item.LocalizedName()}");
        if (amount > 1 && item.StackMaxSize > 1)
        {
            item.StackObjectsCount = Mathf.Clamp(amount, 1, item.StackMaxSize);
        }
        else
        {
            item.StackObjectsCount = 1;
        }

        var stash = Singleton<ItemFactory>.Instance.CreateFakeStash();
        var grid = stash.Grid;
        var tempMove = stash.Grid.AddItemWithoutRestrictions(item, grid.FindFreeSpace(item));
        if (tempMove.Failed)
        {
            LogError($"Failed to move item to temporary stash: {tempMove.Error}");
            return;
        }

        LogInfo($"Item is in {item.Parent.ContainerName}");

        new ItemController(stash, player.InventoryController.ID, "temp item stash", false, EOwnerType.Profile);

        try
        {
            var moveResult = ItemManipulator.QuickFindAppropriatePlace(item, player.InventoryController,
                (player.InventoryController.RootItem as CompoundItem).ToEnumerable(),
                ItemManipulator.EMoveItemOrder.IgnoreItemParent, true);

            if (moveResult.Failed)
            {
                LogError("Failed to spawn item in inventory");
                return;
            }

            LogInfo($"Moved to: {moveResult.Value.ResultItem.Parent.ContainerName}");

            player.InventoryController.ConvertOperationResultToOperation(moveResult.Value).Execute(result =>
            {
                if (result.Failed)
                {
                    LogError($"Operation failed: {result.Error}");
                }
                else
                {
                    LogInfo($"Moved item to: {item.Parent.ContainerName}");
                }
            });

            Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Online,
                item.Template.AllResources.ToHashSet(), JobYieldPriority.Immediate)
                .HandleExceptions();
        }
        catch (Exception ex)
        {
            LogError(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Unused due to de-sync from client to server
    /// </summary>
    /// <param name="templateId"></param>
    /// <param name="amount"></param>
    private static void SpawnItemInMainMenu(string templateId, int amount)
    {
        if (!Singleton<CommonUI>.Instantiated)
        {
            return;
        }

        var itemFactory = Singleton<ItemFactory>.Instance;
        if (itemFactory == null)
        {
            LogError("ItemFactory was null!");
            return;
        }

        var item = itemFactory.GetPresetItem(templateId);
        LogInfo($"Created item {item.LocalizedName()}");
        if (amount > 1 && item.StackMaxSize > 1)
        {
            item.StackObjectsCount = Mathf.Clamp(amount, 1, item.StackMaxSize);
        }
        else
        {
            item.StackObjectsCount = 1;
        }

        var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
        var controller = (InventoryController)Utilities.MainMenuInventoryControllerField.GetValue(inventoryScreen);

        var stash = Singleton<ItemFactory>.Instance.CreateFakeStash();
        var grid = stash.Grid;
        var tempMove = stash.Grid.AddItemWithoutRestrictions(item, grid.FindFreeSpace(item));
        if (tempMove.Failed)
        {
            LogError($"Failed to move item to temporary stash: {tempMove.Error}");
            return;
        }

        LogInfo($"Item is in {item.Parent.ContainerName}");

        new ItemController(stash, controller.ID, "temp item stash", false, EOwnerType.Profile);

        try
        {
            var moveResult = ItemManipulator.QuickFindAppropriatePlace(item, controller,
                (controller.RootItem as CompoundItem).ToEnumerable(),
                ItemManipulator.EMoveItemOrder.IgnoreItemParent, true);

            if (moveResult.Failed)
            {
                LogError("Failed to spawn item in inventory");
                return;
            }

            LogInfo($"Moved to: {moveResult.Value.ResultItem.Parent.ContainerName}");

            controller.ConvertOperationResultToOperation(moveResult.Value).Execute(result =>
            {
                if (result.Failed)
                {
                    LogError($"Operation failed: {result.Error}");
                }
                else
                {
                    LogInfo($"Moved item to: {item.Parent.ContainerName}");
                }
            });

            Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Online,
                item.Template.AllResources.ToHashSet(), JobYieldPriority.Immediate)
                .HandleExceptions();
        }
        catch (Exception ex)
        {
            LogError(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Based on SSH's TarkyMenu command
    /// </summary>
    /// <param name="wildSpawnType"></param>
    /// <param name="amount"></param>
    [ConsoleCommand("spawnNPC", description: "Spawn NPC with specified WildSpawnType")]
    public static void SpawnNPC([ConsoleArgument("assault", "The WildSpawnType to spawn (use help for a list)")] WildSpawnType wildSpawnType,
        [ConsoleArgument(1, "The amount of AI to spawn")] int amount)
    {
        if (amount <= 0)
        {
            LogInfo($"Invalid number: {amount}. Please enter a valid, positive integer.");
            return;
        }

        if (!CheckForGame())
        {
            return;
        }

        SpawnWave newBotData = new()
        {
            BotsCount = amount,
            Side = EPlayerSide.Savage,
            SpawnAreaName = "",
            Time = 0f,
            WildSpawnType = wildSpawnType,
            IsPlayers = false,
            Difficulty = BotDifficulty.easy,
            ChanceGroup = 100f,
            WithCheckMinMax = false
        };

        if (Singleton<AbstractGame>.Instance == null || Singleton<AbstractGame>.Instance is not LocalGame localGame)
        {
            return;
        }

        var botController = (BotsController)Utilities.BotsControllerField.GetValue(localGame);
        if (botController == null)
        {
            LogError("BotsController was null!");
            return;
        }

        botController.BotSpawner.ActivateBotsByWave(newBotData);
        LogInfo($"SpawnNPC completed, requested {amount} of {wildSpawnType}");
    }

    [ConsoleCommand("spawnAirdrop", description: "Spawns an airdrop")]
    public static void SpawnAirdrop()
    {
        if (!CheckForGame())
        {
            return;
        }

        if (Utilities.GameWorld is not ClientGameWorld gameWorld)
        {
            return;
        }

        var serverAirdropManager = gameWorld.ClientSynchronizableObjectLogicProcessor.ServerAirdropManager;
        if (serverAirdropManager == null)
        {
            LogError("ServerAirdropManager was null!");
            return;
        }

        if (!serverAirdropManager.CanSummonAirdrop)
        {
            LogError("Airdrops are disabled!");
            return;
        }

        var dropPoints = serverAirdropManager._projectileSuccessPositions;
        if (dropPoints != null && dropPoints.Count > 0)
        {
            var templateId = serverAirdropManager._overrideLootTemplateId;
            serverAirdropManager.UpdateAirdropTimers(serverAirdropManager.PlaneAirdropCooldown);
            gameWorld.InitAirdrop(templateId, true, serverAirdropManager.GetEquidistantPoint());
            serverAirdropManager._overrideLootTemplateId = null;
            dropPoints.Clear();
            LogInfo("Started airdrop");
            return;
        }

        serverAirdropManager.UpdateAirdropTimers(serverAirdropManager.PlaneAirdropCooldown);
        gameWorld.InitAirdrop();
        LogInfo("Started airdrop");
    }

    [ConsoleCommand("clear", description: "Clears the console output")]
    public static void Clear()
    {
        Singleton<PreloaderUI>.Instance.Console.Clear();
    }

    public static bool CheckForGame(bool silent = false)
    {
        var game = Singleton<AbstractGame>.Instance;
        if (game == null)
        {
            if (!silent)
            {
                LogError("Game was null");
            }
            return false;
        }

        if (game.Status != GameStatus.Started)
        {
            if (!silent)
            {
                LogWarning("Game is not running.");
            }
            return false;
        }

        return true;
    }

    private static void LogInfo(string message)
    {
        ConsoleScreen.Log(message);
        DT_Plugin.DT_Logger.LogInfo(message);
    }

    private static void LogWarning(string message)
    {
        ConsoleScreen.LogWarning(message);
        DT_Plugin.DT_Logger.LogWarning(message);
    }

    private static void LogError(string message)
    {
        ConsoleScreen.LogError(message);
        DT_Plugin.DT_Logger.LogError(message);
    }
}
