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
        PlaybillEntity Get(IPerformanceData data);
        PlaybillEntity Get(int id);
        public IEnumerable<PlaybillEntity> GetOutdatedList();

        Task<PlaybillEntity> AddPlaybill(IPerformanceData data);
        Task<bool> AddChange(PlaybillEntity entity, PlaybillChangeEntity change);
        Task<bool> Update(PlaybillChangeEntity entity);

        Task<bool> Delete(PlaybillEntity entity);
    }
}
