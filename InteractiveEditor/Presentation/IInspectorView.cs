namespace InteractiveEditor.Presentation;

/// <summary>
/// A UI-framework-specific presentation of an <see cref="InteractiveEditor.Inspector"/>.
/// Implementations live in the adapter projects (WinForms, WPF, ...), never in the core.
/// </summary>
public interface IInspectorView
{
    Inspector Inspector { get; }

    void AttachToHost(object host);
}
