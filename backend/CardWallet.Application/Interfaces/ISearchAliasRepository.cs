using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces
{
    public interface ISearchAliasRepository
    {
        Task<SearchAlias?> GetByAliasAsync(string alias, string entityType);
        Task<IEnumerable<SearchAlias>> GetAllAsync();
        Task AddAsync(SearchAlias alias);
        Task DeleteAsync(SearchAlias alias);
        Task<SearchAlias?> GetByIdAsync(Guid id);
        Task SaveChangesAsync();
    }
}