using Autofac;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common.Enums;
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

        public IDbService ConfigureDbService(params int[] months)
        {
            List<SubscriptionEntity> subscriptionEntities = new List<SubscriptionEntity>{new SubscriptionEntity
            {
                TelegramUserId = 1,
                LastUpdate = DateTime.Now.AddDays(-1),
                PerformanceFilter = new PerformanceFilterEntity { PlaybillId = 100 },
                TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
            }};

            foreach (var month in months.Skip(1))
            {
                DateTime startDate = new DateTime(2020, month, 1);
                DateTime endDateTime = startDate.AddMonths(1);

                subscriptionEntities.Add(new SubscriptionEntity
                {
                    TelegramUserId = 2,
                    LastUpdate = DateTime.Now,
                    PerformanceFilter = new PerformanceFilterEntity { StartDate = startDate, EndDate = endDateTime },
                    TrackingChanges = (int)(ReasonOfChanges.PriceDecreased | ReasonOfChanges.StartSales)
                });
            }

            var subscriptionRepo = new Mock<ISubscriptionsRepository>();
            subscriptionRepo.Setup(x => x.GetAllWithFilter()).Returns(subscriptionEntities);

            var playbillRepo = new Mock<IPlaybillRepository>();
            playbillRepo.Setup(x => x.Get(It.IsAny<int>())).Returns(new PlaybillEntity()
            {
                When = new DateTime(2020, months.First(), 1)
            });

            var dbService = new Mock<IDbService>();
            dbService.Setup(x => x.GetSubscriptionRepository()).Returns(subscriptionRepo.Object);
            dbService.Setup(x => x.GetPlaybillRepository()).Returns(playbillRepo.Object);

            return dbService.Object;
        }

        [Theory]
        [InlineData(3, 5, 1, 7, 4, 3, 5)]
        [InlineData(3, 6, 9)]
        public async Task SubscriptionServiceTest(params int[] months)
        {
            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(ConfigureDbService(months)).As<IDbService>().AsImplementedInterfaces().SingleInstance();
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
                    LastUpdate = DateTime.Now,
                    PerformanceFilter = new PerformanceFilterEntity
                    {
                        PlaybillId = -1,
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
            Assert.Equal(12, filters.First().EndDate.Month);
        }
    }
}
