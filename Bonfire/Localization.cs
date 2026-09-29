using System.Collections.Generic;
using System.IO;
using System.Text;

using BepInEx;

using BonfireSilksong;

/// <summary>
/// Parser for text localization file.
/// Uses a custom .loc format which is parsed as key-value pairs:
/// - Keys have no indendation and must end in ':'.
/// - Values require tab/4 spaces indentation. Value ends only when the next non-indendent line ending in ':' is 
/// detected (a new key) or EOF is reached.
/// - End of line is automatically a newline. This of course can create very long horizontal lines of text. This is
/// not an issue for GUI elements because they can wrap it around if necessary, but makes editing the .loc file hard.
/// Best solution for this is to do some manual testing and break long string to multiple rows then check how they look
/// in game.
/// - Comments are supported and are detected via '#' symbol: similar to keys, they must have no indentation.
/// 
/// --- Example ---
/// 
/// # this is a comment
/// 
/// key1:
///     value1
/// key2:
///     value2.a
/// 
///     value2.b
/// 
/// key3:
///     value3.a
///     value3.b
/// # another comment ###
/// key4:
///     value4
/// ### comment before file ends
/// </summary>
public class Localization
{
    public static Localization Loc;

    /// <summary>
    /// Attemps to retrieve data string for given key. If not found, prints errors and return string with input key.
    /// </summary>
    public string Get(string key)
    {
        if (strings.TryGetValue(key, out string value))
            return value;

        Bonfire.Log.LogError($"[Localization] missing key '{key}'.");

        return $"[{key}]";
    }

    /// <summary>
    /// Attemps to retrieve data string for given key and embeds custom data values if string supports them. Value
    /// name/identifier in string must be in wrapped in brackets {} e.g. "the number is {number}" and the value is
    /// injected with (name, value) syntax. 
    /// Example: if key "myValue" points to string value "the number is {number}}" then 
    ///   Get("myValue", ("number", 10)) -> returns the string "the number is 10". 
    /// </summary>
    public string Get(string key, params (string name, object value)[] values)
    {
        string text = Get(key);

        foreach (var value in values)
            text = text.Replace("{" + value.name + "}", value.value?.ToString() ?? "");

        return text;
    }

    /// <summary>
    /// Initializes the data dictionary using a .loc file.
    /// </summary>
    public void Load(string path)
    {
        if (!path.EndsWith(".loc"))
        {
            Bonfire.Log.LogError($"[Localization] {path} needs to be in .loc format");
            return;
        }

        strings.Clear();

        string fullPath = Path.Combine(Paths.PluginPath, path);
        string currentKey = null;
        StringBuilder currentValue = new StringBuilder();
        int lineNumber = 0;

        foreach (string rawLine in File.ReadLines(fullPath, Encoding.UTF8))
        {
            lineNumber++;

            string line = rawLine.TrimEnd();

            // comments outside text blocks
            if (rawLine.Length > 0 && rawLine[0] == '#')
                continue;

            // empty line
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentKey != null)
                    currentValue.AppendLine();

                continue;
            }

            // a non-indented line ending in ':' is treated as a new key
            bool isKey = !char.IsWhiteSpace(rawLine[0]) && line.EndsWith(":");

            if (isKey)
            {
                // save the previous entry
                if (currentKey != null)
                    SaveEntry(currentKey, currentValue, lineNumber);

                currentKey = line.Substring(0, line.Length - 1);
                currentValue.Clear();

                if (string.IsNullOrWhiteSpace(currentKey))
                {
                    currentKey = null;
                    Bonfire.Log.LogError($"[Localization] empty key at line {lineNumber} in '{path}'");
                }

                continue;
            }

            // text appeared without a key
            if (currentKey == null)
            {
                Bonfire.Log.LogError($"[Localization] text found before a key at line {lineNumber} in '{path}'");

                continue;
            }

            // remove indentation from the text
            string content = RemoveIndent(rawLine);

            currentValue.AppendLine(content);
        }

        // save the last entry
        if (currentKey != null)
            SaveEntry(currentKey, currentValue, lineNumber);
    }


    private readonly Dictionary<string, string> strings = new Dictionary<string, string>();

    private void SaveEntry(string key, StringBuilder value, int lineNumber)
    {
        if (strings.ContainsKey(key))
        {
            Bonfire.Log.LogError($"Localization: duplicate key '{key}' near line {lineNumber}");

            return;
        }

        strings[key] = value.ToString().TrimEnd();
    }

    private string RemoveIndent(string line)
    {
        int count = 0;

        // uses 4 space intendation length to separate string value after key.
        while (count < line.Length && count < 4 && line[count] == ' ')
            count++;

        return line.Substring(count);
    }
}