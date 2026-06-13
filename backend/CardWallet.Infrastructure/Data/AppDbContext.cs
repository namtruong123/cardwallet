using CardWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardWallet.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<WalletTransaction> WalletTransactions { get; set; } = null!;
    public DbSet<CardRate> CardRates { get; set; } = null!;
    public DbSet<CardTransaction> CardTransactions { get; set; } = null!;
    public DbSet<SearchAlias> SearchAliases { get; set; } = null!;
    public DbSet<KycRequest> KycRequests { get; set; } = null!;
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; } = null!;
    public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        user.Property(x => x.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        user.HasIndex(x => x.PhoneNumber)
            .IsUnique();

        user.Property(x => x.Email)
            .HasMaxLength(150)
            .IsRequired();

        user.HasIndex(x => x.Email)
            .IsUnique();

        user.Property(x => x.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        user.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        user.Property(x => x.Role)
            .HasMaxLength(40)
            .IsRequired();

        user.Property(x => x.ParentUserId);

        user.Property(x => x.CanManageUsers)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanManageTasks)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanApproveTasks)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanApproveKycWithdraw)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanTransferPoints)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanManageBlog)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.CanExportReports)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(x => x.DeletedAt);

        var refreshToken = modelBuilder.Entity<RefreshToken>();

        refreshToken.ToTable("refresh_tokens");

        refreshToken.HasKey(x => x.Id);

        refreshToken.Property(x => x.Token)
            .HasMaxLength(500)
            .IsRequired();

        refreshToken.HasIndex(x => x.Token)
            .IsUnique();

        refreshToken.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var wallet = modelBuilder.Entity<Wallet>();

        wallet.ToTable("wallets");

        wallet.HasKey(x => x.Id);

        // No concurrency token configured on Wallet (no `Version` property).

        wallet.HasOne(x => x.User)
            .WithOne(x => x.Wallet)
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var transaction = modelBuilder.Entity<WalletTransaction>();

        transaction.ToTable("transactions");

        transaction.HasKey(x => x.Id);

        transaction.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        transaction.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        transaction.Property(x => x.ReferenceCode)
            .HasMaxLength(100);

        transaction.Property(x => x.Description)
            .HasMaxLength(255);

        transaction.HasOne(x => x.Wallet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        var cardRate = modelBuilder.Entity<CardRate>();

        cardRate.ToTable("card_rates");

        cardRate.HasKey(x => x.Id);

        cardRate.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        cardRate.Property(x => x.FaceValue)
            .IsRequired();

        cardRate.Property(x => x.DiscountPercent)
            .HasPrecision(5, 2)
            .IsRequired();

        cardRate.Property(x => x.IsActive)
            .IsRequired();

        cardRate.Property(x => x.CreatedAt)
            .IsRequired();

        cardRate.Property(x => x.UpdatedAt);

        cardRate.HasIndex(x => new { x.Provider, x.FaceValue })
            .IsUnique();

        var cardTransaction = modelBuilder.Entity<CardTransaction>();

        cardTransaction.ToTable("card_transactions");

        cardTransaction.HasKey(x => x.Id);

        cardTransaction.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        cardTransaction.Property(x => x.FaceValue)
            .IsRequired();

        cardTransaction.Property(x => x.DiscountPercent)
            .HasPrecision(5, 2);

        cardTransaction.Property(x => x.ExpectedReceiveAmount)
            .IsRequired();

        cardTransaction.Property(x => x.ActualReceiveAmount);

        cardTransaction.Property(x => x.CardCode)
            .HasMaxLength(200);

        cardTransaction.Property(x => x.Serial)
            .HasMaxLength(200);

        cardTransaction.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        cardTransaction.Property(x => x.ParentTransactionCode)
            .HasMaxLength(200);

        cardTransaction.Property(x => x.FailureReason)
            .HasMaxLength(500);

        cardTransaction.Property(x => x.IdempotencyKey)
            .HasMaxLength(200);

        cardTransaction.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        cardTransaction.Property(x => x.NextRetryAt);

        cardTransaction.Property(x => x.ParentRequestRaw)
            .HasColumnType("longtext");

        cardTransaction.Property(x => x.ParentResponseRaw)
            .HasColumnType("longtext");

        cardTransaction.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        cardTransaction.Property(x => x.CreatedAt)
            .IsRequired();

        cardTransaction.Property(x => x.ProcessedAt);

        cardTransaction.Property(x => x.CompletedAt);

        var searchAlias = modelBuilder.Entity<SearchAlias>();

        searchAlias.ToTable("search_aliases");

        searchAlias.HasKey(x => x.Id);

        searchAlias.Property(x => x.Alias)
            .HasMaxLength(100)
            .IsRequired();

        searchAlias.Property(x => x.Target)
            .HasMaxLength(255)
            .IsRequired();

        searchAlias.HasIndex(x => new { x.Alias, x.EntityType })
            .IsUnique();

        var kycRequest = modelBuilder.Entity<KycRequest>();

        kycRequest.ToTable("kyc_requests");
        kycRequest.HasKey(x => x.Id);
        kycRequest.Property(x => x.FrontIdImagePath).HasMaxLength(500).IsRequired();
        kycRequest.Property(x => x.BackIdImagePath).HasMaxLength(500).IsRequired();
        kycRequest.Property(x => x.SelfieImagePath).HasMaxLength(500).IsRequired();
        kycRequest.Property(x => x.Status).HasMaxLength(30).IsRequired();
        kycRequest.Property(x => x.RejectReason).HasMaxLength(500);
        kycRequest.Property(x => x.CreatedAt).IsRequired();
        kycRequest.HasIndex(x => new { x.UserId, x.Status });
        kycRequest.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var withdrawalRequest = modelBuilder.Entity<WithdrawalRequest>();

        withdrawalRequest.ToTable("withdrawal_requests");
        withdrawalRequest.HasKey(x => x.Id);
        withdrawalRequest.Property(x => x.BankName).HasMaxLength(120).IsRequired();
        withdrawalRequest.Property(x => x.BankAccountNumber).HasMaxLength(80).IsRequired();
        withdrawalRequest.Property(x => x.BankAccountName).HasMaxLength(150).IsRequired();
        withdrawalRequest.Property(x => x.Status).HasMaxLength(30).IsRequired();
        withdrawalRequest.Property(x => x.RejectReason).HasMaxLength(500);
        withdrawalRequest.Property(x => x.CreatedAt).IsRequired();
        withdrawalRequest.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
        withdrawalRequest.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var systemSetting = modelBuilder.Entity<SystemSetting>();

        systemSetting.ToTable("system_settings");
        systemSetting.HasKey(x => x.Id);
        systemSetting.Property(x => x.Key).HasMaxLength(100).IsRequired();
        systemSetting.Property(x => x.Value).HasColumnType("longtext").IsRequired();
        systemSetting.Property(x => x.Description).HasMaxLength(500);
        systemSetting.Property(x => x.CreatedAt).IsRequired();
        systemSetting.HasIndex(x => x.Key).IsUnique();
    }
}
