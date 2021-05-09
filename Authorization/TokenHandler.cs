using System;
using System.IO;

namespace Authorization
{
    public static class TokenHandler
	{
		static TokenHandler()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dirPath = Path.Combine(appDataPath, "FromVkToTg");
			Directory.CreateDirectory(dirPath);
			_path = Path.Combine(dirPath, "token");
			Load();
		}

		private static readonly string _path = null;
		private static string _token = null;

		public static bool TryGet(out string token)
		{
			token = _token;
			return _token is not null;
		}

		public static void Set(string token, bool autoSave = false)
		{
			_token = token;
			
			if (autoSave)
			{
				Save();
			}
		}

		private static void Load()
		{
			if (!File.Exists(_path))
			{
				var sw = File.CreateText(_path);
				sw.Close();
			}

			var content = File.ReadAllText(_path);
			if (string.IsNullOrWhiteSpace(content))
			{
				_token = null;
				return;
			}

			_token = content;
		}

		public static void Save()
		{
			File.WriteAllText(_path, _token);
		}
	}
}
