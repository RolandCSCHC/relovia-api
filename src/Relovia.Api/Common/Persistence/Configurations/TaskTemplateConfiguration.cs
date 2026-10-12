using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Code)
            .HasMaxLength(ChecklistTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => ChecklistTemplateRules.NormalizeCode(code), code => code);

        builder.Property(task => task.Title)
            .HasMaxLength(ChecklistTemplateRules.TitleMaxLength)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(task => task.Phase)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(task => task.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(task => new { task.ChecklistTemplateId, task.Code })
            .IsUnique()
            .HasDatabaseName("ux_task_templates_template_id_code");

        builder.HasData(StarterChecklistTemplates.Tasks.Cast<object>().ToArray());

        builder.HasOne(task => task.ChecklistTemplate)
            .WithMany(template => template.Tasks)
            .HasForeignKey(task => task.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_task_templates_phase",
                "phase IN ('Preparation', 'Housing', 'Administration', 'Finances', 'Moving', 'Settlement')");
            table.HasCheckConstraint(
                "ck_task_templates_priority",
                "priority IN ('Low', 'Normal', 'High')");
        });
    }
}
