using Autofac;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Subscriptions;
using theatrel.Subscriptions.Tests.TestSettings;
using Xunit;
using Xunit.Abstractions;

namespace theatrel.Subscriptions.Tests
{
    [Collection("Subscriptions Service Tests")]
    public class SubscriptionsServiceTest : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;

        public SubscriptionsServiceTest(ITestOutputHelper output, DatabaseFixture fixture)
        {
            _output = output;
            _fixture = fixture;
        }

        private void ConfigureDb(AppDbContext dbContext, params int[] months)
        {
            var tgUser1 = new TelegramUserEntity { Culture = "ru" };
            var tgUser2 = new TelegramUserEntity { Culture = "ru" };

            dbContext.TlUsers.Add(tgUser1);
            dbContext.TlUsers.Add(tgUser2);

            var playBillEntryWithDecreasedPrice = new PlaybillEntity
            {
                Performance = new PerformanceEntity
                {
                    Name = "TestBallet",
                    Location = new LocationsEntity("Room2"),
                    Type = new PerformanceTypeEntity("Ballet")
                },
                Url = "TestUrl",
                When = new DateTime(2020, months.First(), 1),
            };

            dbContext.Playbill.Add(playBillEntryWithDecreasedPrice);

            //subscription for tgUser1 for particular performance
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser1.Id,
                LastUpdate = DateTime.Now,
                PerformanceFilter = new PerformanceFilterEntity { PerformanceId = playBillEntryWithDecreasedPrice.PerformanceId },
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
        [InlineData(3, 5, 1, 7, 4, 3, 5)]
        [InlineData(3, 6, 9)]
        public async Task Test(params int[] months)
        {
            await using AppDbContext db = _fixture.GetDb();
            ConfigureDb(db, months);

            var dbService = new Mock<IDbService>();
            dbService.Setup(x => x.GetDbContext()).Returns(db);

            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(dbService.Object).AsImplementedInterfaces().AsSelf();
                builder.RegisterModule<SubscriptionModule>();
            });

            var service = scope.Resolve<ISubscriptionService>();

            //test
            var filters = service.GetUpdateFilters();

            //check
            Assert.NotNull(filters);

            var sortedMonths = months.Distinct().OrderBy(x => x).ToArray();
            var sortedMonthsFromFilter = filters.Select(f => f.StartDate.Month).OrderBy(x => x).ToArray();
            _output.WriteLine(string.Join(" ", sortedMonths));
            _output.WriteLine(string.Join(" ", sortedMonthsFromFilter));

            Assert.Equal(sortedMonths, sortedMonthsFromFilter);
        }
    }
}
