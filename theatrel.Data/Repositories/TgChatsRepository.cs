using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;

namespace theatrel.DataAccess.Repositories;

internal class TgChatsRepository : ITgChatsRepository
{
    private AppDbContext _dbContext;
    public TgChatsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChatInfoEntity> Get(long chatId)
    {
        try
        {
            return GetChatInfoById(chatId);
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"ChatInfoEntity Get DbException {ex.Message} {ex.InnerException?.Message}");
        }

        return Task.FromResult<ChatInfoEntity>(null);
    }

    private Task<ChatInfoEntity> GetChatInfoById(long chatId)
        => _dbContext.TlChats.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == chatId);

    public async Task<ChatInfoEntity> Create(long chatId, string culture, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new ChatInfoEntity { UserId = chatId, Culture = culture, LastMessage = DateTime.UtcNow };
            _dbContext.TlChats.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.Entry(entity).State = EntityState.Detached;
            return entity;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"ChatInfoEntity Create DbException {ex.Message} {ex.InnerException?.Message}");
            return null;
        }
    }

    public async Task<bool> Delete(ChatInfoEntity chatData)
    {
        _dbContext.TlChats.Remove(chatData);

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete chat item {ex.Message}");
            return false;
        }
    }

    public async Task<bool> Update(ChatInfoEntity newValue)
    {
        try
        {
            ChatInfoEntity oldValue = await GetChatInfoById(newValue.UserId);

            if (oldValue == null)
                return false;

            _dbContext.Entry(newValue).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(newValue).State = EntityState.Detached;

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to update chat item {ex.Message} {ex.InnerException?.Message} {newValue.PreviousStepId} {newValue.CurrentStepId}");
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_dbContext == null)
            return;

        _dbContext.Dispose();
        _dbContext = null;
    }
}