# UnattendLocal — Windows Unattended Setup Configuration Tool

**[Download the latest release](https://github.com/tigerJX95/UnattendLocal/releases/latest)**

[简体中文](README.md) | **English**

UnattendLocal is a Chinese-language Windows desktop tool based on [Christoph Schneegans’ UnattendGenerator](https://github.com/cschneegans/unattend-generator). It lets you edit, check, and generate `autounattend.xml` for Windows 10 / 11 locally.

Current version: **V1.19**. Includes static IPv4/IPv6 dual stack, subnet-mask input, settings search, setup driver directories, Dynamic Update, partitioning, OOBE and account settings, and existing XML import/editing.

## 💡 Why this project exists

> Configure Windows installations offline, and reuse the settings next time.

UnattendLocal started as a personal tool for preparing Windows installation settings locally without relying on an online editor. Saving common settings as a plan makes it easier to reinstall Windows or prepare another computer: load the plan, adjust it for the target environment, and reuse the work.

The project builds on Christoph Schneegans’ established generation core, organizes common options into a Chinese desktop interface, and adds saved plans, configuration summaries, checks, previews, and editing of existing XML. The aim is to make settings easier to understand and verify before generating a file.

Publishing the project makes it available to people with similar needs and allows practical feedback to guide improvements. Development proceeds in small steps, prioritizing common features, configuration reliability, and installation testing.

Offline use means editing, saving, and generating configuration locally. Microsoft account sign-in, image downloads, and other network-dependent scripts still depend on connectivity in the target installation environment.

See [first-logon options](docs/FirstLogon.md) and [XML import behavior](docs/XmlImport.md) for replacement, cancellation and read-only script handling.

## 📸 Screenshots

Screenshot version: **V1.7**. These are actual Windows application screenshots captured on 2026-10-19, preserved as the original PNG files. The application interface is currently in Chinese.

| Configuration plans · Light theme | Configuration plans · Dark theme |
| :---: | :---: |
| ![V1.7 configuration plans in the light theme](docs/images/overview-light.png) | ![V1.7 configuration plans in the dark theme](docs/images/overview-dark.png) |

| Edition and product key · Light theme | Edition and product key · Dark theme |
| :---: | :---: |
| ![V1.7 edition and product key in the light theme](docs/images/edition-and-key-light.png) | ![V1.7 edition and product key in the dark theme](docs/images/edition-and-key-dark.png) |

The displayed product key is a public generic installation key for selecting an edition; it does not grant an activation license.

## ✨ Main features

- Configure static IPv4 and IPv6 independently for a target MAC, including dual-stack addresses, prefixes, gateways and DNS. IPv4 accepts a dotted subnet mask or /prefix in one input.

- View the version, usage notices and embedded third-party licenses through Help and About, with offline reading and copying.

- Search settings through the toolbar magnifier or Ctrl+F using Chinese/English names and aliases, filter by page, then navigate and highlight the result.
- Specify target Windows workgroup names and a Windows PE log directory, preserving unknown domain-join configuration.
- Independently configure Windows registered-owner and organization information, without changing login accounts, computer names or activation.
- Configure architecture, display language, region, keyboard, time zone, computer name, installation edition, and product keys.
- Configure image selection, existing or automatic partitioning, system-partition sizing and a pre-partition pause, and CompactOS. Destructive disk operations include risk explanations and confirmation.
- Configure installation-stage Dynamic Update and UI behavior, plus INF driver directories accessible from target Windows PE.
- Configure local accounts, passwords, initial sign-in, password expiration and lockout policies, Wi-Fi, offline installation, and OOBE.
- Configure File Explorer, the Start menu, taskbar, desktop icons, visual effects, colors, backgrounds, and lock-screen images.
- Select among 60 application and system capability removal items, with Chinese impact descriptions, keyword search, and a selection summary. Items are unchecked by default.
- Configure `$OEM$` media configuration sets and AppLocker policy XML, with policy editing and structural validation.
- Configure Narrator-assisted setup, the Edge uninstall option, and system protection on drive C:. All three are unchecked by default.
- Configure guest tool installation for VMware, VirtualBox, KVM / QEMU (VirtIO), and Parallels, with media requirements and scope descriptions.
- Configure common system behavior, including Fast Startup, long paths, system sounds, automatic device encryption, device-associated apps, Windows Update restart behavior, NTFS last-access timestamps, process creation auditing, UAC/WPBT, Remote Desktop, and Windows PowerShell execution policy.
- Manage System, DefaultUser, FirstLogon, and UserOnce custom scripts using the original execution-stage design. Duplicate scripts with copies disabled by default for review.
- Save and restore JSON plans; review summaries, recommended-setting differences, checks, redacted previews, and XML exports.
- Import existing XML and edit supported settings while preserving unknown content. Export stops with an explanation when changes cannot be merged reliably.

## V1.19 application installation and desktop shortcut templates

- Add a Google Chrome installation template using WinGet at FirstLogon. It waits for the executable, installs by exact ID from the community source, preserves hash checks, skips automatic upgrades and checks the exit code.
- Add a public desktop shortcut template at System for program and web links. Editable defaults link to System Properties and the project release page. The desktop path is resolved by Windows rather than a fixed drive letter.
- Validate the entire shortcut list before writing. Preserve existing files and folders, generate through temporary files and clean temporary data on failure.
- Keep both capabilities in the upstream sample-script category. Entries start disabled, remain editable, and support existing plan and XML workflows.

Requirements and examples: [Practical script templates](docs/ScriptTemplates.md).

## V1.18 network diagnostics templates and script names

- Add a local Ping template allowing IPv4/IPv6 echo requests from the local subnet on Private and Domain profiles. It keeps the firewall enabled and adds no Public-profile exception. Reruns update owned rules; conflicting names belonging to another application stop the script.
- Add a template to turn off solicited Windows Remote Assistance during System. It preserves existing RDP, Quick Assist and third-party remote access settings.
- Add editable script names. Templates supply a name, lists show name, stage and format, and duplication preserves the name. Plans and valid local XML restoration records retain names without changing executable commands or script filenames.
- Capture expected simulated installation warnings inside verification tests, showing an explicit simulation notice and results instead of misleading build warnings.

Details: [Practical script templates](docs/ScriptTemplates.md).

## V1.17 practical script templates

- Add a template selector for offline .NET Framework 3.5 installation, Microsoft Visual C++ v14 Redistributables, and disabling AutoRun on all drives. Each template shows its purpose, execution stage and requirements, and is discoverable through settings search.
- Added scripts are disabled and selected for review. They remain editable ordinary scripts, with duplicate protection, plan persistence and XML export/reimport support.
- The .NET 3.5 template uses local installation media only, with no Windows Update fallback. It stops on build 28000 and newer, which require a standalone installer.
- The VC++ template chooses packages by target architecture and includes x86 on 64-bit systems. It checks a valid Microsoft Authenticode signature before installation, waits for completion, validates exit codes and cleans temporary files without restarting.
- Fix equivalent version comparisons in publishing. Manual builds include version, source commit and file hashes; packaging remains manually triggered only.

Details: [Practical script templates](docs/ScriptTemplates.md).

## V1.16 file import and save improvements

Download the app from the [latest release](https://github.com/tigerJX95/UnattendLocal/releases/latest).

- Share bounded reading across answer XML, JSON plans, Wi-Fi profiles, AppLocker policies, and Start / taskbar layouts. Byte limits are enforced throughout reading.
- Reject malformed UTF-8 and BOM-marked UTF-16 / UTF-32 instead of silently replacing invalid bytes. Valid text and line breaks are preserved. Convert ANSI / GBK files before importing.
- Reuse staged saving for scripts and theme preferences: write and flush a temporary file before replacing the destination. Failed saves preserve the destination and attempt temporary-file cleanup.
- Confirm that upstream commit 27237f8's computer-name prompt is already included in V1.15; no duplicate merge is needed.

## V1.15 power plans and Windows Error Reporting

- Add a preferred power-plan option: keep Windows defaults, select Balanced, High performance or Power saver, or specify an existing plan GUID. The setting is applied in specialize; plans are not created or imported.
- Add native Windows Error Reporting controls: unspecified, enabled or disabled via DisableWER. This does not disable services or all diagnostic collection.
- Support JSON saving, native XML import/editing, summaries, settings search and error navigation. Both options remain unspecified by default.
- Preserve unrecognized values, duplicate entries and other passes as read-only. Editing recognized scalar settings retains unrelated component content.
- Preserve V1.14 extra-XML ownership and pending changes while migrating older JSON baselines.
- Share inspection and migration logic and improve concise paragraph-based help.

See [Power plans and error reporting](docs/NativePreferences.md) for usage and official references.

## V1.14 quick presets and advanced XML components

- Add a minimal preset that leaves language, edition, partitioning and accounts to Windows Setup, retaining installation-sensitive files by default.
- Add the upstream local-account example: one Example administrator account with an empty password and one automatic logon; review or change its credentials before export.
- Add up to 15 extra XML entries with component and configuration-pass selection. Independent child settings are added without replacing generated content; duplicate top-level settings stop export.
- Include a disabled OEM support-information example, fragment checks, settings search and error navigation.
- Support JSON saving and round-trip editing of this application's XML exports. Preview redacts custom XML; saving without sensitive content omits fragments and requires re-entry.
- Improve paragraph-based on-page guidance and tooltip line spacing.

See [Quick presets and advanced XML](docs/AdvancedXml.md). Structural checks do not validate setting values against the target Windows image.

## V1.13 answer-file export improvements

- Improve ISO export with a disc icon, concise paragraph-based on-page guidance and complementary tooltip details.
- Choose `notautounattend.xml` for explicit `setup.exe /Unattend:path` invocation. The filename preference is saved in JSON plans; older plans retain the automatic filename by default.
- Export a non-bootable ISO with one complete answer file at its root, suitable for mounting as a second virtual CD/DVD drive alongside Windows installation media.
- XML and ISO share configuration validation, disk-risk confirmation and atomic saving. The filename choice does not change installation settings or imported XML merging.
- Add help, settings search and offline licensing for the ISO component. Both output formats may contain installation credentials.

For media preparation and explicit invocation, see [XML and ISO export](docs/AnswerFileExport.md).

## 📦 Download and usage

Download and run the matching Windows x64 executable from the repository’s [**Releases**](https://github.com/tigerJX95/UnattendLocal/releases/latest) page: `UnattendLocal_V1.19_x64_SelfContained.exe` or `UnattendLocal_V1.19_x64_FrameworkDependent.exe`. Choose `UnattendLocal_V1.19_x64_SelfContained.exe` for a bundled .NET runtime, or `UnattendLocal_V1.19_x64_FrameworkDependent.exe` if .NET 10 Desktop Runtime x64 is already installed. Both have the same features. Use a virtual machine for initial testing.

1. Choose a recommended scenario or a custom configuration and enter the required account and image settings.
2. Adjust system, appearance, and application removal options as needed.
3. Open checks and previews, review the summary and outstanding items, then export the actual XML.
4. Place `autounattend.xml` in the root of the target installation media. A redacted preview cannot replace the actual XML.

Open a JSON plan to restore saved settings, or import an answer XML to continue editing an existing configuration. Plans saved without credentials may require passwords or keys to be entered again. If the imported original XML was not saved with the plan, import it again.

## 📖 Scope and limitations

### Related guides

- See [IPv4 / IPv6 configuration](docs/StaticIP.md) and [V1.0 scope](docs/V1Scope.md).
- See [Help and About](docs/About.md) for the information window and license texts.
- See [First logon](docs/FirstLogon.md) for sign-in options.
- See [XML import](docs/XmlImport.md) for importing and cancelling.
- See [Settings search](docs/SettingsSearch.md) for finding options.

### Compatibility junctions

- The compatibility-junction option removes items within its specified scope based on the ReparsePoint attribute, which may include other reparse points.
- Deleted links are not restored; validate the operation in a virtual machine first.

### Computer naming

- Choose the naming mode in basic settings.
- Leaving the fixed name empty still lets Windows generate it.
- The installation-time dialog requires valid input before setup can continue.
- A naming script must return a valid computer name (1–15 characters, not entirely numeric, without invalid characters, and different from local account names).
- The tool does not run the script or verify its runtime result.
- Scripts are stored verbatim in plans and XML; check for sensitive information before sharing.

### Languages and keyboards

- The second and third locale/keyboard pairs apply in fixed language/region mode.
- They do not install language packs or change this tool’s Chinese interface.
- Interactive language selection retains these settings in the plan without writing fixed language settings.

### System protection and root permissions

- The four options appear in system settings and are unchecked by default, leaving Windows behavior unchanged.
- Disabling SAC, SmartScreen, or core isolation reduces the corresponding protection.
- Re-enabling SAC may require a Windows reset or reinstall.
- The SmartScreen option also hides the Windows Security tray icon but does not disable the Defender antivirus engine.
- The core-isolation option does not uninstall Hyper-V, WSL2, or VM tools and does not guarantee better performance; Windows version, organization policies, and firmware locks can affect results.
- Restricting C: root permissions may affect programs that expect ordinary users to write there; it does not change permissions throughout the disk or on other drives.

### Setting changes and XML editing

- Clearing an option changes newly generated configuration only; it does not restore an installed system.
- XML editing is available for complete recognized native flows; unknown or modified content is preserved and unreliable replacements are rejected.

### `$OEM$` configuration sets

- The `$OEM$` option generates installation settings only.
- Prepare the media folders alongside `autounattend.xml`: `$$` maps to the Windows directory and `$1` to the system-drive root.
- Copying can replace matching files and does not automatically install applications.
- Do not pre-create future users’ personal profile folders.
- Custom PE copying scripts are preserved and cannot be reliably controlled by this switch.

### AppLocker policy

- AppLocker starts AppIDSvc and runs Set-AppLockerPolicy during specialize without `-Merge`, potentially replacing the existing local policy.
- Enforced rules can block setup scripts, system tools, or administrator applications; audit them in a VM and prepare recovery first.
- Structural checks cannot establish whether a policy is suitable.
- Results depend on Windows version, components, and organization policy.
- JSON always retains policy text, including when credentials are omitted; review paths, SIDs, and other entered information before sharing.
- Preview hides the entire policy XML.
- Clearing the option changes new configuration only and does not restore an installed system.

### Wireless networks and profile XML

- WPA3 requires compatible Windows, wireless drivers, and access points.
- Open Wi-Fi does not encrypt the wireless link and may require a captive portal.
- Wireless profile XML may contain credentials; saving without credentials omits the whole profile.
- Automatic connection requires the profile name to match the SSID; encrypted keys are not portable.
- Prepare required certificates and drivers separately.

### Narrator

- Narrator remains enabled until adjusted in Windows accessibility settings; customized PE media may lack speech or audio support.

### Edge uninstall entry

- The Edge option only attempts a policy-file change: it does not uninstall Edge or WebView2, and Windows updates may revert it.

### System Restore on C:

- Disabling System Restore on C: can delete existing restore points and removes that recovery path.

### UAC and WPBT

- Disabling UAC reduces elevation protection.
- Blocking WPBT does not modify firmware or uninstall software and may affect programs provided through that mechanism.

### VM guest tools and PE drivers

- VM guest tools require attached official tool media compatible with the target Windows version and architecture.
- Missing media means no installation.
- VirtIO Guest Tools do not replace Windows PE drivers; existing PE driver scripts in imported XML are preserved.

### Command-line auditing

- Command-line auditing writes arguments in plain text to the security log and may expose credentials.
- Enabling only auditing does not force existing command-line capture policies off.

### Windows Update and restarts

- Restart behavior depends on Windows version, organization policies, update deadlines, and user-scheduled restarts; this option does not guarantee preventing every restart.
- Schedule restarts yourself to complete updates.

### NTFS last-access timestamps

- Disabling NTFS last-access updates may affect archival, backup, or cleanup rules that depend on those timestamps.

### Desktop and lock-screen images

- Desktop image scripts must return raw image bytes, and the image must be accessible during installation.
- Lock-screen behavior depends on Windows edition and policies.

### Application removal

- Removing applications may affect related features; clearing a removal selection does not restore applications on an already installed system.

### Remote Desktop and PowerShell policy

- Remote Desktop requires a supported target edition.
- Higher-priority policies may override RemoteSigned.

### Sensitive information and disk operations

- Exported XML, JSON plans containing credentials, and custom scripts may contain sensitive data.
- Remove passwords, keys, and tokens before sharing.
- Preview hides the whole wireless profile, but does not automatically identify secrets inside arbitrary scripts.
- Disk wiping and formatting may delete data; verify the target disk and back up first.

## 🛠️ Build from source

Use Visual Studio 2026 and the .NET 10 SDK with the .NET desktop development workload. Open `UnattendLocal.sln` at the repository root. The solution contains Desktop, Core, vendor/UnattendGenerator, and Tests; Desktop is the default startup project.

Alternatively, run these commands from the source root:

```powershell
dotnet restore UnattendLocal.sln
dotnet build UnattendLocal.sln -c Release -m:1 -nr:false
dotnet publish Desktop/Desktop.csproj -c Release -m:1 -nr:false -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

## 🙏 Credits

- **Christoph Schneegans**: [upstream source](https://github.com/cschneegans/unattend-generator) · [website](https://schneegans.de/windows/unattend-generator/) · [usage guide](https://schneegans.de/windows/unattend-generator/usage/) · [script examples](https://schneegans.de/windows/unattend-generator/samples/). This project builds on his generation core with a Chinese interface and local desktop integration.
- **James Newton-King and Newtonsoft.Json contributors**: [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json), used by the upstream generation core for data processing.
- **Microsoft / .NET and WPF contributors**: [.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf), providing the desktop and runtime infrastructure.
- **ChatGPT / Codex (OpenAI)**: [ChatGPT](https://chatgpt.com/) · [Codex](https://openai.com/codex/). Assisted with requirement refinement, implementation, troubleshooting, Chinese-language guidance, and documentation.

Upstream copyright and the MIT license are retained in [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt). Third-party notices and fixed-baseline details are included in the full source package. This tool is not the original author’s official desktop distribution.

## 💬 Feedback

Please open an Issue with the tool version, Windows environment, reproduction steps, and expected and actual behavior. For installation issues, include the target image version, edition, architecture, and the stage where the issue occurred. Share only redacted logs or minimal reproduction configurations.

## 📖 Project license

The local UI and adaptation code are licensed under the [MIT License](LICENSE), Copyright (c) 2026 tigerJX95. Third-party copyrights and licenses are retained in [third-party notices](THIRD_PARTY_NOTICES.md). Full texts are also available offline through Help and About → Open-source Licenses.
