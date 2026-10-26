using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class RelocationTaskConfiguration : IEntityTypeConfiguration<RelocationTask>
{
    public void Configure(EntityTypeBuilder<RelocationTask> builder)
    {
        builder.HasKey(task => task.Id);

        builder.HasAlternateKey(task => new { task.RelocationId, task.Id })
            .HasName("ak_tasks_relocation_id_id");

        builder.Property(task => task.Code)
            .HasMaxLength(ChecklistTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => ChecklistTemplateRules.NormalizeCode(code), code => code);

        builder.Property(task => task.Title)
            .HasMaxLength(ChecklistTemplateRules.TitleMaxLength)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(task => task.Description)
            .HasMaxLength(RelocationTaskRules.DescriptionMaxLength)
            .IsRequired(false)
            .HasConversion(
                description => string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                description => description);

        builder.Property(task => task.Url)
            .HasMaxLength(RelocationTaskRules.UrlMaxLength)
            .IsRequired(false)
            .HasConversion(
                url => string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
                url => url);

        builder.Property(task => task.DueDate)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(task => task.Phase)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(RelocationTaskStatus.Todo);

        builder.Property(task => task.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(task => task.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(task => new { task.RelocationId, task.Code })
            .IsUnique()
            .HasDatabaseName("ux_tasks_relocation_id_code");

        builder.HasOne(task => task.Relocation)
            .WithMany(relocation => relocation.Tasks)
            .HasForeignKey(task => task.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(task => task.SourceTemplateTask)
            .WithMany()
            .HasForeignKey(task => task.SourceTemplateTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("tasks", table =>
        {
            table.HasCheckConstraint(
                "ck_tasks_phase",
                "phase IN ('Preparation', 'Housing', 'Administration', 'Finances', 'Moving', 'Settlement')");
            table.HasCheckConstraint(
                "ck_tasks_priority",
                "priority IN ('Low', 'Normal', 'High')");
            table.HasCheckConstraint(
                "ck_tasks_status",
                "status IN ('Todo', 'InProgress', 'Done', 'Skipped')");
            table.HasCheckConstraint(
                "ck_tasks_description",
                $"description IS NULL OR (char_length(btrim(description)) > 0 AND char_length(description) <= {RelocationTaskRules.DescriptionMaxLength})");
            table.HasCheckConstraint(
                "ck_tasks_url",
                $"url IS NULL OR (char_length(btrim(url)) > 0 AND char_length(url) <= {RelocationTaskRules.UrlMaxLength})");
        });
    }
}
