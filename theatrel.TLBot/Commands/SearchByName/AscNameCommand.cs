using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByName;

internal class AscNameCommand : DialogCommandBase
{
    public override string Name => "Название представление";
    protected override string ReturnCommandMessage { get; set; }

    public AscNameCommand(IDbService dbService) : base(dbService)
    {
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        chatInfo.PerformanceName = message.Trim();
        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse($"{YouSelected} {chatInfo.PerformanceName}"));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => true;

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Напишите название или часть названия спектакля.");
        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(stringBuilder.ToString(), CommandKeyboardMarkup));
    }
}