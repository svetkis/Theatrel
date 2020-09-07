using Autofac;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
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

            var playBillEntry = new PlaybillEntity
            {
                Performance = new PerformanceEntity
                {
                    Name = "TestBallet",
                    Location = new LocationsEntity("Room2"),
                    Type = new PerformanceTypeEntity("Ballet")
                },
                Url = "TestUrl",
                When = new DateTime(2020, months.First(), 1),
                Changes = new List<PlaybillChangeEntity>{
                    new PlaybillChangeEntity
                    {
                        LastUpdate = DateTime.Now,
                        MinPrice = 800,
                        ReasonOfChanges = 8
                    },
                    new PlaybillChangeEntity
                    {
                        LastUpdate = DateTime.Now,
                        MinPrice = 500,
                        ReasonOfChanges = 2
                    }
                }
            };

            dbContext.Playbill.Add(playBillEntry);

            //subscription for tgUser1 for particular performance
            dbContext.Subscriptions.Add(new SubscriptionEntity
            {
                TelegramUserId = tgUser1.Id,
                LastUpdate = DateTime.Now.AddDays(-1),
                PerformanceFilter = new PerformanceFilterEntity { PerformanceId = playBillEntry.PerformanceId },
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
        public async Task SubscriptionServiceTest(params int[] months)
        {
            ConfigureDb(_fixture.GetDbService().GetDbContext(), months);

            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
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

        [Fact]
        public async Task SubscriptionServiceTest2()
        {
            var subscriptionRepo = new Mock<ISubscriptionsRepository>();
            subscriptionRepo.Setup(x => x.GetAllWithFilter()).Returns(new List<SubscriptionEntity>
            {
                new SubscriptionEntity
                {
                    TelegramUserId = 1,
                    LastUpdate = DateTime.Now.AddDays(-1),
                    PerformanceFilter = new PerformanceFilterEntity
                    {
                        PerformanceId = -1,
                        StartDate = new DateTime(2020,9,1),
                        EndDate = new DateTime(2020,11,15)
                    },
                    TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
                }
            });

            var dbService = new Mock<IDbService>();
            dbService.Setup(x => x.GetSubscriptionRepository()).Returns(subscriptionRepo.Object);


            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(dbService.Object).As<IDbService>().AsImplementedInterfaces().SingleInstance(); 
                builder.RegisterModule<SubscriptionModule>();
            });


            var service = scope.Resolve<ISubscriptionService>();

            //test
            var filters = service.GetUpdateFilters();

            //check
            Assert.NotNull(filters);

            Assert.Equal(9, filters.First().StartDate.Month);
            Assert.Equal(11, filters.First().EndDate.Month);
        }

    }
}
