namespace Relovia.Api.Features.Relocations;

public static class ChecklistSelector
{
    public static ChecklistSelection Select(Relocation relocation, IEnumerable<ChecklistTemplate> templates)
    {
        var chosen = templates
            .Where(template => template.IsPublished && Applies(relocation, template))
            .GroupBy(template => ChecklistTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .Select(versions => versions.OrderByDescending(template => template.Version).First())
            .OrderByDescending(Specificity)
            .ThenBy(template => ChecklistTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .ToArray();

        var selected = new List<SelectedTask>();
        var byCode = new Dictionary<string, SelectedTask>(StringComparer.Ordinal);
        foreach (var template in chosen)
        {
            foreach (var task in template.Tasks)
            {
                var code = ChecklistTemplateRules.NormalizeCode(task.Code);
                if (!byCode.TryGetValue(code, out var current))
                {
                    current = new SelectedTask(task);
                    byCode.Add(code, current);
                    selected.Add(current);
                }

                foreach (var dependency in task.Dependencies)
                {
                    current.AddDependency(dependency.DependsOnCode);
                }
            }
        }

        var codes = new HashSet<string>(byCode.Keys, StringComparer.Ordinal);
        var tasks = selected
            .Select(task => task.ToTask(codes))
            .OrderByDescending(task => task.DaysBeforeMove)
            .ThenBy(task => task.Code, StringComparer.Ordinal)
            .ToArray();
        var orderedTemplates = chosen
            .OrderBy(template => ChecklistTemplateRules.NormalizeCode(template.Code), StringComparer.Ordinal)
            .ToArray();

        return new ChecklistSelection(orderedTemplates, tasks);
    }

    private static bool Applies(Relocation relocation, ChecklistTemplate template)
    {
        if (!ScopeMatches(template.Scope, relocation.IsInternational))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(template.DestinationCountryCode)
            && !CountryCodes.AreSameCountry(template.DestinationCountryCode, relocation.DestinationCountryCode))
        {
            return false;
        }

        return template.Purpose is null || template.Purpose == relocation.Purpose;
    }

    private static bool ScopeMatches(ChecklistScope scope, bool isInternational)
    {
        return scope switch
        {
            ChecklistScope.Both => true,
            ChecklistScope.International => isInternational,
            ChecklistScope.Domestic => !isInternational,
            _ => false,
        };
    }

    // A destination country narrows a template more than a purpose. Either narrows it more than leaving both unset.
    private static int Specificity(ChecklistTemplate template)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(template.DestinationCountryCode))
        {
            score += 2;
        }

        if (template.Purpose is not null)
        {
            score += 1;
        }

        return score;
    }

    private sealed class SelectedTask(TaskTemplate source)
    {
        private readonly List<string> _dependencies = [];

        public string Code { get; } = ChecklistTemplateRules.NormalizeCode(source.Code);

        public void AddDependency(string code)
        {
            if (!ChecklistTemplateRules.IsCode(code))
            {
                return;
            }

            var normalized = ChecklistTemplateRules.NormalizeCode(code);
            if (_dependencies.Contains(normalized, StringComparer.Ordinal))
            {
                return;
            }

            _dependencies.Add(normalized);
        }

        public SelectedChecklistTask ToTask(IReadOnlySet<string> selectedCodes)
        {
            var dependencies = _dependencies
                .Where(code => selectedCodes.Contains(code) && !string.Equals(code, Code, StringComparison.Ordinal))
                .ToArray();

            return new SelectedChecklistTask(
                source.Id,
                Code,
                source.Title,
                source.DaysBeforeMove,
                source.Phase,
                source.Priority,
                dependencies);
        }
    }
}

public sealed record ChecklistSelection(
    IReadOnlyList<ChecklistTemplate> Templates,
    IReadOnlyList<SelectedChecklistTask> Tasks);

public sealed record SelectedChecklistTask(
    Guid SourceTemplateTaskId,
    string Code,
    string Title,
    int DaysBeforeMove,
    TaskPhase Phase,
    TaskPriority Priority,
    IReadOnlyList<string> DependsOnCodes);
