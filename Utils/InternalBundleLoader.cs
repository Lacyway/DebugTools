using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Diz.Utils;
using UnityEngine;

namespace DebugTools.Utils;

internal sealed class InternalBundleLoader
{
    public static InternalBundleLoader Instance { get; private set; }

    private AssetBundle _debugToolsBundle;

    public InternalBundleLoader()
    {
        Task.Run(LoadBundles);
        Instance = this;
    }

    public async Task LoadBundles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames())
        {
            await using var stream = assembly.GetManifestResourceStream(name);
            await using MemoryStream memoryStream = new();

            var bundleName = name.Replace("DebugTools.Utils.Files.", "")
                .Replace(".bundle", "");

            if (bundleName == "debugtools")
            {
                await stream.CopyToAsync(memoryStream);
                var assetBundle = AssetBundle.LoadFromMemoryAsync(memoryStream.ToArray());
                while (!assetBundle.isDone)
                {
                    await Task.Yield();
                }

                _debugToolsBundle = assetBundle.assetBundle;
                if (_debugToolsBundle == null)
                {
                    throw new NullReferenceException("DebugToolsBundle was not loaded properly");
                }
                else
                {
                    DT_Plugin.DT_Logger.LogInfo("DebugToolsBundle loaded and cached successfully");
                }
            }
            else
            {
                DT_Plugin.DT_Logger.LogFatal("Unknown bundle loaded! Terminating...");
                AsyncWorker.RunInMainTread(Application.Quit);
            }
        }
    }

    public void UnloadBundles(bool unloadAllLoadedObjects = false)
    {
        if (_debugToolsBundle != null)
        {
            _debugToolsBundle.Unload(unloadAllLoadedObjects);
            _debugToolsBundle = null;
            DT_Plugin.DT_Logger.LogInfo("DebugToolsBundle unloaded successfully");
        }
    }

    internal GameObject GetFreecamUI()
    {
        if (_debugToolsBundle == null)
        {
            throw new NullReferenceException("GetFreecamUI::DebugToolsBundle did not exist!");
        }

        return _debugToolsBundle.LoadAsset<GameObject>("FreecamUI.prefab");
    }

    internal GameObject GetGiveItemUI()
    {
        if (_debugToolsBundle == null)
        {
            throw new NullReferenceException("GetGiveItemUI::DebugToolsBundle did not exist!");
        }

        return _debugToolsBundle.LoadAsset<GameObject>("GiveItemUI.prefab");
    }

    internal GameObject GetDebugMenu()
    {
        if (_debugToolsBundle == null)
        {
            throw new NullReferenceException("GetDebugMenu::DebugToolsBundle did not exist!");
        }

        return _debugToolsBundle.LoadAsset<GameObject>("DebugMenu.prefab");
    }
}