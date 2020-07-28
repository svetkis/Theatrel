using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class StartCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи", "да" };

        private readonly AppDbContext _dbContext;

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public StartCommand(AppDbContext dbContext) : base((int)DialogStep.Start)
        {
            _dbContext = dbContext;
        }

        public override bool IsMessageReturnToStart(string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override async Task<ICommandResponse> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _dbContext.TlUsers.AsNoTracking().AnyAsync(u => u.Id == chatInfo.ChatId, cancellationToken))
                {
                    _dbContext.TlUsers.Add(new TlUser { Id = chatInfo.ChatId });
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            Trace.TraceInformation($"reset chat {chatInfo}");
            chatInfo.Clear();

            return new TlCommandResponse(null);
        }

        public override async Task<ICommandResponse> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => new TlCommandResponse("Вас привествует экономный театрал.");
    }
}
