using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.SearchAliases;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Services
{
    public class AdminSearchAliasService : IAdminSearchAliasService
    {
        private readonly ISearchAliasRepository _aliasRepository;

        public AdminSearchAliasService(ISearchAliasRepository aliasRepository)
        {
            _aliasRepository = aliasRepository;
        }

        public async Task<IEnumerable<SearchAliasDto>> GetAllAliasesAsync()
        {
            var aliases = await _aliasRepository.GetAllAsync();
            return aliases.Select(a => new SearchAliasDto { Id = a.Id, Alias = a.Alias, Target = a.Target, EntityType = a.EntityType, CreatedAt = a.CreatedAt });
        }

        public async Task<SearchAliasDto> CreateAliasAsync(CreateSearchAliasRequest request)
        {
            var existing = await _aliasRepository.GetByAliasAsync(request.Alias, request.EntityType);
            if (existing != null) throw new ConflictException("Alias already exists for this entity type.");

            var newAlias = new SearchAlias
            {
                Alias = request.Alias.ToLower().Trim(),
                Target = request.Target.Trim(),
                EntityType = request.EntityType.Trim()
            };

            await _aliasRepository.AddAsync(newAlias);
            await _aliasRepository.SaveChangesAsync();

            return new SearchAliasDto { Id = newAlias.Id, Alias = newAlias.Alias, Target = newAlias.Target, EntityType = newAlias.EntityType, CreatedAt = newAlias.CreatedAt };
        }

        public async Task DeleteAliasAsync(Guid id)
        {
            var alias = await _aliasRepository.GetByIdAsync(id);
            if (alias == null) throw new NotFoundException("Alias not found.");

            await _aliasRepository.DeleteAsync(alias);
            await _aliasRepository.SaveChangesAsync();
        }
    }
}