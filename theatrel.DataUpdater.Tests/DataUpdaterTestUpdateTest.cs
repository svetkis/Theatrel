using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTestUpdateTest : DataUpdaterTestBase
    {
        private async Task<int> ConfigureDb(ILifetimeScope scope, string url, DateTime performanceDateTime)
        {
            var dbContext = scope.Resolve<AppDbContext>();

            DateTime dt1 = performanceDateTime.AddDays(-3);
            DateTime dt2 = performanceDateTime.AddDays(-2);

            var playbillEntryWithDecreasedPrice = new PlaybillEntity
            {
                Performance = new PerformanceEntity
                {
                    Name = "TestOpera",
                    Location = new LocationsEntity{Name = "TestLocation"},
                    Type = new PerformanceTypeEntity{TypeName = "Opera"}

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

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
            });

            int playbillEntryId = await ConfigureDb(scope, testPerformanceUrl, performanceDateTime);

            var dataUpdater = scope.Resolve<IDataUpdater>();

            await dataUpdater.UpdateAsync(1, performanceDateTime, performanceDateTime, CancellationToken.None);

            var db = scope.Resolve<AppDbContext>();
            var changes = db.PerformanceChanges
                .Where(c => c.PlaybillEntityId == playbillEntryId)
                .OrderBy(d => d.LastUpdate);

            Assert.Equal(2, changes.Count());
            Assert.Equal((int)ReasonOfChanges.PriceDecreased, changes.Last().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.StartSales, changes.First().ReasonOfChanges);
        }
    }
}
