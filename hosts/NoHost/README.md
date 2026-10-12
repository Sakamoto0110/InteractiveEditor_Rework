# NoHost

The owner's local test console (`net10.0-windows`). It prints trees of the demo
objects the way the TuxHost does, and the owner changes it freely.

It stays versioned and in the solution, and its directory is listed in
`.gitignore`, so local changes do not show. To commit a change to its
versioned files, stage them with `git add -u`.

## Build and run

```bash
dotnet run --project hosts/NoHost/NoHost.csproj
```

It needs Windows, or `dotnet exec --runtimeconfig` on another system, since it
touches neither WinForms nor WPF.
