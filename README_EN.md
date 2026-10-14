# UnattendLocal — Windows Unattended Setup Configuration Tool

[简体中文](README.md) | **English**

UnattendLocal is a Chinese-language Windows desktop tool based on [Christoph Schneegans’ UnattendGenerator](https://github.com/cschneegans/unattend-generator). It lets you edit, check, and generate `autounattend.xml` for Windows 10 / 11 locally.

Current version: **V0.5**. This release brings together the V0.4 UAC/WPBT settings and documentation updates, and retains the original generation core and script execution stages. The tool prepares installation configuration; it does not execute the generated installation scripts on your current computer.

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

These are actual V0.1 pre41 screenshots showing the light and dark themes. The current V0.5 release includes navigation changes and additional settings; refer to the current application for the latest interface. The application interface is currently in Chinese.

## Main features

- Configure architecture, display language, region, keyboard, time zone, computer name, installation edition, and product keys.
- Configure image selection, existing or automatic partitioning, and CompactOS. Destructive disk operations include risk explanations and confirmation.
- Configure local accounts, passwords, initial sign-in, password expiration and lockout policies, Wi-Fi, offline installation, and OOBE.
- Configure File Explorer, the Start menu, taskbar, desktop icons, visual effects, colors, backgrounds, and lock-screen images.
- Select among 60 application and system capability removal items, with Chinese impact descriptions, keyword search, and a selection summary. Items are unchecked by default.
- Configure common system behavior, including Fast Startup, long paths, system sounds, automatic device encryption, device-associated apps, Windows Update restart behavior, NTFS last-access timestamps, process creation auditing, UAC/WPBT, Remote Desktop, and Windows PowerShell execution policy.
- Manage System, DefaultUser, FirstLogon, and UserOnce custom scripts using the original execution-stage design, with examples and scope explanations.
- Save and restore JSON plans; review summaries, recommended-setting differences, checks, redacted previews, and XML exports.
- Import existing XML and edit supported settings while preserving unknown content. Export stops with an explanation when changes cannot be merged reliably.

## V0.5 updates

- Retained V0.4 UAC/WPBT settings and configuration integration, with both opt-ins disabled by default.
- Download instructions now use standalone EXE files directly, without requiring ZIP downloads.
- Removed references to build scripts that are not uploaded to the repository.
- Added ChatGPT / Codex acknowledgments in both READMEs, preserving upstream and dependency credits.
- Public documentation focuses on features and usage, omitting validation/build status paragraphs.
- Updated the application and release files to V0.5 while retaining the fixed generator core and script stages.

## Usage

Download and run the matching Windows x64 executable from the repository’s **Releases** page: `UnattendLocal_V0.5_x64_SelfContained.exe` or `UnattendLocal_V0.5_x64_FrameworkDependent.exe`. Choose `UnattendLocal_V0.5_x64_SelfContained.exe` for a bundled .NET runtime, or `UnattendLocal_V0.5_x64_FrameworkDependent.exe` if .NET 10 Desktop Runtime x64 is already installed. Both have the same features. Use a virtual machine for initial testing.

1. Choose a recommended scenario or a custom configuration and enter the required account and image settings.
2. Adjust system, appearance, and application removal options as needed.
3. Open checks and previews, review the summary and outstanding items, then export the actual XML.
4. Place `autounattend.xml` in the root of the target installation media. A redacted preview cannot replace the actual XML.

Open a JSON plan to restore saved settings, or import an answer XML to continue editing an existing configuration. Plans saved without credentials may require passwords or keys to be entered again. If the imported original XML was not saved with the plan, import it again.

## Scope and limitations

This version is published as an ordinary GitHub Release. Some non-core upstream settings will be implemented later. The project uses a fixed generation core with selective synchronization and does not claim complete parity with the latest upstream features.

Disabling UAC reduces elevation protection. Blocking WPBT does not modify firmware or uninstall software and may affect programs provided through that mechanism. Command-line auditing writes arguments in plain text to the security log and may expose credentials. Enabling only auditing does not force existing command-line capture policies off. Restart behavior depends on Windows version, organization policies, update deadlines, and user-scheduled restarts; this option does not guarantee preventing every restart. Schedule restarts yourself to complete updates. Disabling NTFS last-access updates may affect archival, backup, or cleanup rules that depend on those timestamps. Desktop image scripts must return raw image bytes, and the image must be accessible during installation. Lock-screen behavior depends on Windows edition and policies. Removing applications may affect related features; clearing a removal selection does not restore applications on an already installed system. Remote Desktop requires a supported target edition. Higher-priority policies may override RemoteSigned.

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



## Credits

- **Christoph Schneegans**: [upstream source](https://github.com/cschneegans/unattend-generator) · [website](https://schneegans.de/windows/unattend-generator/) · [usage guide](https://schneegans.de/windows/unattend-generator/usage/) · [script examples](https://schneegans.de/windows/unattend-generator/samples/). This project builds on his generation core with a Chinese interface and local desktop integration.
- **James Newton-King and Newtonsoft.Json contributors**: [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json), used by the upstream generation core for data processing.
- **Microsoft / .NET and WPF contributors**: [.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf), providing the desktop and runtime infrastructure.
- **ChatGPT / Codex (OpenAI)**: [ChatGPT](https://chatgpt.com/) · [Codex](https://openai.com/codex/). Assisted with requirement refinement, implementation, troubleshooting, Chinese-language guidance, and documentation.

Upstream copyright and the MIT license are retained in [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt). Third-party notices and fixed-baseline details are included in the full source package. This tool is not the original author’s official desktop distribution.

## Feedback

Please open an Issue with the tool version, Windows environment, reproduction steps, and expected and actual behavior. For installation issues, include the target image version, edition, architecture, and the stage where the issue occurred. Share only redacted logs or minimal reproduction configurations.
