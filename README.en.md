<div align="center">

[简体中文](./README.md) | **English**

</div>

# UnattendLocal · Windows Unattended Setup Configuration Tool

UnattendLocal is a Chinese-language desktop configuration tool for Windows 10/11. Built on the generation core of Schneegans' UnattendGenerator, it provides a graphical interface for creating, reviewing, and exporting Windows unattended setup answer files (`autounattend.xml`).

## Features

- Configure language, region, user accounts, automatic sign-in, networking, and Out-of-Box Experience (OOBE) setup options.
- Configure installation images, target partitions, and CompactOS system-file compression.
- Customize the taskbar, Start menu, File Explorer, desktop appearance, and common Windows settings.
- Support custom setup scripts, saving and restoring configuration profiles in JSON format, configuration summaries, and XML previews.
- Import existing unattended answer files and modify supported settings. Notify users about unsupported settings and preserve them where possible; block export when conflicts cannot be merged reliably.

**Current development version: V0.1 pre40.** This project is under active development and testing. Localization and support for upstream features are being expanded progressively. Some settings still require validation in the target Windows environment.

## Credits and Acknowledgments

Special thanks to **Schneegans (GitHub: cschneegans)** for developing and open-sourcing UnattendGenerator, which provides the Windows answer-file generation core and serves as a functional reference for this project.

- [Original GitHub repository](https://github.com/cschneegans/unattend-generator)
- [Original online answer-file generator](https://schneegans.de/windows/unattend-generator/)
- [Original project's MIT license](https://github.com/cschneegans/unattend-generator/blob/master/LICENSE.txt)

UnattendLocal is an independently maintained Chinese-language desktop localization tool and is not an official release by the original author. Copyright notices and licenses for reused upstream code are retained.
