using System.Collections.Generic;

namespace LatexStudio.Models;

public record LatexSymbol(string Label, string Command, string Description = "");

public static class LatexSymbolData
{
    public static readonly List<LatexSymbol> GreekLetters = new()
    {
        new("α", @"\alpha "), new("β", @"\beta "), new("γ", @"\gamma "), new("δ", @"\delta "),
        new("ε", @"\epsilon "), new("ζ", @"\zeta "), new("η", @"\eta "), new("θ", @"\theta "),
        new("ι", @"\iota "), new("κ", @"\κappa "), new("λ", @"\lambda "), new("μ", @"\mu "),
        new("ν", @"\nu "), new("ξ", @"\xi "), new("π", @"\pi "), new("ρ", @"\rho "),
        new("σ", @"\sigma "), new("τ", @"\tau "), new("υ", @"\upsilon "), new("φ", @"\phi "),
        new("χ", @"\chi "), new("ψ", @"\psi "), new("ω", @"\omega "),
        new("Γ", @"\Gamma "), new("Δ", @"\Delta "), new("Θ", @"\Theta "), new("Λ", @"\Lambda "),
        new("Ξ", @"\Xi "), new("Π", @"\Pi "), new("Σ", @"\Sigma "), new("Φ", @"\Phi "),
        new("Ψ", @"\Psi "), new("Ω", @"\Omega ")
    };

    public static readonly List<LatexSymbol> Operators = new()
    {
        new("+", "+"), new("−", "-"), new("=", "="), new("±", @"\pm "), new("∓", @"\mp "),
        new("×", @"\times "), new("÷", @"\div "), new("∗", @"\ast "), new("★", @"\star "),
        new("∘", @"\circ "), new("•", @"\bullet "), new("·", @"\cdot "),
        new("∩", @"\cap "), new("∪", @"\cup "), new("⊎", @"\uplus "), new("⊓", @"\sqcap "),
        new("⊔", @"\sqcup "), new("∨", @"\vee "), new("∧", @"\wedge "), new("∖", @"\setminus "),
        new("⋄", @"\diamond "), new("△", @"\triangle ")
    };

    public static readonly List<LatexSymbol> Relations = new()
    {
        new("<", "<"), new(">", ">"), new("≤", @"\le "), new("≥", @"\ge "), new("≠", @"\neq "),
        new("≈", @"\approx "), new("≡", @"\equiv "), new("~", @"\sim "), new("≃", @"\simeq "),
        new("⊂", @"\subset "), new("⊃", @"\supset "), new("⊆", @"\subseteq "), new("⊇", @"\supseteq "),
        new("∈", @"\in "), new("∋", @"\ni "), new("∉", @"\notin "), new("∥", @"\parallel "),
        new("⊥", @"\perp "), new("∝", @"\propto ")
    };

    public static readonly List<LatexSymbol> Structures = new()
    {
        new("a/b", @"\frac{ }{ }"), new("√x", @"\sqrt{ }"), new("xⁿ", @"^{ }"), new("xₙ", @"_{ }"),
        new("∑", @"\sum_{ }^{ }"), new("∫", @"\int_{ }^{ }"), new("∏", @"\prod_{ }^{ }"),
        new("lim", @"\lim_{ \to }"), new("∞", @"\infty "), new("∂", @"\partial "),
        new("∇", @"\nabla "), new("∀", @"\forall "), new("∃", @"\exists ")
    };
}
