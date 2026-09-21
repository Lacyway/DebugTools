using System.Reflection;
using SPT.Reflection.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DebugTools.Patches.Logging;

public static class InfoLogPatches
{
    public static void EnableAll()
    {
        new Debug_Log_Patch().Enable();
        new Debug_Log_Context_Patch().Enable();
        new Debug_LogFormat_Patch().Enable();
        new Debug_LogFormat_Context_Patch().Enable();
    }

    internal sealed class Debug_Log_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.Log), [typeof(object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message)
        {
            if (DT_Plugin.LogInfo.Value)
            {
                Logger.LogInfo($"[UNITY INFO] {message}");
            }
        }
    }

    internal sealed class Debug_Log_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.Log), [typeof(object), typeof(Object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message, Object context)
        {
            if (DT_Plugin.LogInfo.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogInfo($"[UNITY INFO][{contextName}] {message}");
            }
        }
    }

    internal sealed class Debug_LogFormat_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogFormat), [typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(string format, object[] args)
        {
            if (DT_Plugin.LogInfo.Value)
            {
                Logger.LogInfo($"[UNITY INFO] {string.Format(format, args)}");
            }
        }
    }

    internal sealed class Debug_LogFormat_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogFormat), [typeof(Object), typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(Object context, string format, object[] args)
        {
            if (DT_Plugin.LogInfo.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogInfo($"[UNITY INFO][{contextName}] {string.Format(format, args)}");
            }
        }
    }
}