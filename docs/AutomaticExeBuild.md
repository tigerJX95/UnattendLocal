# 自动构建与正式发布（V1.20）

- GitHub Desktop 的 **Commit to main** 只是本地提交。执行 **Push origin** 后，推送到 `main` 才会触发 `.github/workflows/build.yml` 自动构建。
- 工作流在 Windows 运行 `verify.ps1`、依赖审计和 `publish.ps1`；成功后向 Actions Artifacts 上传 **两个独立 EXE**，上传时设置 `archive: false`，不创建 ZIP。Artifacts 保留 7 天。
- `workflow_dispatch` 仍可手动执行构建；不自动创建 Tag 或 Release。
- 只有手动创建并推送符合项目版本号的 Tag（如 `v1.20`）才运行同一构建验证流程，并在成功后上传两个 EXE 到 GitHub Release。Tag 必须与 `Desktop/Desktop.csproj` 的 `InformationalVersion` 完全一致且指向 `main` 已有提交。
- 不覆盖已经存在的同名 Release。项目版本没变时可多次日常构建，但不能把旧 Tag 的 Release 当作新版本覆盖。
- `build.cmd` 也已改为只生成两个 EXE；源码打包仅供开发分发，不是自动构建产物。

## GitHub Desktop 工作流程

1. 修改并 `Commit to main`。
2. 点击 `Push origin`。
3. 打开仓库 Actions → `Build two EXEs and publish tagged releases` → 对应运行 → Artifacts，可直接下载两个 `.exe`。
4. 完成 Windows 实机验收并准备正式发布时，手动创建 `v1.20` Tag 并推送；GitHub Actions 校验成功后自动生成 Release。

## 注意

- `archive: false` 依赖 GitHub Actions `actions/upload-artifact@v7`、`actions/download-artifact@v8` 的非 ZIP Artifact 支持。
- 发布任务通过仓库默认 `GITHUB_TOKEN` 写入 Release；无需自定义 Secrets。若仓库策略禁止创建 Release，需检查 Actions 权限和 Tag/Ruleset 策略。
- 国际化 PR 必需检查继续由 `.github/workflows/i18n-check.yml` 负责，不受本次构建调整影响。
