using Autofac;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataUpdater.Tests.TestSettings;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterUpdateTest : DataUpdaterTestBase
    {
        public DataUpdaterUpdateTest(DatabaseFixture fixture) : base(fixture)
        {
        }

        private async Task<int> ConfigureDb(AppDbContext dbContext, string url, DateTime performanceDateTime)
        {
            DateTime dt1 = performanceDateTime.AddDays(-3);
            DateTime dt2 = performanceDateTime.AddDays(-2);

            var playbillEntryWithDecreasedPrice = new PlaybillEntity
            {
                Performance = new PerformanceEntity
                {
                    Name = "TestOpera",
                    Location = new LocationsEntity { Name = "TestLocation" },
                    Type = new PerformanceTypeEntity { TypeName = "Opera" }

                },
                When = performanceDateTime,
                Url = url,
                Changes = new List<PlaybillChangeEntity>
                {
                    new PlaybillChangeEntity
                    {
                        LastUpdate = dt1,
                        MinPrice = 800,
                        ReasonOfChanges =(int)ReasonOfChanges.StartSales,
                    },
                    new PlaybillChangeEntity
                    {
                        LastUpdate = dt2,
                        MinPrice = 500,
                        ReasonOfChanges =(int)ReasonOfChanges.PriceDecreased,
                    }
                }
            };

            dbContext.Playbill.Add(playbillEntryWithDecreasedPrice);
            await dbContext.SaveChangesAsync();

            return playbillEntryWithDecreasedPrice.Id;
        }


        [Fact]
        public async void TestUpdate()
        {
            string testPerformanceUrl = "testUrl";
            DateTime performanceDateTime = DateTime.Now;

            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock("testOpera", 300, "testUrl", performanceDateTime, "loc1", "opera")
                }));

            await using var db = Fixture.RootScope.Resolve<IDbService>().GetDbContext();

            await using ILifetimeScope scope = Fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
            });

            int playbillEntryId = await ConfigureDb(db, testPerformanceUrl, performanceDateTime);

            var dataUpdater = scope.Resolve<IDbPlaybillUpdater>();

            //test
            await dataUpdater.UpdateAsync(1, performanceDateTime, performanceDateTime, CancellationToken.None);

            //check
            var changes = db.PlaybillChanges
                .Where(c => c.PlaybillEntityId == playbillEntryId)
                .OrderBy(d => d.LastUpdate);

            Assert.Equal(2, changes.Count());
            Assert.Equal((int)ReasonOfChanges.PriceDecreased, changes.Last().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.StartSales, changes.First().ReasonOfChanges);
        }
    }
}
