using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;

namespace theatrel.DataAccess.Structures.Interfaces;

public interface ITgChatsRepository : IDIRegistrable, IDisposable
{
    Task<ChatInfoEntity> Get(long chatId);

    Task<ChatInfoEntity> Create(long chatId, string culture, CancellationToken cancellationToken);

    Task<bool> Delete(ChatInfoEntity chatData);
    Task<bool> Update(ChatInfoEntity newValue);
}