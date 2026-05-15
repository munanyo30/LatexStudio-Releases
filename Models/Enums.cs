namespace LatexStudio.Models;

public enum DocumentElementKind { Table, Image, List, Chart }
public enum HorizontalCellAlignment { Left, Center, Right }
public enum VerticalCellAlignment { Top, Middle, Bottom }
public enum BorderStyleKind { None, Solid, Dashed }
public enum TableLayoutMode { Single, TwoSideBySide }
public enum ImageLayoutMode { Single, TwoHorizontal, TwoVertical, Grid2x2, Custom }
public enum ListKind { Unordered, Ordered, Checklist }
public enum ChartKind { Bar, Line, Pie, Scatter, Histogram }
public enum LatexEngineKind { PdfLatex, XeLatex, LuaLatex, Custom }
