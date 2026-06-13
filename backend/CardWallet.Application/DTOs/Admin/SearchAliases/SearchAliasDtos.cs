using System;

namespace CardWallet.Application.DTOs.Admin.SearchAliases
{
    public class SearchAliasDto
    {
        public Guid Id { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSearchAliasRequest
    {
        public string Alias { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
    }
}
