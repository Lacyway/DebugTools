using System.Collections.Generic;
using EFT;

namespace DebugTools.FreeCamera;

public sealed partial class FreeCamera
{
    private bool _hidePlayerList;
    private Player _lastSpectatingPlayer;
    private Dictionary<int, ListPlayer> _playersTracker;

    ECameraState _cameraState;

    private void OnPlayerSpawned(IPlayer player)
    {
#if DEBUG
        DT_Plugin.DT_Logger.LogInfo($"Adding ListPlayer for {player.Profile.GetCorrectedNickname()}");
#endif

        if (!_playersTracker.ContainsKey(player.Id))
        {
            var newObj = Instantiate(_freecamUI.ListPlayerPrefab, _freecamUI.ListOfPlayers.transform);
            var listPlayer = newObj.GetComponent<ListPlayer>();
            _playersTracker.Add(player.Id, listPlayer);
            listPlayer.Init((Player)player);
            RecalculatePlayerList();
            player.OnIPlayerDeadOrUnspawn += OnPlayerDestroyed;
            return;
        }

#if DEBUG
        DT_Plugin.DT_Logger.LogWarning($"ListPlayer for {player.Profile.GetCorrectedNickname()} already exists");
#endif
    }

    private void OnPlayerDestroyed(IPlayer player)
    {
        player.OnIPlayerDeadOrUnspawn -= OnPlayerDestroyed;
        if (_playersTracker.Remove(player.Id, out var listPlayer))
        {
            Destroy(listPlayer.gameObject);
        }

        RecalculatePlayerList();
    }

    private void UpdatePlayerList()
    {
        foreach (var player in _playersTracker.Values)
        {
            player.ManualUpdate();
        }
    }
}
