using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.IntroduceBot
{
    internal class IntroduceMyself : DialogCommandBase
    {
        private readonly string _introduceString = "Добрый день! Вас приветствует Театрал-бот." +
                                         $" Я помогу вам подобрать билеты в Мариинский театр или подписаться на снижение цены и появление билетов в продаже.{Environment.NewLine}" +
                                         $" Список команда бота:{Environment.NewLine}" +
                                         $" /help - показать справку{Environment.NewLine}" +
                                         $" /search - искать с фильтром по дате и типу представления или просто напишите мне Привет!{Environment.NewLine}"+
                                         $" /search2 - искать по имени{Environment.NewLine}" +
                                         $" /subscriptions  управление подписками";

        public override string Name => "IntroduceMyself";
        protected override string ReturnCommandMessage { get; set; }

        public IntroduceMyself(IDbService dbService) : base(dbService)
        {
            var buttonsLine1 = new[]
            {
                new KeyboardButton("/search"),
                new KeyboardButton("/search2"),
            };

            var buttonsLine2 = new[]
            {
                new KeyboardButton("/subscriptions"),
                new KeyboardButton("Ok"),
            };

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(ButtonsInLine, buttonsLine1, buttonsLine2),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => true;

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(_introduceString, CommandKeyboardMarkup));
        }
    }
}
