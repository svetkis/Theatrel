using Autofac;
using Moq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest;

internal static class BotTest
{
    private static bool _isSent;
    private static ITgBotProcessor _tgProcessor;
    private static Mock<ITgBotService> _tgBotServiceMock;

    public static async Task Test()
    {
        await using var scope = await Setup();
        {
            foreach (var unused in Enumerable.Range(0, 1))
            {
                await ProcessChats("прИвет", "апрель", "Нет!", "май", "Суббота", "нет", "понедельник", "кОнцерт",
                    "Подписаться на снижение цены");
            }

            _tgProcessor.Stop();
        }

        _tgBotServiceMock = null;
    }

    private static Task<ILifetimeScope> Setup()
    {
        var performanceFilterMock = new Mock<IPerformanceFilter>();

        var filterServiceMock = new Mock<IFilterService>();
        filterServiceMock.Setup(h => h.IsDataSuitable(It.IsAny<int>(), It.IsAny<string>(),It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>()))
            .Returns(() => true);

        filterServiceMock.Setup(h => h.GetFilter(It.IsAny<IChatDataInfo>()))
            .Returns(() => performanceFilterMock.Object);

        _tgBotServiceMock = new Mock<ITgBotService>();
        _tgBotServiceMock.Setup(x => x.Start()).Verifiable();
        _tgBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgCommandResponse>(), It.IsAny<CancellationToken>()))
            .Callback(() => { _isSent = true; });

        ILifetimeScope scope = Bootstrapper.RootScope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(filterServiceMock.Object).As<IFilterService>().AsImplementedInterfaces();
            builder.RegisterModule<TlBotModule>();
        });

        _tgProcessor = scope.Resolve<ITgBotProcessor>();

        _tgProcessor.Start(_tgBotServiceMock.Object, CancellationToken.None);

        return Task.FromResult(scope);
    }

    private static async Task ProcessChats(params string[] commands)
    {
        try
        {
            foreach (string cmd in commands)
            {
                Trace.TraceInformation($"Send message: {cmd}");
                _tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == cmd && m.ChatId == 1));

                while (!_isSent)
                {
                    Trace.TraceInformation("Waiting for response message");
                    await Task.Delay(10);
                }

                _isSent = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}