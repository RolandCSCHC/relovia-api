using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class TaskTemplateDependencyConfiguration : IEntityTypeConfiguration<TaskTemplateDependency>
{
    public void Configure(EntityTypeBuilder<TaskTemplateDependency> builder)
    {
        builder.HasKey(dependency => dependency.Id);

        builder.Property(dependency => dependency.DependsOnCode)
            .HasMaxLength(ChecklistTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => ChecklistTemplateRules.NormalizeCode(code), code => code);

        builder.HasIndex(dependency => new { dependency.TaskTemplateId, dependency.DependsOnCode })
            .IsUnique()
            .HasDatabaseName("ux_task_template_dependencies_task_id_code");

        builder.HasOne(dependency => dependency.TaskTemplate)
            .WithMany(task => task.Dependencies)
            .HasForeignKey(dependency => dependency.TaskTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
