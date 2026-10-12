using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("task_dependencies");

        builder.HasKey(dependency => dependency.Id);

        builder.HasIndex(dependency => new { dependency.TaskId, dependency.DependsOnTaskId })
            .IsUnique()
            .HasDatabaseName("ux_task_dependencies_task_id_depends_on_task_id");

        builder.HasOne(dependency => dependency.Task)
            .WithMany(task => task.Dependencies)
            .HasForeignKey(dependency => new { dependency.RelocationId, dependency.TaskId })
            .HasPrincipalKey(task => new { task.RelocationId, task.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dependency => dependency.DependsOnTask)
            .WithMany(task => task.Dependents)
            .HasForeignKey(dependency => new { dependency.RelocationId, dependency.DependsOnTaskId })
            .HasPrincipalKey(task => new { task.RelocationId, task.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
