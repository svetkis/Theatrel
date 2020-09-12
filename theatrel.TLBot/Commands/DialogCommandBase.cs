﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal abstract class DialogCommandBase : IDialogCommand
    {
        protected const string ReturnMsg = "Для того что бы выбрать другой вариант напишите Нет.";

        protected const int ButtonsInLine = 3;

        public int Label { get; }
        public abstract string Name { get; }

        protected abstract string ReturnCommandMessage { get; set; }

        protected readonly ReplyKeyboardMarkup ReturnKeyboardMarkup;
        protected ReplyKeyboardMarkup CommandKeyboardMarkup;

        protected readonly IDbService DbService;

        protected DialogCommandBase(int label, IDbService dbService)
        {
            Label = label;
            DbService = dbService;

            if (!string.IsNullOrWhiteSpace(ReturnCommandMessage))
            {
                ReturnKeyboardMarkup = new ReplyKeyboardMarkup(new[] { new KeyboardButton(ReturnCommandMessage) })
                {
                    OneTimeKeyboard = true,
                    ResizeKeyboard = true
                };
            }
        }

        protected KeyboardButton[][] GroupKeyboardButtons(IEnumerable<KeyboardButton> buttons, int maxCount)
        {
            List<List<KeyboardButton>> groupedButtons = new List<List<KeyboardButton>> { new List<KeyboardButton>() };
            foreach (var keyboardButton in buttons)
            {
                if (groupedButtons.Last().Count >= maxCount)
                    groupedButtons.Add(new List<KeyboardButton>());

                groupedButtons.Last().Add(keyboardButton);
            }

            return groupedButtons.Select(list => list.ToArray()).ToArray();
        }


        protected readonly char[] WordSplitters = { ' ', ',', '.' };

        public abstract bool IsMessageCorrect(string message);

        public abstract Task<ITgOutboundMessage> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken);

        public abstract Task<ITgOutboundMessage> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken);

        public bool IsReturnCommand(string message)
            => string.Equals(message, ReturnCommandMessage, StringComparison.CurrentCultureIgnoreCase);
    }
}
