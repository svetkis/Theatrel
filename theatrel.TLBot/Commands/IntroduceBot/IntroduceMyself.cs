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
                                         $" /help           справка по командам{Environment.NewLine}" +
                                         $" /subscriptions  управление подписками{Environment.NewLine}" +
                                         " /search          поиск билетов или просто напишите мне Привет!";

        public override string Name => "IntroduceMyself";
        protected override string ReturnCommandMessage { get; set; }

        public IntroduceMyself(IDbService dbService) : base(dbService)
        {
            var buttons = new[]
            {
                new KeyboardButton("/help"),
                new KeyboardButton("/subscriptions"),
                new KeyboardButton("Ok"),
            };

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(ButtonsInLine, buttons),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override bool IsMessageCorrect(string message) => true;

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(_introduceString, CommandKeyboardMarkup));
        }
    }
}
