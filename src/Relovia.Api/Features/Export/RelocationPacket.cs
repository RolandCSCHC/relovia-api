namespace Relovia.Api.Features.Export;

public sealed record RelocationPacket(
    string FileName,
    string Title,
    string Route,
    string Summary,
    string Progress,
    IReadOnlyList<PacketSection> Sections);

public sealed record PacketSection(string Title, IReadOnlyList<PacketBlock> Blocks);

public abstract record PacketBlock
{
    private PacketBlock()
    {
    }

    public sealed record Paragraph(string Text) : PacketBlock;

    public sealed record Entry(string? Heading, string Title, IReadOnlyList<string> Lines) : PacketBlock;

    public sealed record Table(
        string? Heading,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<string>> Rows) : PacketBlock;
}
