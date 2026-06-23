using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces
{
    public interface ITaskSubmissionRepository
    {
        Task<TaskSubmission?> GetByIdAsync(Guid id);
        Task<List<TaskSubmission>> GetSubmissionsAsync(Guid? collaboratorId = null, Guid? reviewerParentUserId = null, string? status = null);
        Task AddAsync(TaskSubmission submission);
        Task UpdateAsync(TaskSubmission submission);
        Task SaveChangesAsync();
    }
}
