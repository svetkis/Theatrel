using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot
{
    public class TgBotService : ITgBotService
    {
        private readonly ITelegramBotClient _botClient;

        public event EventHandler<ITgInboundMessage> OnMessage;

        public TgBotService()
        {
            try
            {
                WebProxy proxy = string.IsNullOrEmpty(BotSettings.BotProxy)
                    ? null
                    : new WebProxy(BotSettings.BotProxy, BotSettings.BotProxyPort) { UseDefaultCredentials = true };

                _botClient = proxy != null
                    ? new TelegramBotClient(BotSettings.BotToken, proxy)
                    : new TelegramBotClient(BotSettings.BotToken);
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
            => OnMessage?.Invoke(sender, new TgInboundMessage { ChatId = e.Message.Chat.Id, Message = e.Message.Text.Trim() });

        public async Task<bool> SendMessageAsync(long chatId, ITgOutboundMessage message)
        {
            char[] toLog = message.Message?.Take(100).ToArray();
            string msgToLog = toLog == null ? string.Empty : new string(toLog);
            Trace.TraceInformation($"SendMessage id: {chatId} msg: {msgToLog}...");

            try
            {
                IReplyMarkup replyMarkup = message.ReplyKeyboard;
                replyMarkup ??= new ReplyKeyboardRemove();

                foreach (string msg in SplitMessage(message.IsEscaped ? message.Message : message.Message.EscapeMessageForMarkupV2()))
                {
                    await Policy
                        .Handle<HttpRequestException>()
                        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                        .ExecuteAsync(async () =>
                        {
                            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                            await _botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.MarkdownV2, replyMarkup: replyMarkup);
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

        public async Task<bool> SendMessageAsync(long chatId, string message)
            => await SendMessageAsync(chatId, new TgOutboundMessage(message));

        private const int MaxMessageSize = 4096;
        private string[] SplitMessage(string message)
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
