using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.SearchAliases;

namespace CardWallet.Application.Interfaces
{
    public interface IAdminSearchAliasService
    {
        Task<IEnumerable<SearchAliasDto>> GetAllAliasesAsync();
        Task<SearchAliasDto> CreateAliasAsync(CreateSearchAliasRequest request);
        Task DeleteAliasAsync(Guid id);
    }
}