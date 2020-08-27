using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands
{
    internal class StartCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { @"/start", "привет", "hi", "hello", "добрый день", "начать", "давай", "поищи", "да" };

        private readonly IDbService _dbService;

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Старт";

        public StartCommand(IDbService dbService) : base((int)DialogStep.Start)
        {
            _dbService = dbService;
        }

        public override bool IsMessageCorrect(string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override Task<ITgOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            Trace.TraceInformation($"reset chat {chatInfo}");
            chatInfo.Clear();

            return Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage(null));
        }

        public override Task<ITgOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage("Вас приветствует экономный театрал."));
    }
}
