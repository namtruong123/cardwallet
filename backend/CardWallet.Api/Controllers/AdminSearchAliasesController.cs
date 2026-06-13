using System;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Admin.SearchAliases;
using CardWallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardWallet.Api.Controllers
{
    [ApiController]
    [Route("api/admin/search-aliases")]
    [Authorize] // Assuming Admin role is checked by a policy or middleware
    public class AdminSearchAliasesController : ControllerBase
    {
        private readonly IAdminSearchAliasService _aliasService;

        public AdminSearchAliasesController(IAdminSearchAliasService aliasService)
        {
            _aliasService = aliasService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _aliasService.GetAllAliasesAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSearchAliasRequest request)
        {
            var result = await _aliasService.CreateAliasAsync(request);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) { await _aliasService.DeleteAliasAsync(id); return NoContent(); }
    }
}