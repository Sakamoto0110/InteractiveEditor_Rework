namespace InteractiveEditor.Diagnostics;

public sealed class InspectorCreatedEventArgs(Inspector inspector) : InspectorEventArgs(inspector)
{
    public int ErrorCount => Inspector.Report.ErrorCount;
    public int UnhandledErrorCount => Inspector.Report.UnhandledErrorCount;
    public int CriticalErrorCount => Inspector.Report.CriticalErrorCount;
}
