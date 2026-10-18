namespace Relovia.Api.Features.Relocations;

public sealed class PublishedTemplateChangeException()
    : InvalidOperationException("A published template cannot be changed.");
