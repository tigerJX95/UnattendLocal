# UnattendLocal — Windows Unattended Setup Configuration Tool

[简体中文](README.md) | **English**

UnattendLocal is a Chinese-language Windows desktop tool based on [Christoph Schneegans’ UnattendGenerator](https://github.com/cschneegans/unattend-generator). It lets you edit, check, and generate `autounattend.xml` for Windows 10 / 11 locally.

Current version: **V0.1 pre45 (prerelease)**. This release brings together the pre41–pre44 updates while retaining the original generation core and script execution stages. The tool prepares installation configuration; it does not execute the generated installation scripts on your current computer.

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

These are actual V0.1 pre41 screenshots showing the light and dark themes. The current pre45 release includes navigation changes and additional settings; refer to the current application for the latest interface. The application interface is currently in Chinese.

## Main features

- Configure architecture, display language, region, keyboard, time zone, computer name, installation edition, and product keys.
- Configure image selection, existing or automatic partitioning, and CompactOS. Destructive disk operations include risk explanations and confirmation.
- Configure local accounts, passwords, initial sign-in, password expiration and lockout policies, Wi-Fi, offline installation, and OOBE.
- Configure File Explorer, the Start menu, taskbar, desktop icons, visual effects, colors, backgrounds, and lock-screen images.
- Select among 60 application and system capability removal items, with Chinese impact descriptions, keyword search, and a selection summary. Items are unchecked by default.
- Configure common system behavior, including Fast Startup, long paths, system sounds, automatic device encryption, device-associated apps, Remote Desktop, and Windows PowerShell execution policy.
- Manage System, DefaultUser, FirstLogon, and UserOnce custom scripts using the original execution-stage design, with examples and scope explanations.
- Save and restore JSON plans; review summaries, recommended-setting differences, checks, redacted previews, and XML exports.
- Import existing XML and edit supported settings while preserving unknown content. Export stops with an explanation when changes cannot be merged reliably.

## Updates in this release

- Added automatic sign-in after restart, device-associated application settings, and handling of empty Windows.old directories.
- Added script-based loading for desktop and lock-screen images, retaining default and solid-color backgrounds.
- Added application removal lists, search, individual impact descriptions, and save/import integration.
- Added Remote Desktop and Windows PowerShell RemoteSigned options, disabled by default.
- Moved application removal after OOBE and custom scripts before checks and export.
- Standardized interface example labels to “示例” and retained upstream compatibility and configuration-restoration information.

See [CHANGELOG.md](CHANGELOG.md) for the consolidated update record.

## Usage

Download the Windows x64 application package from the repository’s **Releases** page, extract it completely, and run `UnattendLocal.exe`. The self-contained application does not require a separate .NET runtime installation. Use a virtual machine for initial testing.

1. Choose a recommended scenario or a custom configuration and enter the required account and image settings.
2. Adjust system, appearance, and application removal options as needed.
3. Open checks and previews, review the summary and outstanding items, then export the actual XML.
4. Place `autounattend.xml` in the root of the target installation media. A redacted preview cannot replace the actual XML.

Open a JSON plan to restore saved settings, or import an answer XML to continue editing an existing configuration. Plans saved without credentials may require passwords or keys to be entered again. If the imported original XML was not saved with the plan, import it again.

## Scope and limitations

This is a prerelease. Some non-core upstream settings may be implemented later. The project uses a fixed generation core with selective synchronization and does not claim complete parity with the latest upstream features.

XML structure checks do not replace complete Windows installation testing. Desktop image scripts must return raw image bytes, and the image must be accessible during installation. Lock-screen behavior depends on Windows edition and policies. Removing applications may affect related features; clearing a removal selection does not restore applications on an already installed system. Remote Desktop requires a supported target edition. Higher-priority policies may override RemoteSigned.

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

Alternatively, run `test.cmd` / `build.cmd`. The published executable is `publish/win-x64/UnattendLocal.exe`. Upload the application as a Release asset; do not commit build caches or binaries to the source repository.

The pre45 baseline reports 2,841 passing data, generation, and import-integration checks, with zero warnings or errors in the four-project Release build. User desktop tests passed for pre41–pre44. Actual Windows 10 / 11 installation testing, edition coverage, and some system effects still require further verification. The delivered executable was bundled using offline Windows references and runtime files with GenerateBundle; this does not mean an online `dotnet publish` was completed in the build environment. This README update changes documentation and screenshots only.

## Credits

- **Christoph Schneegans**: [upstream source](https://github.com/cschneegans/unattend-generator) · [website](https://schneegans.de/windows/unattend-generator/) · [usage guide](https://schneegans.de/windows/unattend-generator/usage/) · [script examples](https://schneegans.de/windows/unattend-generator/samples/). This project builds on his generation core with a Chinese interface and local desktop integration.
- **James Newton-King and Newtonsoft.Json contributors**: [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json), used by the upstream generation core for data processing.
- **Microsoft / .NET and WPF contributors**: [.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf), providing the desktop and runtime infrastructure.

Upstream copyright and the MIT license are retained in [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for third-party notices and [UPSTREAM.md](UPSTREAM.md) for the fixed baseline and selective synchronization record. This tool is not the original author’s official desktop distribution.

## Feedback

Please open an Issue with the tool version, Windows environment, reproduction steps, and expected and actual behavior. For installation issues, include the target image version, edition, architecture, and the stage where the issue occurred. Share only redacted logs or minimal reproduction configurations.
