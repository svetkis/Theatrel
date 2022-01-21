namespace theatrel.Lib.Utils;

internal class Helper
{
    public static int ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        return int.TryParse(value, out var ret) ? ret : default;
    }
}