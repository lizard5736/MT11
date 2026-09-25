using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Grip.UI.Notes;

/// <summary>
/// Turns Markdown text into a WPF FlowDocument for the scratchpad's read-only preview.
/// Walks Markdig's parse tree by hand rather than going through its HTML renderer: no
/// WebView2/HTML control needed in a single self-contained exe, and a FlowDocument picks
/// up Grip's own theme brushes directly instead of needing separate CSS.
///
/// Covers what a personal note actually uses — headings, emphasis, code, lists, quotes,
/// links, rules — not the full CommonMark surface (no tables, footnotes or fetched
/// images). Markdig types are always fully qualified here on purpose: both it and WPF
/// define a type named "Block" and one named "Inline", and mixing bare usings for both
/// would make every one of those names ambiguous.
///
/// Never throws: a Markdown edge case this doesn't handle falls back to a plain-text
/// paragraph, not a broken preview toggle.
/// </summary>
internal static class MarkdownRenderer
{
    public static FlowDocument Render(string text, FrameworkElement host)
    {
        Brush Res(string key) => host.TryFindResource(key) as Brush ?? Brushes.Gray;
        var doc = new FlowDocument
        {
            FontFamily = host.TryFindResource("Grip.Font") as FontFamily ?? new FontFamily("Segoe UI"),
            FontSize = 13.5,
            Foreground = Res("Grip.Text"),
            PagePadding = new Thickness(0),
        };

        try
        {
            var parsed = Markdig.Markdown.Parse(text ?? "");
            var ctx = new Ctx(Res("Grip.Text"), Res("Grip.TextSecondary"), Res("Grip.Accent"), Res("Grip.Control"), Res("Grip.Line"));
            foreach (var block in parsed)
            {
                var rendered = RenderBlock(block, ctx);
                if (rendered != null) doc.Blocks.Add(rendered);
            }
            if (doc.Blocks.Count == 0) doc.Blocks.Add(new Paragraph());
        }
        catch (Exception)
        {
            doc.Blocks.Clear();
            doc.Blocks.Add(new Paragraph(new Run(text ?? "")));
        }
        return doc;
    }

    private readonly record struct Ctx(Brush Text, Brush TextSecondary, Brush Accent, Brush Control, Brush Line);

    private static Block? RenderBlock(Markdig.Syntax.Block block, Ctx ctx)
    {
        switch (block)
        {
            case Markdig.Syntax.HeadingBlock heading:
            {
                var p = new Paragraph
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = heading.Level switch { 1 => 21, 2 => 18, 3 => 16, _ => 14.5 },
                    Margin = new Thickness(0, heading.Level == 1 ? 2 : 10, 0, 6),
                };
                AddInlines(p.Inlines, heading.Inline, ctx);
                return p;
            }
            case Markdig.Syntax.ParagraphBlock paragraph:
            {
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                AddInlines(p.Inlines, paragraph.Inline, ctx);
                return p;
            }
            case Markdig.Syntax.ListBlock list:
            {
                // Hand-drawn markers rather than List/ListItem's native TextMarkerStyle glyph:
                // that glyph didn't render at all under the Wine preview harness, and a marker
                // that's just text in the paragraph can't fail to show on any renderer.
                var section = new Section { Margin = new Thickness(0, 0, 0, 8) };
                int number = 1;
                foreach (var entry in list)
                {
                    if (entry is not Markdig.Syntax.ListItemBlock item) continue;
                    string marker = list.IsOrdered ? $"{number}." : "•";
                    number++;
                    bool firstBlockInItem = true;
                    foreach (var child in item)
                    {
                        if (child is Markdig.Syntax.ParagraphBlock para)
                        {
                            var p = new Paragraph { Margin = new Thickness(22, 0, 0, 4), TextIndent = -16 };
                            if (firstBlockInItem) p.Inlines.Add(new Run(marker + "  "));
                            AddInlines(p.Inlines, para.Inline, ctx);
                            section.Blocks.Add(p);
                        }
                        else
                        {
                            var rendered = RenderBlock(child, ctx);
                            if (rendered != null) section.Blocks.Add(rendered);
                        }
                        firstBlockInItem = false;
                    }
                }
                return section;
            }
            case Markdig.Syntax.QuoteBlock quote:
            {
                var section = new Section
                {
                    BorderBrush = ctx.Line,
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Padding = new Thickness(10, 0, 0, 0),
                    Margin = new Thickness(0, 0, 0, 8),
                    Foreground = ctx.TextSecondary,
                };
                foreach (var child in quote)
                {
                    var rendered = RenderBlock(child, ctx);
                    if (rendered != null) section.Blocks.Add(rendered);
                }
                return section;
            }
            case Markdig.Syntax.CodeBlock code:
            {
                var sb = new StringBuilder();
                for (int i = 0; i < code.Lines.Count; i++)
                {
                    if (i > 0) sb.Append('\n');
                    sb.Append(code.Lines.Lines[i].Slice.ToString());
                }
                return new Paragraph(new Run(sb.ToString()))
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12.5,
                    Background = ctx.Control,
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 8),
                };
            }
            case Markdig.Syntax.ThematicBreakBlock:
                return new BlockUIContainer(new Border { Height = 1, Background = ctx.Line, Margin = new Thickness(0, 6, 0, 10) });
            default:
                return null;
        }
    }

    private static void AddInlines(InlineCollection into, Markdig.Syntax.Inlines.ContainerInline? container, Ctx ctx)
    {
        if (container == null) return;
        foreach (var inline in container) AddInline(into, inline, ctx);
    }

    private static void AddInline(InlineCollection into, Markdig.Syntax.Inlines.Inline inline, Ctx ctx)
    {
        switch (inline)
        {
            case Markdig.Syntax.Inlines.LiteralInline literal:
                into.Add(new Run(literal.Content.ToString()));
                break;
            case Markdig.Syntax.Inlines.EmphasisInline emphasis:
            {
                Span span = emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                AddInlines(span.Inlines, emphasis, ctx);
                into.Add(span);
                break;
            }
            case Markdig.Syntax.Inlines.CodeInline code:
                into.Add(new Run(code.Content.ToString()) { FontFamily = new FontFamily("Consolas"), Background = ctx.Control });
                break;
            case Markdig.Syntax.Inlines.LineBreakInline:
                into.Add(new LineBreak());
                break;
            case Markdig.Syntax.Inlines.LinkInline { IsImage: true } image:
                into.Add(new Run(image.Title is { Length: > 0 } t ? t : image.Url ?? ""));
                break;
            case Markdig.Syntax.Inlines.LinkInline link:
            {
                var span = new Span { Foreground = ctx.Accent, TextDecorations = TextDecorations.Underline };
                AddInlines(span.Inlines, link, ctx);
                into.Add(span);
                break;
            }
            case Markdig.Syntax.Inlines.AutolinkInline autolink:
                into.Add(new Run(autolink.Url) { Foreground = ctx.Accent, TextDecorations = TextDecorations.Underline });
                break;
            case Markdig.Syntax.Inlines.HtmlEntityInline entity:
                into.Add(new Run(entity.Transcoded.ToString()));
                break;
            case Markdig.Syntax.Inlines.ContainerInline generic:
                AddInlines(into, generic, ctx);
                break;
        }
    }
}
