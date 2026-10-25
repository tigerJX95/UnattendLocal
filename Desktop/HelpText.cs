using System.Text;

namespace UnattendLocal.Desktop;

// Only format application-authored help prose. Never pass scripts or user-entered values here.
public static class HelpText
{
    public static string RemovalDescription(string description)
    {
        int split = description.IndexOf(global::UnattendLocal.Localization.UiText.Source("text.447f01fe8a3a"), StringComparison.Ordinal);
        if (split < 0) return Paragraphs(description);
        var identifiers = description[(split + 4)..].Trim().TrimEnd('。')
            .Split('；', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return global::UnattendLocal.Localization.UiText.Alias(description, global::UnattendLocal.Localization.UiText.Concat(new object?[] { Paragraphs(description[..split].TrimEnd()), global::UnattendLocal.Localization.UiText.Source("text.eeb748f9241b"), global::UnattendLocal.Localization.UiText.Join("\n", identifiers, false) },new bool[]{false,false,false}));
    }

    public static string Paragraphs(string text)
    {
        var result = new StringBuilder(text.Length + 32);
        var closing = new Stack<char>();
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            char end = c switch { '（' => '）', '(' => ')', '【' => '】', '[' => ']', '“' => '”', '「' => '」', _ => '\0' };
            if (end != '\0') closing.Push(end);
            else if (closing.TryPeek(out char expected) && c == expected) closing.Pop();
            result.Append(c);
            if (closing.Count > 0 || c is not ('。' or '；')) continue;
            int next = i + 1;
            while (next < text.Length && text[next] is ' ' or '\t') next++;
            if (next < text.Length && text[next] is not ('\r' or '\n')) result.Append('\n');
            i = next - 1;
        }
        return UnattendLocal.Localization.UiText.Alias(text, result.ToString());
    }
}
