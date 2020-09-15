using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.Subscriptions
{
    internal class StartSubscriptionsManagingCommand : DialogCommandBase
    {
        private static readonly string[] StartCommandVariants
            = { "/subscriptions", "подписк", "редактировать", "unsubscribe", "отписаться"};

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Подписки";

        public StartSubscriptionsManagingCommand(IDbService dbService) : base((int)DialogStep.Start, dbService)
        {
        }

        public override bool IsMessageCorrect(string message) => StartCommandVariants.Any(variant => message.ToLower().Contains(variant));

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse("Управление подписками."));
    }
}
