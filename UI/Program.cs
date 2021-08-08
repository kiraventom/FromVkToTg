using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.WebSockets;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using System.Reflection;
using AppSettingsManagement;
using Authorization;
using VkNet.Model;
using VkWatcher;
using AuthorizationResult = Authorization.AuthorizationResult;

namespace UI
{
    internal static class Program
    {
        static Program()
        {
            const string appName = "FromVkToTg";
            _settingsManager = new AppSettingsManager(appName);
            _authorizer = new Authorizer(_settingsManager);
            _groupsPicker = new GroupsPicker(_settingsManager, _authorizer);
        }
        
        private const int AppId = 7289220;
        private static readonly Authorizer _authorizer;
        private static readonly AppSettingsManager _settingsManager;
        private static readonly GroupsPicker _groupsPicker;

        private static void Main(string[] args)
        {
            start:
            Authorize();

            IEnumerable<Group> savedGroups;
            void LoadGroupsFromSettings()
            {
                // load saved groups (maybe zero)
                var groupsIds = _settingsManager .Load().Groups;
                savedGroups = groupsIds.Any() ? _authorizer.Api.Groups.GetById(groupsIds, null, null) : Enumerable.Empty<Group>();
            }

            LoadGroupsFromSettings();
            while (true)
            {
                Console.WriteLine("Главное меню\n");
                Console.WriteLine("1 Начать отслеживание");
                Console.WriteLine($"2 Выбрать группы для отслеживания ({savedGroups.Count()} выбрано)");
                Console.WriteLine("3 Выйти из аккаунта");
                Console.WriteLine("4 Закрыть программу");
             
                var keyInfo = Console.ReadKey(true);
                Console.Clear();
                
                switch (keyInfo.KeyChar)
                {
                    case '1':
                        if (!savedGroups.Any())
                        {
                            Console.WriteLine("Выберите хотя бы одну группу!");
                            break;
                        }
                        
                        goto watching;

                    case '2':
                        _groupsPicker.PickAndSaveGroups();
                        LoadGroupsFromSettings();
                        break;

                    case '3':
                        bool shouldLogOut = ShouldLogOut();
                        if (shouldLogOut)
                            goto start;
                        break;

                    case '4':
                        return;
                }
            }

            watching:

            Console.WriteLine("Выбранные для отслеживания группы:");
            savedGroups.ToList().ForEach(x => Console.WriteLine(x.Name));
            
            // var watcher = new Watcher(pickedGroups.ToList());

            Console.ReadLine();
        }

        static void Authorize()
        {
            var result = _authorizer.Authorize(); // try authorize via token
            checkAuthorizationResult:
            switch (result)
            {
                case AuthorizationResult.FailedAuth:
                    Console.WriteLine("Ошибка авторизации! Некорректные логин/пароль или истёкший токен");
                    goto case AuthorizationResult.TokenNotFound;

                case AuthorizationResult.TokenNotFound:
                    var (login, password) = InputLoginPassword();
                    result = _authorizer.Authorize(login, password, InputTwoFactorCode);
                    goto checkAuthorizationResult;

                case AuthorizationResult.ConnectionError:
                    Console.WriteLine("Ошибка подключения к ВК! Проверьте подключение к интернету");
                    return;

                case AuthorizationResult.OK:
                    Console.WriteLine(
                        $"Успешный вход под именем {_authorizer.AuthorizedUser.FirstName} {_authorizer.AuthorizedUser.LastName}");
                    break;

                default:
                    throw new NotImplementedException($"Unknown authorization result \"{result}\"");
            }
        }

        static bool ShouldLogOut()
        {
            Console.WriteLine("Вы уверены? Ваши настройки будут удалены! (Y/N)");
            input:
            var keyInfo = Console.ReadKey(true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.Y:
                    _settingsManager.Reset();
                    return true;

                case ConsoleKey.N:
                    return false;

                default:
                    goto input;
            }
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
                Console.WriteLine("Введите код из сообщения / 4 последние цифры номера, с которого поступил звонок сброс:");
                code = Console.ReadLine();
            }

            return code;
        }
    }
}