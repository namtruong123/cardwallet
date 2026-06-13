using System;

namespace CardWallet.Domain.Entities
{
    public class SearchAlias
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Alias { get; set; } = string.Empty; // e.g., "vtt"
        public string Target { get; set; } = string.Empty; // e.g., "Viettel"
        public string EntityType { get; set; } = string.Empty; // e.g., "CardProvider", "UserSearch"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}