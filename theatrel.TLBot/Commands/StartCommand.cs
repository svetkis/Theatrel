using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands
{
    internal class StartCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи", "да" };

        private readonly AppDbContext _dbContext;

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Старт";

        public StartCommand(AppDbContext dbContext) : base((int)DialogStep.Start)
        {
            _dbContext = dbContext;
        }

        public override bool IsMessageCorrect(string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override async Task<ITlOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            try
            {
                if (!_dbContext.TlUsers.AsNoTracking().Any(u => u.Id == chatInfo.ChatId))
                {
                    _dbContext.TlUsers.Add(new TelegramUserEntity {Id = chatInfo.ChatId, Culture = chatInfo.Culture});
                }
                else
                {
                    _dbContext.TlUsers.First(u => u.Id == chatInfo.ChatId).Culture = chatInfo.Culture;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            Trace.TraceInformation($"reset chat {chatInfo}");
            chatInfo.Clear();

            return new TlOutboundMessage(null);
        }

        public override async Task<ITlOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => new TlOutboundMessage("Вас приветствует экономный театрал.");
    }
}
