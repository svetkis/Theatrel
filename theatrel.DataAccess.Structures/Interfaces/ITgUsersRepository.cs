using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;

namespace theatrel.DataAccess.Structures.Interfaces;

public interface ITgUsersRepository : IDIRegistrable, IDisposable
{
    Task<TelegramUserEntity> Get(long userId);

    Task<TelegramUserEntity> Create(long userId, string culture, CancellationToken cancellationToken);

    Task<bool> Delete(TelegramUserEntity userEntity);
    Task<bool> Update(TelegramUserEntity newValue);
}