# AvaloniaHost

The Avalonia view of each demo object, chosen in a list on the left; each is
created with `Inspector.Create<T>()` and bound when it is chosen. The `Gadget`
gets the same buttons, display, and escape valve as in the WindowsHost. A
status line says what the last edit wrote, or why it failed, from the
inspector's events and the view's `RowFailed`.

## Build and run

```bash
dotnet run --project hosts/AvaloniaHost/AvaloniaHost.csproj
```

It runs on Windows and Linux. Without a display, start a virtual one first
(`Xvfb :99 &`, then `DISPLAY=:99`).
