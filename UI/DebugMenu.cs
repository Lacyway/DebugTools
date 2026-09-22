using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using DebugTools;
using DebugTools.Utils;
using EFT;
using EFT.Communications;
using EFT.HealthSystem;
using EFT.InputSystem;
using EFT.UI;
using EFT.Weather;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DebugMenu : InputNode
{
#pragma warning disable CS0649
    [SerializeField] Button CloseButton;

    [SerializeField] Button GodModeButton;
    [SerializeField] Button DespawnAIButton;
    [SerializeField] Button BringAIButton;
    [SerializeField] Button HealButton;
    [SerializeField] Button SuicideButton;
    [SerializeField] Button AirdropButton;
    [SerializeField] Button GoToBTRButton;

    [SerializeField] TMP_Dropdown SpawnAIDropdown;
    [SerializeField] TMP_InputField SpawnAIAmountField;
    [SerializeField] Button SpawnAIButton;

    [SerializeField] TMP_Dropdown DamageLimbDropdown;
    [SerializeField] Button DamageLimbButton;

    [SerializeField] TMP_InputField HourInputField;
    [SerializeField] TMP_InputField MinuteInputField;
    [SerializeField] TMP_Dropdown TimeFlowDropdown;
    [SerializeField] Button SetTimeButton;

    [SerializeField] TMP_Dropdown CloudinessDropdown;
    [SerializeField] TMP_Dropdown RainTypeDropdown;
    [SerializeField] TMP_Dropdown WindSpeedDropdown;
    [SerializeField] TMP_Dropdown FogTypeDropdown;
    [SerializeField] Button SetWeatherButton;
#pragma warning restore CS0649

    public void Awake()
    {
        CloseButton.onClick.AddListener(Toggle);

        GodModeButton.onClick.AddListener(ToggleGodMode);
        AddTooltip(GodModeButton.gameObject, "Toggle god mode on/off");
        DespawnAIButton.onClick.AddListener(DespawnAllAI);
        AddTooltip(DespawnAIButton.gameObject, "Despawns all active AI");
        BringAIButton.onClick.AddListener(Bring);
        AddTooltip(BringAIButton.gameObject, "Teleports all active AI to your position");
        HealButton.onClick.AddListener(Heal);
        AddTooltip(HealButton.gameObject, "Fully heals your character");
        SuicideButton.onClick.AddListener(KillMe);
        AddTooltip(SuicideButton.gameObject, "Kills your character");
        AirdropButton.onClick.AddListener(SpawnAirdrop);
        AddTooltip(AirdropButton.gameObject, "Spawns an airdrop");
        GoToBTRButton.onClick.AddListener(GoToBTR);
        AddTooltip(GoToBTRButton.gameObject, "Teleports to the BTR");

        SpawnAIDropdown.ClearOptions();
        List<string> options = [.. Enum.GetNames(typeof(WildSpawnType))];
        SpawnAIDropdown.AddOptions(options);
        SpawnAIButton.onClick.AddListener(SpawnAI);
        AddTooltip(SpawnAIButton.gameObject, "Spawns X amount of the selected AI\nHold SHIFT to quick spawn, bypassing spawn limits and spawning it in front of the main player");

        DamageLimbDropdown.ClearOptions();
        List<string> options2 = [.. Enum.GetNames(typeof(EBodyPart))];
        options2.RemoveAt(options2.Count - 1); // remove Common
        DamageLimbDropdown.AddOptions(options2);
        DamageLimbButton.onClick.AddListener(DamageLimb);
        AddTooltip(DamageLimbButton.gameObject, "Destroys the selected limb");

        HourInputField.onValidateInput = ValidateHourCharacter;
        HourInputField.onEndEdit.AddListener(FormatHourLeadingZero);
        MinuteInputField.onValidateInput = ValidateMinuteCharacter;
        MinuteInputField.onEndEdit.AddListener(FormatMinuteLeadingZero);
        TimeFlowDropdown.ClearOptions();
        List<string> options3 = [.. Enum.GetNames(typeof(ETimeFlowType))];
        TimeFlowDropdown.AddOptions(options3);
        TimeFlowDropdown.SetValueWithoutNotify(4);
        SetTimeButton.onClick.AddListener(SetTime);

        CloudinessDropdown.ClearOptions();
        List<string> options4 = [.. Enum.GetNames(typeof(ECloudinessType))];
        CloudinessDropdown.AddOptions(options4);
        RainTypeDropdown.ClearOptions();
        List<string> options5 = [.. Enum.GetNames(typeof(ERainType))];
        RainTypeDropdown.AddOptions(options5);
        WindSpeedDropdown.ClearOptions();
        List<string> options6 = [.. Enum.GetNames(typeof(EWindSpeed))];
        WindSpeedDropdown.AddOptions(options6);
        FogTypeDropdown.ClearOptions();
        List<string> options7 = [.. Enum.GetNames(typeof(EFogType))];
        FogTypeDropdown.AddOptions(options7);
        SetWeatherButton.onClick.AddListener(SetWeather);

        gameObject.SetActive(false);

        DT_Plugin.InputTree.Add(this);
        DT_Plugin.DT_Logger.LogInfo("DebugMenu ready");
    }

    public void OnEnable()
    {
        UIEventSystem.Instance.SetTemporaryStatus(true);
    }

    public void OnDisable()
    {
        UIEventSystem.Instance.SetTemporaryStatus(false);
    }

    private void AddTooltip(GameObject target, string text)
    {
        var tooltip = target.AddComponent<HoverTooltipArea>();
        tooltip.enabled = true;
        tooltip.SetMessageText(text);
    }

    private void DamageLimb()
    {
        var selectedIndex = DamageLimbDropdown.value;
        var selectedText = DamageLimbDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<EBodyPart>(selectedText, out var selectedBodyPart))
        {
            LogError("Invalid type");
            return;
        }
        DestroyLimb(selectedBodyPart);
    }

    private void DestroyLimb(EBodyPart bodyPart)
    {
        if (!CheckForGame())
        {
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        if (localPlayer.ActiveHealthController.BodyState.TryGetValue(bodyPart, out var partState))
        {
            localPlayer.ActiveHealthController.ChangeHealth(bodyPart, -partState.Health.Current, DamageHelper.UndefinedDamage);
            localPlayer.ActiveHealthController.DestroyBodyPart(bodyPart, EDamageType.Undefined);
        }
    }

    private void SpawnAI()
    {
        if (!int.TryParse(SpawnAIAmountField.text, out var amount))
        {
            return;
        }
        amount = Math.Clamp(amount, 1, 99);
        SpawnAIAmountField.SetTextWithoutNotify(amount.ToString());
        var selectedIndex = SpawnAIDropdown.value;
        var selectedText = SpawnAIDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<WildSpawnType>(selectedText, out var selectedWildSpawn))
        {
            LogError("Invalid type");
            return;
        }
        _ = SpawnNPC(selectedWildSpawn, amount, Input.GetKey(KeyCode.LeftShift));
    }

    private async Task SpawnNPC(WildSpawnType wildSpawnType, int amount, bool quick)
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

        if (quick)
        {
            var player = Utilities.MainPlayer;
            var pos = player.PlayerColliderPointOnCenterAxis(0f) + (player.Velocity * Time.deltaTime) + (player.Transform.forward * 1.5f);
            var botCreator = (BotCreatorClient)botController.BotSpawner._botCreator;
            var closestZone = botController.BotSpawner.GetClosestZone(pos, out var dist);
            var spawnPointMarker = closestZone.SpawnPointMarkers.RandomElement();
            IGetProfileData getProfileData = new GetProfileDataParams(newBotData.Side, newBotData.WildSpawnType,
                newBotData.Difficulty, newBotData.Time, null, newBotData.KeepZoneOnSpawn);
            var botCreationData = await BotCreationData.Create(getProfileData, botCreator,
                newBotData.BotsCount, botController.BotSpawner);
            botCreationData._positions.Add(new PositionNote(pos, spawnPointMarker.SpawnPoint.CorePointId, false));


            foreach (var profile in botCreationData.Profiles)
            {
                await botCreator.ActivateBot(botCreationData, closestZone, false,
                    botController.BotSpawner.GetGroupAndSetEnemies,
                    (botOwner) => botController.BotSpawner.ActivateBotCallback(botOwner, botCreationData, null, false, Stopwatch.StartNew()),
                    default);
            }
        }
        else
        {
            await botController.BotSpawner.ActivateBotsByWave(newBotData);
        }
        LogInfo($"SpawnNPC completed, requested {amount} of {wildSpawnType}");
    }

    private void ToggleGodMode()
    {
        if (!CheckForGame())
        {
            return;
        }

        var mainPlayer = Utilities.MainPlayer;
        var godOn = Mathf.Approximately(mainPlayer.HealthController.DamageCoeff, 0f);
        var value = godOn ? 1 : 0;
        mainPlayer.ActiveHealthController.SetDamageCoeff(value);
        if (value == 0)
        {
            LogInfo("God mode on");
        }
        else
        {
            LogInfo("God mode off");
        }
    }

    public void DespawnAllAI()
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

    public void Bring()
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

    private void SetWeather()
    {
        if (WeatherController.Instance == null)
        {
            LogError("There is no WeatherController");
            return;
        }

        var selectedIndex = CloudinessDropdown.value;
        var selectedText = CloudinessDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<ECloudinessType>(selectedText, out var cloudinessType))
        {
            LogError("Invalid type");
            return;
        }

        selectedIndex = RainTypeDropdown.value;
        selectedText = RainTypeDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<ERainType>(selectedText, out var rainType))
        {
            LogError("Invalid type");
            return;
        }

        selectedIndex = WindSpeedDropdown.value;
        selectedText = WindSpeedDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<EWindSpeed>(selectedText, out var windSpeed))
        {
            LogError("Invalid type");
            return;
        }

        selectedIndex = FogTypeDropdown.value;
        selectedText = FogTypeDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<EFogType>(selectedText, out var fogType))
        {
            LogError("Invalid type");
            return;
        }

        var dateTime = DateTimeExtensions.StartOfDay();
        var dateTime2 = dateTime.AddDays(1);

        var weather = WeatherNode.CreateDefault();
        var weather2 = WeatherNode.CreateDefault();
        weather.Cloudness = weather2.Cloudness = cloudinessType.ToValue();
        weather.Rain = weather2.Rain = rainType.ToValue();
        weather.Wind = weather2.Wind = windSpeed.ToValue();
        weather.ScaterringFogDensity = weather2.ScaterringFogDensity = fogType.ToValue();
        weather.Time = dateTime.Ticks;
        weather2.Time = dateTime2.Ticks;
        WeatherNode[] weatherClasses = [weather, weather2];
        WeatherController.Instance.SetWeatherNodes(weatherClasses);
    }

    private void SetTime()
    {
        if (Singleton<AbstractGame>.Instance is not LocalGame localGame)
        {
            LogError("AbstractGame was not LocalGame");
            return;
        }

        var gameTime = localGame.GameDateTime;
        if (gameTime == null)
        {
            LogError("There is no GameDateTime to change");
            return;
        }
        var backendTime = Traverse.Create(localGame)
            .Field<GameDateTime>("_backendDateTime").Value;

        if (!int.TryParse(HourInputField.text, out var hour) || !int.TryParse(MinuteInputField.text, out var minute))
        {
            LogError("Invalid hour or minute value");
            return;
        }

        var selectedIndex = TimeFlowDropdown.value;
        var selectedText = TimeFlowDropdown.options[selectedIndex].text;
        if (!Enum.TryParse<ETimeFlowType>(selectedText, out var timeFlow))
        {
            LogError("Invalid type");
            return;
        }

        var newTimeFlow = timeFlow.ToTimeFlow();
        var currentTime = backendTime.StatedGameDateTime;
        DateTime newTime = new(currentTime.Year, currentTime.Month, currentTime.Day, hour,
            minute, currentTime.Second, currentTime.Millisecond);
        gameTime.TimeFactor = newTimeFlow;
        gameTime.Reset(newTime);

        LogInfo($"Set time of day to: {hour}:{minute} with a timeflow of {selectedText}");
    }

    private char ValidateHourCharacter(string text, int charIndex, char addedChar)
    {
        if (!char.IsDigit(addedChar))
        {
            return '\0';
        }

        var prospectiveText = text.Insert(charIndex, addedChar.ToString());

        if (int.TryParse(prospectiveText, out var parsedValue))
        {
            if (parsedValue > 24)
            {
                return '\0';
            }

            if (parsedValue == 24)
            {
                MinuteInputField.SetTextWithoutNotify("00");
            }
        }

        return addedChar;
    }

    private char ValidateMinuteCharacter(string text, int charIndex, char addedChar)
    {
        if (!char.IsDigit(addedChar))
        {
            return '\0';
        }

        if (int.TryParse(HourInputField.text, out var hourValue) && hourValue == 24)
        {
            if (addedChar != '0')
            {
                return '\0';
            }
        }

        var prospectiveText = text.Insert(charIndex, addedChar.ToString());

        if (int.TryParse(prospectiveText, out var parsedValue))
        {
            if (parsedValue > 59)
            {
                return '\0';
            }
        }

        return addedChar;
    }

    private void FormatHourLeadingZero(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        if (int.TryParse(input, out var parsedValue))
        {
            HourInputField.SetTextWithoutNotify(parsedValue.ToString("D2"));
        }
    }

    private void FormatMinuteLeadingZero(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        if (int.TryParse(input, out var parsedValue))
        {
            MinuteInputField.SetTextWithoutNotify(parsedValue.ToString("D2"));
        }
    }

    public void Heal()
    {
        if (!CheckForGame())
        {
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        localPlayer.ActiveHealthController.RestoreFullHealth();
    }

    public void KillMe()
    {
        if (!CheckForGame())
        {
            return;
        }

        var localPlayer = Utilities.GameWorld.MainPlayer;
        localPlayer.KillMe(EBodyPartColliderType.HeadCommon, 10_000f);
    }

    public void SpawnAirdrop()
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
        if (dropPoints?.Count > 0)
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

    public void GoToBTR()
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
                            myPlayer.Teleport(btrTransform.position + (Vector3.forward * 3f));
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

    private static bool CheckForGame()
    {
        var game = Singleton<AbstractGame>.Instance;
        if (game == null)
        {
            LogError("Game was null");
            return false;
        }

        if (game.Status != GameStatus.Started)
        {
            LogError("Game was null");
            return false;
        }

        return true;
    }

    private static void LogInfo(string message)
    {
        ConsoleScreen.Log(message);
        DT_Plugin.DT_Logger.LogInfo(message);
        NotificationManager.DisplayMessageNotification(message);
    }

    private static void LogWarning(string message)
    {
        ConsoleScreen.LogWarning(message);
        DT_Plugin.DT_Logger.LogWarning(message);
        NotificationManager.DisplayWarningNotification(message);
    }

    private static void LogError(string message)
    {
        ConsoleScreen.LogError(message);
        DT_Plugin.DT_Logger.LogError(message);
        NotificationManager.DisplayMessageNotification(message, iconType: ENotificationIconType.Alert);
    }

    public void Toggle()
    {
#if RELEASE
        if (!CheckForGame())
        {
            return;
        } 
#endif

        gameObject.SetActive(!gameObject.activeSelf);
        if (!gameObject.activeSelf)
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuEscape);
        }
    }

    public override ETranslateResult TranslateCommand(ECommand command)
    {
        if (command.IsCommand(ECommand.Escape))
        {
            Toggle();
            return ETranslateResult.BlockAll;
        }

        return GetDefaultBlockResult(command);
    }

    public override ECursorResult ShouldLockCursor()
    {
        return ECursorResult.ShowCursor;
    }

    public override void TranslateAxes(ref float[] axes)
    {
        axes = null;
    }
}