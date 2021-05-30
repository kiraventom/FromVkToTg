using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VkNet;
using VkNet.AudioBypassService.Exceptions;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.AudioBypassService.Extensions;

namespace Authorization
{
	public enum AuthorizationResult 
	{
		OK, // Авторизация прошла успешно
		TokenNotFound, // Не найден сохранённый токен
		FailedAuth, // Неверный логин/пароль, устаревший токен
		ConnectionError // ошибка подключения
	}

    public static class Authorizer
	{
		static Authorizer()
		{
			// to avoid authorization error
			var services = new ServiceCollection();
			services.AddAudioBypass();
			Api = new VkApi(services);
		}

		public static bool IsAuthorized => AuthorizedUser is not null;
		public static User AuthorizedUser { get; private set; }

		public static VkApi Api { get; }
		private const int AppId = 7289220;

		/// <summary>
		/// Авторизация ВК через логин и пароль.
		/// <returns>
		/// <list type="bullet">
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.FailedAuth"/>, если ВК отклонил логин и пароль или произошла ошибка подключения.</description>
		/// </item>
		/// <item>
		/// <description>Возвращает <see cref="AuthorizationResult.OK"/>, если авторизация прошла успешно.</description>
		/// </item>
		/// </list>
		/// </returns>
		/// </summary>
		public static AuthorizationResult Authorize(string login, string password, Func<string> twoFactorFunc)
		{
			try
			{
				var authParams = new ApiAuthParams()
				{
					ApplicationId = AppId,
					Login = login,
					Password = password,
					Settings = Settings.Groups | Settings.Offline,
					TwoFactorAuthorization = twoFactorFunc
				};

				Api.Authorize(authParams);
			}
			catch (VkAuthorizationException)
			{
				return AuthorizationResult.FailedAuth;
			}
			catch (VkAuthException) // from audiobypass
			{
				return AuthorizationResult.FailedAuth;
			}
			catch (System.Net.Http.HttpRequestException)
			{
				return AuthorizationResult.ConnectionError;
			}

			AuthorizedUser = Api.Users.Get(Array.Empty<long>()).First();
			TokenHandler.Set(Api.Token);
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
		/// <description>Возвращает <see cref="AuthorizationResult.FailedAuth"/>, если токен устарел или произошла ошибка подключения к ВК.</description>
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
				TokenHandler.Clear();
				return AuthorizationResult.FailedAuth;
			}
			catch (System.Net.Http.HttpRequestException)
			{
				return AuthorizationResult.ConnectionError;
			}

			AuthorizedUser = Api.Users.Get(Array.Empty<long>()).First();
			return AuthorizationResult.OK;
		}
	}
}
