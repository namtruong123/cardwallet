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
    public class TaskSubmissionRepository : ITaskSubmissionRepository
    {
        private readonly AppDbContext _context;

        public TaskSubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<TaskSubmission?> GetByIdAsync(Guid id)
        {
            return _context.TaskSubmissions
                .Include(s => s.Task)
                .Include(s => s.Collaborator)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<List<TaskSubmission>> GetSubmissionsAsync(Guid? collaboratorId = null, Guid? reviewerParentUserId = null, string? status = null)
        {
            IQueryable<TaskSubmission> query = _context.TaskSubmissions
                .Include(s => s.Task)
                .Include(s => s.Collaborator);

            if (collaboratorId.HasValue)
            {
                query = query.Where(s => s.CollaboratorId == collaboratorId.Value);
            }

            if (reviewerParentUserId.HasValue)
            {
                // Submissions from collaborators managed by reviewerParentUserId
                query = query.Where(s => s.Collaborator != null && s.Collaborator.ParentUserId == reviewerParentUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            return query.OrderByDescending(s => s.SubmittedAt).ToListAsync();
        }

        public async Task AddAsync(TaskSubmission submission)
        {
            await _context.TaskSubmissions.AddAsync(submission);
        }

        public Task UpdateAsync(TaskSubmission submission)
        {
            _context.TaskSubmissions.Update(submission);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
