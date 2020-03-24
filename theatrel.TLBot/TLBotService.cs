using Polly;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class TLBotService : ITLBotService
    {
        private readonly ITelegramBotClient _botClient;

        public event EventHandler<ITLMessage> OnMessage;

        public TLBotService()
        {
            try
            {
                var Proxy = string.IsNullOrEmpty(ThSettings.Config.BotProxy)
                    ? null
                    : new WebProxy(ThSettings.Config.BotProxy, ThSettings.Config.BotProxyPort) { UseDefaultCredentials = true };

                _botClient = Proxy != null ? new TelegramBotClient(ThSettings.Config.BotToken, Proxy) : new TelegramBotClient(ThSettings.Config.BotToken);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }
        }

        public void Start()
        {
            Trace.TraceInformation("Start TLBotService");

            _botClient.OnMessage += BotOnMessage;
            _botClient.OnCallbackQuery += BotOnCallbackQuery;

            if (_botClient.IsReceiving)
                return;

            Trace.TraceInformation("TLBot StartReceiving");
            _botClient.StartReceiving();
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
            //throw new NotImplementedException();
        }

        private void BotOnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
            => OnMessage?.Invoke(sender, new TLMessage() { ChatId = e.Message.Chat.Id, Message = e.Message.Text.Trim() });

        public async void SendMessageAsync(long chatId, string message)
        {
            Trace.TraceInformation($"SendMessage id: {chatId} msg: {new string(message.Take(100).ToArray())}...");
            try
            {
                await Policy
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(async () =>
                    {
                        await _botClient.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                        await _botClient.SendTextMessageAsync(chatId, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    });
            }
            catch (HttpRequestException ex)
            {
                Trace.TraceInformation("SendMessage: {chatId} {message} failed");
            }
        }
    }
}
