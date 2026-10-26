using System.Globalization;
using Relovia.Api.Features.Applications;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Housing;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Export;

public static class RelocationPacketComposer
{
    public static RelocationPacket Compose(
        RelocationResponse relocation,
        RelocationTasksResponse tasks,
        BudgetResponse budget,
        IReadOnlyList<ExpenseResponse> expenses,
        IReadOnlyList<InventoryItemResponse> items,
        IReadOnlyList<HousingOptionResponse> homes,
        IReadOnlyList<JobApplicationResponse> applications,
        IReadOnlyList<CalendarEventResponse> events,
        IReadOnlyList<DocumentResponse> documents,
        TimeZoneInfo timeZone)
    {
        var taskTitles = tasks.Tasks.ToDictionary(task => task.Id, task => task.Title);
        var sections = new List<PacketSection>
        {
            Tasks(tasks, taskTitles),
            Budget(budget, expenses, taskTitles),
            Inventory(items),
        };
        Add(sections, Homes(relocation, homes));
        Add(sections, Applications(relocation, applications));
        sections.Add(Calendar(events, timeZone));
        sections.Add(Documents(documents, taskTitles));
        return new RelocationPacket(
            ExportText.FileName(relocation.Name),
            relocation.Name,
            $"{Place(relocation.OriginCity, relocation.OriginCountryCode)} to {Place(relocation.DestinationCity, relocation.DestinationCountryCode)}",
            Join(
                relocation.TargetDate is DateOnly target ? ExportText.Date(target) : null,
                relocation.IsInternational ? "International" : "Domestic",
                relocation.Purpose.ToString(),
                relocation.Status.ToString()),
            Progress(tasks),
            sections);
    }

    public static RelocationPacket ComposeSummary(
        RelocationResponse relocation,
        RelocationTasksResponse tasks,
        BudgetResponse budget,
        IReadOnlyList<InventoryItemResponse> items,
        IReadOnlyList<CalendarEventResponse> events,
        IReadOnlyList<DocumentResponse> documents,
        TimeZoneInfo timeZone)
    {
        var taskTitles = tasks.Tasks.ToDictionary(task => task.Id, task => task.Title);
        return new RelocationPacket(
            ExportText.SummaryFileName(relocation.Name),
            relocation.Name,
            $"{Place(relocation.OriginCity, relocation.OriginCountryCode)} to {Place(relocation.DestinationCity, relocation.DestinationCountryCode)}",
            Join(
                relocation.TargetDate is DateOnly target ? ExportText.Date(target) : null,
                relocation.IsInternational ? "International" : "Domestic",
                relocation.Purpose.ToString(),
                relocation.Status.ToString()),
            Progress(tasks),
            [
                OpenTasks(tasks, taskTitles),
                SummaryMoney(budget),
                StillToPack(items),
                SummaryDates(events, documents, timeZone),
            ]);
    }

    private static PacketSection OpenTasks(
        RelocationTasksResponse tasks,
        IReadOnlyDictionary<Guid, string> taskTitles)
    {
        var open = tasks.Tasks
            .OrderBy(task => task.Position)
            .ThenBy(task => task.Id)
            .Where(task => task.Status is RelocationTaskStatus.Todo or RelocationTaskStatus.InProgress)
            .ToArray();
        if (open.Length == 0)
        {
            return new PacketSection("Open tasks", [new PacketBlock.Paragraph("No open tasks.")]);
        }

        var blocks = open.Select(task =>
        {
            var lines = new List<string>
            {
                Join(task.Phase.ToString(), task.Priority.ToString(), Due(task.DueDate)),
            };
            if (task.Dependencies.Count > 0)
            {
                var notes = task.Dependencies.Select(dependency =>
                {
                    var title = taskTitles.GetValueOrDefault(dependency.TaskId) ?? dependency.Code;
                    return dependency.IsSatisfied ? title : $"{title} (waiting)";
                });
                lines.Add("After " + string.Join(", ", notes));
            }

            return (PacketBlock)new PacketBlock.Entry(null, task.Title, lines);
        }).ToArray();
        return new PacketSection("Open tasks", blocks);
    }

    private static PacketSection SummaryMoney(BudgetResponse budget)
    {
        if (budget.CurrencyCode is not string currency)
        {
            return new PacketSection("Money", [new PacketBlock.Paragraph("The budget is not set up yet.")]);
        }

        var blocks = new List<PacketBlock>
        {
            new PacketBlock.Paragraph($"Estimated {ExportText.Money(budget.Totals.Estimated, currency)}"),
            new PacketBlock.Paragraph($"Spent {ExportText.Money(budget.Totals.Actual, currency)}"),
            new PacketBlock.Paragraph($"Remaining {ExportText.Remaining(budget.Totals.Remaining, currency)}"),
        };
        if (budget.RatesUnavailable)
        {
            blocks.Add(new PacketBlock.Paragraph(
                "Exchange rates could not be loaded right now, so amounts in other currencies are not counted."));
        }

        var leftOut = LeftOut(budget, currency);
        if (leftOut is not null)
        {
            blocks.Add(new PacketBlock.Paragraph(leftOut));
        }

        if (budget.RatesAsOf is DateTimeOffset ratesAsOf)
        {
            blocks.Add(new PacketBlock.Paragraph(
                $"Amounts in other currencies use the rates of {ExportText.Date(DateOnly.FromDateTime(ratesAsOf.UtcDateTime))}."));
        }

        foreach (var category in budget.Categories)
        {
            blocks.Add(new PacketBlock.Paragraph(
                $"{category.Name} · {ExportText.Remaining(category.Remaining, currency)}"));
        }

        return new PacketSection("Money", blocks);
    }

    private static PacketSection StillToPack(IReadOnlyList<InventoryItemResponse> items)
    {
        var bringing = items.Where(item => item.MovingAction == MovingAction.Bring).ToArray();
        var waiting = bringing.Where(item => item.PackingStatus == PackingStatus.NotPacked).ToArray();
        if (bringing.Length == 0)
        {
            return new PacketSection("Still to pack", [new PacketBlock.Paragraph("Nothing is marked to bring.")]);
        }

        if (waiting.Length == 0)
        {
            return new PacketSection("Still to pack", [new PacketBlock.Paragraph("Nothing left to pack.")]);
        }

        var blocks = waiting.Select(item =>
        {
            var title = item.Quantity > 1 ? $"{item.Name} × {item.Quantity}" : item.Name;
            var line = Join(Room(item.Room), "Not packed", Acquisition(item.Acquisition), "Bring");
            return (PacketBlock)new PacketBlock.Entry(null, title, [line]);
        }).ToArray();
        return new PacketSection("Still to pack", blocks);
    }

    private static PacketSection SummaryDates(
        IReadOnlyList<CalendarEventResponse> events,
        IReadOnlyList<DocumentResponse> documents,
        TimeZoneInfo timeZone)
    {
        var rows = new List<(DateOnly Date, string Text)>();
        foreach (var calendarEvent in events)
        {
            if (calendarEvent.Kind == CalendarEventKind.Milestone && calendarEvent.OccursOn is DateOnly day)
            {
                rows.Add((day, Join(calendarEvent.Title, "Milestone", calendarEvent.Location)));
            }

            if (calendarEvent.Kind == CalendarEventKind.Appointment && calendarEvent.StartsAt is DateTimeOffset start)
            {
                rows.Add((
                    ExportText.LocalDate(start, timeZone),
                    Join(calendarEvent.Title, ExportText.Clock(start, calendarEvent.EndsAt, timeZone), calendarEvent.Location)));
            }
        }

        foreach (var document in documents)
        {
            if (document.ExpiresOn is DateOnly expires)
            {
                rows.Add((expires, Join(document.FileName, "expires", document.Category?.ToString())));
            }
        }

        if (rows.Count == 0)
        {
            return new PacketSection("Dates", [new PacketBlock.Paragraph("No dates yet.")]);
        }

        var blocks = rows
            .OrderBy(row => row.Date)
            .ThenBy(row => row.Text, StringComparer.Ordinal)
            .Select(row => (PacketBlock)new PacketBlock.Entry(null, ExportText.Date(row.Date), [row.Text]))
            .ToArray();
        return new PacketSection("Dates", blocks);
    }

    private static PacketSection Tasks(
        RelocationTasksResponse tasks,
        IReadOnlyDictionary<Guid, string> taskTitles)
    {
        if (tasks.Tasks.Count == 0)
        {
            return new PacketSection("Tasks", [new PacketBlock.Paragraph("None yet.")]);
        }

        var blocks = tasks.Tasks
            .OrderBy(task => task.Position)
            .ThenBy(task => task.Id)
            .Select(task =>
            {
                var lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(task.Description))
                {
                    lines.Add(task.Description);
                }

                if (!string.IsNullOrWhiteSpace(task.Url))
                {
                    lines.Add(task.Url);
                }

                lines.Add(Join(task.Phase.ToString(), Status(task.Status), task.Priority.ToString(), Due(task.DueDate)));
                if (task.Dependencies.Count > 0)
                {
                    var notes = task.Dependencies.Select(dependency =>
                    {
                        var title = taskTitles.GetValueOrDefault(dependency.TaskId) ?? dependency.Code;
                        return dependency.IsSatisfied ? title : $"{title} (waiting)";
                    });
                    lines.Add("After " + string.Join(", ", notes));
                }

                if (task.DocumentIds.Count == 1)
                {
                    lines.Add("1 document");
                }
                else if (task.DocumentIds.Count > 1)
                {
                    lines.Add($"{task.DocumentIds.Count} documents");
                }

                return (PacketBlock)new PacketBlock.Entry(null, task.Title, lines);
            })
            .ToArray();

        return new PacketSection("Tasks", blocks);
    }

    private static PacketSection Budget(
        BudgetResponse budget,
        IReadOnlyList<ExpenseResponse> expenses,
        IReadOnlyDictionary<Guid, string> taskTitles)
    {
        if (budget.CurrencyCode is not string currency)
        {
            return new PacketSection("Budget", [new PacketBlock.Paragraph("Not set up yet.")]);
        }

        var blocks = new List<PacketBlock>
        {
            new PacketBlock.Paragraph($"Budget currency: {currency}"),
        };
        if (budget.MonthlyNetIncome is decimal income && budget.MonthlyNetIncomeCurrency is string incomeCurrency)
        {
            blocks.Add(new PacketBlock.Paragraph(
                $"Monthly net income: {ExportText.Money(income, incomeCurrency, exact: true)}"));
        }

        blocks.Add(new PacketBlock.Paragraph($"Estimated {ExportText.Money(budget.Totals.Estimated, currency)}"));
        blocks.Add(new PacketBlock.Paragraph($"Spent {ExportText.Money(budget.Totals.Actual, currency)}"));
        blocks.Add(new PacketBlock.Paragraph($"Remaining {ExportText.Remaining(budget.Totals.Remaining, currency)}"));
        if (budget.RatesUnavailable)
        {
            blocks.Add(new PacketBlock.Paragraph(
                "Exchange rates could not be loaded right now, so amounts in other currencies are not counted."));
        }
        else if (budget.RatesAsOf is DateTimeOffset ratesAsOf)
        {
            blocks.Add(new PacketBlock.Paragraph(
                $"Amounts in other currencies use the rates of {ExportText.Date(DateOnly.FromDateTime(ratesAsOf.UtcDateTime))}."));
        }

        var leftOut = LeftOut(budget, currency);
        if (leftOut is not null)
        {
            blocks.Add(new PacketBlock.Paragraph(leftOut));
        }

        if (budget.Categories.Count > 0)
        {
            blocks.Add(new PacketBlock.Table(
                "Categories",
                ["Category", "Estimated", "Spent", "Remaining"],
                budget.Categories
                    .Select(category => (IReadOnlyList<string>)
                    [
                        category.Name,
                        ExportText.Money(category.Estimated, currency),
                        ExportText.Money(category.Actual, currency),
                        ExportText.Remaining(category.Remaining, currency),
                    ])
                    .ToArray()));
        }

        var estimates = budget.Categories
            .SelectMany(category => category.Estimates.Select(estimate => (category.Name, estimate)))
            .ToArray();
        if (estimates.Length > 0)
        {
            blocks.Add(new PacketBlock.Table(
                "Estimates",
                ["Estimate", "Category", "Amount", "In the budget"],
                estimates
                    .Select(row => (IReadOnlyList<string>)
                    [
                        row.estimate.Label,
                        row.Name,
                        ExportText.Money(row.estimate.Amount, row.estimate.CurrencyCode, exact: true),
                        InBudget(row.estimate.CurrencyCode, currency, row.estimate.ConvertedAmount),
                    ])
                    .ToArray()));
        }

        var names = budget.Categories.ToDictionary(category => category.Id, category => category.Name);
        for (var index = 0; index < expenses.Count; index++)
        {
            var expense = expenses[index];
            var lines = new List<string>
            {
                Join(
                    ExportText.Date(expense.PaidOn),
                    names.GetValueOrDefault(expense.CategoryId) ?? "Category",
                    expense.TaskId is Guid taskId ? taskTitles.GetValueOrDefault(taskId) : null),
                Join(
                    ExportText.Money(expense.Amount, expense.CurrencyCode, exact: true),
                    Converted(expense.CurrencyCode, currency, expense.ConvertedAmount)),
            };
            if (!string.IsNullOrWhiteSpace(expense.Notes))
            {
                lines.Add(expense.Notes.Trim());
            }

            blocks.Add(new PacketBlock.Entry(index == 0 ? "Expenses" : null, expense.Description, lines));
        }

        var shopping = budget.Categories
            .SelectMany(category => category.Shopping.Select(line => (category.Name, line)))
            .ToArray();
        for (var index = 0; index < shopping.Length; index++)
        {
            var (categoryName, line) = shopping[index];
            var lines = new List<string> { categoryName };
            if (line.EstimatedPrice is decimal estimated)
            {
                lines.Add(Join(
                    "Estimate " + ExportText.Money(estimated, line.CurrencyCode, exact: true),
                    Converted(line.CurrencyCode, currency, line.ConvertedEstimated)));
            }

            if (line.ActualPrice is decimal actual)
            {
                lines.Add(Join(
                    "Paid " + ExportText.Money(actual, line.CurrencyCode, exact: true),
                    Converted(line.CurrencyCode, currency, line.ConvertedActual)));
            }

            if (line.EstimatedPrice is null && line.ActualPrice is null)
            {
                lines.Add("No price yet.");
            }

            blocks.Add(new PacketBlock.Entry(index == 0 ? "Shopping" : null, line.Name, lines));
        }

        return new PacketSection("Budget", blocks);
    }

    private static PacketSection Inventory(IReadOnlyList<InventoryItemResponse> items)
    {
        if (items.Count == 0)
        {
            return new PacketSection("Inventory", [new PacketBlock.Paragraph("None yet.")]);
        }

        var blocks = new List<PacketBlock>();
        string? room = null;
        foreach (var item in items)
        {
            var heading = Room(item.Room);
            var showHeading = heading != room;
            room = heading;
            var lines = new List<string>
            {
                Join(
                    item.Category?.ToString(),
                    Acquisition(item.Acquisition),
                    item.MovingAction?.ToString(),
                    Packing(item.PackingStatus),
                    item.Priority.ToString()),
            };
            if (item.EstimatedPrice is decimal estimated && item.CurrencyCode is string estimatedCode)
            {
                lines.Add("Estimated " + ExportText.Money(estimated, estimatedCode, exact: true));
            }

            if (item.ActualPrice is decimal actual && item.CurrencyCode is string actualCode)
            {
                lines.Add("Actual " + ExportText.Money(actual, actualCode, exact: true));
            }

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                lines.Add(item.Notes.Trim());
            }

            var title = item.Quantity > 1 ? $"{item.Name} × {item.Quantity}" : item.Name;
            blocks.Add(new PacketBlock.Entry(showHeading ? heading : null, title, lines));
        }

        return new PacketSection("Inventory", blocks);
    }

    private static PacketSection Calendar(IReadOnlyList<CalendarEventResponse> events, TimeZoneInfo timeZone)
    {
        if (events.Count == 0)
        {
            return new PacketSection("Calendar", [new PacketBlock.Paragraph("None yet.")]);
        }

        var blocks = events.Select(calendarEvent =>
        {
            var when = calendarEvent.Kind switch
            {
                CalendarEventKind.Milestone when calendarEvent.OccursOn is DateOnly day => ExportText.Date(day),
                CalendarEventKind.Appointment when calendarEvent.StartsAt is DateTimeOffset start =>
                    ExportText.Appointment(start, calendarEvent.EndsAt, timeZone),
                _ => null,
            };
            var lines = new List<string>();
            var schedule = Join(calendarEvent.Kind.ToString(), when);
            if (schedule.Length > 0)
            {
                lines.Add(schedule);
            }

            if (!string.IsNullOrWhiteSpace(calendarEvent.Location))
            {
                lines.Add(calendarEvent.Location.Trim());
            }

            return (PacketBlock)new PacketBlock.Entry(null, calendarEvent.Title, lines);
        }).ToArray();
        return new PacketSection("Calendar", blocks);
    }

    private static void Add(List<PacketSection> sections, PacketSection? section)
    {
        if (section is not null)
        {
            sections.Add(section);
        }
    }

    private static PacketSection? Homes(
        RelocationResponse relocation,
        IReadOnlyList<HousingOptionResponse> homes)
    {
        if (homes.Count == 0)
        {
            return relocation.IsInternational
                ? new PacketSection("Housing", [new PacketBlock.Paragraph("None yet.")])
                : null;
        }

        var blocks = homes.Select(home =>
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(home.Area))
            {
                lines.Add(home.Area);
            }

            var money = Join(
                home.Rent is decimal rent && home.CurrencyCode is string currency
                    ? RentLine(home, rent, currency)
                    : null,
                home.Deposit is decimal deposit && home.CurrencyCode is string depositCurrency
                    ? $"Deposit {ExportText.Money(deposit, depositCurrency, exact: true)}"
                    : null);
            if (money.Length > 0)
            {
                lines.Add(money);
            }

            var facts = Join(
                home.Size,
                home.Furnishing?.ToString(),
                home.AvailableOn is DateOnly available ? $"Available {ExportText.Date(available)}" : null);
            if (facts.Length > 0)
            {
                lines.Add(facts);
            }

            if (!string.IsNullOrWhiteSpace(home.Notes))
            {
                lines.Add(home.Notes);
            }

            return (PacketBlock)new PacketBlock.Entry(home.Status.ToString(), home.Name, lines);
        }).ToArray();
        return new PacketSection("Housing", blocks);
    }

    private static string RentLine(HousingOptionResponse home, decimal rent, string currency)
    {
        var period = home.RentPeriod switch
        {
            RentPeriod.Weekly => "weekly",
            RentPeriod.Monthly => "monthly",
            _ => null,
        };
        var amount = $"Rent {ExportText.Money(rent, currency, exact: true)}";
        var withPeriod = period is null ? amount : $"{amount} {period}";
        if (home.RentPercentOfIncome is not decimal percent)
        {
            return withPeriod;
        }

        return $"{withPeriod} · {percent.ToString("0.0", CultureInfo.InvariantCulture)}% of income";
    }

    private static PacketSection? Applications(
        RelocationResponse relocation,
        IReadOnlyList<JobApplicationResponse> applications)
    {
        if (applications.Count == 0)
        {
            return relocation.IsInternational
                ? new PacketSection("Applications", [new PacketBlock.Paragraph("None yet.")])
                : null;
        }

        var blocks = applications.Select(application =>
        {
            var lines = new List<string>();
            var place = Join(application.Role, application.Location);
            if (place.Length > 0)
            {
                lines.Add(place);
            }

            var dates = Join(
                application.AppliedOn is DateOnly applied ? $"Applied {ExportText.Date(applied)}" : null,
                application.NextOn is DateOnly next ? $"Next {ExportText.Date(next)}" : null);
            if (dates.Length > 0)
            {
                lines.Add(dates);
            }

            if (application.IncludesSupport is bool support)
            {
                lines.Add(support ? "Visa or relocation support" : "No visa or relocation support");
            }

            if (!string.IsNullOrWhiteSpace(application.Notes))
            {
                lines.Add(application.Notes);
            }

            return (PacketBlock)new PacketBlock.Entry(application.Status.ToString(), application.Employer, lines);
        }).ToArray();
        return new PacketSection("Applications", blocks);
    }

    private static PacketSection Documents(
        IReadOnlyList<DocumentResponse> documents,
        IReadOnlyDictionary<Guid, string> taskTitles)
    {
        if (documents.Count == 0)
        {
            return new PacketSection("Documents", [new PacketBlock.Paragraph("None yet.")]);
        }

        var blocks = documents.Select(document =>
        {
            var lines = new List<string>
            {
                Join(
                    document.Category?.ToString(),
                    document.ExpiresOn is DateOnly expires ? $"Expires {ExportText.Date(expires)}" : null,
                    ExportText.Size(document.SizeBytes)),
            };
            var supports = document.TaskIds
                .Select(id => taskTitles.GetValueOrDefault(id))
                .Where(title => title is not null)
                .OrderBy(title => title, StringComparer.Ordinal)
                .ToArray();
            if (supports.Length > 0)
            {
                lines.Add("Supports " + string.Join(", ", supports));
            }

            return (PacketBlock)new PacketBlock.Entry(null, document.FileName, lines);
        }).ToArray();
        return new PacketSection("Documents", blocks);
    }

    private static string? LeftOut(BudgetResponse budget, string currency)
    {
        var rows = budget.Unconverted
            .Select(UnconvertedRow)
            .Where(row => row.Length > 0)
            .ToArray();
        return rows.Length == 0
            ? null
            : $"Not counted, because there is no exchange rate to {currency}: {string.Join("; ", rows)}.";
    }

    private static string UnconvertedRow(UnconvertedAmountsResponse row)
    {
        var parts = new List<string>();
        if (row.ExpenseCount > 0)
        {
            var word = row.ExpenseCount == 1 ? "expense" : "expenses";
            parts.Add($"{ExportText.Money(row.ExpenseTotal, row.CurrencyCode)} spent ({row.ExpenseCount} {word})");
        }

        if (row.EstimateCount > 0)
        {
            var word = row.EstimateCount == 1 ? "estimate" : "estimates";
            parts.Add($"{ExportText.Money(row.EstimateTotal, row.CurrencyCode)} estimated ({row.EstimateCount} {word})");
        }

        if (row.ShoppingCount > 0)
        {
            var money = new List<string>();
            if (row.ShoppingEstimated > 0)
            {
                money.Add($"{ExportText.Money(row.ShoppingEstimated, row.CurrencyCode)} estimated");
            }

            if (row.ShoppingSpent > 0)
            {
                money.Add($"{ExportText.Money(row.ShoppingSpent, row.CurrencyCode)} spent");
            }

            var itemWord = row.ShoppingCount == 1 ? "item" : "items";
            var shopping = money.Count == 0 ? "shopping" : string.Join(" and ", money);
            parts.Add($"{shopping} ({row.ShoppingCount} {itemWord} to buy)");
        }

        return string.Join(" and ", parts);
    }

    private static string InBudget(string amountCurrency, string budgetCurrency, decimal? converted)
    {
        return Converted(amountCurrency, budgetCurrency, converted) ?? "—";
    }

    private static string? Converted(string amountCurrency, string budgetCurrency, decimal? converted)
    {
        if (string.Equals(amountCurrency, budgetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return converted is null
            ? "no exchange rate"
            : $"≈ {ExportText.Money(converted.Value, budgetCurrency)}";
    }

    private static string Progress(RelocationTasksResponse tasks)
    {
        if (tasks.Tasks.Count == 0)
        {
            return "Checklist not confirmed";
        }

        if (tasks.Progress.Counted == 0)
        {
            return "No tasks count toward progress";
        }

        return $"{tasks.Progress.Done} of {tasks.Progress.Counted} done";
    }

    private static string Place(string city, string countryCode)
    {
        var country = Country(countryCode);
        return string.IsNullOrWhiteSpace(city) ? country : $"{city.Trim()}, {country}";
    }

    private static string Country(string countryCode)
    {
        if (string.Equals(countryCode, "XK", StringComparison.OrdinalIgnoreCase))
        {
            return "Kosovo";
        }

        try
        {
            return new RegionInfo(countryCode).EnglishName;
        }
        catch (ArgumentException)
        {
            return countryCode;
        }
    }

    private static string? Due(DateOnly? dueDate)
    {
        return dueDate is DateOnly date ? $"Due {ExportText.Date(date)}" : null;
    }

    private static string Status(RelocationTaskStatus status)
    {
        return status switch
        {
            RelocationTaskStatus.Todo => "To do",
            RelocationTaskStatus.InProgress => "In progress",
            _ => status.ToString(),
        };
    }

    private static string Acquisition(AcquisitionStatus acquisition)
    {
        return acquisition == AcquisitionStatus.ToPurchase ? "To purchase" : "Owned";
    }

    private static string? Packing(PackingStatus? status)
    {
        return status switch
        {
            PackingStatus.NotPacked => "Not packed",
            PackingStatus.Packed => "Packed",
            PackingStatus.Unpacked => "Unpacked",
            _ => null,
        };
    }

    private static string Room(ItemRoom? room)
    {
        return room switch
        {
            null => "No room",
            ItemRoom.LivingRoom => "Living room",
            ItemRoom.DiningRoom => "Dining room",
            _ => room.Value.ToString(),
        };
    }

    private static string Join(params string?[] parts)
    {
        return string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
