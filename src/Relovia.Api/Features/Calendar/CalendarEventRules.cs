namespace Relovia.Api.Features.Calendar;

public static class CalendarEventRules
{
    public const int TitleMaxLength = 200;

    public const int LocationMaxLength = 200;

    public const string TitleRequiredMessage = "Title is required.";

    public const string TitleTooLongMessage = "Title must be 200 characters or fewer.";

    public const string UnknownKindMessage = "Choose a milestone or an appointment.";

    public const string MilestoneDateRequiredMessage = "A milestone needs a date.";

    public const string MilestoneHasTimeMessage = "A milestone is a date, without a start or end time.";

    public const string AppointmentStartRequiredMessage = "An appointment needs a start time.";

    public const string AppointmentHasDateMessage = "An appointment uses a start time, not a milestone date.";

    public const string EndBeforeStartMessage = "The end must be after the start.";

    public const string LocationTooLongMessage = "Location must be 200 characters or fewer.";

    public static Dictionary<string, string[]> Validate(SaveCalendarEventRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var title = NormalizeTitle(request.Title);
        if (title.Length == 0)
        {
            errors["title"] = [TitleRequiredMessage];
        }
        else if (title.Length > TitleMaxLength)
        {
            errors["title"] = [TitleTooLongMessage];
        }

        var location = NormalizeLocation(request.Location);
        if (location is not null && location.Length > LocationMaxLength)
        {
            errors["location"] = [LocationTooLongMessage];
        }

        if (!Enum.IsDefined(request.Kind))
        {
            errors["kind"] = [UnknownKindMessage];
            return errors;
        }

        if (request.Kind == CalendarEventKind.Milestone)
        {
            if (request.OccursOn is null)
            {
                errors["occursOn"] = [MilestoneDateRequiredMessage];
            }

            if (request.StartsAt is not null || request.EndsAt is not null)
            {
                errors["startsAt"] = [MilestoneHasTimeMessage];
            }
        }
        else
        {
            if (request.OccursOn is not null)
            {
                errors["occursOn"] = [AppointmentHasDateMessage];
            }

            if (request.StartsAt is null)
            {
                errors["startsAt"] = [AppointmentStartRequiredMessage];
            }
            else if (request.EndsAt is { } endsAt && endsAt <= request.StartsAt)
            {
                errors["endsAt"] = [EndBeforeStartMessage];
            }
        }

        return errors;
    }

    public static string NormalizeTitle(string title)
    {
        return title.Trim();
    }

    public static string? NormalizeLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        return location.Trim();
    }
}
