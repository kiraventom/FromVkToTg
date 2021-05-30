using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Authorization;
using AppSettingsManagement;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace UI
{
    internal static class GroupsPicker
    {
        private const int groupsOnPage = 20;

        /// <summary>
        /// Выбор групп для отслеживания.
        /// <returns>
        /// <list type="bullet">
        /// <item>
        /// <description>Возвращает <see langword="null"/>, если пользователь не подписан ни на одну группу</description>
        /// </item>
        /// </list>
        /// Иначе возвращает список выбранных групп.
        /// </returns>
        /// </summary>
        // TODO: Split megamethod to load, pick and save groups separately
        public static void PickAndSaveGroups() 
        {
            Console.CursorVisible = false;
            
            // load saved groups (maybe zero)
            var groupsIds = AppSettingsManager.Load().Groups;
            var savedGroups = groupsIds.Any() ? Authorizer.Api.Groups.GetById(groupsIds, null, null) : Enumerable.Empty<Group>();
            
            List<Group> selectedGroups = new(savedGroups ?? Enumerable.Empty<Group>());
            int prevPageIdx = -1; // to force redraw on start
            int pageIdx = 0;
            int prevLineIdx = 0;
            int lineIdx = 0;
            bool lineChanged = false;

            var groups = GetGroups(0);
            ulong totalCount = groups.TotalCount;
            if (totalCount == 0)
                return;

            int totalPagesCount = Convert.ToInt32(Math.Ceiling((double) totalCount / groupsOnPage));

            var group = groups[0];

            while (true)
            {
                if (prevPageIdx != pageIdx)
                {
                    RedrawPage(groups, pageIdx, lineIdx, selectedGroups, totalPagesCount);
                    prevPageIdx = pageIdx;
                }
                else if (prevLineIdx != lineIdx || lineChanged)
                {
                    if (prevLineIdx != lineIdx)
                        RedrawLine(prevLineIdx, groups[prevLineIdx], pageIdx, lineIdx, selectedGroups);

                    RedrawLine(lineIdx, group, pageIdx, lineIdx, selectedGroups);
                    prevLineIdx = lineIdx;
                    lineChanged = false;
                }

                var action = ReadInput();
                switch (action)
                {
                    case Action.Up:
                        prevLineIdx = lineIdx;
                        lineIdx = lineIdx != 0 ? lineIdx - 1 : groups.Count - 1;
                        if (prevLineIdx == lineIdx)
                            continue;

                        group = groups[lineIdx];
                        break;

                    case Action.Down:
                        prevLineIdx = lineIdx;
                        lineIdx = lineIdx != groups.Count - 1 ? lineIdx + 1 : 0;
                        if (prevLineIdx == lineIdx)
                            continue;

                        group = groups[lineIdx];
                        break;

                    case Action.Previous:
                        prevPageIdx = pageIdx;
                        pageIdx = pageIdx != 0 ? pageIdx - 1 : totalPagesCount - 1;
                        if (prevPageIdx == pageIdx)
                            continue;

                        prevLineIdx = 0;
                        lineIdx = 0;
                        groups = GetGroups(pageIdx);
                        group = groups[lineIdx];
                        break;

                    case Action.Next:
                        prevPageIdx = pageIdx;
                        pageIdx = pageIdx != totalPagesCount - 1 ? pageIdx + 1 : 0;
                        if (prevPageIdx == pageIdx)
                            continue;

                        prevLineIdx = 0;
                        lineIdx = 0;
                        groups = GetGroups(pageIdx);
                        group = groups[lineIdx];
                        break;

                    case Action.Select:
                        var already = selectedGroups.FirstOrDefault(g => g.Id == group.Id);
                        if (already is not null)
                        {
                            selectedGroups.Remove(already);
                        }
                        else
                        {
                            selectedGroups.Add(group);
                        }

                        lineChanged = true;
                        break;

                    case Action.Confirm:
                        Console.Clear();
                        Console.CursorVisible = true;
                        SaveGroups(selectedGroups);
                        // return selectedGroups.AsEnumerable();
                        return;

                    case Action.None:
                        break;

                    default:
                        throw new NotImplementedException($"Unknown action \"{action}\"");
                }
            }
        }

        private static void SaveGroups(IEnumerable<Group> groupsToSave)
        {
            var current = AppSettingsManager.Load();
            AppSettingsManager.Save(current with { Groups = groupsToSave.Select(g => g.Id.ToString()) });
        }

        private static void RedrawPage(VkCollection<Group> groups, int pageIdx, int groupIdx,
            ICollection<Group> selectedGroups, int totalPagesCount)
        {
            var user = Authorizer.AuthorizedUser;
            Console.Clear();
            Console.WriteLine($"Список сообществ {user.FirstNameAcc} {user.LastNameAcc}");

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                RedrawLine(i, group, pageIdx, groupIdx, selectedGroups);
            }

            if (pageIdx > 0)
            {
                Console.Write("<< ");
            }

            Console.Write($"Страница {pageIdx + 1} из {totalPagesCount}");
            if (pageIdx != totalPagesCount - 1)
            {
                Console.Write(" >>");
            }
        }

        private static void RedrawLine(int i, Group group, int selectedPageIdx, int selectedGroupIdx,
            ICollection<Group> selectedGroups)
        {
            Console.SetCursorPosition(0, i + 1); // + 1 because of header

            var backColor = Console.BackgroundColor;
            var foreColor = Console.ForegroundColor;

            if (i == selectedGroupIdx)
            {
                Console.BackgroundColor = foreColor;
                Console.ForegroundColor = backColor;
            }

            if (selectedGroups.Any(g => g.Id == group.Id))
                Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine($"{selectedPageIdx * groupsOnPage + i + 1}. {group.Name}");

            if (Console.BackgroundColor != backColor)
                Console.BackgroundColor = backColor;

            if (Console.ForegroundColor != foreColor)
                Console.ForegroundColor = foreColor;
        }

        private enum Action
        {
            Up,
            Down,
            Next,
            Previous,
            Select,
            Confirm,
            None
        }

        private static Action ReadInput()
        {
            var cki = Console.ReadKey(true);
            return cki.Key switch
            {
                ConsoleKey.UpArrow => Action.Up,
                ConsoleKey.DownArrow => Action.Down,
                ConsoleKey.LeftArrow => Action.Previous,
                ConsoleKey.RightArrow => Action.Next,
                ConsoleKey.Spacebar => Action.Select,
                ConsoleKey.Enter => Action.Confirm,
                _ => Action.None
            };
        }

        private static VkCollection<Group> GetGroups(int page)
        {
            var groupGetParams = new GroupsGetParams()
            {
                UserId = Authorizer.AuthorizedUser.Id,
                Offset = groupsOnPage * page,
                Count = groupsOnPage,
                Extended = true
            };

            var groups = Authorizer.Api.Groups.Get(groupGetParams);
            return groups;
        }
    }
}