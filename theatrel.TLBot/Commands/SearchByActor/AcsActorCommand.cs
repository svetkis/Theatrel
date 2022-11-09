using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByActor;

internal class AcsActorCommand : DialogCommandBase
{
    public override string Name => "Актер";
    protected override string ReturnCommandMessage { get; set; }

    public AcsActorCommand(IDbService dbService) : base(dbService)
    {
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        chatInfo.Actor = message.Trim();
        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse($"{YouSelected} {chatInfo.Actor}"));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => true;

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Напишите имя актера так как оно пишется в афише.");
        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(stringBuilder.ToString(), CommandKeyboardMarkup));
    }
}
