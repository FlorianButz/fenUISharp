# Getting Started

## Prerequisites

- .NET 9.0 or newer
- Windows 10 (build 19041+) or newer
- A C# editor (Visual Studio, VS Code, Rider)

## Project Setup

1. Create a new **Console App** project.
2. Create a `lib` folder and place `fenUI.dll` inside it.
3. Update your `.csproj`:

```xml
<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

4. Add these package references:

```xml
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
<PackageReference Include="OpenTK" Version="4.9.4" />
<PackageReference Include="SharpDX" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct3D11" Version="4.2.0" />
<PackageReference Include="SharpDX.DirectComposition" Version="4.2.0" />
<PackageReference Include="SharpDX.DXGI" Version="4.2.0" />
<PackageReference Include="SkiaSharp" Version="2.88.6" />
<PackageReference Include="SkiaSharp.Views" Version="3.116.1" />
<PackageReference Include="SkiaSharp.Views.Desktop.Common" Version="1.68.1" />
```

5. Reference the DLL:

```xml
<Reference Include="FenUISharp">
  <HintPath>lib\fenUI.dll</HintPath>
  <Private>true</Private>
</Reference>
```

6. Set up your `Program.cs`:

```csharp
using FenUISharp;

public class Program
{
    [STAThread]
    public static void Main()
    {
        FenUI.Init();
        FenUI.SetupAppModel("myapp.id");

        // Create a native window
        FNativeWindow window = new();
        window.Properties.Title = "My App";
        window.Properties.Width = 800;
        window.Properties.Height = 600;
        window.Show();

        // Run the built-in demo
        // FenUI.Demo();
    }
}
```

If this builds and runs, you are good to go.

## Next Steps

- Learn about [UIObject](docs/core/uiobject.md) - the base class for all UI elements
- Understand [States](docs/core/states.md) - the reactive value system
- Set up [Windows](docs/windows.md) for your app
- Read the [Components Overview](docs/components/overview.md) for available UI elements
