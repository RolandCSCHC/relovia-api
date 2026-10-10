namespace Relovia.Api.Common.Persistence;

public interface ICreationTimestamp
{
    DateTimeOffset CreatedAt { get; set; }
}
