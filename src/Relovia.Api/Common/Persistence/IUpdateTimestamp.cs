namespace Relovia.Api.Common.Persistence;

public interface IUpdateTimestamp
{
    DateTimeOffset UpdatedAt { get; set; }
}
