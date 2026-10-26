using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Applications;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.HasKey(application => application.Id);

        builder.Property(application => application.Employer)
            .HasMaxLength(JobApplicationRules.EmployerMaxLength)
            .IsRequired()
            .HasConversion(value => value.Trim(), value => value);

        builder.Property(application => application.Role)
            .HasMaxLength(JobApplicationRules.RoleMaxLength)
            .IsRequired()
            .HasConversion(value => value.Trim(), value => value);

        builder.Property(application => application.Location)
            .HasMaxLength(JobApplicationRules.LocationMaxLength)
            .IsRequired(false)
            .HasConversion(
                location => string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
                location => location);

        builder.Property(application => application.AppliedOn)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(application => application.NextOn)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(application => application.Notes)
            .HasMaxLength(JobApplicationRules.NotesMaxLength)
            .IsRequired(false)
            .HasConversion(
                notes => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                notes => notes);

        builder.Property(application => application.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(application => application.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(application => application.RelocationId)
            .HasDatabaseName("ix_job_applications_relocation_id");

        builder.HasOne(application => application.Relocation)
            .WithMany()
            .HasForeignKey(application => application.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("job_applications", table =>
        {
            table.HasCheckConstraint(
                "ck_job_applications_employer",
                $"char_length(btrim(employer)) > 0 AND char_length(employer) <= {JobApplicationRules.EmployerMaxLength}");
            table.HasCheckConstraint(
                "ck_job_applications_role",
                $"char_length(btrim(role)) > 0 AND char_length(role) <= {JobApplicationRules.RoleMaxLength}");
            table.HasCheckConstraint(
                "ck_job_applications_location",
                $"location IS NULL OR (char_length(btrim(location)) > 0 AND char_length(location) <= {JobApplicationRules.LocationMaxLength})");
            table.HasCheckConstraint(
                "ck_job_applications_notes",
                $"notes IS NULL OR (char_length(btrim(notes)) > 0 AND char_length(notes) <= {JobApplicationRules.NotesMaxLength})");
            table.HasCheckConstraint(
                "ck_job_applications_status",
                "status IN ('Applied', 'Interviewing', 'Offer', 'Accepted', 'Rejected', 'Withdrawn')");
        });
    }
}
