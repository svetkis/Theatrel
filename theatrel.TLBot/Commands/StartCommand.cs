using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.TLBot.Entities;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class StartCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи", "да" };

        public StartCommand() : base((int)DialogStep.Start)
        {
        }

        public override bool IsMessageReturnToStart(string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override async Task<string> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            try
            {
                await using var db = new ApplicationContext();
                if (!await db.TlUsers.AnyAsync(u => u.Id == chatInfo.ChatId))
                {
                    await db.TlUsers.AddAsync(new TlUser { Id = chatInfo.ChatId });
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            Trace.TraceInformation($"reset chat {chatInfo}");
            chatInfo.Clear();

            return null;
        }

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => "Вас привествует экономный театрал.";
    }
}
