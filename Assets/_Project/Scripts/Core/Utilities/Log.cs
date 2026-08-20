using System;
using System.Diagnostics;
using UnityEngine;

namespace MoveRush.Core.Utilities
{
    /// <summary>
    /// Central logging facade. Every system logs through here instead of calling
    /// <see cref="UnityEngine.Debug"/> directly, which gives a single place to add tags,
    /// route messages to an analytics backend, or strip logging from release builds.
    /// Informational calls are compiled out of non-development player builds.
    /// </summary>
    public static class Log
    {
        private const string Prefix = "<b>[MoveRush]</b>";

        /// <summary>Enables or disables verbose informational logging at runtime.</summary>
        public static bool VerboseEnabled { get; set; } = true;

        /// <summary>Writes an informational message. Stripped from release player builds.</summary>
        /// <param name="message">Message body.</param>
        /// <param name="context">Optional Unity object highlighted when the entry is clicked.</param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, UnityEngine.Object context = null)
        {
            if (!VerboseEnabled)
            {
                return;
            }

            UnityEngine.Debug.Log($"{Prefix} {message}", context);
        }

        /// <summary>Writes a warning. Kept in all builds.</summary>
        /// <param name="message">Message body.</param>
        /// <param name="context">Optional Unity object highlighted when the entry is clicked.</param>
        public static void Warning(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning($"{Prefix} {message}", context);
        }

        /// <summary>Writes an error. Kept in all builds.</summary>
        /// <param name="message">Message body.</param>
        /// <param name="context">Optional Unity object highlighted when the entry is clicked.</param>
        public static void Error(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError($"{Prefix} {message}", context);
        }

        /// <summary>Writes a caught exception together with the operation that failed.</summary>
        /// <param name="exception">Exception that was caught.</param>
        /// <param name="message">Description of the operation that failed.</param>
        /// <param name="context">Optional Unity object highlighted when the entry is clicked.</param>
        public static void Exception(Exception exception, string message = null, UnityEngine.Object context = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                UnityEngine.Debug.LogError($"{Prefix} {message}", context);
            }

            UnityEngine.Debug.LogException(exception, context);
        }
    }
}
