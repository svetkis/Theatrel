using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace theatrel.Subscriptions.Tests
{
    public class SubscriptionProcessorTest
    {
        private readonly ITestOutputHelper _output;

        public SubscriptionProcessorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private async Task<long[]> ConfigureDb(AppDbContext dbContext)
        {
            DateTime dt1 = DateTime.Now.AddDays(-3);
            DateTime dt2 = DateTime.Now.AddDays(-2);
            DateTime dt3 = DateTime.Now.AddDays(-1);

            DateTime performanceDateTime = DateTime.Now.AddDays(10);

            var tgUser1 = new TelegramUserEntity { Culture = "ru"};
            var tgUser2 = new TelegramUserEntity { Culture = "ru" };
            var tgUser3 = new TelegramUserEntity { Culture = "ru" };

            dbContext.TlUsers.Add(tgUser1);
            dbContext.TlUsers.Add(tgUser2);

            var performanceWithDecreasedPrice = new PerformanceEntity
            {
                Name = "TestOpera",
                DateTime = performanceDateTime,
                Changes = new List<PerformanceChangeEntity>(new[]
                {
                    new PerformanceChangeEntity
                    {
                        LastUpdate = dt1,
                        MinPrice = 700,
                        ReasonOfChanges = (int) ReasonOfChanges.Creation,
                    },
                    new PerformanceChangeEntity
                    {
                        LastUpdate = dt3,
                        MinPrice = 500,
                        ReasonOfChanges = (int) ReasonOfChanges.PriceDecreased,
                    }
                })
            };

            dbContext.Performances.Add(performanceWithDecreasedPrice);

            //subscription for tgUser1 for particular performance
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser1.Id,
                LastUpdate = dt2,
                PerformanceFilter = new PerformanceFilterEntity {PerformanceId = performanceWithDecreasedPrice.Id},
                TrackingChanges = (int)(ReasonOfChanges.PriceDecreased|ReasonOfChanges.StartSales)
            });

            //subscription user2 for filter by date
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser2.Id,
                LastUpdate = dt2,
                PerformanceFilter = new PerformanceFilterEntity { StartDate = performanceDateTime.AddDays(-1), EndDate = performanceDateTime.AddDays(2)},
                TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
            });

            //subscription for user2 for particular performance with events price increased and start sales
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser3.Id,
                LastUpdate = dt2,
                PerformanceFilter = new PerformanceFilterEntity { PerformanceId = performanceWithDecreasedPrice.Id },
                TrackingChanges = (int)(ReasonOfChanges.PriceIncreased | ReasonOfChanges.StartSales)
            });

            await dbContext.SaveChangesAsync();

            return new[] {tgUser1.Id, tgUser2.Id};
        }

        [Fact]
        public async Task Test()
        {
            Mock<ITLBotService> telegramService = new Mock<ITLBotService>();

            telegramService.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(true));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(telegramService.Object).As<ITLBotService>().AsImplementedInterfaces().SingleInstance();
            });

            try
            {
                var db = scope.Resolve<AppDbContext>();
                var usersWhoShouldGetMessage = await ConfigureDb(db);

                var subscriptionProcessor = new SubscriptionProcessor(telegramService.Object, scope.Resolve<IFilterChecker>(), db);

                //test
                bool result = await subscriptionProcessor.ProcessSubscriptions();

                //check
                Assert.True(result);
                telegramService.Verify(x => x.SendMessageAsync(It.Is<long>(id
                        => usersWhoShouldGetMessage.Contains(id)), It.IsAny<string>()),
                    Times.Exactly(2));
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.Message);
                _output.WriteLine(ex.StackTrace);
            }
        }
    }
}
