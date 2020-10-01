using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataAccess.Structures.Interfaces
{
    public interface IPlaybillRepository : IDisposable, IDIRegistrable
    {
        IEnumerable<PlaybillEntity> GetList(DateTime from, DateTime to);
        IEnumerable<PlaybillEntity> GetListByName(string name);

        PlaybillEntity Get(IPerformanceData data);
        PlaybillEntity Get(int id);
        PlaybillEntity GetWithName(int id);

        IEnumerable<PlaybillEntity> GetOutdatedList();

        Task<PlaybillEntity> AddPlaybill(IPerformanceData data);
        Task<bool> AddChange(PlaybillEntity entity, PlaybillChangeEntity change);
        Task<bool> UpdateChangeLastUpdate(int changeId);
        Task<bool> UpdateUrl(int playbillEntityId, string url);
        Task<bool> UpdateTicketsUrl(int playbillEntityId, string url);

        Task<bool> UpdateCast(PlaybillEntity playbillEntry, IPerformanceData data);
        bool IsCastEqual(PlaybillEntity playbillEntry, IPerformanceData data);

        Task<bool> Delete(PlaybillEntity entity);

        IEnumerable<LocationsEntity> GetLocationsList();
    }
}
