using System;
using System.Linq;
using System.Text;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace UI
{
	static class Program
	{
		static Program()
		{
			Api = new VkApi();
		}

		private const int AppId = 7289220;
		private static VkApi Api { get; }

		static void Main(string[] args)
		{
			while (true)
			{
				string login = string.Empty;
				StringBuilder password = new(string.Empty);

				while (string.IsNullOrWhiteSpace(login))
				{
					Console.WriteLine("Введите логин:");
					login = Console.ReadLine();
				}

				while (string.IsNullOrWhiteSpace(password.ToString()))
				{
					Console.WriteLine("Введите пароль:");
					while (true)
					{
						ConsoleKeyInfo i = Console.ReadKey(true);
						if (i.Key == ConsoleKey.Enter)
						{
							Console.Write('\n');
							break;
						}
						else if (i.Key == ConsoleKey.Backspace)
						{
							password = password.Remove(1, password.Length - 1);
							Console.Write("\b \b");
						}
						else
						{
							password.Append(i.KeyChar);
							Console.Write("*");
						}
					}
					//password = Console.ReadLine();
				}

				try
				{
					var authParams = new ApiAuthParams()
					{
						ApplicationId = AppId,
						Login = login,
						Password = password.ToString(),
						Settings = Settings.Groups | Settings.Offline,
						TwoFactorAuthorization = static () =>
						{
							string code = string.Empty;

							while (string.IsNullOrWhiteSpace(code) || code.Any(c => !char.IsDigit(c)))
							{
								Console.WriteLine("Введите код двухфакторной авторизации:");
								code = Console.ReadLine();
							}

							return code;
						}
					};

					Api.Authorize(authParams);
					break;
				}
				catch (VkAuthorizationException e)
				{
					Console.WriteLine(e.Message);
				}
			}

			Console.WriteLine(Api.Token);
			var authorizedUser = Api.Users.Get(Array.Empty<long>()).First();

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
	}
}
