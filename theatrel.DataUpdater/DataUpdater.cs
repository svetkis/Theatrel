using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;

namespace theatrel.DataUpdater
{
    public class DataUpdater : IDataUpdater
    {
        private readonly IPlayBillDataResolver _dataResolver;
        private readonly AppDbContext _dbContext;

        public DataUpdater(IPlayBillDataResolver dataResolver, AppDbContext dbContext)
        {
            _dataResolver = dataResolver;
            _dbContext = dbContext;
        }

        public async Task<bool> UpdateAsync(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Get saved performances from db");
            PerformanceEntity[] savedPerformances =
                _dbContext.Performances
                    .Where(p => p.PerformanceDateTime >= startDate && p.PerformanceDateTime <= endDate).ToArray();

            Trace.TraceInformation("Request new data");
            IPerformanceData[] performances = await _dataResolver.RequestProcess(startDate, endDate, null, cancellationToken);
            foreach (var freshPerformanceData in performances)
            {
                Trace.TraceInformation($"Process {freshPerformanceData.Name}");
                var performance = savedPerformances.FirstOrDefault(p =>
                    string.Compare(p.Url, freshPerformanceData.Url, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (performance == null)
                {
                    _dbContext.Performances.Add(CreatePerformanceEntity(freshPerformanceData));
                    Trace.TraceInformation($"Performance {freshPerformanceData.Url} was added to database");
                }
                else
                {
                    PerformanceChangeEntity lastChange = performance.Changes.OrderByDescending(x => x.LastUpdate)
                        .FirstOrDefault();

                    var compareResult = ComparePerformanceData(lastChange, freshPerformanceData);
                    if (compareResult == ReasonOfChangesEnum.NoReason)
                    {
                        if (lastChange != null)
                            lastChange.LastUpdate = DateTime.Now;

                        continue;
                    }

                    performance.Changes.Add(CreatePerformanceChangeEntity(freshPerformanceData.Tickets.GetMinPrice(), compareResult));
                    Trace.TraceInformation($"Performance change information added {freshPerformanceData.Url}");
                }
            }

            Trace.TraceInformation("Save Changes to db");
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private PerformanceChangeEntity CreatePerformanceChangeEntity(int minPrice, ReasonOfChangesEnum reason)
        {
            return new PerformanceChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = minPrice,
                ReasonOfChanges = (int)reason,
            };
        }

        private PerformanceEntity CreatePerformanceEntity(IPerformanceData data) =>
            new PerformanceEntity
            {
                PerformanceDateTime = data.DateTime,
                Url = data.Url,
                Changes = new List<PerformanceChangeEntity>
                {
                    new PerformanceChangeEntity
                    {
                        LastUpdate = DateTime.Now,
                        MinPrice = data.Tickets.GetMinPrice(),
                        ReasonOfChanges = (int) ReasonOfChangesEnum.Creation,
                    }
                }
            };

        private ReasonOfChangesEnum ComparePerformanceData(PerformanceChangeEntity lastChange, IPerformanceData freshData)
        {
            if (lastChange == null)
                return ReasonOfChangesEnum.Creation;

            int freshMinPrice = freshData.Tickets.GetMinPrice();
            if (lastChange.MinPrice == 0)
                return freshMinPrice == 0 ? ReasonOfChangesEnum.NoReason : ReasonOfChangesEnum.StartSales;

            if (lastChange.MinPrice > freshMinPrice)
                return ReasonOfChangesEnum.PriceDecreased;

            if (lastChange.MinPrice < freshMinPrice)
                return ReasonOfChangesEnum.PriceIncreased;

            return ReasonOfChangesEnum.NoReason;
        }
    }
}
