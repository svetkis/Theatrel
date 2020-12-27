using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.IntroduceBot
{
    internal class IntroduceStart : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants = { @"/start", "/help" };

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Introduce myself";

        public IntroduceStart(IDbService dbService) : base(dbService)
        {
        }

        public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => StartCommandVariants.Any(variant => message.ToLower().StartsWith(variant));

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse("Вас приветствует экономный театрал."));
    }

}
