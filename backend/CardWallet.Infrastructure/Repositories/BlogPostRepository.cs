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
    public class BlogPostRepository : IBlogPostRepository
    {
        private readonly AppDbContext _context;

        public BlogPostRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<BlogPost?> GetByIdAsync(Guid id)
        {
            return _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }

        public Task<List<BlogPost>> GetBlogsAsync(Guid? authorId = null, bool approvedOnly = false)
        {
            IQueryable<BlogPost> query = _context.BlogPosts.Where(b => !b.IsDeleted);

            if (authorId.HasValue)
            {
                query = query.Where(b => b.AuthorId == authorId.Value);
            }

            if (approvedOnly)
            {
                query = query.Where(b => b.Status == "Approved");
            }

            return query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(BlogPost post)
        {
            await _context.BlogPosts.AddAsync(post);
        }

        public Task UpdateAsync(BlogPost post)
        {
            _context.BlogPosts.Update(post);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
