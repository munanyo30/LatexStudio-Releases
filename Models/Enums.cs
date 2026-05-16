namespace LatexStudio.Models;

public enum DocumentElementKind { Table, Image, List, Chart, Text, Equation, Bibliography, Code, Theorem, CustomCode }
public enum TheoremKind { Theorem, Lemma, Corollary, Proposition, Definition, Example, Remark, Proof }
public enum TextAlignment { Left, Center, Right, Justify }
public enum SectionLevel { Paragraph, Section, Subsection, Subsubsection, Chapter }
public enum HorizontalCellAlignment { Left, Center, Right }
public enum VerticalCellAlignment { Top, Middle, Bottom }
public enum BorderStyleKind { None, Solid, Dashed }
public enum TableLayoutMode { Single, TwoSideBySide }
public enum ImageLayoutMode { Single, Grid1x1, Grid2x1, Grid1x2, Grid2x2, Custom }
public enum ListKind { Unordered, Ordered, Checklist }
public enum ChartKind { Bar, Line, Pie, Scatter, Histogram }
public enum LatexEngineKind { PdfLatex, XeLatex, LuaLatex, Custom }
