# Release 附件检查与自动构建 / Release assets and automatic builds

[简体中文](#简体中文) | [English](#english)

## 简体中文

从 V1.22 起，`.github/workflows/build.yml` 只监听 Release 的 `published` 事件及手动运行，不再监听分支提交或标签推送。普通提交、仅创建标签、保存 Release 草稿均不自动构建 EXE。

发布时先运行只读附件检查。只要已有一个上传完成且非空的 `.exe` 附件（名称大小写不限，自定义文件名也有效），即认为用户选择手动提供程序，跳过整个 Windows 编译、打包和自动上传。即使只上传了一种版本，也不擅自补齐另一种。文件名判断不能验证 EXE 的功能或版本，请自行核对手动附件。

没有 EXE 时，工作流按该 Release 的标签源码构建两种程序，检查版本、main 历史、依赖及回归测试，生成 Actions 产物，并附加到同一条已发布的 Release。源码 ZIP、说明文档等非 EXE 附件不阻止构建。标签须为与项目版本一致的 `v1.22` 形式。

上传前重新查询附件；若用户在编译期间补传了 EXE，则停止自动上传。上传不覆盖或删除任何已有文件，不修改 Release 标题、更新说明或标签。已上传成功但随后失败的文件保留；若附件仅部分上传，检查并保留已有文件后，可手动补传缺少的程序。API 请求失败、未完成/空 EXE、标签身份不匹配或不可追加附件的不可变 Release 均明确失败，不把“查询失败”当成“没有附件”。

`workflow_dispatch` 手动运行只构建并保留 Actions 产物，不附加到 Release。自动运行的检查任务仅需读取权限；需要生成附件的 Windows 构建任务使用 `contents: write`，上传步骤通过仓库令牌写入附件，无需另设个人令牌。仓库策略仍须允许该权限。

### 测试方式

1. **已有 EXE**：新建版本 Release 草稿，上传真实程序，确认上传完成后点击发布。Actions 应只显示附件检查完成，Windows 构建为跳过，已有附件不变。
2. **没有 EXE**：新建与项目版本匹配的标签及 Release，不上传 EXE，点击发布。Actions 应构建并自动添加自包含版和依赖框架版两种 EXE。
3. **普通提交和独立标签**：仅提交修改或创建标签，不发布 Release，不应启动本工作流。
4. **手动构建**：通过 Actions 手动运行，应只出现可下载的构建产物，不修改已有 Release。

同一项目版本没有变更时，不要为了重复测试移动已发布标签。测试账号/仓库可独立执行两条路径；正式仓库也可先用同版本的手动附件验证跳过，再在后续新版本验证无附件路径。切勿把占位文件作为对外发布的 EXE。

GitHub 启用不可变 Release 后，已发布版本不能追加附件；这种仓库需改用先构建草稿附件、再发布的流程，本工作流会停止并提示。

## English

V1.22 listens only to `release: published` and `workflow_dispatch`. Ordinary commits, standalone tag pushes and saved Release drafts do not build EXEs.

A lightweight read-only job queries every asset page. Any completely uploaded, non-empty `.exe` attachment, including a custom filename or one variant alone, skips the entire Windows build and automatic upload. File names do not establish executable correctness or version; verify manual attachments yourself.

With no EXE attachments, build both variants from the release tag, validate the tag/version and main history, audit dependencies, run regression checks, retain Actions outputs and attach the EXEs to the same Release. Non-EXE assets do not prevent a build. The tag must match the project version, for example `v1.22`.

Recheck before writing to preserve EXEs uploaded during compilation. Never overwrite/delete assets or edit Release titles, notes or tags. API failures, incomplete EXEs, identity mismatches and immutable Releases stop rather than proceed. If a later upload fails, keep earlier successful uploads and complete the missing assets manually. Manual Actions builds do not modify Releases.

Test both branches using a test repository or successive matching project versions: publish with a real EXE to verify the Windows build is skipped; publish without EXEs to verify both attachments appear. Branch/tag pushes alone must not run this workflow. Do not move published version tags to repeat a test.

An immutable published Release cannot accept further attachments and requires a separate draft-first process.

## 官方依据 / Official references

- [Release workflow events](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#release)
- [Release assets API](https://docs.github.com/en/rest/releases/assets)
- [Managing Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
