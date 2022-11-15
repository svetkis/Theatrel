using Autofac;
using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Subscriptions;
using theatrel.Subscriptions.Tests.TestSettings;
using theatrel.TLBot.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace theatrel.Subscriptions.Tests;

public class SubscriptionUpdaterTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SubscriptionUpdaterTest(ITestOutputHelper output, DatabaseFixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    private static async Task<long[]> ConfigureDb(AppDbContext dbContext)
    {
        var tgUser1 = new TelegramUserEntity { Culture = "ru", Id = 103 };

        dbContext.TlUsers.Add(tgUser1);

        await dbContext.SaveChangesAsync();

        return new[] { tgUser1.Id };
    }

    [Fact]
    public async Task ProlongSubscriptionTest()
    {
        Mock<ITgBotService> telegramService = new Mock<ITgBotService>();

        telegramService.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(true));

        var dbService = _fixture.GetDbService();

        await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(telegramService.Object).As<ITgBotService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterModule<SubscriptionModule>();
        });

        try
        {
            var dbContext = dbService.GetDbContext();
            var usersWhoShouldGetMessage = await ConfigureDb(dbContext);
            var subscriptionsUpdaterService = scope.Resolve<ISubscriptionsUpdaterService>();
            var subscriptionsRepository = dbService.GetSubscriptionRepository();

            Environment.SetEnvironmentVariable("AutoProlongFullSubscriptionsUsers", usersWhoShouldGetMessage.First().ToString());

            //test
            bool result = await subscriptionsUpdaterService.ProlongSubscriptions(CancellationToken.None);
            await Task.Delay(500);
            await subscriptionsUpdaterService.ProlongSubscriptions(CancellationToken.None);
            await Task.Delay(1000);
            await subscriptionsUpdaterService.ProlongSubscriptions(CancellationToken.None);
            await Task.Delay(500);
            await subscriptionsUpdaterService.ProlongSubscriptions(CancellationToken.None);

            //check
            Assert.True(result);
            var subscriptions = subscriptionsRepository.GetUserSubscriptions(usersWhoShouldGetMessage.First());
            subscriptions.Count().Should().Be(1);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
            _output.WriteLine(ex.StackTrace);
        }
    }
}
