using Terminal.Gui.App;

namespace TerminalHost;

internal static class Program
{
    // TerminalHost              interactive inspector
    // TerminalHost --dump [T]   prints the whole tree and the binding check, for one target or all; exit
    //                           code 1 on a mismatch, 2 for an unknown target
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--dump")
            return Dump.Run(args.Length > 1 ? args[1] : null);

        using IApplication app = Application.Create();
        app.Init();

        using var window = new InspectorWindow();
        app.Run(window);

        return 0;
    }
}
