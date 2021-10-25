using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.FormatHelper;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace theatrel.TLBot
{
    internal class TgBotService : ITgBotService
    {
        private readonly ITelegramBotClient _botClient;
        private CancellationTokenSource _cst;

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

        public void Start()
        {
            Trace.TraceInformation("TgBotService is starting");

            _cst = new CancellationTokenSource();
            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), _cst.Token);

            Trace.TraceInformation("TLBot Started");
        }

        public void Stop()
        {
            _cst.Cancel();
        }

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            switch(update.Type)
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                case UpdateType.Message:
                    BotOnMessageReceived(botClient, update.Message);
                    break;
                case UpdateType.EditedMessage:
                    BotOnMessageReceived(botClient, update.EditedMessage);
                    break;
                //UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery),
                case UpdateType.InlineQuery:
                    //BotOnInlineQueryReceived(botClient, update.InlineQuery);
                    break;
                //UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult),
            };

            return Task.FromResult(true);
        }

        private static async Task BotOnInlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };

            await botClient.AnswerInlineQueryAsync(
                inlineQueryId: inlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        private void BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == null) //as example messages about adding bot to channel
                return;

            OnMessage?.Invoke(botClient, new TgInboundMessage { ChatId = message.Chat.Id, Message = message.Text.Trim() });
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
                if (exception.Message.Contains("Forbidden: bot was blocked by the user"))
                {
                    //ToDo clean up DB
                    Trace.TraceInformation($"SendMessage: {chatId} failed. Bot was blocked by the user.");
                    return true;
                }

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
