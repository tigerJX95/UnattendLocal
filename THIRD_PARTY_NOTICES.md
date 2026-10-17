# 第三方引用与鸣谢

本项目本地界面与适配代码采用 [MIT License](LICENSE)，Copyright (c) 2026 tigerJX95。以下第三方组件的原版权、许可与声明独立保留。

The local UI and adaptation code use the [MIT License](LICENSE), Copyright (c) 2026 tigerJX95. Original third-party copyrights, licenses and notices are retained separately.

## UnattendGenerator

作者：Christoph Schneegans。

源码：https://github.com/cschneegans/unattend-generator

原作者网站：https://schneegans.de/windows/unattend-generator/

本地界面使用其生成核心并保留原许可证。固定基线与选择性同步范围见 UPSTREAM.md。

MIT License

Copyright (c) 2024-2025 Christoph Schneegans

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


## Newtonsoft.Json

作者：James Newton-King 与贡献者。

源码与许可证：https://github.com/JamesNK/Newtonsoft.Json

通过上游项目的标准 NuGet 引用使用 Newtonsoft.Json 13.0.4；该依赖的许可由其项目声明。

## .NET 与 WPF

Microsoft 与项目贡献者提供运行时及桌面框架。

源码与许可证：https://github.com/dotnet/runtime · https://github.com/dotnet/wpf

第三方依赖采用各自的许可声明；本项目的 MIT 全文另见根目录 LICENSE。

## 完整离线许可 / Full offline licenses

程序“帮助与关于 → 开源许可”以及源码 `docs/licenses/` 保留完整原文：

| 组件 / Component | 版本 / Version | 文件 / File | 来源 / Source |
| --- | --- | --- | --- |
| UnattendGenerator | 固定基线，见 UPSTREAM.md / fixed baseline | UnattendGenerator.txt | vendor/UnattendGenerator/LICENSE.txt |
| Newtonsoft.Json | 13.0.4 | Newtonsoft.Json.txt | NuGet 包 / package LICENSE.md |
| .NET Runtime | 10.0.13 | DotNetRuntime.txt、DotNetThirdParty.txt | Microsoft.NETCore.App.Runtime.win-x64 NuGet 包 / package |
| Windows Desktop / WPF | 10.0.13 | WindowsDesktop.txt | Microsoft.WindowsDesktop.App.Runtime.win-x64 NuGet 包 / package LICENSE |
| WPF 第三方声明 / third-party notices | v10.0.13 | WpfThirdParty.txt | https://github.com/dotnet/wpf/blob/v10.0.13/THIRD-PARTY-NOTICES.TXT |

许可文本逐字保留，本项目许可全文与第三方许可分别展示。自包含版集成运行时；依赖运行时版需另行安装。更新依赖或运行时版本时，应同步核对这些文件。

License texts are preserved verbatim. The project MIT license and third-party licenses are shown separately. The self-contained edition bundles the runtime; the framework-dependent edition requires a separate installation. Review these files when updating dependencies or runtime versions.
