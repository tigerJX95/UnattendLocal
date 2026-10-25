namespace UnattendLocal.Desktop;

// Match application-produced diagnostic states, never generic words in user content.
public static class DiagnosticText
{
    public static bool IsErrorSummary(string? text)
    {
        string value = (text ?? "").TrimStart();
        return new[] {
            global::UnattendLocal.Localization.UiText.Source("text.f3b91a2a704a"), global::UnattendLocal.Localization.UiText.Source("text.bff1c2725d0e"), global::UnattendLocal.Localization.UiText.Source("text.df3172bebdcb"),
            global::UnattendLocal.Localization.UiText.Source("text.7db8f86a9f4b"), global::UnattendLocal.Localization.UiText.Source("text.6ef5844b40db"), global::UnattendLocal.Localization.UiText.Source("text.ba9df495f9f6"),
            global::UnattendLocal.Localization.UiText.Source("text.a3249b4df773"), global::UnattendLocal.Localization.UiText.Source("text.f70ae0f7b32a"),
            global::UnattendLocal.Localization.UiText.Source("text.d854f9e7c35d"), global::UnattendLocal.Localization.UiText.Source("text.eb5412c216e0")
        }.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
    }
}
