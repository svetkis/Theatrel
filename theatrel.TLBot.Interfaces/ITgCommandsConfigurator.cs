using theatrel.Interfaces.Autofac;

namespace theatrel.TLBot.Interfaces;

public interface ITgCommandsConfigurator : IDIRegistrable
{
    IDialogCommand[][] GetDialogCommands();
}