using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Documents;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(document => document.Id);

        builder.Property(document => document.StorageKey)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(document => document.FileName)
            .HasMaxLength(DocumentRules.FileNameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.SizeBytes)
            .IsRequired();

        builder.Property(document => document.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(document => document.ExpiresOn)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(document => document.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(document => document.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasAlternateKey(document => new { document.RelocationId, document.Id })
            .HasName("ak_documents_relocation_id_id");

        builder.HasIndex(document => document.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_documents_storage_key");

        builder.HasIndex(document => document.RelocationId)
            .HasDatabaseName("ix_documents_relocation_id");

        builder.HasOne(document => document.Relocation)
            .WithMany()
            .HasForeignKey(document => document.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("documents", table =>
        {
            table.HasCheckConstraint(
                "ck_documents_storage_key",
                "storage_key ~ '^[0-9a-f]{32}$'");
            table.HasCheckConstraint(
                "ck_documents_file_name",
                $"char_length(btrim(file_name)) > 0 AND char_length(file_name) <= {DocumentRules.FileNameMaxLength}");
            table.HasCheckConstraint(
                "ck_documents_content_type",
                "content_type IN ('application/pdf', 'image/jpeg', 'image/png', 'image/webp')");
            table.HasCheckConstraint(
                "ck_documents_size_bytes",
                $"size_bytes > 0 AND size_bytes <= {DocumentRules.MaxBytes}");
            table.HasCheckConstraint(
                "ck_documents_category",
                "category IS NULL OR category IN ('Identity', 'Visa', 'Housing', 'Employment', 'Education', 'Insurance', 'Health', 'Finance', 'Moving', 'Other')");
        });
    }
}
