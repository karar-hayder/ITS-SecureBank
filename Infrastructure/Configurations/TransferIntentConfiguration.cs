using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransferIntentConfiguration : IEntityTypeConfiguration<TransferIntents>
{
    public void Configure(EntityTypeBuilder<TransferIntents> builder)
    {
        builder.HasKey(x => x.TransferIntentId);
        builder.Property(x => x.TransferIntentId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.HasOne(x => x.FromAccount).WithMany().HasForeignKey(x => x.FromAccountId);
        builder.HasOne(x => x.ToAccount).WithMany().HasForeignKey(x => x.ToAccountId);
    }
}