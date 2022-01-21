using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;

namespace theatrel.DataAccess.Repositories;

internal class TgUsersRepository : ITgUsersRepository
{
    private AppDbContext _dbContext;
    public TgUsersRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TelegramUserEntity> Get(long userId)
    {
        try
        {
            return GetById(userId);
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"DbException {ex.Message}");
        }

        return Task.FromResult<TelegramUserEntity>(null);
    }

    private Task<TelegramUserEntity> GetById(long userId)
        =>  _dbContext.TlUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

    public async Task<TelegramUserEntity> Create(long userId, string culture, CancellationToken cancellationToken)
    {
        var entity = new TelegramUserEntity { Id = userId, Culture = culture };

        try
        {
            _dbContext.TlUsers.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.Entry(entity).State = EntityState.Detached;

            return entity;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"DbException {ex.Message}");
            return null;
        }
    }

    public async Task<bool> Delete(TelegramUserEntity userEntity)
    {
        _dbContext.TlUsers.Remove(userEntity);

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete user item {ex.Message}");
            return false;
        }
    }

    public async Task<bool> Update(TelegramUserEntity newValue)
    {
        TelegramUserEntity oldValue = await GetById(newValue.Id);

        if (oldValue == null)
            return false;

        _dbContext.Entry(newValue).State = EntityState.Modified;

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to update chat item {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_dbContext == null)
            return;

        try
        {
            _dbContext?.Dispose();
            _dbContext = null;
        }
        catch(Exception ex)
        {
            Trace.TraceError($"Exception while dispose {ex.Message}");
        }
    }
}