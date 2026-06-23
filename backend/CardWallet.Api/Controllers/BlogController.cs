using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Blog;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers
{
    [ApiController]
    [Route("api/blogs")]
    [Authorize]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogController(IBlogService blogService)
        {
            _blogService = blogService;
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

        private string GetUserName()
        {
            return User.FindFirst("fullName")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "Anonymous";
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs()
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var blogs = await _blogService.GetBlogsAsync(userId, userRole);
            return Ok(blogs);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogDetail(Guid id)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var blog = await _blogService.GetBlogDetailAsync(id, userId, userRole);
            return Ok(blog);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogRequest request)
        {
            var role = GetUserRole();
            var canManageBlog = User.HasClaim("canManageBlog", "True") || role == "Admin";
            if (!canManageBlog) return Forbid();

            var authorId = GetUserId();
            var authorName = GetUserName();
            var blog = await _blogService.CreateBlogAsync(request, authorId, role, authorName);
            return CreatedAtAction(nameof(GetBlogDetail), new { id = blog.Id }, blog);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(Guid id, [FromBody] UpdateBlogRequest request)
        {
            var role = GetUserRole();
            var canManageBlog = User.HasClaim("canManageBlog", "True") || role == "Admin";
            if (!canManageBlog) return Forbid();

            var actorId = GetUserId();
            var blog = await _blogService.UpdateBlogAsync(id, request, actorId, role);
            return Ok(blog);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(Guid id)
        {
            var role = GetUserRole();
            var canManageBlog = User.HasClaim("canManageBlog", "True") || role == "Admin";
            if (!canManageBlog) return Forbid();

            var actorId = GetUserId();
            await _blogService.DeleteBlogAsync(id, actorId, role);
            return NoContent();
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveBlog(Guid id)
        {
            var adminId = GetUserId();
            await _blogService.ApproveBlogAsync(id, adminId);
            return NoContent();
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectBlog(Guid id, [FromBody] RejectBlogRequest request)
        {
            var adminId = GetUserId();
            await _blogService.RejectBlogAsync(id, request.Reason, adminId);
            return NoContent();
        }
    }
}
