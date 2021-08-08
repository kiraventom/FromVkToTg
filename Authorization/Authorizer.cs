using System;
using System.Linq;
using AppSettingsManagement;
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

    public class Authorizer
    {
        public Authorizer(AppSettingsManager _settingsManager)
        {
            // to avoid authorization error
            var services = new ServiceCollection();
            services.AddAudioBypass();
            Api = new VkApi(services);
            _tokenHandler = new TokenHandler(_settingsManager);
        }

        public bool IsAuthorized => AuthorizedUser is not null;
        public User AuthorizedUser { get; private set; }

        public VkApi Api { get; }
        private const int AppId = 7289220;
        private readonly TokenHandler _tokenHandler;

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
        public AuthorizationResult Authorize(string login, string password, Func<string> twoFactorFunc)
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
            _tokenHandler.Set(Api.Token);
            return AuthorizationResult.OK;
        }

        /// <summary>
        /// Авторизация ВК через сохранённый токен.
        /// <returns>
        /// <list type="bullet">
        /// <item>
        /// <description>Возвращает <see cref="AuthorizationResult.TokenNotFound"/>, если не найден сохранённый токен.</description>
        /// </item>
        /// <item>		/// <description>Возвращает <see cref="AuthorizationResult.FailedAuth"/>, если токен устарел или произошла ошибка подключения к ВК.</description>
        /// </item>
        /// <item>
        /// <description>Возвращает <see cref="AuthorizationResult.OK"/>, если авторизация прошла успешно.</description>
        /// </item>
        /// </list>
        /// </returns>
        /// </summary>
        public AuthorizationResult Authorize()
        {
            bool isTokenSaved = _tokenHandler.TryGet(out string token);
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
                _tokenHandler.Clear();
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