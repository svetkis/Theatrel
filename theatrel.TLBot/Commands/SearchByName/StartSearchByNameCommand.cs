using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByName
{
    internal class StartSearchByNameCommand : DialogCommandBase
    {
        private const string MainCommandKey = "/search2";

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "StartSearchByName";

        public StartSearchByNameCommand(IDbService dbService) : base(dbService)
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
