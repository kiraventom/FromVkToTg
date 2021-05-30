using System;
using System.IO;
using AppSettingsManagement;

namespace Authorization
{
    public static class TokenHandler // TODO: remove this and use AppSettings directly instead?
    {
        public static bool TryGet(out string token)
        {
            token = AppSettingsManager.Load().Token;
            return !string.IsNullOrWhiteSpace(token);
        }

        public static void Clear() => Set(null);

        public static void Set(string token)
        {
            var current = AppSettingsManager.Load();
            AppSettingsManager.Save(current with { Token = token });
        }
    }
}