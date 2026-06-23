using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Blog;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Services
{
    public class BlogService : IBlogService
    {
        private readonly IBlogPostRepository _blogRepository;

        public BlogService(IBlogPostRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<List<BlogDto>> GetBlogsAsync(Guid userId, string userRole)
        {
            List<BlogPost> blogs;

            if (userRole == "Admin")
            {
                // Admin sees all blogs
                blogs = await _blogRepository.GetBlogsAsync();
            }
            else if (userRole == "CentralManager" || userRole == "PartnerOrg")
            {
                // Managers/partners see approved blogs and their own posts (including pending/rejected)
                var approvedBlogs = await _blogRepository.GetBlogsAsync(approvedOnly: true);
                var myBlogs = await _blogRepository.GetBlogsAsync(authorId: userId);
                
                // Combine and distinct by ID
                blogs = approvedBlogs.Concat(myBlogs)
                    .GroupBy(b => b.Id)
                    .Select(g => g.First())
                    .OrderByDescending(b => b.CreatedAt)
                    .ToList();
            }
            else
            {
                // Collaborators and customers only see approved posts
                blogs = await _blogRepository.GetBlogsAsync(approvedOnly: true);
            }

            return blogs.Select(MapToDto).ToList();
        }

        public async Task<BlogDto> GetBlogDetailAsync(Guid id, Guid userId, string userRole)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
                throw new NotFoundException("Không tìm thấy bài viết");

            if (blog.Status != "Approved" && userRole != "Admin" && blog.AuthorId != userId)
            {
                throw new BadRequestException("Bạn không có quyền truy cập bài viết này.");
            }

            return MapToDto(blog);
        }

        public async Task<BlogDto> CreateBlogAsync(CreateBlogRequest request, Guid authorId, string authorRole, string authorName)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new BadRequestException("Tiêu đề không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Nội dung không được để trống.");

            var blog = new BlogPost
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                AuthorId = authorId,
                AuthorName = authorName,
                Status = authorRole == "Admin" ? "Approved" : "PendingApproval",
                CreatedAt = DateTime.UtcNow
            };

            await _blogRepository.AddAsync(blog);
            await _blogRepository.SaveChangesAsync();

            return MapToDto(blog);
        }

        public async Task<BlogDto> UpdateBlogAsync(Guid id, UpdateBlogRequest request, Guid actorId, string actorRole)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new BadRequestException("Tiêu đề không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Nội dung không được để trống.");

            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
                throw new NotFoundException("Không tìm thấy bài viết");

            if (actorRole != "Admin" && blog.AuthorId != actorId)
                throw new BadRequestException("Bạn không có quyền sửa bài viết này.");

            blog.Title = request.Title.Trim();
            blog.Content = request.Content.Trim();
            blog.Status = actorRole == "Admin" ? "Approved" : "PendingApproval";
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync();

            return MapToDto(blog);
        }

        public async Task DeleteBlogAsync(Guid id, Guid actorId, string actorRole)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
                throw new NotFoundException("Không tìm thấy bài viết");

            if (actorRole != "Admin" && blog.AuthorId != actorId)
                throw new BadRequestException("Bạn không có quyền xóa bài viết này.");

            blog.IsDeleted = true;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync();
        }

        public async Task ApproveBlogAsync(Guid id, Guid adminId)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
                throw new NotFoundException("Không tìm thấy bài viết");

            blog.Status = "Approved";
            blog.RejectReason = null;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync();
        }

        public async Task RejectBlogAsync(Guid id, string reason, Guid adminId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new BadRequestException("Vui lòng nhập lý do từ chối.");

            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
                throw new NotFoundException("Không tìm thấy bài viết");

            blog.Status = "Rejected";
            blog.RejectReason = reason.Trim();
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync();
        }

        private static BlogDto MapToDto(BlogPost blog)
        {
            return new BlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Content = blog.Content,
                AuthorId = blog.AuthorId,
                AuthorName = blog.AuthorName,
                Status = blog.Status,
                RejectReason = blog.RejectReason,
                CreatedAt = blog.CreatedAt,
                UpdatedAt = blog.UpdatedAt
            };
        }
    }
}
