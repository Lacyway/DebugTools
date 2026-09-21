using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;

namespace DebugTools.Utils;

internal static class Utilities
{
    public static GameWorld GameWorld => Singleton<GameWorld>.Instance;
    public static Player MainPlayer => GameWorld.MainPlayer;

    public static FieldInfo BotsControllerField => typeof(LocalGame)
        .GetField("BotsController", BindingFlags.NonPublic | BindingFlags.Instance);
    public static FieldInfo MainMenuInventoryControllerField => typeof(InventoryScreen)
        .GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);
}
