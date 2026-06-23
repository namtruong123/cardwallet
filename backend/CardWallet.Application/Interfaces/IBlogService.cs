using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Blog;

namespace CardWallet.Application.Interfaces
{
    public interface IBlogService
    {
        Task<List<BlogDto>> GetBlogsAsync(Guid userId, string userRole);
        Task<BlogDto> GetBlogDetailAsync(Guid id, Guid userId, string userRole);
        Task<BlogDto> CreateBlogAsync(CreateBlogRequest request, Guid authorId, string authorRole, string authorName);
        Task<BlogDto> UpdateBlogAsync(Guid id, UpdateBlogRequest request, Guid actorId, string actorRole);
        Task DeleteBlogAsync(Guid id, Guid actorId, string actorRole);
        Task ApproveBlogAsync(Guid id, Guid adminId);
        Task RejectBlogAsync(Guid id, string reason, Guid adminId);
    }
}
