namespace Relovia.Api.Features.Relocations;

public sealed class PublishedTemplateChangeException()
    : InvalidOperationException("A published checklist template cannot be changed.");
