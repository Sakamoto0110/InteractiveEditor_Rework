# WindowsHost

The WinForms view of a `Gadget`, which has a member for each editor of the views, two
members with a long help, and two that scrub (one across, one up and down). By
hand, it adds a button that reads the object again, a `Total` display, and a
button that binds a second gadget, to show the values the two do not share.
An escape valve aligns `Count` and `Ratio` to the right, which the agnostic
configuration does not do. It subscribes to the view's `ControlCreated` for
that.

## Build and run

On Windows:

```bash
dotnet run --project hosts/WindowsHost/WindowsHost.csproj
```

On Linux it runs under Wine, published for `win-x64`; see the
[development guide](../../docs/development.md#windowed-hosts-on-linux). Under
Wine the fonts are substitutes and there is no visual theme.
