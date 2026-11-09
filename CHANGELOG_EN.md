# Version history

[Back to the README](README_EN.md) · [简体中文](CHANGELOG.md)

## V1.41.1 Security and reliability fixes · 2026-11-09

- The XML preview recognizes embedded WLAN profiles by their root element and redacts their keys even when the files have been renamed.
- WinPE review exempts only exact matches for known hardware-check commands. Loading a legacy JSON plan rechecks the retained source XML to determine whether review is required.
- AppLocker import and export review recognize legacy paths consistently and distinguish audit-only policies from enforced policies.
- XML import accepts files with out-of-range recovery partition sizes. The original command is kept read-only and must be reviewed before export. Recovery size and other numeric overflow notices are available in Chinese and English.

## V1.41 AppLocker review before exporting answer files · 2026-11-09

- The preview shows AppLocker rule collections, enforcement modes, and warnings from the generated answer file.
- XML and ISO export ask for review when the policy has warnings or when retained AppLocker commands cannot be fully recognized. An audit-only policy without warnings goes directly to the save dialog.

See [Configuration sets and AppLocker](docs/DeploymentOptions.md).

## V1.40 AppLocker policy review · 2026-11-07

- Show each AppLocker rule collection's enforcement mode and allow/deny counts in the policy check dialog.
- Flag collections that may enforce rules, including unconfigured collections with rules and collections containing only deny rules.

See [Configuration sets and AppLocker](docs/DeploymentOptions.md).

## V1.39 Import additional XML fragments · 2026-11-05

- Import component settings from a local file. The new draft uses the selected component and pass, starts disabled, and leaves the existing entry intact.
- Import checks encoding, size, and fragment structure. Exporting an enabled draft still requires confirmation and checks for conflicting settings.

See [Quick presets and additional XML](docs/AdvancedXml.md).

## V1.38 Import hardening and source verification · 2026-11-04

- Read external language packs and manifests through one size-limited file handle. Invalid Unicode and duplicate properties are rejected, preserving the active translations and language list.
- Bound the context-matching work when merging edits into scripts from imported XML. Export stops when the context is ambiguous or the work exceeds the limit.
- Remove generated WPF compilation projects from source control and check for them before verification builds.

## V1.37 PE paging-file settings · 2026-11-04

- Add an opt-in option under Image and partition to disable paging files in the installed Windows when using PE script installation.
- After image application, the option clears the target paging-file list. Custom sizes are kept as a draft and apply again when the option is cleared.
- The summary shows the effective mode. Changing it requires a fresh disk-erasure confirmation.

See [Deployment settings](docs/DeploymentTuning.md#v137-pe-无分页文件--no-paging-file-in-pe).

## V1.36 PE interaction summaries and pause notices · 2026-11-03

- Describe the actual PE disk selector, partition layout and image index instead of using the regular Windows Setup fixed-partition description.
- Stop incorrectly listing manual disk selection for fixed-number and unique-match selectors. Retain review notices for interactive selection, custom VBScript and unrecognized imported flows.
- Omit the Windows Setup product-key prompt from the PE interaction summary while retaining it for regular installation.
- List enabled pauses before automatic partitioning and before restarting at the end of the PE stage. Omit disabled pauses and those not used by the selected installation mode.
- Preserve disk operations, WinRE, image-index handling and existing safety checks. Reading a summary never grants disk-erasure consent.

See [PE script installation](docs/GeneratedPe.md).

## V1.35 PE firmware and partition-layout preflight · 2026-11-03

- Before disk selection or partitioning, reject fixed UEFI/GPT or BIOS/MBR layouts when the actual Windows PE boot firmware does not match, with an explicit error message.
- Keep the automatic layout option adaptive to the detected firmware without imposing fixed-layout restrictions.
- Preserve existing image-index, disk-selection, pre-format recheck, WinRE, and ordinary Windows Setup behavior.

See [PE script installation](docs/GeneratedPe.md).

## V1.34 PE failure handling and preflight checks · 2026-11-03

- Confirm that the selected installation image exists before DISM inspection; stop before disk discovery and partitioning when it is missing.
- Require an exact zero exit status after DiskPart; any other return stops deployment before applying the Windows image.
- Retain the V1.33 recovery-partition, WinRE, and prerequisite-aware search behavior without changing the selected disk mode.
- Keep the existing Windows Setup installation path unchanged and extend PE regression checks.

See [PE script installation](docs/GeneratedPe.md).



## V1.33 PE recovery partition option · 2026-11-02

- Add Create a recovery partition and retain Windows RE (PE, recommended) to automatic partitioning under Image & partitions. It appears when PE script installation is enabled and defaults to checked.
- When unchecked, no recovery space is reserved and no recovery partition is created. The applied image's winre.wim is removed, making Windows RE unavailable after installation. Prepare external installation or recovery media.
- Hide the inactive recovery-size editor while retaining its value. Partition summaries, warnings and the basic capacity floor follow the actual layout. Changing this option requires renewed disk-erasure confirmation.
- Older JSON and known XML without the new field retain the recovery partition. Support persistence, import and direct checkbox search navigation; unknown or modified PE XML remains protected.
- Regular Windows Setup retains its existing behavior. PE image verification, CompactOS, paging and disk rechecks remain in effect.
- Preserve dedicated search-result descriptions instead of replacing them with generic guidance.
- Fix recovery-option navigation when PE is enabled but automatic erasure/partitioning is not selected: locate the disk-mode selector instead of the reboot-pause checkbox. Locate the PE toggle when PE is off, and the actual editor when its prerequisites are met. A hidden recovery-size editor resolves to its recovery toggle. Navigation never changes configuration.
- Resolve recovery-related search destinations from configuration before checking visibility, without falling back to adjacent controls. Status messages identify the required PE switch, partitioning mode, or recovery switch. An unavailable explicit destination reports a navigation failure.

See [PE script installation](docs/GeneratedPe.md).



## V1.32 PE image integrity verification option · 2026-11-01

- Add Check installation image integrity (PE, recommended) to the PE installation area. It defaults to enabled, including for older plans.
- Enabled retains DISM `/CheckIntegrity /Verify` during image application. Disabled omits only those two options and displays guidance.
- Image-index, architecture, target-disk and pre-partition checks remain enabled. Regular installation is unaffected. Changes require renewed disk-erasure confirmation.
- Update deployment summaries, search navigation and JSON/XML persistence while retaining paging file and CompactOS settings.

Usage: [PE installation](docs/GeneratedPe.md). Consider disabling apply-stage checks only for a trusted image that you have independently verified.


## V1.31 Linked deployment settings and PE CompactOS · 2026-10-30

- Remove duplicate heading/editor search results. Individual settings navigate to their controls, while independent groups and summaries navigate to their full content areas; existing search keywords remain available.
- Show CompactOS status and Go to CompactOS settings in the PE area, linking to the shared selector on the same page without changing its selection.
- Show the current paging file status in the PE installation area, with Go to settings to open OOBE and focus/highlight the existing option without enabling it or changing sizes.
- Group the OOBE paging file option under System performance and storage. It configures virtual memory in the installed Windows system and is shared by regular and PE installation.
- Support the existing CompactOS option during PE image application. Enabled requests system-file compression; default and disabled omit the compression parameter and do not decompress files already compressed in the image.
- Allow CompactOS editing for imported PE XML that exactly matches this application's generated pipeline. External or modified scripts remain protected; regular installation keeps its existing behavior.

Usage: [Deployment settings](docs/DeploymentTuning.md) · [PE installation](docs/GeneratedPe.md).

## V1.30 PE paging file configuration · 2026-10-30

- Apply the existing custom C: paging file sizes through the PE generator after image application.
- Reuse existing controls, size validation and plan persistence. Regular installation retains System-phase configuration; PE installation omits the duplicate System paging script.
- Upgrade exact recognized older PE XML documents while preserving user scripts. Unknown or modified content remains protected.
- Clarify the scope difference: PE replaces the paging file list with the specified C: file rather than preserving other entries from the image.

Usage: [Deployment settings](docs/DeploymentTuning.md) · [PE installation](docs/GeneratedPe.md).

## V1.29 PE compatibility and configuration guidance · 2026-10-29

- Missing imported XML risk consent now offers Go to confirmation and scrolls to, focuses and highlights the exact checkbox without selecting it.
- Restore editing of disk selection, capacity limits and automatic partitioning when importing the recognized older PE XML format. Validate the complete old pipeline before unlocking, upgrade scripts on export and preserve protection for externally modified content.
- Require an exact zero exit status for PE disk selection and the immediate pre-partition recheck, including negative runtime errors. Stop with diagnostics on failure, clear stale target indices and reject empty results.
- Run custom disk-selection VBScript with the actual current script-host path instead of constructing it from SystemRoot. Keep interactive selection prompts visible.
- Left-align explanatory text, preservation notices and risk warnings in the imported XML panel for consistent reading and wrapping.
- Fix blank PE disk-capacity fields being treated as required. Leave either bound blank in VBScript, interactive and other selection modes; supplied bounds still enforce capacity conditions and ranges.
- Rename the key option to Enter manually in Windows Setup while preserving the persisted selection identity and ordinary Setup key behavior.
- Clarify that direct PE + DISM deployment does not use the Windows Setup key-entry step, including page guidance, tooltips and conflict messages.
- Deploy the explicitly selected image index and validate its index and architecture. Edition ID (SKU) is informational only: missing, duplicate or unrecognized edition metadata does not block installation. Do not match SKUs dynamically or change the selected index.
- Remove the findstr dependency from PE image validation, fixing false non-x64 errors in minimal PE environments. Parse English DISM image information with the existing cscript/VBScript dependency.
- Keep the x64 gate and distinguish DISM failures, unverifiable architecture and known incompatible architectures. Unverifiable images still stop installation.
- Avoid passing IME GUID profiles to the PE keyboard command. Retain the current PE layout for those profiles and report failures for basic layouts without changing the target Windows input settings.
- Clarify that PE installation selects the Windows edition by the actual image index, not by SKU or installation key.
- Explain the compatibility role of Enter a product key during setup and the key restrictions in the PE path. Preserve existing SKU, key and draft values when switching installation paths.
- Clarify that a separate activation key must match the target edition; it does not select an image index or change the applied edition.
- Navigate to and highlight missing disk-wipe, format and custom-partition-script risk confirmations. Users must still review and confirm them manually.
- Add setting destinations for image names and indices, target disks and partitions, partition layouts and sizes.
- Reuse the existing error-navigation and settings-search paths without relaxing validation or destructive-operation consent.
- Update Chinese and English page guidance, dynamic tooltips, export errors and regression coverage.

Usage: [PE installation](docs/GeneratedPe.md).


## V1.28 Deployment checks and configuration reliability

- Stop PE selection and pre-partition verification when mapped partition counts disagree with the disk report or a mapped volume has an invalid drive identifier.
- Omit inactive PE selector scripts from XML recovery metadata. Active scripts remain recoverable, and JSON plans retain editable drafts.
- Validate custom selector line limits consistently for CR, LF and CRLF line endings.
- Reject duplicate JSON members recursively and validate null string fields before compatibility migration.
- Reuse the validated JSON document to reduce repeated parsing during plan loading.
- Add partition validation, configuration recovery and Windows selector simulation cases.
- Fix untranslated account destinations and tooltips in English export validation dialogs. Use “Go to” for the navigation button, explain its purpose and destination in the tooltip, and prevent clipped button labels.
- Localize tooltips consistently for search, theme switching, color selection and configuration summaries.
- Fix untranslated disk summaries, preview notices and redaction placeholders in the English XML preview. Existing previews follow UI language changes while preserving user text, scripts and actual export content.

Usage: [PE installation](docs/GeneratedPe.md).

## V1.27 Disk deployment safety and boot compatibility

- Add fixed, unique-match, interactive and custom-VBScript target selectors to PE installation. Older plans retain fixed numbering and their installation path.
- Support byte-exact capacity bounds, optional matching index, unpartitioned disks, IDE/SCSI interfaces and fixed media. Zero or multiple matches stop rather than selecting the first disk.
- Record and recheck disk index, device identity, capacity and firmware immediately before the destructive partition call, after any optional pause. Query failures, changed targets and failed conditions stop that call.
- Exclude disks mapped to identified installation-image, answer-file and PE system volumes. Retain erase consent and clear it when relevant settings change.
- Support x64 PE with UEFI/GPT, BIOS/MBR or firmware-matched layout. Conflicting fixed layouts stop; firmware-boot-manager bcdedit remains GPT-only.
- Require one valid index from custom scripts within 60 seconds, with exit code 0 and empty stderr. Extra output and failures stop selection. These scripts are trusted executable code, not sandboxed.
- Include bilingual UI, search, error locations, condition summaries, JSON persistence and original-XML restoration, with short-fragment export for PE command-length limits.
- Include the six-case VM checklist and Windows-only selector runtime simulations.

Usage: [PE installation](docs/GeneratedPe.md) · [Deployment validation](docs/PeDeploymentValidation.md).

## V1.26 MSI installation and Windows Sandbox

- Add a generic local MSI template, running during System and added disabled.
- Configure a full local MSI path and trusted SHA256. Copy and verify the package before invoking the system Windows Installer, without downloads or package discovery.
- Request silent installation with no automatic restart and wait for completion. Exit code 3010 warns that a later restart is required; other nonzero codes fail without automatic retry.
- Initially support single-file MSI packages with embedded resources. External CAB files, MST transforms, MSP patches, UNC paths and extra installer arguments are outside scope.
- Add a Windows Sandbox template for first administrator logon. Enable the component and dependencies, skip an enabled feature and only warn if enablement is pending.
- Add the Sandbox template disabled, suppress automatic restart and prevent Windows Update source fallback. Unsupported features, missing sources and unexpected states produce errors.
- Both templates include bilingual descriptions, settings search, JSON persistence and editable restoration from app-exported XML. Existing saved script bodies remain unchanged.
- Retain the V1.25 PE installation mode and bilingual layout fixes.

Usage: [Practical script templates](docs/ScriptTemplates.md).

## V1.25 Basic PE installation pipeline

- Add an opt-in PE script installation mode, initially supporting x64, UEFI/GPT, wiping one specified target disk and image selection by index.
- Use consistent, function-focused wording for PE option names, help, configuration summaries and search terms in both languages.
- Remove first-line-only indentation from multiline descriptions on Image and partitions for consistent left alignment.
- Left-align option summaries and short prompts across the interface, retain consistent paragraph indentation for Chinese prose, and keep English text left-aligned.
- Group disk warnings by risk, preparation and responsibility, with consistent paragraph indentation, tighter spacing and matching English content.
- Stretch IPv4/IPv6 content to align with shared network notes; center only protocol headers and keep selected-tab styling confined to the header.
- Reuse the pinned generation core for disk selection, partitioning, image application, boot files and restart, retaining image integrity checks.
- Check x64 PE, UEFI boot, image-index readability and image architecture before partitioning; stop on a mismatch.
- Support pauses before partitioning and before the final PE restart, erase consent, JSON persistence, editable restoration from original XML, and bilingual search and help.
- Keep the ordinary Setup path as the default. Reject unsupported combinations with Setup driver directories, Dynamic Update, a PE log directory, Compact OS, hardware-check bypass and extra windowsPE XML.
- Retain the confirmed third-party-notices baseline, with Project labels and complete copyright and licence texts.

Usage: [PE script installation](docs/GeneratedPe.md).

## V1.24 MAC-based naming, temporary update pause and security fix

- Add MAC-based computer naming using one physical Ethernet adapter, with explicit errors for missing or ambiguous candidates.
- Add an optional one-hour setup update-pause template that retains later existing expiry times, without disabling services or creating recurring tasks. Added disabled.
- Fix external language-pack placeholder backtracking using bounded non-backtracking matching and safe timeout rejection.
- Keep the project GitHub hyperlink on the About page, opening in the default browser. Retain generator attribution and keep upstream URLs and full licence texts in the licensing section and source documentation.
- Remove the shared bottom Close and Copy page buttons from About, Legal information and usage, and Open-source licences; retain the title-bar Close button.
- Improve paragraph spacing and sentence grouping in About and legal information, add bilingual component notices, and make URLs in component and licence documents clickable.
- Refresh English light/dark screenshots using the supplied V1.23 captures, labeled with their actual version.
- Remove the duplicate download link at the top. Keep the Release link in Download and usage, with all history preserved in the collapsed section.

Usage: [Computer naming](docs/ComputerNameAndLanguages.md), [Script templates](docs/ScriptTemplates.md).

## V1.23 Generic application installation and advanced diagnostics

- Place script actions directly above the script list, after the explanatory text, to reduce scrolling and searching.
- Size the navigation column to the longest label in the current language, keeping labels on one line and reserving selected-weight space to avoid width changes when switching pages.
- Organize installation by method: generic WinGet application installation and generic local EXE silent installation, rather than one template per application.
- Configure a package ID for WinGet, or an installer path, SHA256 and vendor arguments for EXE. Chrome and 7-Zip appear only as documentation examples.
- Retain optional advanced firewall dropped-packet logging, without changing filtering rules.
- Templates are added disabled. Existing script bodies in saved plans and XML remain unchanged when templates are reorganized.
- Retain template-name display and current-script clipboard fixes. Show the latest updates by default and preserve all history in a collapsed section.

Usage: [Practical script templates](docs/ScriptTemplates.md).

## V1.22 System volume label and Ultimate Performance templates

- Fix the custom script template dropdown displaying complete template objects and script bodies. Show only template names, wrap long labels and keep the popup aligned with the selector width.
- Fix the script editor copy button copying placeholder instructions. Copy the complete current script text, disable copying for empty content and retain clipboard feedback.
- Add a system-volume label template with automatic drive detection and NTFS label validation, without formatting or repartitioning.
- Add an Ultimate Performance template that creates and activates its managed plan, reuses it on repeat runs and reports unsupported configurations.
- Both templates are added disabled and support editing, naming, plan persistence and restoration from this app's original XML exports, with Chinese and English help.
- Check attachments when a Release is published. Skip automatic compilation if any EXE is already attached; otherwise build and attach both executables. Ordinary commits and standalone tag creation do not build EXEs.
- Recheck before upload, preserve existing assets and Release text, and keep manual builds as Actions-only outputs.

## V1.21 Deployment settings and language-menu fix

- Prevent transient double check marks when switching interface languages.
- Add an optional policy to disable creation of new NTFS 8.3 names without removing existing short names.
- Add optional initial/maximum sizes for the C: page file, applied during system setup while preserving page files on other drives. Existing settings remain unchanged by default.
- Include bilingual help, settings search, configuration summaries, JSON persistence and editable restoration from XML exported by this application.
- Build both EXEs automatically only for version tag pushes (for example v1.21), with manual builds also available. Ordinary commits do not trigger EXE builds. Releases are published manually.

Usage: [Deployment settings](docs/DeploymentTuning.md#english).

## V1.20 Chinese and English interface

- Switch instantly between Simplified Chinese and English, with the choice remembered separately from the target Windows installation language.
- Localize titles, options, help, summaries, diagnostics and confirmation dialogs through independent JSON resources, with wrapped and paragraphed explanations.
- Update translations in the adjacent `Languages` folder. Embedded resources provide a fallback; invalid packs are rejected without replacing valid translations.
- Add stable resource keys, translation-state checks and provider-neutral job export/import for incremental maintenance and future automated translation.
- Preserve existing plan values, original IDs, scripts, user input and generated XML. Program archives include editable language files.

Usage and maintenance: [Internationalization and language packs](docs/Localization.md).

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
