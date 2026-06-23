using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories
{
    public class PartnerTaskRepository : IPartnerTaskRepository
    {
        private readonly AppDbContext _context;

        public PartnerTaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<PartnerTask?> GetByIdAsync(Guid id)
        {
            return _context.PartnerTasks.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public Task<List<PartnerTask>> GetTasksAsync(Guid? creatorId = null, string? scope = null, bool activeOnly = true)
        {
            IQueryable<PartnerTask> query = _context.PartnerTasks.Where(t => !t.IsDeleted);

            if (creatorId.HasValue)
            {
                query = query.Where(t => t.CreatedByUserId == creatorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(scope))
            {
                query = query.Where(t => t.Scope == scope);
            }

            if (activeOnly)
            {
                query = query.Where(t => t.IsActive);
            }

            return query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(PartnerTask task)
        {
            await _context.PartnerTasks.AddAsync(task);
        }

        public Task UpdateAsync(PartnerTask task)
        {
            _context.PartnerTasks.Update(task);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
