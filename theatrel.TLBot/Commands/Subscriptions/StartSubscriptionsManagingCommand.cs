using System;
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
        private const string MainCommandKey = "/subscriptions";
        private static readonly string[] StartCommandVariants
            = { "подписк", "редактировать", "unsubscribe", "отписаться" };

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Подписки";

        public StartSubscriptionsManagingCommand(IDbService dbService) : base(dbService)
        {
        }

        public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) =>
            string.Compare(MainCommandKey, message, StringComparison.InvariantCultureIgnoreCase) == 0 
            || StartCommandVariants.Any(variant => message.ToLower().Contains(variant));

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse("Управление подписками."));
    }
}
