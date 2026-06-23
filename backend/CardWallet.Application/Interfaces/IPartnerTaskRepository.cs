using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces
{
    public interface IPartnerTaskRepository
    {
        Task<PartnerTask?> GetByIdAsync(Guid id);
        Task<List<PartnerTask>> GetTasksAsync(Guid? creatorId = null, string? scope = null, bool activeOnly = true);
        Task AddAsync(PartnerTask task);
        Task UpdateAsync(PartnerTask task);
        Task SaveChangesAsync();
    }
}
