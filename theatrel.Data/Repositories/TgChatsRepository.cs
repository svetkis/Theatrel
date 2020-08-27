using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;

namespace theatrel.DataAccess.Repositories
{
    internal class TgChatsRepository : ITgChatsRepository
    {
        private readonly AppDbContext _dbContext;
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
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            return Task.FromResult<ChatInfoEntity>(null);
        }

        private Task<ChatInfoEntity> GetChatInfoById(long chatId)
            => _dbContext.TlChats.AsNoTracking().SingleOrDefaultAsync(u => u.ChatId == chatId);

        public async Task<ChatInfoEntity> Create(long chatId, string culture, CancellationToken cancellationToken)
        {
            try
            {
                var entity = new ChatInfoEntity { ChatId = chatId, Culture = culture };
                _dbContext.TlChats.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
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
            ChatInfoEntity oldValue = await GetChatInfoById(newValue.ChatId);

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
            _dbContext?.Dispose();
        }
    }
}
