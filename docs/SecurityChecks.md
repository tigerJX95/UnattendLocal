# 拉取请求安全检查 / Pull request security checks

[简体中文](#中文) | [English](#english)

## 中文

### 检查范围

- **Code quality**：发布脚本预检、编译、回归及启动版本显示检查。
- **Security → CodeQL (csharp)**：在 GitHub 托管的临时 Windows 环境中跟踪构建 C# / WPF 项目，运行 `security-extended` 安全查询，包含本地适配代码和 vendor 核心。
- **Security → CodeQL (actions)**：扫描 GitHub Actions 工作流中的危险用法。
- **Security → Dependency review**：对 PR 新增的依赖变化检查已知漏洞，低危及以上即失败。不执行 PR 的构建或测试，不自动发表评论。
- **Security → NuGet security audit**：提交、PR 和手动触发时检查全部直接与间接 NuGet 依赖。漏洞警告和审计源错误视为失败；手动打包也先执行这一检查。

第三方 Actions 固定到已核对的完整提交 SHA，更新时需审查新版本后调整。普通检查和打包只有仓库读取权限；CodeQL 作业额外获得上传安全报告所需的 `security-events: write` 与 `actions: read`，没有 Release 发布权限。PR 使用 `pull_request`，不使用 `pull_request_target` 执行外部分支代码，也不使用自托管运行器或共享构建缓存。

这些检查不能证明代码安全。CodeQL 不能替代对 PowerShell、CMD、安装脚本、MSBuild 目标及依赖来源的人工审核；已知漏洞库也无法识别全部恶意依赖。审查 PR 时应核对命令执行、下载后执行、删除与覆盖路径、凭据读取、联网目标，以及工作流权限的变化。

### 仓库保护配置

以下设置由仓库所有者管理，与工作流文件分别配置。

1. 自定义工作流应位于默认分支。在仓库 **Settings → Code security / Advanced Security** 中启用 Dependency graph。CodeQL 使用仓库中的自定义工作流（Advanced setup）；如已启用 CodeQL Default setup，先关闭 Default setup，避免与自定义分析冲突。查看 **Security → Code scanning** 确认 C# 和 Actions 两项分析都产生结果。
2. 在 **Settings → Actions → General** 中，将外部协作者的工作流审批设为 **Require approval for all outside collaborators**。保持默认 token 只读，不给来自 fork 的 PR 发送写 token 或 secrets。批准运行前先看工作流、`.csproj`、构建及测试代码的变化；编译和测试本身会执行 PR 代码。
3. 在默认分支的 Ruleset 或 Branch protection 中启用“必须通过 PR 合并”、至少一名审核者、**Require review from Code Owners**，并在新提交后撤销旧批准。`.github/CODEOWNERS` 将全部文件交给 `@tigerJX95` 审核，包括工作流、构建脚本及 CODEOWNERS 自身。
4. 工作流至少运行一次后，将这些状态设为必需：`Build and regression checks`、`Dependency review`、`NuGet security audit`、`CodeQL (csharp)`、`CodeQL (actions)`。以仓库界面实际列出的检查名称为准；Dependency review 只在 PR 上运行。
5. 同时添加 **Require code scanning results** 规则，工具选 **CodeQL**，安全告警门槛选 **Medium or higher**。CodeQL 作业成功表示分析完成，不表示没有漏洞；不能仅以两个 CodeQL 作业的绿色状态作为安全准入依据。若使用经典分支保护，请按 GitHub 的 code scanning merge protection 设置告警失败门槛，并将对应的 code scanning 结果检查设为必需。

外部 PR 需由代码所有者审核后合并。PR 作者不能批准自己的 PR。个人仓库如需直接维护默认分支，可仅为仓库所有者设置绕过权限；其他贡献者仍需经过 PR 和必需检查。绕过权限会跳过对应保护。

运行前还应确认仓库的 Actions 策略允许仓库中固定 SHA 的 GitHub 官方 Actions。CodeQL、Dependency review 及合并保护的可用性取决于仓库可见性和 GitHub 套餐；这些配置适用于公开的 UnattendLocal 仓库。功能不可用或报告上传失败时应排查设置，不应把失败改成忽略后继续合并。

## English

**Code quality** checks publishing preflight, builds and runs regressions. **Security** adds CodeQL `security-extended` queries for both C# (a traced Windows / WPF build, including vendor code) and GitHub Actions, PR dependency review failing on newly introduced known vulnerabilities of low severity or above, and NuGet auditing of direct and transitive packages. Audit warnings and source errors fail the audit. Manual packaging also runs the NuGet audit first.

Actions are pinned to reviewed commit SHAs. Ordinary jobs have read-only repository access. Only CodeQL jobs additionally request `security-events: write` and `actions: read` to upload analysis reports. There are no Release publishing permissions, PR secrets, self-hosted runners, shared build caches, or `pull_request_target` builds in these workflows. Static analysis does not establish that code is safe, particularly PowerShell / CMD scripts, MSBuild targets, tests, and dependency sources.

Repository protection is configured separately from the workflow files:

1. Enable the dependency graph. Use CodeQL **Advanced setup**, disabling Default setup if already enabled. Confirm that both C# and Actions reports appear in **Security → Code scanning**.
2. Require workflow approval for **all outside collaborators**, keep default token permissions read-only, and do not send write tokens or secrets to fork PRs. Inspect build scripts, workflows, project files and tests before approving execution: building and testing executes contributor code.
3. Protect the default branch with required PRs, at least one approval, required Code Owner review, and dismissal of stale approvals. `.github/CODEOWNERS` assigns all files to `@tigerJX95`, including itself.
4. After the first run, require `Build and regression checks`, `Dependency review`, `NuGet security audit`, `CodeQL (csharp)`, and `CodeQL (actions)`. Confirm the actual names shown by GitHub. Dependency review runs only on PRs.
5. Also enable **Require code scanning results**, selecting **CodeQL** and blocking security alerts of **Medium or higher**. A successful CodeQL workflow job means analysis completed, not that it found no vulnerabilities. For classic branch protection, configure the code scanning merge-protection failure threshold and require its corresponding result check.

PR authors cannot approve their own PRs. If needed for a solo repository, grant an explicit bypass only to the repository owner for reviewed maintenance; it bypasses the corresponding protections. Ensure repository policy permits the pinned official Actions. Feature availability depends on repository visibility and GitHub plan; these files target the public UnattendLocal repository. Do not turn unavailable or failed security checks into ignored successes.

## 官方依据 / Official references

- [CodeQL 编译语言构建方式 / Compiled-language build options](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/manage-your-configuration/codeql-for-compiled-languages)
- [CodeQL Actions 查询 / GitHub Actions queries](https://docs.github.com/en/code-security/reference/code-scanning/codeql/codeql-queries/actions-built-in-queries)
- [依赖评审 / Dependency review action](https://github.com/actions/dependency-review-action)
- [NuGet 依赖漏洞审计 / NuGet auditing](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
- [CODEOWNERS](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [安全扫描合并保护 / Code scanning merge protection](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/manage-your-configuration/set-merge-protection)
- [Actions 仓库设置 / Repository Actions settings](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-github-actions-settings-for-a-repository)
