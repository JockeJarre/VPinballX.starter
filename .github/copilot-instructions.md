# GitHub Copilot Instructions for VPinballX.starter

## Repository Overview

**VPinballX.starter** is a small Windows utility written in C# that acts as a launcher for different versions of VPinballX (Visual Pinball X) based on table file version detection. It reads table version information from .vpx files and launches the appropriate VPinballX executable version according to user configuration.

### High-Level Repository Information
- **Project Type**: Windows WPF Application (.NET 8.0)
- **Language**: C# with WPF UI framework
- **Target Runtime**: .NET 8.0 Windows-specific (`net8.0-windows`)
- **Repository Size**: Small single-project solution (~50 files)
- **Main Dependencies**: 
  - OpenMcdf (for reading compound document files)
  - Salaros.ConfigParser (for INI file parsing)
  - System.Management (for process management)

## Build Instructions

### Prerequisites
- .NET 8.0 SDK or later
- Windows operating system (WPF dependency)
- Visual Studio 2019+ or VS Code with C# extension (optional)

### Build Commands

**ALWAYS run these commands from the repository root directory.**

#### Clean
```powershell
dotnet clean
```
- **Purpose**: Removes all build artifacts and intermediate files
- **Required**: No, but recommended before fresh builds
- **Duration**: < 5 seconds

#### Build (Debug)
```powershell
dotnet build
```
- **Purpose**: Builds the solution in Debug configuration
- **Required**: Yes, before running or testing
- **Duration**: 5-15 seconds
- **Output**: `VPinballX.starter\bin\Debug\net8.0-windows\VPinballX.starter.exe`

#### Build (Release)
```powershell
dotnet build --configuration Release
```
- **Purpose**: Builds optimized release version
- **Required**: For production/distribution builds
- **Duration**: 5-15 seconds
- **Output**: `VPinballX.starter\bin\Release\net8.0-windows\VPinballX.starter.exe`

#### Publish Single-File Distribution
```powershell
cd VPinballX.starter
dotnet publish -r win-x64 -c Release --self-contained false -p:PublishSingleFile=true
```
- **Purpose**: Creates deployable single-file executable
- **Required**: For distribution and GitHub releases
- **Duration**: 10-20 seconds
- **Output**: `VPinballX.starter\bin\Release\net8.0-windows\win-x64\publish\VPinballX.starter.exe`

#### Run Application
```powershell
dotnet run --project VPinballX.starter
```
- **Note**: This will launch the WPF GUI application
- **Testing**: For testing without GUI, the application expects VPX table files as arguments

### Build Validation
1. **Always** clean before major changes: `dotnet clean`
2. **Always** build after code changes: `dotnet build`
3. **Always** test publish process before releases: `dotnet publish` command above
4. Build artifacts are automatically generated in `bin/` and `obj/` folders
5. No explicit test suite exists - validation is manual through application execution

### Known Build Issues & Workarounds
- **Issue**: Application requires Windows-specific APIs (WPF, Win32)
- **Workaround**: Only build/run on Windows environments
- **Issue**: Single-file publish may require Microsoft Visual C++ Redistributable
- **Workaround**: Document this requirement for end users

## Project Layout & Architecture

### Solution Structure
```
VPinballX.starter.sln              # Main solution file
├── VPinballX.starter/              # Main project directory
│   ├── VPinballX.starter.csproj    # Project file with dependencies
│   ├── App.xaml                    # WPF application definition
│   ├── App.xaml.cs                 # Main application logic (600+ lines)
│   ├── AssemblyInfo.cs             # Assembly metadata
│   └── Properties/                 # Project properties and resources
└── doc/                            # License files for dependencies
```

### Key Source Files
- **`VPinballX.starter/App.xaml.cs`**: Contains ALL application logic including:
  - Main entry point (`Application_Startup`)
  - VPX table version detection using OpenMcdf
  - Process management and child process tracking
  - INI file configuration parsing
  - Pre/Post command execution
  - Native Windows API calls via P/Invoke
- **`VPinballX.starter.csproj`**: NuGet dependencies, build configuration
- **`README.md`**: Comprehensive user documentation with configuration examples

### Configuration Files
- **No linting configuration**: No `.editorconfig`, no code analysis rules
- **No test configuration**: No test projects or test files
- **Build profiles**: `Properties/PublishProfiles/FolderProfile.pubxml` for deployment

### CI/CD Pipeline
Located in `.github/workflows/`:
- **`VPinballX.starter.yml`**: Main build workflow triggered on all pushes
  - Uses Windows latest runner (windows-latest)
  - Builds for win-x64 architecture
  - Creates artifacts with version tagging
  - Bundles with documentation and licenses
- **`prerelease.yml`**: Manual release workflow using existing artifacts

### Architecture Notes
- **Single-file design**: All logic in one large `App.xaml.cs` file (~600 lines)
- **No separation of concerns**: UI, business logic, and file I/O mixed together
- **Native interop heavy**: Extensive use of Win32 APIs via P/Invoke
- **Process management**: Uses Windows Job Objects for child process cleanup
- **File format parsing**: Direct binary reading of VPX table files (OLE compound documents)

### Dependencies Not Obvious from Structure
- **Windows-specific**: Requires Windows OS for WPF and Win32 APIs
- **VPinballX ecosystem**: Designed to work with VPinballX table files (.vpx)
- **INI configuration**: Runtime dependency on external `VPinballX.starter.ini` file
- **Process spawning**: Creates child processes for actual VPX executables

### Validation Steps for Changes
1. **Build validation**: `dotnet build` must succeed
2. **Publish validation**: `dotnet publish` command must create single file
3. **Manual testing**: Run with sample .vpx files or configuration
4. **CI validation**: GitHub Actions must pass (automatic on push)
5. **No automated tests**: All validation is manual/integration-based

### Common Modification Areas
- **Version mapping logic**: Lines ~400-430 in `App.xaml.cs`
- **Configuration parsing**: Lines ~260-350 in `App.xaml.cs`
- **Process launching**: `StartAnotherProgram` method around line 554
- **Table version detection**: Lines ~385-400 using OpenMcdf library
- **Pre/Post command execution**: Lines ~440-480 and ~507-525

**IMPORTANT**: Always trust these instructions and only explore the codebase if information here is incomplete or incorrect. The project structure is simple but the single main file is large and complex.
