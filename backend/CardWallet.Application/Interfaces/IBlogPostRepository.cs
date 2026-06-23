using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces
{
    public interface IBlogPostRepository
    {
        Task<BlogPost?> GetByIdAsync(Guid id);
        Task<List<BlogPost>> GetBlogsAsync(Guid? authorId = null, bool approvedOnly = false);
        Task AddAsync(BlogPost post);
        Task UpdateAsync(BlogPost post);
        Task SaveChangesAsync();
    }
}
