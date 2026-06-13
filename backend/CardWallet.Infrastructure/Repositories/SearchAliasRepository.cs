using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Repositories
{
    public class SearchAliasRepository : ISearchAliasRepository
    {
        private readonly AppDbContext _context;

        public SearchAliasRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<SearchAlias?> GetByAliasAsync(string alias, string entityType)
        {
            return _context.SearchAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Alias == alias && a.EntityType == entityType);
        }

        public async Task<IEnumerable<SearchAlias>> GetAllAsync()
        {
            return await _context.SearchAliases.AsNoTracking().ToListAsync();
        }
        
        public Task<SearchAlias?> GetByIdAsync(Guid id)
        {
            return _context.SearchAliases.FindAsync(id).AsTask();
        }

        public async Task AddAsync(SearchAlias alias)
        {
            await _context.SearchAliases.AddAsync(alias);
        }

        public Task DeleteAsync(SearchAlias alias)
        {
            _context.SearchAliases.Remove(alias);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}