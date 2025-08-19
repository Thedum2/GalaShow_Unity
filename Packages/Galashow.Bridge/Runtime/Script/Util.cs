using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Galashow.Bridge
{
    public class Util
    {
        public static (string routeName, string action) ParseRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
                return (string.Empty, string.Empty);

            var parts = route.Split('_', 2);
            return parts.Length >= 2 
                ? (parts[0], parts[1]) 
                : (route, string.Empty);
        }

        public static bool TryTo<T>(object raw, out T model, out string error)
        {
            try
            {
                if (raw is T t) { model = t; error = null; return true; }
                var token = raw as JToken ?? JToken.FromObject(raw);
                model = token.ToObject<T>();
                error = null;
                return true;
            }
            catch (Exception e)
            {
                model = default;
                error = e.Message;
                return false;
            }
        }
        
        public static void Log(string log)
        {
            var callerType = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            Debug.Log($"[{callerType}] - {log}");
        }

        public static void LogWarning(string log)
        {
            var callerType = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            Debug.LogWarning($"[{callerType}] - {log}");
        }
        public static void LogError(string log)
        {
            var callerType = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
            Debug.LogError($"[{callerType}] - {log}");
        }
    }
}