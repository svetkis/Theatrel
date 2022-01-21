using Autofac;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Subscriptions;
using theatrel.Subscriptions.Tests.TestSettings;
using theatrel.TLBot.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace theatrel.Subscriptions.Tests;

public class SubscriptionProcessorTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SubscriptionProcessorTest(ITestOutputHelper output, DatabaseFixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    private static async Task<long[]> ConfigureDb(AppDbContext dbContext)
    {
        DateTime dt1 = DateTime.UtcNow.AddDays(-3);
        DateTime dt2 = DateTime.UtcNow.AddDays(-2);
        DateTime dt3 = DateTime.UtcNow.AddDays(-1);

        DateTime performanceDateTime = DateTime.UtcNow.AddDays(10);

        var tgUser1 = new TelegramUserEntity { Culture = "ru", Id = 103 };
        var tgUser2 = new TelegramUserEntity { Culture = "ru", Id = 205 };
        var tgUser3 = new TelegramUserEntity { Culture = "ru", Id = 1008 };

        dbContext.TlUsers.Add(tgUser1);
        dbContext.TlUsers.Add(tgUser2);

        var playbillEntryWithDecreasedPrice = new PlaybillEntity()
        {
            Performance = new PerformanceEntity
            {
                Name = "TestOpera",
                Location = new LocationsEntity("TestLocation"),
                Type = new PerformanceTypeEntity("Opera")
            },
            When = performanceDateTime,
            Changes = new List<PlaybillChangeEntity>(new[]
            {
                new PlaybillChangeEntity
                {
                    LastUpdate = dt1,
                    MinPrice = 700,
                    ReasonOfChanges = (int) ReasonOfChanges.Creation,
                },
                new PlaybillChangeEntity
                {
                    LastUpdate = dt3,
                    MinPrice = 500,
                    ReasonOfChanges = (int) ReasonOfChanges.PriceDecreased,
                }
            })
        };

        dbContext.Playbill.Add(playbillEntryWithDecreasedPrice);

        //subscription for tgUser1 for particular performance
        dbContext.Subscriptions.Add(new SubscriptionEntity
        {
            TelegramUserId = tgUser1.Id,
            LastUpdate = dt2,
            PerformanceFilter = new PerformanceFilterEntity { PlaybillId = playbillEntryWithDecreasedPrice.PerformanceId },
            TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
        });

        //subscription user2 for filter by date
        dbContext.Subscriptions.Add(new SubscriptionEntity
        {
            TelegramUserId = tgUser2.Id,
            LastUpdate = dt2,
            PerformanceFilter = new PerformanceFilterEntity { StartDate = performanceDateTime.AddDays(-1), EndDate = performanceDateTime.AddDays(2) },
            TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
        });

        //subscription for user2 for particular performance with events price increased and start sales
        dbContext.Subscriptions.Add(new SubscriptionEntity
        {
            TelegramUserId = tgUser3.Id,
            LastUpdate = dt2,
            PerformanceFilter = new PerformanceFilterEntity { PlaybillId = playbillEntryWithDecreasedPrice.PerformanceId },
            TrackingChanges = (int)(ReasonOfChanges.PriceIncreased | ReasonOfChanges.StartSales)
        });

        await dbContext.SaveChangesAsync();

        return new[] { tgUser1.Id, tgUser2.Id };
    }

    [Fact]
    public async Task Test()
    {
        Mock<ITgBotService> telegramService = new Mock<ITgBotService>();

        telegramService.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(true));


        await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(telegramService.Object).As<ITgBotService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterModule<SubscriptionModule>();
        });

        try
        {
            var usersWhoShouldGetMessage = await ConfigureDb(_fixture.GetDbService().GetDbContext());
            var subscriptionProcessor = scope.Resolve<ISubscriptionProcessor>();

            //test
            bool result = await subscriptionProcessor.ProcessSubscriptions();

            //check
            Assert.True(result);
            telegramService.Verify(x => x.SendMessageAsync(It.Is<long>(id
                    => usersWhoShouldGetMessage.Contains(id)), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
            _output.WriteLine(ex.StackTrace);
        }
    }
}