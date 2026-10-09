namespace InteractiveEditor.Options;

// A rule for the text typed in the view, applied in the node's order before the text is converted.
public sealed class TextRule(Func<string, string> apply)
{
    // Keeps only the digits.
    public static TextRule Digits { get; } = new(text => new string(text.Where(char.IsAsciiDigit).ToArray()));

    // Keeps what a number can hold: digits, signs and the decimal separators.
    public static TextRule Number { get; } = new(text => new string(text.Where(c => char.IsAsciiDigit(c) || c is '-' or '+' or '.' or ',').ToArray()));

    public string Apply(string text) => apply(text);

    public static TextRule MaxLength(int length) => new(text => text.Length > length ? text[..length] : text);

    // Keeps only the given characters.
    public static TextRule Only(string allowed) => new(text => new string(text.Where(allowed.Contains).ToArray()));
}
