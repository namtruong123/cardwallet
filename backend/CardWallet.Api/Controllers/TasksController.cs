using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Task;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var tasks = await _taskService.GetTasksAsync(userId, userRole);
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userRole = GetUserRole();
            var canManageTasks = User.HasClaim("canManageTasks", "True") || userRole == "Admin";
            if (!canManageTasks) return Forbid();

            var creatorId = GetUserId();
            var task = await _taskService.CreateTaskAsync(request, creatorId, userRole);
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var userRole = GetUserRole();
            var canManageTasks = User.HasClaim("canManageTasks", "True") || userRole == "Admin";
            if (!canManageTasks) return Forbid();

            var actorId = GetUserId();
            await _taskService.DeleteTaskAsync(id, actorId, userRole);
            return NoContent();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTask([FromBody] SubmitTaskRequest request)
        {
            var collaboratorId = GetUserId();
            var submission = await _taskService.SubmitTaskAsync(request, collaboratorId);
            return Ok(submission);
        }

        [HttpGet("submissions")]
        public async Task<IActionResult> GetSubmissions([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var submissions = await _taskService.GetSubmissionsAsync(userId, userRole, status);
            return Ok(submissions);
        }

        [HttpPost("submissions/{id}/approve")]
        public async Task<IActionResult> ApproveSubmission(Guid id)
        {
            var userRole = GetUserRole();
            var canApproveTasks = User.HasClaim("canApproveTasks", "True") || userRole == "Admin";
            if (!canApproveTasks) return Forbid();

            var reviewerId = GetUserId();
            await _taskService.ApproveSubmissionAsync(id, reviewerId, userRole);
            return NoContent();
        }

        [HttpPost("submissions/{id}/reject")]
        public async Task<IActionResult> RejectSubmission(Guid id, [FromBody] ReviewSubmissionRequest request)
        {
            var userRole = GetUserRole();
            var canApproveTasks = User.HasClaim("canApproveTasks", "True") || userRole == "Admin";
            if (!canApproveTasks) return Forbid();

            var reviewerId = GetUserId();
            await _taskService.RejectSubmissionAsync(id, request.RejectReason ?? string.Empty, reviewerId, userRole);
            return NoContent();
        }
    }
}
