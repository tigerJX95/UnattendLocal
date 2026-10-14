# UnattendLocal — Windows Unattended Setup Configuration Tool

[简体中文](README.md) | **English**

UnattendLocal is a Chinese-language Windows desktop tool based on [Christoph Schneegans’ UnattendGenerator](https://github.com/cschneegans/unattend-generator). It lets you edit, check, and generate `autounattend.xml` for Windows 10 / 11 locally.

Current version: **V0.3**. This release builds on V0.2 and adds process creation auditing, and retains the original generation core and script execution stages. The tool prepares installation configuration; it does not execute the generated installation scripts on your current computer.

## Why this project exists

> Configure Windows installations offline, and reuse the settings next time.

UnattendLocal started as a personal tool for preparing Windows installation settings locally without relying on an online editor. Saving common settings as a plan makes it easier to reinstall Windows or prepare another computer: load the plan, adjust it for the target environment, and reuse the work.

The project builds on Christoph Schneegans’ established generation core, organizes common options into a Chinese desktop interface, and adds saved plans, configuration summaries, checks, previews, and editing of existing XML. The aim is to make settings easier to understand and verify before generating a file.

Publishing the project makes it available to people with similar needs and allows practical feedback to guide improvements. Development proceeds in small steps, prioritizing common features, configuration reliability, and installation testing.

Offline use means editing, saving, and generating configuration locally. Microsoft account sign-in, image downloads, and other network-dependent scripts still depend on connectivity in the target installation environment.

## Screenshots

| Configuration plans · Light theme | Edition and product key · Dark theme |
| :---: | :---: |
| ![Configuration plans in the light theme](docs/images/overview.png) | ![Edition and product key in the dark theme](docs/images/edition-and-key-dark.png) |

These are actual V0.1 pre41 screenshots showing the light and dark themes. The current V0.3 release includes navigation changes and additional settings; refer to the current application for the latest interface. The application interface is currently in Chinese.

## Main features

- Configure architecture, display language, region, keyboard, time zone, computer name, installation edition, and product keys.
- Configure image selection, existing or automatic partitioning, and CompactOS. Destructive disk operations include risk explanations and confirmation.
- Configure local accounts, passwords, initial sign-in, password expiration and lockout policies, Wi-Fi, offline installation, and OOBE.
- Configure File Explorer, the Start menu, taskbar, desktop icons, visual effects, colors, backgrounds, and lock-screen images.
- Select among 60 application and system capability removal items, with Chinese impact descriptions, keyword search, and a selection summary. Items are unchecked by default.
- Configure common system behavior, including Fast Startup, long paths, system sounds, automatic device encryption, device-associated apps, Windows Update restart behavior, NTFS last-access timestamps, process creation auditing, Remote Desktop, and Windows PowerShell execution policy.
- Manage System, DefaultUser, FirstLogon, and UserOnce custom scripts using the original execution-stage design, with examples and scope explanations.
- Save and restore JSON plans; review summaries, recommended-setting differences, checks, redacted previews, and XML exports.
- Import existing XML and edit supported settings while preserving unknown content. Export stops with an explanation when changes cannot be merged reliably.

## V0.3 updates

- Added process creation auditing with three modes: Windows defaults, audit enabled without configuring command-line capture, and audit with command-line capture. The default leaves policies unchanged.
- Integrated JSON plans, configuration summaries, recommendation differences/restoration, help, and previews.
- XML import checks actual audit commands and the execution pipeline. Altered, duplicated, missing, or inactive payloads are preserved and cannot be overwritten; older imported plans can migrate.
- Release executable names now follow Program_Version_Architecture_RuntimeType, with both publishing variants updated.
- Retained V0.2 maintenance settings, existing functionality, fixed upstream core, and original script execution stages.

## Usage

Download the Windows x64 application package from the repository’s **Releases** page, extract it completely, and run the matching `UnattendLocal_V0.3_x64_SelfContained.exe` or `UnattendLocal_V0.3_x64_FrameworkDependent.exe`. Choose `UnattendLocal_V0.3_x64_SelfContained.zip` for a bundled .NET runtime, or `UnattendLocal_V0.3_x64_FrameworkDependent.zip` if .NET 10 Desktop Runtime x64 is already installed. Both have the same features. Use a virtual machine for initial testing.

1. Choose a recommended scenario or a custom configuration and enter the required account and image settings.
2. Adjust system, appearance, and application removal options as needed.
3. Open checks and previews, review the summary and outstanding items, then export the actual XML.
4. Place `autounattend.xml` in the root of the target installation media. A redacted preview cannot replace the actual XML.

Open a JSON plan to restore saved settings, or import an answer XML to continue editing an existing configuration. Plans saved without credentials may require passwords or keys to be entered again. If the imported original XML was not saved with the plan, import it again.

## Scope and limitations

This is a V0.x development release, published as an ordinary GitHub Release. It has not yet met the complete V1.0 acceptance requirements. Some non-core upstream settings may be implemented later. The project uses a fixed generation core with selective synchronization and does not claim complete parity with the latest upstream features.

XML structure checks do not replace complete Windows installation testing. Command-line auditing writes arguments in plain text to the security log and may expose credentials. Enabling only auditing does not force existing command-line capture policies off. Restart behavior depends on Windows version, organization policies, update deadlines, and user-scheduled restarts; this option does not guarantee preventing every restart. Schedule restarts yourself to complete updates. Disabling NTFS last-access updates may affect archival, backup, or cleanup rules that depend on those timestamps. Desktop image scripts must return raw image bytes, and the image must be accessible during installation. Lock-screen behavior depends on Windows edition and policies. Removing applications may affect related features; clearing a removal selection does not restore applications on an already installed system. Remote Desktop requires a supported target edition. Higher-priority policies may override RemoteSigned.

Exported XML, JSON plans containing credentials, and custom scripts may contain sensitive data. Remove passwords, keys, and tokens before sharing. The preview does not automatically identify secrets inside arbitrary scripts. Disk wiping and formatting may delete data; verify the target disk and back up first.

## Build from source

Use Visual Studio 2026 and the .NET 10 SDK with the .NET desktop development workload. Open `UnattendLocal.sln` at the repository root. The solution contains Desktop, Core, vendor/UnattendGenerator, and Tests; Desktop is the default startup project.

Run from the source root:

```powershell
dotnet restore UnattendLocal.sln
dotnet build UnattendLocal.sln -c Release -m:1 -nr:false
dotnet run --project Tests/Tests.csproj -c Release --no-build
dotnet publish Desktop/Desktop.csproj -c Release -m:1 -nr:false -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Alternatively, run `test.cmd` for checks. `build.cmd` invokes `publish.ps1` to produce both ZIP variants in `UnattendLocal-release/v0.3` next to the source directory. Upload both as assets of the same `v0.3` Release, with pre-release unchecked. Do not commit build caches or release binaries to the source repository.

Validation results are provided with the full source package. All 3,366 automated checks passed; the four-project Release build completed without warnings or errors, and both Windows x64 single-file executables were published. V0.3 UI execution, actual Windows installation, and both published variants still require local Windows acceptance testing.

## Credits

- **Christoph Schneegans**: [upstream source](https://github.com/cschneegans/unattend-generator) · [website](https://schneegans.de/windows/unattend-generator/) · [usage guide](https://schneegans.de/windows/unattend-generator/usage/) · [script examples](https://schneegans.de/windows/unattend-generator/samples/). This project builds on his generation core with a Chinese interface and local desktop integration.
- **James Newton-King and Newtonsoft.Json contributors**: [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json), used by the upstream generation core for data processing.
- **Microsoft / .NET and WPF contributors**: [.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf), providing the desktop and runtime infrastructure.

Upstream copyright and the MIT license are retained in [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt). Third-party notices and fixed-baseline details are included in the full source package. This tool is not the original author’s official desktop distribution.

## Feedback

Please open an Issue with the tool version, Windows environment, reproduction steps, and expected and actual behavior. For installation issues, include the target image version, edition, architecture, and the stage where the issue occurred. Share only redacted logs or minimal reproduction configurations.
