namespace UnattendLocal.Core;

// Carries an explicit setting identity; UI navigation never guesses from exception text.
public sealed class ConfigurationValidationException(string field, string message, Exception? inner = null, int? userIndex = null)
    : ArgumentException(message, inner)
{
    public string Field { get; } = field;
    public int? UserIndex { get; } = userIndex;

    public static void Check(string field, Action check) => Read(field, () => { check(); return true; });

    public static T Read<T>(string field, Func<T> read)
    {
        try { return read(); }
        catch (ConfigurationValidationException) { throw; }
        catch (ArgumentException error) { throw new ConfigurationValidationException(field, error.Message, error); }
    }
}
