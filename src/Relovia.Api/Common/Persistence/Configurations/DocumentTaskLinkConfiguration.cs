using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Documents;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class DocumentTaskLinkConfiguration : IEntityTypeConfiguration<DocumentTaskLink>
{
    public void Configure(EntityTypeBuilder<DocumentTaskLink> builder)
    {
        builder.ToTable("document_task_links");

        builder.HasKey(link => link.Id);

        builder.HasIndex(link => new { link.DocumentId, link.TaskId })
            .IsUnique()
            .HasDatabaseName("ux_document_task_links_document_id_task_id");

        builder.HasOne(link => link.Document)
            .WithMany(document => document.Links)
            .HasForeignKey(link => new { link.RelocationId, link.DocumentId })
            .HasPrincipalKey(document => new { document.RelocationId, document.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Task)
            .WithMany()
            .HasForeignKey(link => new { link.RelocationId, link.TaskId })
            .HasPrincipalKey(task => new { task.RelocationId, task.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
