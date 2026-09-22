using System;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DebugTools.Patches;
using DebugTools.Patches.Logging;
using DebugTools.Utils;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using UnityEngine;

namespace DebugTools;

[BepInPlugin("lcw.lacyway.dt", "DebugTools", PluginVersion)]
[BepInIncompatibility("com.fika.core")] // free cam collides
internal sealed class DT_Plugin : BaseUnityPlugin
{
    public const string PluginVersion = "1.1.0";

    public static ConfigEntry<bool> AZERTYMode { get; set; }
    public static ConfigEntry<bool> ShowOverlay { get; set; }
    public static ConfigEntry<bool> DroneMode { get; set; }
    public static ConfigEntry<KeyboardShortcut> FreeCamButton { get; set; }
    public static ConfigEntry<KeyboardShortcut> GiveItemUIButton { get; set; }
    public static ConfigEntry<KeyboardShortcut> DebugMenuButton { get; set; }
    public static ConfigEntry<bool> LogInfo { get; set; }
    public static ConfigEntry<bool> LogWarning { get; set; }
    public static ConfigEntry<bool> LogError { get; set; }
    public static ConfigEntry<bool> CatchBotErrors { get; set; }

    public static InputTree InputTree
    {
        get
        {
            if (_inputTree == null)
            {
                var inputObj = GameObject.Find("___Input")
                    ?? throw new NullReferenceException("Could not find InputTree object!");

                _inputTree = inputObj.GetComponent<InputTree>();
            }

            return _inputTree;
        }
    }

    private static InputTree _inputTree;

    internal static DT_Plugin Instance { get; private set; }
    internal static ManualLogSource DT_Logger;
    internal static InternalBundleLoader BundleLoader { get; private set; }

    internal ItemUIService ItemUIService { get; private set; }

    private GiveItemUI _giveItemUI;
    private DebugMenu _debugMenu;
    private bool _itemsReady;

    private void Awake()
    {
        Instance = this;

        DT_Logger = Logger;
        DT_Logger.LogInfo($"{nameof(DT_Plugin)} has been loaded.");

        AZERTYMode = Config.Bind("Free camera", "AZERTY Mode",
            false, new ConfigDescription("If AZERTY mode should be used for controls in free cam."));
        ShowOverlay = Config.Bind("Free camera", "Show Overlay",
            true, new ConfigDescription("If the keybind overlay should be shown in free cam."));
        DroneMode = Config.Bind("Free camera", "Drone Mode",
            false, new ConfigDescription("If drone mode controls should be enabled in free cam."));

        FreeCamButton = Config.Bind("Key Binds", "Free Cam Button",
            new KeyboardShortcut(KeyCode.F9), new ConfigDescription("The key used to toggle free cam in raid."));
        GiveItemUIButton = Config.Bind("Key Binds", "Item Database Button",
            new KeyboardShortcut(KeyCode.F10), new ConfigDescription("The key used to toggle the item database.\nWill not work until the database and locales are loaded."));
        DebugMenuButton = Config.Bind("Key Binds", "Debug Menu Button",
            new KeyboardShortcut(KeyCode.F11), new ConfigDescription("The key used to toggle the debug menu.\nOnly works in raids."));

        LogInfo = Config.Bind("Logging", "Log Info",
            false, new ConfigDescription("If Unity.LogInfo should be logged to BepInEx."));
        LogWarning = Config.Bind("Logging", "Log Warning",
            false, new ConfigDescription("If Unity.LogWarning should be logged to BepInEx."));
        LogError = Config.Bind("Logging", "Log Error",
            false, new ConfigDescription("If Unity.LogError should be logged to BepInEx."));
        CatchBotErrors = Config.Bind("Bots", "Catch Bot Errors",
            false, new ConfigDescription("If bot activation errors should be caught and logged during initialization."));

        new PlayerOwner_vmethod_0_Patch().Enable();
        new LocalGame_Stop_Patch().Enable();
        new LabsKeycardDebugPatch().Enable();
        new TasksExtensions_HandleFinishedTask_Patch1().Enable();
        new TasksExtensions_HandleFinishedTask_Patch2().Enable();
        new PlayerCameraController_LateUpdate_Transpiler().Enable();
        new ClientBackendSession_SetBotSettings_Patch().Enable();
        new BotOwner_method_10_Patch().Enable();

        InfoLogPatches.EnableAll();
        WarningLogPatches.EnableAll();
        ErrorLogPatches.EnableAll();

        ConsoleScreen.Processor.RegisterCommandGroup<DebugCommands>();
        ConsoleScreen.Processor.RegisterCommand<DebugCommands.RecreateBackend>();

        BundleLoader = new InternalBundleLoader();

        _ = Task.Run(CreateItemUIService);
    }

    public void CreateGiveItemUI()
    {
        if (!_itemsReady)
        {
            DT_Logger.LogWarning("Item service not ready");
            return;
        }

        var giveItemUI = BundleLoader.GetGiveItemUI();
        var newUI = GameObject.Instantiate(giveItemUI);
        DontDestroyOnLoad(newUI);
        newUI.transform.SetParent(transform);
        _giveItemUI = newUI.GetComponent<GiveItemUI>();
    }

    public void CreateDebugMenu()
    {
        if (!_itemsReady)
        {
            DT_Logger.LogWarning("Item service not ready");
            return;
        }

        var debugTemplate = BundleLoader.GetDebugMenu();
        var copy = GameObject.Instantiate(debugTemplate);
        DontDestroyOnLoad(copy);
        copy.name = "DebugMenu";
        if (!copy.TryGetComponent<DebugMenu>(out var debugMenu))
        {
            DT_Logger.LogError("Could not find DebugMenu when creating it");
            GameObject.Destroy(copy);
            return;
        }
        _debugMenu = debugMenu;
    }

    private void Update()
    {
        if (GiveItemUIButton.Value.IsDown())
        {
            if (_giveItemUI == null)
            {
                CreateGiveItemUI();
            }

            if (_giveItemUI != null)
            {
                _giveItemUI.Toggle();
            }
        }

        if (DebugMenuButton.Value.IsDown())
        {
            if (_debugMenu == null)
            {
                CreateDebugMenu();
            }

            if (_debugMenu != null)
            {
                _debugMenu.Toggle();
            }
        }
    }

    public async Task CreateItemUIService()
    {
        ItemUIService = new ItemUIService();
        while (!Singleton<ItemFactory>.Instantiated)
        {
            await Task.Delay(TimeSpan.FromSeconds(1d));
        }

        DT_Logger.LogInfo("ItemFactory ready, caching items");
        try
        {
            await ItemUIService.Init();
            _itemsReady = true;
        }
        catch (Exception ex)
        {
            DT_Logger.LogError(ex);
            throw;
        }
    }
}
