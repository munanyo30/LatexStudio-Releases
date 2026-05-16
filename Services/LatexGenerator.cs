using System.Globalization;
using System.Text;
using LatexStudio.Models;

namespace LatexStudio.Services;

public sealed class LatexGenerator
{
    private readonly TemplateService templateService;

    public LatexGenerator(TemplateService? templateService = null)
    {
        this.templateService = templateService ?? new TemplateService();
    }

    public LatexGenerationResult Generate(AcademicDocument document, LatexTemplate? template = null)
    {
        var registry = new ReferenceRegistry();
        var packages = new SortedSet<string>(StringComparer.Ordinal)
        {
            "\\usepackage[utf8]{inputenc}",
            "\\usepackage[T1]{fontenc}",
            "\\usepackage{graphicx}",
            "\\usepackage{xcolor}"
        };

        var body = new StringBuilder();

        // Metadata
        if (!string.IsNullOrWhiteSpace(document.Name)) body.AppendLine($"\\title{{{Escape(document.Name)}}}");
        
        var authorLine = new StringBuilder();
        authorLine.Append(Escape(document.Author));
        if (!string.IsNullOrWhiteSpace(document.Institution)) authorLine.Append($"\\\\ \\small {Escape(document.Institution)}");
        body.AppendLine($"\\author{{{authorLine}}}");

        if (!string.IsNullOrWhiteSpace(document.Advisor))
        {
            var advisors = new List<string> { $"Orientador: {Escape(document.Advisor)}" };
            if (!string.IsNullOrWhiteSpace(document.CoAdvisor)) advisors.Add($"Co-orientador: {Escape(document.CoAdvisor)}");
            body.AppendLine($"\\date{{\\vspace{{1em}} {string.Join(" \\\\ ", advisors)}}}");
        }

        if (!string.IsNullOrWhiteSpace(document.Name) || !string.IsNullOrWhiteSpace(document.Author)) body.AppendLine("\\maketitle\n");

        if (!string.IsNullOrWhiteSpace(document.AbstractText))
        {
            body.AppendLine("\\begin{abstract}");
            body.AppendLine(document.AbstractText);
            body.AppendLine("\\end{abstract}\n");
        }

        if (!string.IsNullOrWhiteSpace(document.Keywords))
        {
            body.AppendLine($"\\noindent \\textbf{{Palavras-chave:}} {Escape(document.Keywords)}\n");
            body.AppendLine("\\vspace{2em}");
        }

        // Indexes
        if (document.IncludeTableOfContents) body.AppendLine("\\tableofcontents\n\\newpage");
        if (document.IncludeListOfFigures) body.AppendLine("\\listoffigures\n\\newpage");
        if (document.IncludeListOfTables) body.AppendLine("\\listoftables\n\\newpage");

        var bibElements = document.Elements.OfType<BibliographyElement>().ToList();
        
        if (bibElements.Any())
        {
            packages.Add("\\usepackage{filecontents}");
            foreach (var bib in bibElements)
            {
                body.AppendLine("\\begin{filecontents*}{references.bib}");
                body.AppendLine(bib.BibContent);
                body.AppendLine("\\end{filecontents*}");
            }
        }

        template ??= templateService.LoadTemplates().First();
        bool isBeamer = template.Body.Contains("\\documentclass{beamer}");

        foreach (var element in document.Elements.Where(e => e.IncludeInExport))
        {
            var reference = registry.Assign(element);
            var elementCode = element switch
            {
                TableElement table => GenerateTable(table, reference, packages),
                ImageElement image => GenerateImage(image, reference, packages),
                ListElement list => GenerateList(list, packages),
                ChartElement chart => GenerateChart(chart, reference, packages),
                TextElement text => GenerateText(text, reference, packages),
                EquationElement equation => GenerateEquation(equation, reference, packages),
                CodeElement codeElement => GenerateCode(codeElement, packages),
                TheoremElement theorem => GenerateTheorem(theorem, reference, packages),
                CustomCodeElement custom => custom.RawLatex,
                BibliographyElement bib => GenerateBibliography(bib),
                _ => ""
            };

            if (isBeamer && element is not BibliographyElement)
            {
                body.AppendLine("\\begin{frame}");
                if (!string.IsNullOrWhiteSpace(element.Title)) body.AppendLine($"\\frametitle{{{Escape(element.Title)}}}");
                body.AppendLine(elementCode);
                body.AppendLine("\\end{frame}\n");
            }
            else
            {
                body.AppendLine(elementCode);
            }
        }

        var packageBlock = string.Join(Environment.NewLine, packages);
        var finalCode = template.Body
            .Replace("{{packages}}", packageBlock, StringComparison.Ordinal)
            .Replace("{{body}}", body.ToString().Trim(), StringComparison.Ordinal);

        return new LatexGenerationResult(finalCode.Trim() + Environment.NewLine, packages);
    }

    private static string GenerateTable(TableElement table, ReferenceInfo reference, ISet<string> packages)
    {
        packages.Add("\\usepackage{booktabs}");
        packages.Add("\\usepackage{tabularx}");
        packages.Add("\\usepackage{multirow}");
        packages.Add("\\usepackage{colortbl}");
        packages.Add("\\usepackage{array}");

        var columnCount = table.Rows.FirstOrDefault()?.Cells.Count ?? 0;
        var columns = table.UseTabularX
            ? string.Concat(Enumerable.Repeat(">{\\centering\\arraybackslash}X", columnCount))
            : string.Concat(Enumerable.Repeat("c", columnCount));

        var builder = new StringBuilder();
        builder.AppendLine("\\begin{table}[htbp]");
        builder.AppendLine("\\centering");
        builder.AppendLine($"\\caption{{{Escape(CaptionOrTitle(table, reference.DisplayName))}}}");
        builder.AppendLine($"\\label{{{reference.Label}}}");
        builder.AppendLine(table.UseTabularX
            ? $"\\begin{{tabularx}}{{\\textwidth}}{{{columns}}}"
            : $"\\begin{{tabular}}{{{columns}}}");

        if (table.UseBooktabs) builder.AppendLine("\\toprule");

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            var cells = new List<string>();
            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cell = row.Cells[c];

                // If this cell is a merged child, we might still need to skip it or add a placeholder
                if (cell.IsMergedChild)
                {
                    // If it's a child of a horizontal merge (columnSpan > 1 in SAME row), skip it.
                    // LaTeX's \multicolumn handles the horizontal space.
                    // If it's ONLY a child of a vertical merge (from a row ABOVE), 
                    // we need to provide an empty slot (&) unless it's ALSO covered by a horizontal span from above.
                    
                    // Logic: Find the "root" cell for this coordinate.
                    // For simplicity in tabular, we skip children that are part of a horizontal span.
                    // But we MUST account for vertical spans from rows above.
                    
                    bool isHorizontalChild = false;
                    for (int checkCol = c - 1; checkCol >= 0; checkCol--)
                    {
                        var leftCell = row.Cells[checkCol];
                        if (!leftCell.IsMergedChild && leftCell.ColumnSpan > (c - checkCol))
                        {
                            isHorizontalChild = true;
                            break;
                        }
                    }

                    if (isHorizontalChild) continue;

                    // If it's NOT a horizontal child, it must be a vertical child.
                    // We need an empty & slot here. If the parent ABOVE also had ColumnSpan > 1, 
                    // we should use multicolumn{N}{c}{} to keep borders correct.
                    
                    int parentColSpan = 1;
                    for (int checkRow = r - 1; checkRow >= 0; checkRow--)
                    {
                        var aboveCell = table.Rows[checkRow].Cells[c];
                        if (!aboveCell.IsMergedChild)
                        {
                            parentColSpan = aboveCell.ColumnSpan;
                            break;
                        }
                    }

                    if (parentColSpan > 1)
                    {
                        cells.Add($"\\multicolumn{{{parentColSpan}}}{{c}}{{}}");
                        c += (parentColSpan - 1); // Skip the rest of this multicolumn span
                    }
                    else
                    {
                        cells.Add("");
                    }
                    continue;
                }

                var text = Escape(cell.Text);
                if (cell.IsHeader) text = $"\\textbf{{{text}}}";
                if (!IsWhite(cell.Background))
                {
                    text = $"\\cellcolor[HTML]{{{ToHtmlColor(cell.Background)}}}{text}";
                }

                // If both spans are active, multirow goes inside multicolumn
                if (cell.RowSpan > 1)
                {
                    text = $"\\multirow{{{cell.RowSpan}}}{{*}}{{{text}}}";
                }
                
                if (cell.ColumnSpan > 1)
                {
                    text = $"\\multicolumn{{{cell.ColumnSpan}}}{{c}}{{{text}}}";
                }

                cells.Add(text);
                if (cell.ColumnSpan > 1) c += (cell.ColumnSpan - 1);
            }

            builder.AppendLine(string.Join(" & ", cells) + " \\\\");
            if (r == 0 && table.HasHeader && table.UseBooktabs) builder.AppendLine("\\midrule");
        }

        if (table.UseBooktabs) builder.AppendLine("\\bottomrule");
        builder.AppendLine(table.UseTabularX ? "\\end{tabularx}" : "\\end{tabular}");
        builder.AppendLine("\\end{table}");
        return builder.ToString();
    }

    private static string GenerateImage(ImageElement image, ReferenceInfo reference, ISet<string> packages)
    {
        packages.Add("\\usepackage{subcaption}");
        packages.Add("\\usepackage{float}");

        var builder = new StringBuilder();
        builder.AppendLine("\\begin{figure}[htbp]");
        builder.AppendLine("\\centering");

        int columns = image.LayoutMode switch
        {
            ImageLayoutMode.Grid2x1 or ImageLayoutMode.Grid2x2 => 2,
            _ => 1
        };

        double defaultWidth = image.LayoutMode switch
        {
            ImageLayoutMode.Grid2x1 or ImageLayoutMode.Grid2x2 => 0.46,
            ImageLayoutMode.Grid1x2 => 0.9,
            ImageLayoutMode.Grid1x1 or ImageLayoutMode.Single => 0.85,
            _ => 0.46
        };

        for (int i = 0; i < image.Images.Count; i++)
        {
            var item = image.Images[i];
            var width = image.LayoutMode == ImageLayoutMode.Custom ? item.WidthPercent : defaultWidth;
            
            if (image.UseSubfigures && image.Images.Count > 1)
            {
                builder.AppendLine($"\\begin{{subfigure}}{{{width.ToString("0.##", CultureInfo.InvariantCulture)}\\textwidth}}");
                builder.AppendLine("\\centering");
                builder.AppendLine($"\\includegraphics[width=\\linewidth]{{{NormalizePath(item.Path)}}}");
                if (!string.IsNullOrWhiteSpace(item.Caption)) builder.AppendLine($"\\caption{{{Escape(item.Caption)}}}");
                builder.AppendLine("\\end{subfigure}");
                
                // Layout logic
                bool isLastInRow = (i + 1) % columns == 0;
                bool isLastOverall = (i + 1) == image.Images.Count;

                if (isLastInRow || isLastOverall)
                {
                    builder.AppendLine("\\\\ \\vspace{0.5em}");
                }
                else
                {
                    builder.AppendLine("\\hfill");
                }
            }
            else
            {
                builder.AppendLine($"\\includegraphics[width={width.ToString("0.##", CultureInfo.InvariantCulture)}\\textwidth]{{{NormalizePath(item.Path)}}}");
            }
        }

        builder.AppendLine($"\\caption{{{Escape(CaptionOrTitle(image, reference.DisplayName))}}}");
        builder.AppendLine($"\\label{{{reference.Label}}}");
        builder.AppendLine("\\end{figure}");
        return builder.ToString();
    }

    private static string GenerateList(ListElement list, ISet<string> packages)
    {
        packages.Add("\\usepackage{enumitem}");
        if (list.ListKind == ListKind.Checklist) packages.Add("\\usepackage{amssymb}");

        var env = list.ListKind == ListKind.Ordered ? "enumerate" : "itemize";
        var builder = new StringBuilder();
        builder.AppendLine($"\\begin{{{env}}}[itemsep={list.ItemSpacing.ToString("0.#", CultureInfo.InvariantCulture)}pt,leftmargin=*]");
        foreach (var item in list.Items)
        {
            AppendListItem(builder, item, list.ListKind, 0);
        }
        builder.AppendLine($"\\end{{{env}}}");
        return builder.ToString();
    }

    private static void AppendListItem(StringBuilder builder, ListItemNode item, ListKind kind, int depth)
    {
        var prefix = kind == ListKind.Checklist ? (item.IsChecked ? "$\\boxtimes$ " : "$\\square$ ") : "";
        builder.AppendLine($"{new string(' ', depth * 2)}\\item {prefix}{Escape(item.Text)}");
        if (item.Children.Count == 0) return;

        var env = kind == ListKind.Ordered ? "enumerate" : "itemize";
        builder.AppendLine($"{new string(' ', depth * 2)}\\begin{{{env}}}");
        foreach (var child in item.Children)
        {
            AppendListItem(builder, child, kind, depth + 1);
        }
        builder.AppendLine($"{new string(' ', depth * 2)}\\end{{{env}}}");
    }

    private static string GenerateChart(ChartElement chart, ReferenceInfo reference, ISet<string> packages)
    {
        packages.Add("\\usepackage{pgfplots}");
        packages.Add("\\pgfplotsset{compat=1.18}");
        if (chart.ChartKind == ChartKind.Pie) packages.Add("\\usepackage{pgf-pie}");

        var builder = new StringBuilder();
        builder.AppendLine("\\begin{figure}[htbp]");
        builder.AppendLine("\\centering");

        if (chart.ChartKind == ChartKind.Pie)
        {
            var firstSeries = chart.Series.FirstOrDefault();
            var slices = firstSeries?.Values.Select((value, index) =>
            {
                var label = chart.Categories.ElementAtOrDefault(index) ?? $"Item {index + 1}";
                return $"{value.ToString("0.###", CultureInfo.InvariantCulture)}/{Escape(label)}";
            }) ?? [];

            builder.AppendLine("\\begin{tikzpicture}");
            builder.AppendLine("\\pie[text=legend,radius=2.6]{" + string.Join(",", slices) + "}");
            builder.AppendLine("\\end{tikzpicture}");
            builder.AppendLine($"\\caption{{{Escape(CaptionOrTitle(chart, reference.DisplayName))}}}");
            builder.AppendLine($"\\label{{{reference.Label}}}");
            builder.AppendLine("\\end{figure}");
            return builder.ToString();
        }

        builder.AppendLine("\\begin{tikzpicture}");
        builder.AppendLine($"\\begin{{axis}}[{AxisOptions(chart)}]");

        foreach (var series in chart.Series)
        {
            var coordinates = series.Values.Select((value, index) =>
            {
                var x = chart.Categories.ElementAtOrDefault(index) ?? index.ToString(CultureInfo.InvariantCulture);
                return $"({{{Escape(x)}}},{value.ToString("0.###", CultureInfo.InvariantCulture)})";
            });
            var plotKind = chart.ChartKind switch
            {
                ChartKind.Bar => "ybar",
                ChartKind.Scatter => "only marks",
                ChartKind.Histogram => "ybar interval",
                _ => "mark=*"
            };
            builder.AppendLine($"\\addplot+[{plotKind},{series.Color}] coordinates {{{string.Join(" ", coordinates)}}};");
            if (chart.ShowLegend) builder.AppendLine($"\\addlegendentry{{{Escape(series.Name)}}}");
        }

        builder.AppendLine("\\end{axis}");
        builder.AppendLine("\\end{tikzpicture}");
        builder.AppendLine($"\\caption{{{Escape(CaptionOrTitle(chart, reference.DisplayName))}}}");
        builder.AppendLine($"\\label{{{reference.Label}}}");
        builder.AppendLine("\\end{figure}");
        return builder.ToString();
    }

    private static string GenerateText(TextElement text, ReferenceInfo reference, ISet<string> packages)
    {
        var builder = new StringBuilder();
        var cmd = text.Level switch
        {
            SectionLevel.Chapter => "\\chapter",
            SectionLevel.Section => "\\section",
            SectionLevel.Subsection => "\\subsection",
            SectionLevel.Subsubsection => "\\subsubsection",
            SectionLevel.Paragraph => null, // Just text
            _ => null
        };

        if (cmd != null)
        {
            builder.AppendLine($"{cmd}{{{text.Title}}}");
            if (!string.IsNullOrWhiteSpace(reference.Label))
                builder.AppendLine($"\\label{{{reference.Label}}}");
        }

        var content = text.Content;
        if (text.IsBold) content = $"\\textbf{{{content}}}";
        if (text.IsItalic) content = $"\\textit{{{content}}}";

        if (text.Alignment != TextAlignment.Justify)
        {
            var env = text.Alignment switch
            {
                TextAlignment.Center => "center",
                TextAlignment.Left => "flushleft",
                TextAlignment.Right => "flushright",
                _ => "center"
            };
            builder.AppendLine($"\\begin{{{env}}}");
            builder.AppendLine(content);
            builder.AppendLine($"\\end{{{env}}}");
        }
        else
        {
            builder.AppendLine(content);
        }

        return builder.ToString();
    }

    private static string GenerateEquation(EquationElement equation, ReferenceInfo reference, ISet<string> packages)
    {
        packages.Add("\\usepackage{amsmath}");
        var builder = new StringBuilder();
        var env = equation.IsNumbered ? "equation" : "equation*";
        
        builder.AppendLine($"\\begin{{{env}}}");
        builder.AppendLine(equation.Formula);
        if (equation.IsNumbered && !string.IsNullOrWhiteSpace(reference.Label))
            builder.AppendLine($"\\label{{{reference.Label}}}");
        builder.AppendLine($"\\end{{{env}}}");

        return builder.ToString();
    }

    private static string GenerateBibliography(BibliographyElement bib)
    {
        var builder = new StringBuilder();
        var style = string.IsNullOrWhiteSpace(bib.Style) ? "plain" : bib.Style;
        builder.AppendLine($"\\bibliographystyle{{{style}}}");
        builder.AppendLine("\\bibliography{references}");
        return builder.ToString();
    }

    private static string GenerateCode(CodeElement code, ISet<string> packages)
    {
        packages.Add("\\usepackage{listings}");
        packages.Add("\\usepackage{xcolor}");
        
        var builder = new StringBuilder();
        builder.AppendLine("\\lstset{");
        builder.AppendLine($"  language={code.Language},");
        builder.AppendLine($"  basicstyle=\\ttfamily\\small,");
        builder.AppendLine($"  numbers={(code.ShowLineNumbers ? "left" : "none")},");
        builder.AppendLine($"  breaklines={(code.BreakLines ? "true" : "false")},");
        builder.AppendLine("  keywordstyle=\\color{blue},");
        builder.AppendLine("  commentstyle=\\color{gray},");
        builder.AppendLine("  stringstyle=\\color{orange},");
        builder.AppendLine("  frame=single,");
        builder.AppendLine("  showstringspaces=false");
        builder.AppendLine("}");
        
        builder.AppendLine("\\begin{lstlisting}");
        builder.AppendLine(code.Code);
        builder.AppendLine("\\end{lstlisting}");
        
        return builder.ToString();
    }

    private static string GenerateTheorem(TheoremElement theorem, ReferenceInfo reference, ISet<string> packages)
    {
        packages.Add("\\usepackage{amsthm}");
        
        // Define theorem environments if not already defined (basic set)
        packages.Add("\\newtheorem{theorem}{Teorema}[section]");
        packages.Add("\\newtheorem{lemma}[theorem]{Lema}");
        packages.Add("\\newtheorem{corollary}[theorem]{Corolário}");
        packages.Add("\\newtheorem{proposition}[theorem]{Proposição}");
        packages.Add("\\theoremstyle{definition}");
        packages.Add("\\newtheorem{definition}{Definição}[section]");
        packages.Add("\\newtheorem{example}{Exemplo}[section]");
        packages.Add("\\theoremstyle{remark}");
        packages.Add("\\newtheorem*{remark}{Nota}");

        var type = theorem.TheoremKind.ToString().ToLowerInvariant();
        var builder = new StringBuilder();
        
        var title = string.IsNullOrWhiteSpace(theorem.Subtitle) ? "" : $"[{theorem.Subtitle}]";
        builder.AppendLine($"\\begin{{{type}}}{title}");
        builder.AppendLine(theorem.Content);
        if (!string.IsNullOrWhiteSpace(reference.Label))
            builder.AppendLine($"\\label{{{reference.Label}}}");
        builder.AppendLine($"\\end{{{type}}}");
        
        return builder.ToString();
    }

    private static string AxisOptions(ChartElement chart)
    {
        var options = new List<string>
        {
            $"title={{{Escape(chart.Title)}}}",
            $"xlabel={{{Escape(chart.XLabel)}}}",
            $"ylabel={{{Escape(chart.YLabel)}}}",
            "width=0.85\\textwidth",
            "height=7cm"
        };
        if (chart.ShowGrid) options.Add("grid=both");
        if (chart.ChartKind == ChartKind.Bar) options.Add("symbolic x coords={" + string.Join(",", chart.Categories.Select(Escape)) + "},xtick=data");
        return string.Join(",", options);
    }

    private static string CaptionOrTitle(DocumentElement element, string fallback)
        => string.IsNullOrWhiteSpace(element.Caption) ? (string.IsNullOrWhiteSpace(element.Title) ? fallback : element.Title) : element.Caption;

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\textbackslash{}",
                '&' => "\\&",
                '%' => "\\%",
                '$' => "\\$",
                '#' => "\\#",
                '_' => "\\_",
                '{' => "\\{",
                '}' => "\\}",
                '~' => "\\textasciitilde{}",
                '^' => "\\textasciicircum{}",
                _ => character.ToString()
            });
        }
        return builder.ToString();
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');
    private static bool IsWhite(string color) => color.Equals("#FFFFFFFF", StringComparison.OrdinalIgnoreCase) || color.Equals("white", StringComparison.OrdinalIgnoreCase);
    private static string ToHtmlColor(string color) => color.TrimStart('#').Length == 8 ? color.TrimStart('#')[2..] : color.TrimStart('#');
}
