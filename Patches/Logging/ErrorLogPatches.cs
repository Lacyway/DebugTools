using System;
using System.Reflection;
using SPT.Reflection.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DebugTools.Patches.Logging;

public static class ErrorLogPatches
{
    public static void EnableAll()
    {
        new Debug_LogError_Patch().Enable();
        new Debug_LogError_Context_Patch().Enable();
        new Debug_LogErrorFormat_Patch().Enable();
        new Debug_LogErrorFormat_Context_Patch().Enable();
        new Debug_LogException_Patch().Enable();
        new Debug_LogException_Context_Patch().Enable();
    }

    internal sealed class Debug_LogError_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogError), [typeof(object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message)
        {
            if (DT_Plugin.LogError.Value)
            {
                Logger.LogError($"[UNITY ERROR] {message}");
            }
        }
    }

    internal sealed class Debug_LogError_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogError), [typeof(object), typeof(Object)]);
        }

        [PatchPostfix]
        public static void Postfix(object message, Object context)
        {
            if (DT_Plugin.LogError.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogError($"[UNITY ERROR][{contextName}] {message}");
            }
        }
    }

    internal sealed class Debug_LogErrorFormat_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogErrorFormat), [typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(string format, object[] args)
        {
            if (DT_Plugin.LogError.Value)
            {
                Logger.LogError($"[UNITY ERROR] {string.Format(format, args)}");
            }
        }
    }

    internal sealed class Debug_LogErrorFormat_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogErrorFormat), [typeof(Object), typeof(string), typeof(object[])]);
        }

        [PatchPostfix]
        public static void Postfix(Object context, string format, object[] args)
        {
            if (DT_Plugin.LogError.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogError($"[UNITY ERROR][{contextName}] {string.Format(format, args)}");
            }
        }
    }

    internal sealed class Debug_LogException_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogException), [typeof(Exception)]);
        }

        [PatchPostfix]
        public static void Postfix(Exception exception)
        {
            if (DT_Plugin.LogError.Value)
            {
                Logger.LogError($"[UNITY ERROR] {exception?.Message}\n{exception?.StackTrace}");
            }
        }
    }

    internal sealed class Debug_LogException_Context_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Debug).GetMethod(nameof(Debug.LogException), [typeof(Exception), typeof(Object)]);
        }

        [PatchPostfix]
        public static void Postfix(Exception exception, Object context)
        {
            if (DT_Plugin.LogError.Value)
            {
                var contextName = context != null ? context.name : "null";
                Logger.LogError($"[UNITY ERROR][{contextName}] {exception?.Message}\n{exception?.StackTrace}");
            }
        }
    }
}