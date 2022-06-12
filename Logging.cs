using System;

namespace MessageBasedSockets {
    public static class Logging {
        private static Action<string> _actInfo;
        private static Action<string> _actError;
        private static Action<string> _actDebug;

        public static void SetActions(Action<string> actInfo, Action<string> actError, Action<string> actDebug) {
            _actInfo = actInfo;
            _actError = actError;
            _actDebug = actDebug;
        }

        public static void Info(string text) {
            _actInfo?.Invoke(text);
        }

        public static void Error(string text) {
            _actError?.Invoke(text);
        }

        public static void Debug(string text) {
            _actDebug?.Invoke(text);
        }

        public static void Debug(string prefix, string text) {
            Debug($"[{prefix}] {text}");
        }
    }
}