using System;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.DataUpdater;

public interface IPlaybillCleanUpService : IDIRegistrable
{
    Task<bool> CleanUp();
}