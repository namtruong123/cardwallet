using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Task;

namespace CardWallet.Application.Interfaces
{
    public interface ITaskService
    {
        Task<List<TaskDto>> GetTasksAsync(Guid userId, string userRole);
        Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, Guid creatorId, string creatorRole);
        Task DeleteTaskAsync(Guid id, Guid actorId, string actorRole);
        
        Task<TaskSubmissionDto> SubmitTaskAsync(SubmitTaskRequest request, Guid collaboratorId);
        Task<List<TaskSubmissionDto>> GetSubmissionsAsync(Guid userId, string userRole, string? status = null);
        Task ApproveSubmissionAsync(Guid submissionId, Guid reviewerId, string reviewerRole);
        Task RejectSubmissionAsync(Guid submissionId, string reason, Guid reviewerId, string reviewerRole);
    }
}
