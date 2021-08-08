using System;
using System.IO;
using AppSettingsManagement;

namespace Authorization
{
    public class TokenHandler // TODO: remove this and use AppSettings directly instead?
    {
        public TokenHandler(AppSettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        private readonly AppSettingsManager _settingsManager;
        
        public bool TryGet(out string token)
        {
            token = _settingsManager.Load().Token;
            return !string.IsNullOrWhiteSpace(token);
        }

        public void Set(string token)
        {
            var current = _settingsManager.Load();
            _settingsManager.Save(current with { Token = token });
        }

        public void Clear() => Set(null);
    }
}