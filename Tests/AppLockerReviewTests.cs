using UnattendLocal.Core;
using UnattendLocal.Localization;

static class AppLockerReviewTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception("AppLocker review: " + message);
            count++;
        }

        var example = AppControlSettings.Example;
        var audit = AppControlSettings.ReviewPolicy(example);
        Check(!audit.HasWarnings && audit.Message.Contains("审核") && audit.Message.Contains("允许 1 条"),
            "audit-only mode and direct rule counts");

        var enabled = example.Replace("AuditOnly", "Enabled");
        var explicitReview = AppControlSettings.ReviewPolicy(enabled);
        Check(explicitReview.HasWarnings && explicitReview.Message.Contains("可能拦截") &&
            !explicitReview.Message.Contains("未配置模式的集合"), "explicit enforcement warns");
        foreach (var implicitMode in new[] { example.Replace("AuditOnly", "NotConfigured"),
            example.Replace(" EnforcementMode=\"AuditOnly\"", "") })
        {
            var review = AppControlSettings.ReviewPolicy(implicitMode);
            Check(review.HasWarnings && review.Message.Contains("未配置模式的集合"), "unconfigured collection with rules may enforce");
        }

        var deny = AppControlSettings.ReviewPolicy(enabled.Replace("Action=\"Allow\"", "Action=\"Deny\""));
        Check(deny.HasWarnings && deny.Message.Contains("拒绝 1 条") && deny.Message.Contains("只有拒绝规则"), "deny-only enforcement warns");
        var auditDeny = AppControlSettings.ReviewPolicy(example.Replace("Action=\"Allow\"", "Action=\"Deny\""));
        Check(!auditDeny.HasWarnings, "audit-only deny rules do not receive enforcement warnings");

        var empty = AppControlSettings.ReviewPolicy("<AppLockerPolicy Version='1'><RuleCollection Type='Exe' EnforcementMode='Enabled'/></AppLockerPolicy>");
        Check(empty.HasWarnings && empty.Message.Contains("没有文件规则") && !empty.Message.Contains("可能拦截"),
            "empty collection has no file rules to enforce");
        var unknown = AppControlSettings.ReviewPolicy(example.Replace("Type=\"Exe\"", "Type=\"PrivateCollectionToken\""));
        Check(unknown.HasWarnings && unknown.Message.Contains("未识别的规则集合") && !unknown.Message.Contains("PrivateCollectionToken"),
            "unknown types produce a bounded notice without echoing input");
        Check(!explicitReview.Message.Contains("%WINDIR%") && !explicitReview.Message.Contains("S-1-1-0") &&
            !explicitReview.Message.Contains("d3b66501"), "review does not echo paths, SIDs or rule IDs");
        string english = UiText.EnglishText(deny.Message);
        Check(english.Contains("1 deny") && english.Contains("only deny rules") && !english.Contains("强制"), "English review is fully localized");
        return count;
    }
}
