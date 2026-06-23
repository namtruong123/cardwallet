using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Task;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;

namespace CardWallet.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly IPartnerTaskRepository _taskRepository;
        private readonly ITaskSubmissionRepository _submissionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWalletRepository _walletRepository;

        public TaskService(
            IPartnerTaskRepository taskRepository,
            ITaskSubmissionRepository submissionRepository,
            IUserRepository userRepository,
            IWalletRepository walletRepository)
        {
            _taskRepository = taskRepository;
            _submissionRepository = submissionRepository;
            _userRepository = userRepository;
            _walletRepository = walletRepository;
        }

        public async Task<List<TaskDto>> GetTasksAsync(Guid userId, string userRole)
        {
            List<PartnerTask> tasks;

            if (userRole == "Admin")
            {
                tasks = await _taskRepository.GetTasksAsync(activeOnly: false);
            }
            else if (userRole == "CentralManager" || userRole == "PartnerOrg")
            {
                tasks = await _taskRepository.GetTasksAsync(creatorId: userId, activeOnly: false);
            }
            else // Collaborator
            {
                var me = await _userRepository.GetByIdAsync(userId);
                if (me == null) throw new UnauthorizedException("Phiên làm việc không hợp lệ.");

                var systemTasks = await _taskRepository.GetTasksAsync(scope: "System");
                List<PartnerTask> partnerTasks = new List<PartnerTask>();
                if (me.ParentUserId.HasValue)
                {
                    partnerTasks = await _taskRepository.GetTasksAsync(creatorId: me.ParentUserId.Value, scope: "Partner");
                }

                tasks = systemTasks.Concat(partnerTasks).ToList();
            }

            return tasks.Select(MapToDto).ToList();
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, Guid creatorId, string creatorRole)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new BadRequestException("Tiêu đề không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Description))
                throw new BadRequestException("Mô tả không được để trống.");
            if (request.RewardCoins <= 0)
                throw new BadRequestException("Số xu thưởng phải lớn hơn 0.");

            var scope = request.Scope;
            if (creatorRole != "Admin")
            {
                scope = "Partner";
            }

            var task = new PartnerTask
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                RewardCoins = request.RewardCoins,
                CreatedByUserId = creatorId,
                Scope = scope,
                Deadline = request.Deadline,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            return MapToDto(task);
        }

        public async Task DeleteTaskAsync(Guid id, Guid actorId, string actorRole)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) throw new NotFoundException("Không tìm thấy nhiệm vụ.");

            if (actorRole != "Admin" && task.CreatedByUserId != actorId)
                throw new BadRequestException("Bạn không có quyền xóa nhiệm vụ này.");

            task.IsDeleted = true;
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();
        }

        public async Task<TaskSubmissionDto> SubmitTaskAsync(SubmitTaskRequest request, Guid collaboratorId)
        {
            var collaborator = await _userRepository.GetByIdAsync(collaboratorId);
            if (collaborator == null)
                throw new UnauthorizedException("Phiên làm việc không hợp lệ.");

            var task = await _taskRepository.GetByIdAsync(request.TaskId);
            if (task == null || !task.IsActive)
                throw new NotFoundException("Không tìm thấy nhiệm vụ hoặc nhiệm vụ đã hết hạn.");

            if (task.Scope == "Partner" && task.CreatedByUserId != collaborator.ParentUserId)
            {
                throw new BadRequestException("Bạn không thuộc nhóm quản lý của nhiệm vụ này.");
            }

            if (string.IsNullOrWhiteSpace(request.ProofUrl))
                throw new BadRequestException("Vui lòng cung cấp link bằng chứng hoàn thành.");

            var submission = new TaskSubmission
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                CollaboratorId = collaboratorId,
                ProofUrl = request.ProofUrl.Trim(),
                Note = request.Note?.Trim(),
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            await _submissionRepository.AddAsync(submission);
            await _submissionRepository.SaveChangesAsync();

            submission.Task = task;
            submission.Collaborator = collaborator;

            return MapToSubmissionDto(submission);
        }

        public async Task<List<TaskSubmissionDto>> GetSubmissionsAsync(Guid userId, string userRole, string? status = null)
        {
            List<TaskSubmission> submissions;

            if (userRole == "Admin")
            {
                submissions = await _submissionRepository.GetSubmissionsAsync(status: status);
            }
            else if (userRole == "CentralManager" || userRole == "PartnerOrg")
            {
                submissions = await _submissionRepository.GetSubmissionsAsync(reviewerParentUserId: userId, status: status);
            }
            else
            {
                submissions = await _submissionRepository.GetSubmissionsAsync(collaboratorId: userId, status: status);
            }

            return submissions.Select(MapToSubmissionDto).ToList();
        }

        public async Task ApproveSubmissionAsync(Guid submissionId, Guid reviewerId, string reviewerRole)
        {
            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new NotFoundException("Không tìm thấy đơn nộp.");

            if (submission.Status != "Pending")
                throw new BadRequestException("Đơn này đã được xử lý từ trước.");

            if (reviewerRole != "Admin" && submission.Collaborator?.ParentUserId != reviewerId)
            {
                throw new BadRequestException("Bạn không có quyền duyệt nhiệm vụ của CTV này.");
            }

            var rewardCoins = submission.Task?.RewardCoins ?? 0;
            if (rewardCoins <= 0)
                throw new BadRequestException("Nhiệm vụ này không có phần thưởng hợp lệ.");

            var admin = await _userRepository.GetByEmailAsync("admin@example.com");
            if (admin == null)
            {
                var (users, _) = await _userRepository.GetPagedUsersAsync("", "Active", null, null, 1, 10);
                admin = users.FirstOrDefault(u => u.Role == "Admin");
                if (admin == null)
                    throw new BadRequestException("Không tìm thấy tài khoản ADMIN hệ thống để trích xu.");
            }

            var adminWallet = await _walletRepository.GetByUserIdForUpdateAsync(admin.Id);
            if (adminWallet == null)
                throw new BadRequestException("Ví ADMIN chưa được khởi tạo.");

            var collaboratorWallet = await _walletRepository.GetByUserIdForUpdateAsync(submission.CollaboratorId);
            if (collaboratorWallet == null)
                throw new BadRequestException("Ví CTV chưa được khởi tạo.");

            if (adminWallet.Balance < rewardCoins)
                throw new BadRequestException("Ví ADMIN không đủ xu để chi trả phần thưởng.");

            var referenceCode = $"TASK_REWARD:{submission.Id:N}";
            var description = $"Hoàn thành nhiệm vụ: {submission.Task?.Title}";

            var adminBefore = adminWallet.Balance;
            adminWallet.Balance -= rewardCoins;
            adminWallet.UpdatedAt = DateTime.UtcNow;

            var collaboratorBefore = collaboratorWallet.Balance;
            collaboratorWallet.Balance += rewardCoins;
            collaboratorWallet.UpdatedAt = DateTime.UtcNow;

            var adminTx = new WalletTransaction
            {
                WalletId = adminWallet.Id,
                UserId = admin.Id,
                Type = WalletTransactionType.AdminTransferOut,
                Status = WalletTransactionStatus.Completed,
                Amount = -rewardCoins,
                BalanceBefore = adminBefore,
                BalanceAfter = adminWallet.Balance,
                Description = $"Trả thưởng task cho CTV {submission.Collaborator?.FullName}. {description}",
                ReferenceCode = referenceCode,
                IdempotencyKey = referenceCode
            };

            var collaboratorTx = new WalletTransaction
            {
                WalletId = collaboratorWallet.Id,
                UserId = submission.CollaboratorId,
                Type = WalletTransactionType.AdminTransferIn,
                Status = WalletTransactionStatus.Completed,
                Amount = rewardCoins,
                BalanceBefore = collaboratorBefore,
                BalanceAfter = collaboratorWallet.Balance,
                Description = $"Nhận thưởng hoàn thành task. {description}",
                ReferenceCode = referenceCode,
                IdempotencyKey = referenceCode
            };

            await _walletRepository.AddTransactionAsync(adminTx);
            await _walletRepository.AddTransactionAsync(collaboratorTx);
            
            submission.Status = "Approved";
            submission.ReviewedAt = DateTime.UtcNow;
            submission.ReviewedByUserId = reviewerId;

            await _submissionRepository.UpdateAsync(submission);
            await _walletRepository.SaveChangesAsync();
            await _submissionRepository.SaveChangesAsync();
        }

        public async Task RejectSubmissionAsync(Guid submissionId, string reason, Guid reviewerId, string reviewerRole)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new BadRequestException("Vui lòng cung cấp lý do từ chối.");

            var submission = await _submissionRepository.GetByIdAsync(submissionId);
            if (submission == null) throw new NotFoundException("Không tìm thấy đơn nộp.");

            if (submission.Status != "Pending")
                throw new BadRequestException("Đơn này đã được xử lý từ trước.");

            if (reviewerRole != "Admin" && submission.Collaborator?.ParentUserId != reviewerId)
            {
                throw new BadRequestException("Bạn không có quyền từ chối nhiệm vụ của CTV này.");
            }

            submission.Status = "Rejected";
            submission.RejectReason = reason.Trim();
            submission.ReviewedAt = DateTime.UtcNow;
            submission.ReviewedByUserId = reviewerId;

            await _submissionRepository.UpdateAsync(submission);
            await _submissionRepository.SaveChangesAsync();
        }

        private static TaskDto MapToDto(PartnerTask task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                RewardCoins = task.RewardCoins,
                CreatedByUserId = task.CreatedByUserId,
                Scope = task.Scope,
                Deadline = task.Deadline,
                IsActive = task.IsActive
            };
        }

        private static TaskSubmissionDto MapToSubmissionDto(TaskSubmission submission)
        {
            return new TaskSubmissionDto
            {
                Id = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task?.Title ?? "Unknown Task",
                TaskRewardCoins = submission.Task?.RewardCoins ?? 0,
                CollaboratorId = submission.CollaboratorId,
                CollaboratorName = submission.Collaborator?.FullName ?? "Unknown Collaborator",
                ProofUrl = submission.ProofUrl,
                Note = submission.Note,
                Status = submission.Status,
                RejectReason = submission.RejectReason,
                SubmittedAt = submission.SubmittedAt,
                ReviewedAt = submission.ReviewedAt
            };
        }
    }
}
