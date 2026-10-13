# Development and Validation Guide

**Document ID:** GLOBAL-DEVELOPMENT-GUIDE

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** building, checking, and committing a change to PixieInspector

**Surface:** guide

**Boundary:** global

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

This guide collects the practices needed to change PixieInspector safely. It
does not replace the sources that own technical truth: source code owns
behavior, the project files own targets and references, the module
`REFERENCE.md` files own the documented contracts, and the
[documentation policy](governance/documentation-policy.md) owns the commit
rules.

## Working agreements

- Change the design only after the owner confirms it. When the owner asks for
  a change, apply it, verify it, and push it.
- Simplify, but keep one concept per file; do not merge files to lower the
  count.
- Redesign one thing at a time, and fix the cause rather than a symptom.
- Method names in a request are examples, not specifications.
- Ask numbered questions, so the owner can answer by number ("2.3: yes"). When
  the owner asks for an opinion, give a recommendation, not a list of options.
- Keep the assembly count low: a consumer takes the core, one view, and
  PixieLib. A new assembly needs a stated reason.
- `hosts/NoHost/` is the owner's local test console. It stays versioned and in
  the solution, and its directory stays in `.gitignore` so local changes do not
  show; stage its versioned files with `git add -u`.

## Build

The .NET 10 SDK builds everything, the Windows views included, on any system:

```bash
dotnet build PixieInspector.slnx -p:EnableWindowsTargeting=true
```

A change is not done until the solution builds with 0 warnings and 0 errors.
Without the SDK, the official script installs it into the user profile:

```bash
curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
bash dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
```

## Checks

Save each output before a change and compare it after:

- **TuxHost** prints 67 lines: the trees of the demo objects under each layer
  of options. Compare with `cmp`.
- **TerminalHost `--dump`** prints every demo target's tree and checks each
  member's binding against plain reflection; every `bindings:` line must say
  `ok`, and the exit code is 1 on a mismatch.

```bash
dotnet run --project hosts/TuxHost/TuxHost.csproj > tux.txt
dotnet run --project hosts/TerminalHost/TerminalHost.csproj -- --dump > dump.txt
```

To test a behavior, write a small console outside the repository that
references `PixieInspector/PixieInspector.csproj` (and
`hosts/DemoObjects/DemoObjects.csproj` for the demo types). Before calling a
change done, check that the new tests catch the error: undo the change, or
break a copy on purpose, and watch them fail.

Kinds of console that have proved useful, none in the repository yet:

- A **parity** console that creates every demo type several times and prints
  the tree with every option and the report, to `diff` before and after a
  change to discovery or options.
- A **missing-assembly** library: reference a project with
  `Private="false"`, so its assembly is not copied, and the members and
  attributes that use it fail to read. Run it with `dotnet X.dll`, since
  `dotnet run` would copy the assembly back.
- An **unload** test: load an assembly into a collectible
  `AssemblyLoadContext`, create inspectors of its types, unload it, and check
  that the context is collected, with a control that holds a type on purpose
  and must fail.
- A **headless ImGui** harness that drives the ImGui view through ImGui's own
  input queue (see [PXI-PROPOSAL-003](proposals/imgui-harness.md)).

### Windowed hosts on Linux

The Avalonia and ImGui hosts open on a virtual display (`Xvfb :99`, then
`DISPLAY=:99`); Mesa's software renderer provides OpenGL. The WinForms and WPF
hosts need Wine: the .NET desktop runtime exists only for Windows, so the app
is published for `win-x64` with the runtime included.

```bash
apt-get install -y --no-install-recommends wine64 libwine fonts-wine x11-utils imagemagick xdotool
Xvfb :99 -screen 0 1024x768x24 &
export WINEPREFIX=<scratch>/wineprefix DISPLAY=:99 WINEDEBUG=-all
export WINEDLLOVERRIDES="mshtml=;winedbg.exe=d"
dotnet publish App.csproj -r win-x64 --self-contained -o "$WINEPREFIX/drive_c/app"
/usr/lib/wine/wine64 'C:\app\App.exe'
```

`import -window root shot.png` takes a screenshot, `xwininfo -root -tree` tells
when the window opened, and `xdotool` clicks and types. Wine is not Windows:
fonts are substitutes and there is no visual theme, so the owner checks the
final look on Windows.

## Packages

The core and the four views are five packages on nuget.org, always released
together at one version:

- `Directory.Build.props` holds the `Version` and the metadata the packages
  share. A project is a package only if it sets `IsPackable`; the hosts and
  `DemoObjects` do not.
- `Directory.Build.targets` makes each view require the core of its own
  version exactly (`[0.1.0]`), because the views use the core's internal
  `ViewRules`. A consumer that forces another core version gets `NU1608`.
- To see what would be published:

  ```bash
  dotnet pack PixieInspector.slnx -c Release -p:EnableWindowsTargeting=true -o artifacts
  ```

  Restore the packages into a scratch `NUGET_PACKAGES` directory, not the
  global one: a local build cached as `0.1.0` would shadow the published
  package of the same version.

CI (`.github/workflows/ci.yml`) builds on Linux and Windows with warnings as
errors, runs the two checks above, and packs, on every pull request and push
to `main`. To release:

1. Raise the `Version` in `Directory.Build.props`, and merge it into `main`.
2. On GitHub, run Actions, Publish, Run workflow, on `main`. It runs CI, stops
   if the tag `v<Version>` already exists, pushes the five packages to
   nuget.org by trusted publishing, and makes the release with that tag.

A version on nuget.org cannot be deleted, only unlisted. A run that failed
halfway through the pushes can be run again; it skips the packages already
there.

## Git

- Configure the identity before the first commit in a new clone:

  ```bash
  git config user.name "Rafael Sakamoto"
  git config user.email "rafael.sakamoto1@hotmail.com"
  git config commit.gpgsign false
  ```

- Follow the [commit rules](governance/documentation-policy.md#commits): no
  co-author or tool lines, English messages with a prefix, code and
  documentation in separate commits, and a branch per task that reaches `main`
  through a pull request.
- After a push, check that GitHub shows the commits under the login
  `Sakamoto0110`, two lines (author and committer) per commit:

  ```bash
  curl -s "https://api.github.com/repos/Sakamoto0110/PixieInspector/commits?sha=<branch>&per_page=N" |
    grep '"login"'
  ```

- On 2026-10-09 the history since `99518fa` was rewritten to remove tool
  trailers. A clone from before that diverges; bring only the new commits on
  top of the branch, without force.

## Known pitfalls

C# and the SDK:

- In C# 14, `field` is a keyword inside property accessors, so the code names
  reflection locals `fi` and `pi`.
- `extension` blocks accept properties and operators but not conversions
  (CS9282).
- Almost every `.cs` file starts with a UTF-8 BOM, and files end differently,
  with or without a final newline. Keep both as they are when editing.
- A `-windows` target drops `System.IO` and `System.Net.Http` from the implicit
  usings; a file that uses `File` or `Path` needs the `using`.
- A `dotnet publish` from the repository builds in Release; `.gitignore`
  covers `bin/Release`.
- Never stop a background app with `pkill -f <name>`: the pattern also matches
  the shell running `pkill`. Kill by PID, or `wineserver -k` for Wine.

The core:

- Reflection is lazy: `GetMembers` succeeds even when an assembly a member uses
  is missing; reading the signature (`PropertyType`, `FieldType`,
  `GetIndexParameters()`) is what throws.
- The creation events are static: a test console must unsubscribe at the end,
  or the subscription carries over into the next test.
- `Refresh()` does nothing unless the binder control reads from the objects
  (`InstanceToView`); the manual read is `Reload()`. A view shows `ViewValue`,
  while `GetValue()` reads the object at once.
- A member hidden with `new` sits in the group of the type that declares it:
  the path is `Base.Value` or `Derived.Value`, and `Value` alone finds the
  derived one.
- The PixieLib primitives the core uses are `double`: `(int)node.GetValue()` on
  a `PxPoint` field throws `InvalidCastException`.
- A collection always has the row of its chosen item below it: a field of the
  item is `Items.Item.MooX`, and its members are added with
  `inspector["Items.Item"].Add("MooX")`.
- Text typed into an `int` that is not an integer ("7.6") is refused; the
  rounding rule applies to number editors, not to text.
- PixieLib's `PxColorRgba` takes alpha last, `new(r, g, b, a = 255)`; four bytes
  in another order compile and give another color.
- Since PixieLib 0.2.0, point arithmetic is affine: `point - point` is a vector,
  and a point or a rectangle moves by a vector. `point + point`, `point * k`, and
  `-point` do not compile; subtract a reference point first.
- PixieLib comes from the nuget.org package. A change to it goes to
  `Sakamoto0110/PixieLib` and returns as a new package version.

The views:

- Inside the namespace `PixieInspector.ImGui`, the bare name `ImGui` is the
  namespace, not Hexa.NET.ImGui's class; the files use the alias `Gui`.
- In ImGui 1.92 (Hexa.NET.ImGui 2.2.9), a text field with `EnterReturnsTrue`
  hands its text back only when it is let go, and on Esc it hands back what was
  typed; the text editor treats Esc as a cancel. Headless, the font atlas is a
  renderer texture: after `Render`, give each texture in `WantCreate` an ID and
  the status `Ok`, and keep the navigation cursor visible so Space presses.
- The Avalonia view makes its rows when it is loaded: a test must put the view
  in a window and drain the UI queue before looking for controls. Simulated
  pointer events need a test-only `Avalonia.Input.Pointer`; `xdotool` gives a
  real mouse to compare with.
- A view test that changes a node's option (a range, an editor) must undo it,
  or later tests inherit it.

Under Wine:

- `wine64` lives in `/usr/lib/wine/wine64`, off the `PATH`. Do not disable
  `mscoree`, or .NET cannot load its own assemblies. Without `fonts-wine`, WPF
  fails in font mapping.
- `SendKeys.SendWait` works; simulated Shift and Ctrl are lost on mouse
  movement, because Wine rereads the keys from the X server, so a test asks
  `xdotool` to press them for real.
- Output lines end in `\r\n`. A narrow WinForms `Label` wraps instead of
  clipping, which is why the `(?)` mark is drawn.
- Xvfb can die between runs (`Error creating window handle`); start it again
  after deleting `/tmp/.X11-unix/X99` and `/tmp/.X99-lock`.
