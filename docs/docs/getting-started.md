# Getting Started

## Quick Start

This basic guide will walk you through the process of setting up a project with fenUISharp.

### Prerequisites
1. Basic knowledge of .NET and C#.
2. Visual Studio, Visual Studio Code or any other working editor
3. .NET Build Tools for Windows / .NET Runtime 9.0 or up
4. The fenUISharp DLLs

### Project Setup
1. Create a new C# Console App project
2. Create a 'lib' folder inside your project root and drop in your `fenUI.dll` and `.pdb`
3. Several things have to be changed in your `.csproj` file
- Change your target framework to `<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>`
- Limit the runtime identifier to `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`
- Add those package references inside your `<ItemGroup>`
```
    <PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
    <PackageReference Include="OpenTK" Version="4.9.4" />
    <PackageReference Include="SharpDX" Version="4.2.0" />
    <PackageReference Include="SharpDX.Direct3D11" Version="4.2.0" />
    <PackageReference Include="SharpDX.DirectComposition" Version="4.2.0" />
    <PackageReference Include="SharpDX.DXGI" Version="4.2.0" />
    <PackageReference Include="SkiaSharp" Version="3.116.1" />
    <PackageReference Include="SkiaSharp.Views" Version="3.116.1" />
    <PackageReference Include="SkiaSharp.Views.Desktop.Common" Version="1.68.1" />
    <PackageReference Include="SkiaSharp" Version="2.88.6" /> 
```
- Reference the `fenUI.dll` inside your `<ItemGroup>`
```
    <Reference Include="FenUISharp">
      <HintPath>lib\fenUI.dll</HintPath>
      <Private>true</Private>
    </Reference>
```
4. Go in to your `Program.cs` file and place in this demo code:

    ```csharp
    using FenUISharp;

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            FenUI.Init();
            FenUI.SetupAppModel("fenuisharp.testapp");

            FenUI.Demo();
        }
    }
    ```

The code above shows the most basic implementation, displaying the demo app. If this does not build, you've setup something wrong.