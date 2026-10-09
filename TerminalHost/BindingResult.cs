using InteractiveEditor;

namespace TerminalHost;

// What the binding check found for one node: one value per bound object, read through the inspector and
// read with plain reflection, or why they could not be read.
internal sealed record BindingResult(InspectorNode Node, IReadOnlyList<object?> Bound, IReadOnlyList<object?> Direct, string? Error)
{
    public bool Ok => Error == null && Bound.Count == Direct.Count && Bound.Zip(Direct).All(pair => BindingCheck.Same(pair.First, pair.Second));

    public string Describe()
    {
        return Error != null
            ? $"{Node.Path}: {Error}"
            : $"{Node.Path}: inspector {NodeText.Format(Node, Bound)}, object {NodeText.Format(Node, Direct)}";
    }
}
