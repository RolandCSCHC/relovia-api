using Relovia.Api.Features.Applications;

namespace Relovia.Api.UnitTests;

public sealed class JobApplicationRulesTests
{
    [Fact]
    public void An_application_keeps_a_trimmed_employer_and_role()
    {
        var errors = JobApplicationRules.Validate(new SaveJobApplicationRequest
        {
            Employer = " Northwind ",
            Role = " Engineer ",
            Location = "  ",
            Status = JobApplicationStatus.Applied,
        });

        Assert.Empty(errors);
        Assert.Equal("Northwind", JobApplicationRules.NormalizeRequired(" Northwind "));
        Assert.Null(JobApplicationRules.NormalizeOptional("  "));
    }

    [Fact]
    public void An_application_needs_an_employer_a_role_and_a_known_status()
    {
        var errors = JobApplicationRules.Validate(new SaveJobApplicationRequest
        {
            Employer = " ",
            Role = "",
            Status = (JobApplicationStatus)100,
        });

        Assert.Equal(["Employer is required."], errors["employer"]);
        Assert.Equal(["Role is required."], errors["role"]);
        Assert.Equal([JobApplicationRules.UnknownStatusMessage], errors["status"]);
    }
}
