using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using Xunit;

namespace theatrel.Subscriptions.Tests
{
    public class SubscriptionsServiceTest
    {
        private void ConfigureDb(params int[] months)
        {
            var dbContext = DIContainerHolder.RootScope.Resolve<AppDbContext>();

            DateTime performanceDateTime = new DateTime(2020, months.First(), 1);

            var tgUser1 = new TelegramUserEntity { Culture = "ru" };
            var tgUser2 = new TelegramUserEntity { Culture = "ru" };

            dbContext.TlUsers.Add(tgUser1);
            dbContext.TlUsers.Add(tgUser2);

            var playBillEntryWithDecreasedPrice = new PlaybillEntity
            {
                Performance = new PerformanceEntity()
                {
                    Name = "TestBallet",
                    Location = new LocationsEntity("Room2"),
                    Type = new PerformanceTypeEntity("Ballet")
                },
                Url = "TestUrl",
                When = performanceDateTime,
            };

            dbContext.Playbill.Add(playBillEntryWithDecreasedPrice);

            //subscription for tgUser1 for particular performance
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser1.Id,
                LastUpdate = DateTime.Now,
                PerformanceFilter = new PerformanceFilterEntity { PerformanceId = playBillEntryWithDecreasedPrice.PerformanceId},
                TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
            });

            foreach (var month in months.Skip(1))
            {
                DateTime startDate = new DateTime(2020, month, 1);
                DateTime endDateTime = startDate.AddMonths(1).AddDays(-1);
                //subscription user2 for filter by date
                dbContext.Subscriptions.Add(new SubscriptionEntity
                {
                    TelegramUserId = tgUser2.Id,
                    LastUpdate = DateTime.Now,
                    PerformanceFilter = new PerformanceFilterEntity { StartDate = startDate, EndDate = endDateTime },
                    TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
                });
            }

            dbContext.SaveChanges();
        }

        [Theory]
        [InlineData(3, 5, 6, 7, 4)]
        [InlineData(3, 5, 6, 7, 4, 3, 5)]
        public void Test(params int[] months)
        {
            ConfigureDb(months);

            var service = DIContainerHolder.RootScope.Resolve<ISubscriptionService>();

            //test
            var filters = service.GetUpdateFilters();

            //check
            Assert.NotNull(filters);
            Assert.Equal(months.Distinct().OrderBy(x => x).ToArray(), filters.Select(f => f.StartDate.Month).OrderBy(x => x));
        }
    }

}
