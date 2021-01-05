using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common.FormatHelper;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot
{
    internal class TgBotService : ITgBotService
    {
        private readonly ITelegramBotClient _botClient;

        public event EventHandler<ITgInboundMessage> OnMessage;

        public TgBotService()
        {
            try
            {
                _botClient = new TelegramBotClient(BotSettings.BotToken, new HttpClient());
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Start TgBotService");

            _botClient.OnMessage += BotOnMessage;
            _botClient.OnCallbackQuery += BotOnCallbackQuery;

            if (_botClient.IsReceiving)
                return;

            Trace.TraceInformation("TLBot StartReceiving");
            _botClient.StartReceiving(new[] { UpdateType.Message }, cancellationToken);
        }

        public void Stop()
        {
            if (!_botClient.IsReceiving)
                return;

            Trace.TraceInformation("TLBot StopReceiving");
            _botClient.StopReceiving();
        }

        private void BotOnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
        }

        private void BotOnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Text == null) //as example messages about adding bot to channel
                return;

            OnMessage?.Invoke(sender, new TgInboundMessage {ChatId = e.Message.Chat.Id, Message = e.Message.Text.Trim()});
        }

        public async Task<bool> SendMessageAsync(long chatId, ITgOutboundMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message.Message))
                return true;

            char[] toLog = message.Message?.Take(100).ToArray();
            Trace.TraceInformation($"SendMessage id: {chatId} msg: {new string(toLog)}...");

            try
            {
                Chat chat = await _botClient.GetChatAsync(chatId, cancellationToken);

                IReplyMarkup replyMarkup = chat.Type == ChatType.Private
                    ? (IReplyMarkup)message.ReplyKeyboard ?? new ReplyKeyboardRemove()
                    : null;

                foreach (string msg in SplitMessage(message.IsEscaped ? message.Message : message.Message.EscapeMessageForMarkupV2()))
                {
                    await Policy
                        .Handle<HttpRequestException>()
                        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                        .ExecuteAsync(async () =>
                        {
                            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
                            await _botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.MarkdownV2,
                                replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                        });
                }
            }
            catch (Exception exception)
            {
                Trace.TraceInformation($"SendMessage: {chatId} {message} failed. Exception {exception.Message}{Environment.NewLine}{exception.StackTrace}");
                return false;
            }

            return true;
        }

        public async Task<bool> SendMessageAsync(long chatId, string message, CancellationToken cancellationToken)
            => await SendMessageAsync(chatId, new TgOutboundMessage(message), cancellationToken);

        public async Task<bool> SendEscapedMessageAsync(long chatId, string message, CancellationToken cancellationToken)
            => await SendMessageAsync(chatId, new TgOutboundMessage(message) { IsEscaped = true }, cancellationToken);

        private const int MaxMessageSize = 4096;
        private static string[] SplitMessage(string message)
        {
            if (message.Length < MaxMessageSize || string.IsNullOrEmpty(message))
                return new[] { message };

            string[] lines = message.Split(Environment.NewLine);
            List<StringBuilder> messages = new List<StringBuilder> { new StringBuilder(lines.First()) };

            foreach (var line in lines.Skip(1))
            {
                if (messages.Last().Length + line.Length >= MaxMessageSize - Environment.NewLine.Length)
                {
                    messages.Add(new StringBuilder(line));
                    continue;
                }

                messages.Last().Append(Environment.NewLine);
                messages.Last().Append(line);
            }

            return messages.Select(sb => sb.ToString()).ToArray();
        }
    }
}
