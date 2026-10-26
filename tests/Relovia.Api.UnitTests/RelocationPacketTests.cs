using Relovia.Api.Features.Applications;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Export;
using Relovia.Api.Features.Housing;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;
using UglyToad.PdfPig;

namespace Relovia.Api.UnitTests;

public sealed class RelocationPacketTests
{
    private static readonly Guid NoticeId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000021");
    private static readonly Guid FlightId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000022");
    private static readonly Guid BoxesId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000023");
    private static readonly Guid HousingId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000031");
    private static readonly Guid DocumentId = Guid.Parse("0199f0a1-7c00-7000-8000-000000000061");

    [Fact]
    public void The_packet_contains_the_relocation_and_an_empty_one_says_so()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(-3), "Test", "Test");
        var packet = RelocationPacketComposer.Compose(
            Relocation(),
            Tasks(),
            Budget(),
            Expenses(),
            Items(),
            Homes(),
            Applications(),
            Events(),
            Documents(),
            zone);

        Assert.Equal("Berlin move.pdf", packet.FileName);
        Assert.Equal("Santiago, Chile to Berlin, Germany", packet.Route);
        Assert.Equal("1 Dec 2026 · International · Work · Active", packet.Summary);
        Assert.Equal("0 of 1 done", packet.Progress);

        var tasks = Section(packet, "Tasks");
        Assert.Equal(
            ["Give notice", "Book the flight", "Pack boxes"],
            tasks.Blocks.OfType<PacketBlock.Entry>().Select(entry => entry.Title));
        var notice = Entry(tasks, "Give notice");
        Assert.Null(notice.Heading);
        Assert.Contains("Tell the landlord in writing.", notice.Lines);
        Assert.Contains("https://example.com/notice", notice.Lines);
        Assert.Contains("Housing · To do · High · Due 2 Oct 2026", notice.Lines);
        var flight = Entry(tasks, "Book the flight");
        Assert.Null(flight.Heading);
        Assert.Contains("Housing · Skipped · Normal · Due 1 Nov 2026", flight.Lines);
        Assert.Contains("After Give notice (waiting)", flight.Lines);
        Assert.Contains("1 document", flight.Lines);
        var boxes = Entry(tasks, "Pack boxes");
        Assert.Null(boxes.Heading);
        Assert.Contains("Preparation · To do · Normal", boxes.Lines);

        var budget = Section(packet, "Budget");
        Assert.Contains(budget.Blocks, block => block is PacketBlock.Paragraph paragraph
            && paragraph.Text == "Monthly net income: EUR $4,000.00");
        Assert.Contains(budget.Blocks, block => block is PacketBlock.Paragraph paragraph
            && paragraph.Text == "Estimated EUR $1,200.00");
        Assert.Contains(budget.Blocks, block => block is PacketBlock.Paragraph paragraph
            && paragraph.Text.Contains("XTS $900,000.00 spent (1 expense)", StringComparison.Ordinal));
        var categories = Assert.IsType<PacketBlock.Table>(budget.Blocks.Single(block => block is PacketBlock.Table table && table.Heading == "Categories"));
        Assert.Equal(["Housing", "EUR $1,000.00", "EUR $200.00", "EUR $800.00 left"], categories.Rows[0]);
        var estimates = Assert.IsType<PacketBlock.Table>(budget.Blocks.Single(block => block is PacketBlock.Table table && table.Heading == "Estimates"));
        Assert.Equal("Deposit", estimates.Rows[0][0]);
        Assert.Equal("—", estimates.Rows[0][3]);
        Assert.Equal("≈ EUR $1,000.00", estimates.Rows[1][3]);
        var movers = Entry(budget, "Movers");
        Assert.Equal("Expenses", movers.Heading);
        Assert.Contains("20 Oct 2026 · Housing · Give notice", movers.Lines);
        Assert.Contains("CLP $900,000 · ≈ EUR $900.00", movers.Lines);
        Assert.Contains("Paid in cash", movers.Lines);
        var lamp = Entry(budget, "Lamp");
        Assert.Equal("Shopping", lamp.Heading);
        Assert.Contains("Housing", lamp.Lines);
        Assert.Contains("Estimate USD $45.50 · ≈ EUR $22.75", lamp.Lines);

        var inventory = Section(packet, "Inventory");
        var sheets = Entry(inventory, "Sheets × 2");
        Assert.Equal("Bedroom", sheets.Heading);
        Assert.Contains("Linens · Owned · Bring · Not packed · Normal", sheets.Lines);
        Assert.Equal("Living room", Entry(inventory, "Sofa").Heading);

        var housing = Section(packet, "Housing");
        var flat = Entry(housing, "Kreuzberg flat");
        Assert.Equal("Chosen", flat.Heading);
        Assert.Contains("Kreuzberg", flat.Lines);
        Assert.Contains("Rent EUR $1,200.00 monthly · 30.0% of income · Deposit EUR $2,400.00", flat.Lines);
        Assert.Contains("2 bedrooms · Furnished · Available 1 Nov 2026", flat.Lines);
        Assert.Contains("Pets allowed", flat.Lines);

        var applications = Section(packet, "Applications");
        var northwind = Entry(applications, "Northwind");
        Assert.Equal("Interviewing", northwind.Heading);
        Assert.Contains("Engineer · Berlin", northwind.Lines);
        Assert.Contains("Applied 1 Oct 2026 · Next 15 Nov 2026", northwind.Lines);
        Assert.Contains("Visa or relocation support", northwind.Lines);
        Assert.Contains("Second round", northwind.Lines);

        var calendar = Section(packet, "Calendar");
        var handover = Entry(calendar, "Key handover");
        Assert.Contains("Appointment · 15 Nov 2026, 09:30 – 10:00", handover.Lines);
        Assert.Contains("Landlord's office", handover.Lines);
        Assert.Contains("Milestone · 1 Dec 2026", Entry(calendar, "Move day").Lines);

        var documents = Section(packet, "Documents");
        var passport = Entry(documents, "passport.pdf");
        Assert.Contains("Visa · Expires 1 Jun 2027 · 12 KB", passport.Lines);
        Assert.Contains("Supports Book the flight", passport.Lines);

        var empty = RelocationPacketComposer.Compose(
            Relocation(),
            new RelocationTasksResponse(new TaskProgressResponse(0, 0, 0), []),
            new BudgetResponse(null, [], new BudgetTotalsResponse(0, 0, 0), [], null, false),
            [],
            [],
            [],
            [],
            [],
            [],
            TimeZoneInfo.Utc);
        Assert.Equal("Checklist not confirmed", empty.Progress);
        Assert.Equal("None yet.", Paragraph(Section(empty, "Tasks")));
        Assert.Equal("Not set up yet.", Paragraph(Section(empty, "Budget")));
        Assert.Equal("None yet.", Paragraph(Section(empty, "Inventory")));
        Assert.Equal("None yet.", Paragraph(Section(empty, "Housing")));
        Assert.Equal("None yet.", Paragraph(Section(empty, "Applications")));
        Assert.Equal("None yet.", Paragraph(Section(empty, "Calendar")));
        Assert.Equal("None yet.", Paragraph(Section(empty, "Documents")));

        var domestic = RelocationPacketComposer.Compose(
            Relocation() with { DestinationCountryCode = "CL", IsInternational = false },
            new RelocationTasksResponse(new TaskProgressResponse(0, 0, 0), []),
            new BudgetResponse(null, [], new BudgetTotalsResponse(0, 0, 0), [], null, false),
            [],
            [],
            [],
            [],
            [],
            [],
            TimeZoneInfo.Utc);
        Assert.DoesNotContain(domestic.Sections, section => section.Title is "Housing" or "Applications");
    }

    [Fact]
    public void The_pdf_contains_the_packet_text()
    {
        var packet = RelocationPacketComposer.Compose(
            Relocation(),
            Tasks(),
            Budget(),
            Expenses(),
            Items(),
            Homes(),
            Applications(),
            Events(),
            Documents(),
            TimeZoneInfo.Utc);
        var bytes = RelocationPdf.Render(packet);

        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
        using var document = PdfDocument.Open(bytes);
        var text = string.Join('\n', document.GetPages().Select(page => page.Text));
        Assert.Contains("Berlin move", text);
        Assert.Contains("Give notice", text);
        Assert.Contains("Tell the landlord in writing.", text);
        Assert.Contains("https://example.com/notice", text);
        Assert.Contains("EUR $1,200.00", text);
        Assert.Contains("passport.pdf", text);
        Assert.Contains("Key handover", text);
        Assert.Contains("Sheets", text);
        Assert.Contains("Kreuzberg flat", text);
        Assert.Contains("Monthly net income", text);
        Assert.Contains("30.0% of income", text);
        Assert.Contains("Northwind", text);
    }

    [Fact]
    public void The_summary_pdf_keeps_the_short_page()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(-3), "Test", "Test");
        var packet = RelocationPacketComposer.ComposeSummary(
            Relocation(),
            Tasks(),
            Budget(),
            Items(),
            Events(),
            Documents(),
            zone);

        Assert.Equal("Berlin move summary.pdf", packet.FileName);
        var openTasks = Section(packet, "Open tasks").Blocks.OfType<PacketBlock.Entry>().Select(entry => entry.Title);
        Assert.Equal(["Give notice", "Pack boxes"], openTasks);
        var openNotice = Entry(Section(packet, "Open tasks"), "Give notice");
        Assert.Equal("Give notice", openNotice.Title);
        Assert.Contains("Housing · High · Due 2 Oct 2026", openNotice.Lines);
        Assert.DoesNotContain("Tell the landlord in writing.", openNotice.Lines);
        Assert.DoesNotContain("https://example.com/notice", openNotice.Lines);
        Assert.DoesNotContain(Section(packet, "Open tasks").Blocks.OfType<PacketBlock.Entry>(), entry => entry.Title == "Book the flight");
        Assert.Contains(
            Section(packet, "Money").Blocks.OfType<PacketBlock.Paragraph>(),
            paragraph => paragraph.Text == "Housing · EUR $800.00 left");
        Assert.DoesNotContain(
            Section(packet, "Money").Blocks.OfType<PacketBlock.Paragraph>(),
            paragraph => paragraph.Text.Contains("Monthly net income", StringComparison.Ordinal));
        Assert.DoesNotContain(
            Section(packet, "Money").Blocks.OfType<PacketBlock.Paragraph>(),
            paragraph => paragraph.Text.Contains("Movers", StringComparison.Ordinal));
        Assert.Equal("Sheets × 2", Entry(Section(packet, "Still to pack"), "Sheets × 2").Title);
        Assert.DoesNotContain(Section(packet, "Still to pack").Blocks.OfType<PacketBlock.Entry>(), entry => entry.Title == "Sofa");
        var dates = Section(packet, "Dates").Blocks.OfType<PacketBlock.Entry>().ToArray();
        Assert.Equal(["15 Nov 2026", "1 Dec 2026", "1 Jun 2027"], dates.Select(entry => entry.Title));
        Assert.Contains("Key handover · 09:30 – 10:00 · Landlord's office", dates[0].Lines);
        Assert.Contains("passport.pdf · expires · Visa", dates[2].Lines);

        var bytes = RelocationPdf.Render(packet);
        using var document = PdfDocument.Open(bytes);
        var text = string.Join('\n', document.GetPages().Select(page => page.Text));
        Assert.Contains("Give notice", text);
        Assert.DoesNotContain("Tell the landlord in writing.", text);
        Assert.DoesNotContain("https://example.com/notice", text);
        Assert.DoesNotContain("Movers", text);
        Assert.DoesNotContain("Monthly net income", text);
        Assert.DoesNotContain("% of income", text);
    }

    private static PacketSection Section(RelocationPacket packet, string title)
    {
        return Assert.Single(packet.Sections, section => section.Title == title);
    }

    private static PacketBlock.Entry Entry(PacketSection section, string title)
    {
        return Assert.Single(section.Blocks.OfType<PacketBlock.Entry>(), entry => entry.Title == title);
    }

    private static string Paragraph(PacketSection section)
    {
        return Assert.Single(section.Blocks.OfType<PacketBlock.Paragraph>()).Text;
    }

    private static RelocationResponse Relocation()
    {
        return new RelocationResponse(
            Guid.Parse("0199f0a1-7c00-7000-8000-000000000010"),
            "Berlin move",
            "CL",
            "Santiago",
            "DE",
            "Berlin",
            new DateOnly(2026, 12, 1),
            RelocationPurpose.Work,
            RelocationStatus.Active,
            true,
            DateTimeOffset.Parse("2026-10-12T12:00:00Z"),
            DateTimeOffset.Parse("2026-10-12T12:00:00Z"));
    }

    private static RelocationTasksResponse Tasks()
    {
        return new RelocationTasksResponse(
            new TaskProgressResponse(0, 1, 0),
            [
                new RelocationTaskResponse(
                    BoxesId,
                    "pack-boxes",
                    "Pack boxes",
                    null,
                    null,
                    3,
                    null,
                    TaskPhase.Preparation,
                    TaskPriority.Normal,
                    RelocationTaskStatus.Todo,
                    null,
                    [],
                    [],
                    2),
                new RelocationTaskResponse(
                    NoticeId,
                    "give-notice",
                    "Give notice",
                    "Tell the landlord in writing.",
                    "https://example.com/notice",
                    60,
                    new DateOnly(2026, 10, 2),
                    TaskPhase.Housing,
                    TaskPriority.High,
                    RelocationTaskStatus.Todo,
                    null,
                    [],
                    [],
                    0),
                new RelocationTaskResponse(
                    FlightId,
                    "book-flight",
                    "Book the flight",
                    null,
                    null,
                    30,
                    new DateOnly(2026, 11, 1),
                    TaskPhase.Housing,
                    TaskPriority.Normal,
                    RelocationTaskStatus.Skipped,
                    null,
                    [new TaskDependencyResponse(NoticeId, "give-notice", false)],
                    [DocumentId],
                    1),
            ]);
    }

    private static BudgetResponse Budget()
    {
        return new BudgetResponse(
            "EUR",
            [
                new BudgetCategoryResponse(
                    HousingId,
                    "Housing",
                    [
                        new BudgetEstimateResponse(
                            Guid.Parse("0199f0a1-7c00-7000-8000-000000000041"),
                            HousingId,
                            "Deposit",
                            1000m,
                            "EUR",
                            1000m),
                        new BudgetEstimateResponse(
                            Guid.Parse("0199f0a1-7c00-7000-8000-000000000042"),
                            HousingId,
                            "Sofa",
                            1200m,
                            "NZD",
                            1000m),
                    ],
                    1000m,
                    200m,
                    800m,
                    1,
                    [
                        new ShoppingLineResponse(
                            Guid.Parse("0199f0a1-7c00-7000-8000-000000000043"),
                            "Lamp",
                            45.5m,
                            null,
                            "USD",
                            22.75m,
                            null),
                    ]),
            ],
            new BudgetTotalsResponse(1200m, 200m, 1000m),
            [
                new UnconvertedAmountsResponse("XTS", 1, 900000m, 0, 0, 0, 0, 0),
            ],
            DateTimeOffset.Parse("2026-10-17T00:00:00Z"),
            false,
            4000m,
            "EUR");
    }

    private static IReadOnlyList<ExpenseResponse> Expenses()
    {
        return
        [
            new ExpenseResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000051"),
                HousingId,
                NoticeId,
                "Movers",
                900000m,
                "CLP",
                900m,
                new DateOnly(2026, 10, 20),
                " Paid in cash "),
        ];
    }

    private static IReadOnlyList<InventoryItemResponse> Items()
    {
        return
        [
            new InventoryItemResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000071"),
                "Sheets",
                ItemCategory.Linens,
                ItemRoom.Bedroom,
                2,
                AcquisitionStatus.Owned,
                MovingAction.Bring,
                PackingStatus.NotPacked,
                ItemPriority.Normal,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            new InventoryItemResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000072"),
                "Sofa",
                ItemCategory.Furniture,
                ItemRoom.LivingRoom,
                1,
                AcquisitionStatus.Owned,
                MovingAction.Bring,
                PackingStatus.Packed,
                ItemPriority.High,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
        ];
    }

    private static IReadOnlyList<HousingOptionResponse> Homes()
    {
        return
        [
            new HousingOptionResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000071"),
                "Kreuzberg flat",
                "Kreuzberg",
                1200m,
                2400m,
                "EUR",
                "2 bedrooms",
                HousingFurnishing.Furnished,
                new DateOnly(2026, 11, 1),
                HousingStatus.Chosen,
                "Pets allowed",
                RentPeriod.Monthly,
                30.0m),
        ];
    }

    private static IReadOnlyList<JobApplicationResponse> Applications()
    {
        return
        [
            new JobApplicationResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000081"),
                "Northwind",
                "Engineer",
                "Berlin",
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 11, 15),
                JobApplicationStatus.Interviewing,
                true,
                "Second round"),
        ];
    }

    private static IReadOnlyList<CalendarEventResponse> Events()
    {
        return
        [
            new CalendarEventResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000081"),
                "Key handover",
                CalendarEventKind.Appointment,
                null,
                new DateTimeOffset(2026, 11, 15, 12, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 15, 13, 0, 0, TimeSpan.Zero),
                "Landlord's office"),
            new CalendarEventResponse(
                Guid.Parse("0199f0a1-7c00-7000-8000-000000000082"),
                "Move day",
                CalendarEventKind.Milestone,
                new DateOnly(2026, 12, 1),
                null,
                null,
                null),
        ];
    }

    private static IReadOnlyList<DocumentResponse> Documents()
    {
        return
        [
            new DocumentResponse(
                DocumentId,
                "passport.pdf",
                "application/pdf",
                12 * 1024,
                DateTimeOffset.Parse("2026-10-24T13:50:00Z"),
                DocumentCategory.Visa,
                new DateOnly(2027, 6, 1),
                [FlightId]),
        ];
    }
}
