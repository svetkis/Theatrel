using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByActor
{
    internal class StartSearchByActorCommand : DialogCommandBase
    {
        private const string MainCommandKey = "/actor";

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "StartSearchByActor";

        public StartSearchByActorCommand(IDbService dbService) : base(dbService)
        {
        }

        public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) =>
            string.Compare(MainCommandKey, message, StringComparison.InvariantCultureIgnoreCase) == 0;

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
            => Task.FromResult<ITgCommandResponse>(new TgCommandResponse("Вас приветствует экономный театрал."));
    }
}
