using Booking.Domain.Accounting;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class AccountingCategoryConfiguration : IEntityTypeConfiguration<AccountingCategory>
{
    public void Configure(EntityTypeBuilder<AccountingCategory> builder)
    {
        builder.ToTable("AccountingCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.EntryType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(category => new { category.RestaurantId, category.EntryType, category.Name }).IsUnique();
        ConfigureRestaurantRelationship(builder);
    }

    private static void ConfigureRestaurantRelationship(EntityTypeBuilder<AccountingCategory> builder) =>
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(category => category.RestaurantId).OnDelete(DeleteBehavior.Restrict);
}

public sealed class AccountingEntryConfiguration : IEntityTypeConfiguration<AccountingEntry>
{
    public void Configure(EntityTypeBuilder<AccountingEntry> builder)
    {
        builder.ToTable("AccountingEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.EntryType).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Description).HasMaxLength(300).IsRequired();
        builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();
        builder.Property(entry => entry.GrossAmount).HasPrecision(18, 2);
        builder.OwnsMany(entry => entry.Splits, splits =>
        {
            splits.ToJson();
            splits.Property(split => split.GrossAmount).HasPrecision(18, 2);
            splits.Property(split => split.NetAmount).HasPrecision(18, 2);
            splits.Property(split => split.VatAmount).HasPrecision(18, 2);
            splits.Property(split => split.DeductibleVatAmount).HasPrecision(18, 2);
        });
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(entry => entry.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingSourceTransaction>().WithMany().HasForeignKey(entry => entry.SourceTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingEntry>().WithMany().HasForeignKey(entry => entry.CorrectionOfEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.RestaurantId, entry.EntryDate });
        builder.HasIndex(entry => entry.SourceTransactionId);
        builder.HasIndex(entry => entry.CorrectionOfEntryId);
    }
}

public sealed class AccountingSourceTransactionConfiguration : IEntityTypeConfiguration<AccountingSourceTransaction>
{
    public void Configure(EntityTypeBuilder<AccountingSourceTransaction> builder)
    {
        builder.ToTable("AccountingSourceTransactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.SourceKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(transaction => transaction.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(transaction => transaction.Fingerprint).HasMaxLength(128).IsRequired();
        builder.Property(transaction => transaction.Description).HasMaxLength(500).IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.Currency).HasMaxLength(3).IsRequired();
        builder.Property(transaction => transaction.RawPayload).HasColumnType("nvarchar(max)");
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(transaction => transaction.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingImportBatch>().WithMany().HasForeignKey(transaction => transaction.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(transaction => new { transaction.RestaurantId, transaction.Fingerprint }).IsUnique();
        builder.HasIndex(transaction => new { transaction.RestaurantId, transaction.Status, transaction.TransactionDate });
    }
}

public sealed class AccountingImportBatchConfiguration : IEntityTypeConfiguration<AccountingImportBatch>
{
    public void Configure(EntityTypeBuilder<AccountingImportBatch> builder)
    {
        builder.ToTable("AccountingImportBatches");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.ImportKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(batch => batch.FileName).HasMaxLength(260).IsRequired();
        builder.Property(batch => batch.FileChecksum).HasMaxLength(128).IsRequired();
        builder.Property(batch => batch.MappingJson).HasColumnType("nvarchar(max)");
        builder.Property(batch => batch.StorageKey).HasMaxLength(500).IsRequired();
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(batch => batch.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(batch => new { batch.RestaurantId, batch.FileChecksum }).IsUnique();
    }
}

public sealed class AccountingConnectionConfiguration : IEntityTypeConfiguration<AccountingConnection>
{
    public void Configure(EntityTypeBuilder<AccountingConnection> builder)
    {
        builder.ToTable("AccountingConnections");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Provider).HasConversion<string>().HasMaxLength(20);
        builder.Property(connection => connection.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(connection => connection.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(connection => connection.EncryptedAccessToken).HasColumnType("nvarchar(max)");
        builder.Property(connection => connection.EncryptedRefreshToken).HasColumnType("nvarchar(max)");
        builder.Property(connection => connection.LastError).HasMaxLength(1000);
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(connection => connection.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(connection => new { connection.RestaurantId, connection.Provider, connection.ExternalId }).IsUnique();
    }
}

public sealed class AccountingMatchConfiguration : IEntityTypeConfiguration<AccountingMatch>
{
    public void Configure(EntityTypeBuilder<AccountingMatch> builder)
    {
        builder.ToTable("AccountingMatches");
        builder.HasKey(match => match.Id);
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(match => match.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingSourceTransaction>().WithMany().HasForeignKey(match => match.BankSourceTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingSourceTransaction>().WithMany().HasForeignKey(match => match.MatchedSourceTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(match => new { match.RestaurantId, match.BankSourceTransactionId }).IsUnique();
    }
}

public sealed class AccountingAttachmentConfiguration : IEntityTypeConfiguration<AccountingAttachment>
{
    public void Configure(EntityTypeBuilder<AccountingAttachment> builder)
    {
        builder.ToTable("AccountingAttachments");
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.FileName).HasMaxLength(260).IsRequired();
        builder.Property(attachment => attachment.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(attachment => attachment.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(attachment => attachment.Checksum).HasMaxLength(128).IsRequired();
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(attachment => attachment.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingEntry>().WithMany().HasForeignKey(attachment => attachment.AccountingEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attachment => new { attachment.RestaurantId, attachment.AccountingEntryId });
    }
}
