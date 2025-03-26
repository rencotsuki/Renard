/// <summary>※Renard拡張機能</summary>
public class Invalid
{
    public static readonly char[] Chars =
        {
            '\'', '\\', '\"', '`', ':', ';', ',', '.',
            '#', '$', '@', '%', '&', '?', '!',
            '|', '+', '-', '=', '/', '^', '~', '*',
            '(', ')', '[', ']', '{', '}' , '>', '<'
        };

    public static bool Url(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }
        return System.Text.RegularExpressions.Regex.IsMatch(
           url,
           @"^s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+$"
        );
    }
}
