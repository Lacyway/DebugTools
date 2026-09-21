using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using UnityEngine;

namespace DebugTools.FreeCamera;

public sealed class FreeCameraController : MonoBehaviour
{
    private Player Player
    {
        get
        {
            return Singleton<GameWorld>.Instance.MainPlayer;
        }
    }

    public bool IsScriptActive
    {
        get
        {
            if (_freeCamScript != null)
            {
                return _freeCamScript.IsActive;
            }
            return false;
        }
    }

    public Camera CameraMain { get; private set; }

    private EftBattleUIScreen BattleUI
    {
        get
        {
            if (_playerUI == null)
            {
                var gameObject = GameObject.Find("BattleUIScreen");
                if (gameObject == null)
                {
                    return null;
                }

                _playerUI = gameObject.GetComponent<EftBattleUIScreen>();
                if (_playerUI == null)
                {
                    return null;
                }
            }

            return _playerUI;
        }
        set
        {
            _playerUI = value;
        }
    }

    private EftBattleUIScreen _playerUI;
    private GameObject _cameraParent;
    private FreeCamera _freeCamScript;
    private bool _uiHidden;
    private GamePlayerOwner _gamePlayerOwner;
    private Vector3 _lastKnownPosition;
    private DisablerCullingObjectBase[] _allCullingObjects;
    private List<PerfectCullingBakeGroup> _previouslyActiveBakeGroups;
    private bool _hasEnabledCulling;

    private void Awake()
    {
        _cameraParent = new GameObject("CameraParent");
        var FCamera = _cameraParent.GetOrAddComponent<Camera>();
        FCamera.enabled = false;
    }

    private void Start()
    {
        // Find Main Camera
        CameraMain = CameraManager.Instance.Camera;
        if (CameraMain == null)
        {
            return;
        }

        // Add Freecam script to main camera in scene
        _freeCamScript = CameraMain.gameObject.AddComponent<FreeCamera>();
        if (_freeCamScript == null)
        {
            return;
        }

        // Get GamePlayerOwner component
        _gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();
        if (_gamePlayerOwner == null)
        {
            return;
        }

        _allCullingObjects = FindObjectsOfType<DisablerCullingObjectBase>();
        _previouslyActiveBakeGroups = [];

        Player.ActiveHealthController.DiedEvent += MainPlayer_DiedEvent;
    }

    private void MainPlayer_DiedEvent(EDamageType obj)
    {
        Player.ActiveHealthController.DiedEvent -= MainPlayer_DiedEvent;
        Destroy(this);
    }

    private void Update()
    {
        if (_gamePlayerOwner == null)
        {
            return;
        }

        if (Player == null)
        {
            return;
        }

        if (Player.ActiveHealthController == null)
        {
            return;
        }

        if (DT_Plugin.FreeCamButton.Value.IsDown())
        {
            ToggleCamera();
            ToggleUI();
        }
    }

    /// <summary>
    /// Toggles the Freecam mode
    /// </summary>
    public void ToggleCamera()
    {
        // Get our own Player instance. Null means we're not in a raid
        if (Player == null)
        {
            return;
        }

        if (!_freeCamScript.IsActive)
        {
            SetPlayerToFreecamMode(Player);
        }
        else
        {
            SetPlayerToFirstPersonMode(Player);
        }
    }

    public void ToggleSpectateCamera()
    {
        if (Player == null)
        {
            return;
        }

        if (!_freeCamScript.IsActive)
        {
            var aliveTargets = _freeCamScript.RecalculateAndGetPlayers();

            if (aliveTargets.Count == 0)
            {
#if DEBUG
                DT_Plugin.DT_Logger.LogInfo("FreecamController: No players found");
#endif
                _freeCamScript.transform.position = _lastKnownPosition;
                ToggleCamera();
                return;
            }

            var fikaPlayer = aliveTargets[0];
            _freeCamScript.SetCurrentPlayer(fikaPlayer);
#if DEBUG
            DT_Plugin.DT_Logger.LogInfo("FreecamController: Spectating new target: " + fikaPlayer.Profile.Info.MainProfileNickname);
#endif

            Player.PointOfView = EPointOfView.ThirdPerson;
            if (Player.PlayerBody != null)
            {
                Player.PlayerBody.PointOfView.Value = EPointOfView.FreeCamera;
                Player.GetComponent<PlayerCameraController>().UpdatePointOfView();
            }
            _gamePlayerOwner.enabled = false;
            _freeCamScript.SetActive(true);

            _freeCamScript.Attach3rdPerson();
        }
    }

    /// <summary>
    /// Hides the main UI (health, stamina, stance, hotbar, etc.)
    /// </summary>
    private void ToggleUI()
    {
        // Check if we're currently in a raid
        if (Player == null)
        {
            return;
        }

        if (BattleUI == null || BattleUI.gameObject == null)
        {
            return;
        }

        BattleUI.gameObject.SetActive(_uiHidden);
        _uiHidden = !_uiHidden;
    }

    /// <summary>
    /// A helper method to set the Player into Freecam mode
    /// </summary>
    /// <param name="localPlayer"></param>
    private void SetPlayerToFreecamMode(Player localPlayer)
    {
        // We set the player to third person mode
        // This means our character will be fully visible, while letting the camera move freely
        localPlayer.PointOfView = EPointOfView.ThirdPerson;

        if (localPlayer.PlayerBody != null)
        {
            localPlayer.PlayerBody.PointOfView.Value = EPointOfView.FreeCamera;
            localPlayer.GetComponent<PlayerCameraController>().UpdatePointOfView();
        }

        _gamePlayerOwner.enabled = false;
        _freeCamScript.SetActive(true);
    }

    /// <summary>
    /// A helper method to reset the player view back to First Person
    /// </summary>
    /// <param name="localPlayer"></param>
    private void SetPlayerToFirstPersonMode(Player localPlayer)
    {
        // re-enable _gamePlayerOwner
        _gamePlayerOwner.enabled = true;
        _freeCamScript.SetActive(false);

        localPlayer.PointOfView = EPointOfView.FirstPerson;
        CameraManager.Instance.SetOcclusionCullingEnabled(true);

        if (_hasEnabledCulling)
        {
            EnableAllCullingObjects();
        }
    }

    public void DisableAllCullingObjects()
    {
        var count = 0;
        foreach (var cullingObject in _allCullingObjects)
        {
            if (cullingObject.HasEntered)
            {
                continue;
            }
            count++;
            cullingObject.SetComponentsEnabled(true);
        }
#if DEBUG
        DT_Plugin.DT_Logger.LogWarning($"Enabled {count} Culling Triggers.");
#endif

        var perfectCullingAdaptiveGrid = FindObjectOfType<PerfectCullingAdaptiveGrid>();
        if (perfectCullingAdaptiveGrid != null)
        {
            if (perfectCullingAdaptiveGrid.RuntimeGroupMapping.Count > 0)
            {
                foreach (var sceneGroup in perfectCullingAdaptiveGrid.RuntimeGroupMapping)
                {
                    foreach (var bakeGroup in sceneGroup.bakeGroups)
                    {
                        if (!bakeGroup.IsEnabled)
                        {
                            bakeGroup.IsEnabled = true;
                            continue;
                        }

                        _previouslyActiveBakeGroups.Add(bakeGroup);
                    }

                    sceneGroup.enabled = false;
                }
            }
        }

        _hasEnabledCulling = true;
    }

    public void EnableAllCullingObjects()
    {
        var count = 0;
        foreach (var cullingObject in _allCullingObjects)
        {
            if (cullingObject.HasEntered)
            {
                continue;
            }
            count++;
            cullingObject.SetComponentsEnabled(false);
        }
#if DEBUG
        DT_Plugin.DT_Logger.LogWarning($"Disabled {count} Culling Triggers.");
#endif

        var perfectCullingAdaptiveGrid = FindObjectOfType<PerfectCullingAdaptiveGrid>();
        if (perfectCullingAdaptiveGrid != null)
        {
            if (perfectCullingAdaptiveGrid.RuntimeGroupMapping.Count > 0)
            {
                foreach (var sceneGroup in perfectCullingAdaptiveGrid.RuntimeGroupMapping)
                {
                    sceneGroup.enabled = true;

                    foreach (var bakeGroup in sceneGroup.bakeGroups)
                    {
                        if (bakeGroup.IsEnabled && !_previouslyActiveBakeGroups.Contains(bakeGroup))
                        {
                            bakeGroup.IsEnabled = false;
                            continue;
                        }

                        _previouslyActiveBakeGroups.Remove(bakeGroup);
                    }

                    _previouslyActiveBakeGroups.Clear();
                }
            }
        }

        _hasEnabledCulling = false;
    }

    /// <summary>
    /// Gets the current <see cref="EFT.Player"/> instance if it's available
    /// </summary>
    /// <returns>Local <see cref="EFT.Player"/> instance; returns null if the game is not in raid</returns>
    private Player GetLocalPlayerFromWorld()
    {
        // If the GameWorld instance is null or has no RegisteredPlayers, it most likely means we're not in a raid
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null || gameWorld.MainPlayer == null)
        {
            return null;
        }

        // One of the RegisteredPlayers will have the IsYourPlayer flag set, which will be our own Player instance
        return gameWorld.MainPlayer;
    }

    private void OnDestroy()
    {
        Singleton<FreeCameraController>.Release(this);
        Destroy(_cameraParent);

        // Destroy FreeCamScript before FreeCamController if exists
        Destroy(_freeCamScript);
        Destroy(this);
    }
}
