using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Moq;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using Xunit;

namespace theatrel.Subscriptions.Tests
{
    public class SubscriptionTest
    {
        private async Task ConfigureDb(AppDbContext dbContext, long userId)
        {
            DateTime dt1 = DateTime.Now.AddDays(-3);
            DateTime dt2 = DateTime.Now.AddDays(-2);
            DateTime dt3 = DateTime.Now.AddDays(-1);

            dbContext.TlUsers.Add(new TelegramUserEntity { Id = userId, Culture = "ru" });
            dbContext.TlUsers.Add(new TelegramUserEntity { Id = userId+1, Culture = "ru" });

            var performance = new PerformanceEntity
            {
                Name = "TestOpera",
                DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                MinPrice = 500,
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

            dbContext.Performances.Add(performance);

            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = userId,
                LastUpdate = dt2,
                PerformanceFilter = new PerformanceFilterEntity {PerformanceId = performance.Id},
                TrackingChanges = (int)(ReasonOfChanges.PriceDecreased|ReasonOfChanges.StartSales)
            });

            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = userId+1,
                LastUpdate = dt2,
                PerformanceFilter = new PerformanceFilterEntity { PerformanceId = performance.Id },
                TrackingChanges = (int)(ReasonOfChanges.PriceIncreased | ReasonOfChanges.StartSales)
            });

            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task Test()
        {
            Mock<ITLBotService> telegramService = new Mock<ITLBotService>();

            telegramService.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(true));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(telegramService.Object).As<ITLBotService>().AsImplementedInterfaces();
            });

            var db = scope.Resolve<AppDbContext>();
            await ConfigureDb(db, 1);

            //var subscriptionProcessor = scope.Resolve<ISubscriptionProcessor>();
            var subscriptionProcessor =
                new SubscriptionProcessor(telegramService.Object, scope.Resolve<IFilterChecker>(), db);

            //test
            bool result = await subscriptionProcessor.ProcessSubscriptions();

            //check
            Assert.True(result);
            telegramService.Verify(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Once);
        }
    }
}
