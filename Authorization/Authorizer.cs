using System;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;

namespace Authorization
{
	public enum AuthorizationResult 
	{
		OK, // Авторизация прошла успешно
		TokenNotFound, // Не найден сохранённый токен
		Error // Неверный логин/пароль, устаревший токен, ошибка подключения, т.п. 
	}

    public static class Authorizer
	{
		static Authorizer()
		{
			Api = new VkApi();
		}

		public static bool IsAuthorized => AuthorizedUser is not null;
		public static User AuthorizedUser { get; private set; }

		private static VkApi Api { get; set; }
		private const int AppId = 7289220;

		/// <summary>
		/// Авторизация ВК через логин и пароль.
		/// <returns>
		/// <list type="bullet">
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.Error"/>, если ВК отклонил логин и пароль или произошла ошибка подключения.</description>
		/// </item>
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.OK"/>, если авторизация прошла успешно.</description>
		/// </item>
		/// </list>
		/// </returns>
		/// </summary>
		public static AuthorizationResult Authorize(string login, string password)
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
			catch (VkAuthorizationException)
			{
				return AuthorizationResult.Error;
			}

			AuthorizedUser = Api.Users.Get(Array.Empty<long>()).First();
			return AuthorizationResult.OK;
		}

		/// <summary>
		/// Авторизация ВК через сохранённый токен.
		/// <returns>
		/// <list type="bullet">
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.TokenNotFound"/>, если не найден сохранённый токен.</description>
		/// </item>
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.Error"/>, если токен устарел или произошла ошибка подключения к ВК.</description>
		/// </item>
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.OK"/>, если авторизация прошла успешно.</description>
		/// </item>
		/// </list>
		/// </returns>
		/// </summary>
		public static AuthorizationResult Authorize()
		{
			bool isTokenSaved = TokenHandler.TryGet(out string token);
			if (!isTokenSaved)
			{
				return AuthorizationResult.TokenNotFound;
			}

			try
			{
				var authParams = new ApiAuthParams()
				{
					AccessToken = token
				};

				Api.Authorize(authParams);
			}
			catch (VkAuthorizationException)
			{
				return AuthorizationResult.Error;
			}

			AuthorizedUser = Api.Users.Get(Array.Empty<long>()).First();
			return AuthorizationResult.OK;
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
