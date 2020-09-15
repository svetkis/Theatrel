using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.Subscriptions
{
    internal class ManageSubscriptionsCommand : DialogCommandBase
    {
        private const string DeleteAll = "Удалить все";
        private const string DeleteMany = "Удалить";
        private const string NothingTodo = "Оставить как есть";

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Редактировать подписки";

        public ManageSubscriptionsCommand(IDbService dbService) : base((int)DialogStep.SelectMonth, dbService)
        {
        }

        public override bool IsMessageCorrect(string message)
        {
            string trimMsg = message.Trim();
            if (string.Equals(trimMsg, DeleteAll, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (string.Equals(trimMsg, NothingTodo, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if ( new[] { DeleteMany }.Any(s => trimMsg.StartsWith(s, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return false;
        }

        private int GetInt(string msg)
        {
            if (int.TryParse(msg, out var value))
            {
                return value - 1;
            }

            return -1;
        }

        private int[] GetInts(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return null;

            string[] splitData = msg.Split(",");

            return splitData.Select(s => GetInt(s.Trim())).ToArray();
        }

        public override async Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            string trimMsg = message.Trim().ToLower();

            bool isDeleteAll = string.Equals(trimMsg, DeleteAll, StringComparison.InvariantCultureIgnoreCase);
            bool isDeleteMany = trimMsg.StartsWith(DeleteMany, StringComparison.InvariantCultureIgnoreCase);

            if (!(isDeleteAll || isDeleteMany))
                return new TgCommandResponse(null);

            using var subscriptionRepository = DbService.GetSubscriptionRepository();

            SubscriptionEntity[] toDelete = null;

            if (isDeleteAll)
                toDelete = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);

            if (isDeleteMany)
            {
                int[] indexes = GetInts(trimMsg.Substring(DeleteMany.Length + 1));
                var subscriptions = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);
                if (indexes == null || indexes.Any(i => i > subscriptions.Length - 1 || i < 0))
                {
                    return new TgCommandResponse("Произошла ошибка при удаление подписок. Не правильный индекс подписки.");
                }

                toDelete= subscriptions.Select((s, i) => new {idx = i, subscription = s})
                    .Where(d => indexes.Contains(d.idx)).Select(d => d.subscription).ToArray();
            }

            if (null == toDelete) // no command
            {
                return new TgCommandResponse(null);
            }

            bool result = await subscriptionRepository.DeleteRange(toDelete);

            return result
                ? new TgCommandResponse("Ваши подписки были успешно удалены.") {NeedToRepeat = isDeleteMany}
                : new TgCommandResponse("Произошла ошибка при удалении подписок.");
        }

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            using var subscriptionRepository = DbService.GetSubscriptionRepository();
            SubscriptionEntity[] subscriptions = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);

            using var playbillRepository = DbService.GetPlaybillRepository();

            if (!subscriptions.Any())
                Task.FromResult<ITgCommandResponse>(new TgCommandResponse("У вас нет подписок."));

            List<KeyboardButton> buttons = new List<KeyboardButton>();

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Ваши подписки:");

            for (int i = 0; i < subscriptions.Length; ++i)
            {
                var filter = subscriptions[i].PerformanceFilter;
                var changesDescription = subscriptions[i].TrackingChanges.GetTrackingChangesDescription().ToLower();

                if (filter.PerformanceId == -1)
                {
                    string monthName = culture.DateTimeFormat.GetMonthName(filter.StartDate.Month);

                    string days = filter.DaysOfWeek == null
                        ? "любой день недели"
                        : string.Join(" или ", filter.DaysOfWeek.Select(d => culture.DateTimeFormat.GetDayName(d)));

                    string types = filter.PerformanceTypes == null
                        ? "все представления"
                        : $"{string.Join("или ", filter.PerformanceTypes)}";

                    stringBuilder.AppendLine($" {i+1}. {monthName} {filter.StartDate.Year}, {types} {days} {changesDescription}");

                    buttons.Add(new KeyboardButton($"Удалить {i+1}"));
                }
                else
                {
                    var playbillEntry = playbillRepository.GetWithName(filter.PerformanceId);
                    var date = playbillEntry.When.AddHours(3).ToString("g", culture);
                    stringBuilder.AppendLine($" {i+1}. {playbillEntry.Performance.Name} {date} {changesDescription}");

                    buttons.Add(new KeyboardButton($"Удалить {i+1}"));
                }
            }

            stringBuilder.AppendLine(" Что бы удалить несколько подписок напишите текстом Удалить и номера через запятую, например Удалить 1,2,3");

            return Task.FromResult<ITgCommandResponse>(new TgCommandResponse($"{stringBuilder}", new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(ButtonsInLine, buttons, new []{ new KeyboardButton(DeleteAll), new KeyboardButton(NothingTodo), }),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            }));
        }
    }
}
