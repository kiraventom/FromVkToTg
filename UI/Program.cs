using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.WebSockets;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using System.Reflection;
using Authorization;
using AuthorizationResult = Authorization.AuthorizationResult;

namespace UI
{
	class Program
	{
		private const int AppId = 7289220;

		static void Main(string[] args)
		{
			var result = Authorizer.Authorize(); // try authorize via token
			checkAuthorizationResult:
			switch (result)
			{
				case AuthorizationResult.FailedAuth:
					Console.WriteLine("Ошибка авторизации! Некорректные логин/пароль или истёкший токен");
					goto case AuthorizationResult.TokenNotFound;

				case AuthorizationResult.TokenNotFound:
					var (login, password) = InputLoginPassword();
					result = Authorizer.Authorize(login, password, InputTwoFactorCode);
					goto checkAuthorizationResult;

				case AuthorizationResult.ConnectionError:
					Console.WriteLine("Ошибка подключения к ВК! Проверьте подключение к интернету");
					return;

				case AuthorizationResult.OK:
					Console.WriteLine($"Успешный вход под именем {Authorizer.AuthorizedUser.FirstName} {Authorizer.AuthorizedUser.LastName}");
					break;

				default:
					throw new NotImplementedException($"Unknown authorization result \"{result}\"");
			}

			var api = Authorizer.Api;
			var user = Authorizer.AuthorizedUser;

			var pickedGroups = GroupsPicker.GetPickedGroups();
			if (pickedGroups is null)
			{
				Console.WriteLine("Вы не подписаны ни на одну группу!");
				return;
			}

			pickedGroups.ToList().ForEach(x => Console.WriteLine(x.Name));

			Console.ReadLine();
		}

		static (string, string) InputLoginPassword()
		{
			string login = string.Empty;
			StringBuilder passwordSB = new(string.Empty);

			while (string.IsNullOrWhiteSpace(login))
			{
				Console.WriteLine("Введите логин:");
				login = Console.ReadLine();
			}

			while (string.IsNullOrWhiteSpace(passwordSB.ToString()))
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
						passwordSB = passwordSB.Remove(1, passwordSB.Length - 1);
						Console.Write("\b \b");
					}
					else
					{
						passwordSB.Append(i.KeyChar);
						Console.Write("*");
					}
				}
			}

			return (login, passwordSB.ToString());
		}

		private static string InputTwoFactorCode()
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
}
