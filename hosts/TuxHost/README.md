# TuxHost

The verification console. It prints the trees of the demo objects under each
layer of options: `Foo` with reflection only, `Foo` with a manual layer (a
label, a scrub multiplier, a tooltip, a collapsed group, an ignored member),
the attributed `Boo` with one attribute overridden by the manual layer, the
same `Boo` with `GlobalOptions.RequireExpandableAttribute` on, and the
`KeyNotFoundException` an unknown path throws. Each row shows its label, its
value, and its path, editor, and options.

It consumes only the core and `DemoObjects`, and runs on any system.

## Build and run

```bash
dotnet run --project hosts/TuxHost/TuxHost.csproj
```

The output is 67 lines and changes only when the core's behavior does. Save it
before a change and compare it after with `cmp`; see the
[development guide](../../docs/development.md#checks).
