# TerminalHost

A terminal inspector, on Terminal.Gui 2.5.0, for looking at the tree, the
bindings, and each node's options without a window system. It lists the demo
targets (`Foo`, `Moo`, `Doo`, `Hoo`, the struct `Boo`, the attributed `Boo`,
and `Gadget`); the chosen one shows as a tree, with the selected node's
information, an edit line, and a log. It uses only the core's public API.

## Build and run

```bash
dotnet run --project hosts/TerminalHost/TerminalHost.csproj
dotnet run --project hosts/TerminalHost/TerminalHost.csproj -- --dump
dotnet run --project hosts/TerminalHost/TerminalHost.csproj -- --dump Gadget
```

## Options

| Option | Meaning |
|---|---|
| none | The interactive inspector |
| `--dump [target]` | Prints the tree of one target, or of all, and checks every member's binding against plain reflection. Exit code 0 when every `bindings:` line says `ok`, 1 on a mismatch, 2 for an unknown target |

The binding check reads each member through the inspector and again by walking
the member names with reflection from each bound object, so it catches a read
of the wrong member and a write that never reached the real object.

## Keys

| Key | Action |
|---|---|
| F6 | Next panel |
| Enter | Applies the edit line; `null` clears a nullable or reference member |
| F5 | Reads the objects again |
| F3 | Checks every binding |
| F7 | Resets the target |
| Esc | Quits |
