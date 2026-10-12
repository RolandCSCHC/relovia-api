namespace Relovia.Api.Features.Relocations;

public static class StarterChecklistTemplates
{
    public const string DomesticCode = "domestic-core";

    public const string InternationalCode = "international-core";

    public static readonly DateTimeOffset PublishedAt = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid DomesticTemplateId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000001");

    public static readonly Guid InternationalTemplateId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000002");

    private static readonly Guid GiveNoticeDomesticId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000010");

    private static readonly Guid BookMoversId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000011");

    private static readonly Guid TransferUtilitiesId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000012");

    private static readonly Guid ChangeAddressId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000013");

    private static readonly Guid UnpackEssentialsId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000014");

    private static readonly Guid CheckPassportId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000020");

    private static readonly Guid GiveNoticeInternationalId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000021");

    private static readonly Guid ArrangeHousingId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000022");

    private static readonly Guid NotifyBankId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000023");

    private static readonly Guid BookTravelId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000024");

    private static readonly Guid RegisterArrivalId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000025");

    public static IReadOnlyList<ChecklistTemplate> Templates { get; } =
    [
        Template(DomesticTemplateId, DomesticCode, "Domestic move", ChecklistScope.Domestic),
        Template(InternationalTemplateId, InternationalCode, "International move", ChecklistScope.International),
    ];

    public static IReadOnlyList<TaskTemplate> Tasks { get; } =
    [
        Task(GiveNoticeDomesticId, DomesticTemplateId, "give-notice", "Give notice on your current home", 60, TaskPhase.Preparation, TaskPriority.High),
        Task(BookMoversId, DomesticTemplateId, "book-movers", "Book movers or a van", 30, TaskPhase.Moving, TaskPriority.High),
        Task(TransferUtilitiesId, DomesticTemplateId, "transfer-utilities", "Set up utilities at the new home", 14, TaskPhase.Housing, TaskPriority.Normal),
        Task(ChangeAddressId, DomesticTemplateId, "change-address", "Update your address", 7, TaskPhase.Administration, TaskPriority.Normal),
        Task(UnpackEssentialsId, DomesticTemplateId, "unpack-essentials", "Unpack the essentials", -1, TaskPhase.Settlement, TaskPriority.Normal),
        Task(CheckPassportId, InternationalTemplateId, "check-passport", "Check that your passport is valid", 120, TaskPhase.Preparation, TaskPriority.High),
        Task(GiveNoticeInternationalId, InternationalTemplateId, "give-notice", "Give notice on your current home", 60, TaskPhase.Preparation, TaskPriority.High),
        Task(ArrangeHousingId, InternationalTemplateId, "arrange-housing", "Arrange the first place to stay", 45, TaskPhase.Housing, TaskPriority.High),
        Task(NotifyBankId, InternationalTemplateId, "notify-bank", "Tell your bank you are moving", 30, TaskPhase.Finances, TaskPriority.Normal),
        Task(BookTravelId, InternationalTemplateId, "book-travel", "Book travel for the move", 21, TaskPhase.Moving, TaskPriority.High),
        Task(RegisterArrivalId, InternationalTemplateId, "register-arrival", "Register your arrival locally", -14, TaskPhase.Settlement, TaskPriority.High),
    ];

    public static IReadOnlyList<TaskTemplateDependency> Dependencies { get; } =
    [
        Dependency(Guid.Parse("0199f0a1-7c00-7000-8000-000000000101"), BookMoversId, "give-notice"),
        Dependency(Guid.Parse("0199f0a1-7c00-7000-8000-000000000102"), UnpackEssentialsId, "book-movers"),
        Dependency(Guid.Parse("0199f0a1-7c00-7000-8000-000000000103"), BookTravelId, "check-passport"),
        Dependency(Guid.Parse("0199f0a1-7c00-7000-8000-000000000104"), RegisterArrivalId, "arrange-housing"),
    ];

    private static ChecklistTemplate Template(Guid id, string code, string title, ChecklistScope scope)
    {
        return new ChecklistTemplate
        {
            Id = id,
            Code = code,
            Version = 1,
            Title = title,
            Scope = scope,
            PublishedAt = PublishedAt,
            CreatedAt = PublishedAt,
            UpdatedAt = PublishedAt,
        };
    }

    private static TaskTemplate Task(
        Guid id,
        Guid templateId,
        string code,
        string title,
        int daysBeforeMove,
        TaskPhase phase,
        TaskPriority priority)
    {
        return new TaskTemplate
        {
            Id = id,
            ChecklistTemplateId = templateId,
            Code = code,
            Title = title,
            DaysBeforeMove = daysBeforeMove,
            Phase = phase,
            Priority = priority,
            CreatedAt = PublishedAt,
            UpdatedAt = PublishedAt,
        };
    }

    private static TaskTemplateDependency Dependency(Guid id, Guid taskId, string dependsOnCode)
    {
        return new TaskTemplateDependency
        {
            Id = id,
            TaskTemplateId = taskId,
            DependsOnCode = dependsOnCode,
        };
    }
}
