using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Relovia.Api.Features.Export;

public static class RelocationPdf
{
    private static readonly Color Green = Color.FromHex("#17623C");
    private static readonly Color Gold = Color.FromHex("#B87A00");
    private static readonly Color Ink = Color.FromHex("#182019");
    private static readonly Color Muted = Color.FromHex("#4A574C");
    private static readonly Color Line = Color.FromHex("#CDD2C0");
    private static readonly Color Sand = Color.FromHex("#F1EFE3");

    public static byte[] Render(RelocationPacket packet)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.6f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Ink).LineHeight(1.35f));

                page.Header().PaddingBottom(8).BorderBottom(1).BorderColor(Gold).Row(row =>
                {
                    row.ConstantItem(72).Text("Relovia").SemiBold().FontColor(Green);
                    row.RelativeItem().AlignRight().Text(packet.Title).FontColor(Muted);
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(9).FontColor(Muted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });

                page.Content().PaddingTop(16).Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Text(packet.Title).FontSize(22).SemiBold().FontColor(Green);
                    column.Item().Text(packet.Route);
                    column.Item().Text(packet.Summary).FontColor(Muted);
                    column.Item().PaddingBottom(6).Text(packet.Progress).FontColor(Muted);

                    foreach (var section in packet.Sections)
                    {
                        column.Item().PaddingTop(14).Text(section.Title).FontSize(15).SemiBold().FontColor(Green);
                        column.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Gold);
                        foreach (var block in section.Blocks)
                        {
                            Render(column, block);
                        }
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void Render(ColumnDescriptor column, PacketBlock block)
    {
        switch (block)
        {
            case PacketBlock.Paragraph paragraph:
                column.Item().Text(paragraph.Text).FontColor(Muted);
                break;
            case PacketBlock.Entry entry:
                column.Item().Column(group =>
                {
                    group.Spacing(1);
                    if (entry.Heading is not null)
                    {
                        group.Item().PaddingTop(8).Text(entry.Heading).SemiBold().FontColor(Green);
                    }

                    group.Item().PaddingTop(4).Text(entry.Title).SemiBold();
                    foreach (var line in entry.Lines)
                    {
                        group.Item().Text(line).FontSize(9).FontColor(Muted);
                    }
                });
                break;
            case PacketBlock.Table table:
                if (table.Heading is not null)
                {
                    column.Item().PaddingTop(8).Text(table.Heading).SemiBold().FontColor(Green);
                }

                column.Item().PaddingTop(4).Table(pdfTable =>
                {
                    pdfTable.ColumnsDefinition(columns =>
                    {
                        for (var index = 0; index < table.Columns.Count; index++)
                        {
                            if (index == 0)
                            {
                                columns.RelativeColumn(2);
                            }
                            else
                            {
                                columns.RelativeColumn();
                            }
                        }
                    });
                    pdfTable.Header(header =>
                    {
                        foreach (var name in table.Columns)
                        {
                            header.Cell().Background(Sand).Padding(4).Text(name).SemiBold().FontSize(9).FontColor(Green);
                        }
                    });
                    foreach (var row in table.Rows)
                    {
                        for (var index = 0; index < table.Columns.Count; index++)
                        {
                            var cell = index < row.Count ? row[index] : "";
                            pdfTable.Cell().BorderBottom(0.5f).BorderColor(Line).Padding(4).Text(cell).FontSize(9);
                        }
                    }
                });
                break;
        }
    }
}
