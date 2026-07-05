using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Maple.Core
{
    /// <summary>
    /// 轻量日志封装，支持 Tag 分类和条件编译剥离。
    /// Info/Warning 在 Release 构建中自动剥离，Error/Exception 始终保留。
    /// 为使 Console 双击跳转到真实调用处，需在 Console 窗口菜单中开启「Strip logging callstack」。
    /// </summary>
    public static class GLogger
    {
        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string tag, string msg)
        {
            Debug.Log($"[{tag}] {msg}");
        }

        [HideInCallstack]
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string tag, string msg)
        {
            Debug.LogWarning($"[{tag}] {msg}");
        }

        [HideInCallstack]
        public static void LogError(string tag, string msg)
        {
            Debug.LogError($"[{tag}] {msg}");
        }

        [HideInCallstack]
        public static void LogException(Exception ex, string tag = "Exception", string msg = null)
        {
            if (!string.IsNullOrEmpty(msg))
                Debug.LogError($"[{tag}] {msg}");
            Debug.LogException(ex);
        }
    }
}