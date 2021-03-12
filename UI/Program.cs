using System;
using System.Linq;
using System.IO;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.RequestParams;
using System.Reflection;

namespace UI
{
	class Program
	{
		static Program()
		{
			Api = new VkApi();
		}

		private const int AppId = 7289220;
		private static VkApi Api { get; }

		static void Main(string[] args)
		{
			User authorizedUser;
			bool isTokenSaved = TokenHandler.TryGet(out string token);
			if (isTokenSaved)
			{
				authorizedUser = Authorize(token);
			}
			else
			{
				var (login, password) = InputLoginPassword();
				authorizedUser = Authorize(login, password);
				TokenHandler.Set(Api.Token, true);
			}

			var groupGetParams = new GroupsGetParams()
			{
				UserId = authorizedUser.Id,
				Count = 1000,
				Extended = true
			};

			var groups = Api.Groups.Get(groupGetParams);
			Console.WriteLine($"Список сообществ {authorizedUser.FirstNameAcc} {authorizedUser.LastNameAcc}");
			for (int i = 0; i < groups.Count; i++)
			{
				Group group = groups[i];
				Console.WriteLine($"{i + 1}. {group.Name}");
			}	
		}

		static User Authorize(string login, string password)
		{
			try
			{
				var authParams = new ApiAuthParams()
				{
					ApplicationId = AppId,
					Login = login,
					Password = password,
					Settings = Settings.Groups | Settings.Offline,
					TwoFactorAuthorization = InputTwoFactorCode
				};

				Api.Authorize(authParams);
			}
			catch (VkAuthorizationException e)
			{
				Console.WriteLine(e.Message);
				return null;
			}

			return Api.Users.Get(Array.Empty<long>()).First();
		}

		static User Authorize(string token)
		{
			try
			{
				var authParams = new ApiAuthParams()
				{
					AccessToken = token
				};

				Api.Authorize(authParams);
			}
			catch (VkAuthorizationException e)
			{
				Console.WriteLine(e.Message);
				return null;
			}

			return Api.Users.Get(Array.Empty<long>()).First();
		}

		static (string, string) InputLoginPassword()
		{
			string login = string.Empty;
			string password = string.Empty;

			while (string.IsNullOrWhiteSpace(login))
			{
				Console.WriteLine("Введите логин:");
				login = Console.ReadLine();
			}

			while (string.IsNullOrWhiteSpace(password))
			{
				Console.WriteLine("Введите пароль:");
				password = Console.ReadLine();
			}

			return (login, password);
		}

		static string InputTwoFactorCode()
		{
			string code = string.Empty;

			while (string.IsNullOrWhiteSpace(code) || code.Any(c => !char.IsDigit(c)))
			{
				Console.WriteLine("Введите код двухфакторной авторизации:");
				code = Console.ReadLine();
			}

			return code;
		}
	}
	
	static class TokenHandler
	{
		static TokenHandler()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dirPath = Path.Combine(appDataPath, "FromVkToTg");
			Directory.CreateDirectory(dirPath);
			_path = Path.Combine(dirPath, "token");
			Load();
		}

		private static string _path = null;
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
