using System.Reflection;
using SPT.Reflection.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DebugTools.Patches.Logging;

public static class WarningLogPatches
{
    public static void EnableAll()
    {
        new Debug_LogWarning_Patch().Enable();
        new Debug_LogWarning_Context_Patch().Enable();
        new Debug_LogWarningFormat_Patch().Enable();
        new Debug_LogWarningFormat_Context_Patch().Enable();
    }

    internal sealed class Debug_LogWarning_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogWarning), [typeof(object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message)
        {
            if (DT_Plugin.LogWarning.Value)
            {
                Logger.LogWarning($"[UNITY WARNING] {message}");
            }
        }
    }

    internal sealed class Debug_LogWarning_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogWarning), [typeof(object), typeof(Object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message, Object context)
        {
            if (DT_Plugin.LogWarning.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogWarning($"[UNITY WARNING][{contextName}] {message}");
            }
        }
    }

    internal sealed class Debug_LogWarningFormat_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogWarningFormat), [typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(string format, object[] args)
        {
            if (DT_Plugin.LogWarning.Value)
            {
                Logger.LogWarning($"[UNITY WARNING]{string.Format(format, args)}");
            }
        }
    }

    internal sealed class Debug_LogWarningFormat_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogWarningFormat), [typeof(Object), typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(Object context, string format, object[] args)
        {
            if (DT_Plugin.LogWarning.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogWarning($"[UNITY WARNING][{contextName}] {string.Format(format, args)}");
            }
        }
    }
}