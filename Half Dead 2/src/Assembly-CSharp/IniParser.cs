using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class IniParser
{
    private Dictionary<string, Dictionary<string, string>> sections;

    public IniParser()
    {
        sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    }

    public void Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("INI file not found", filePath);

        string currentSection = null;
        string[] lines = File.ReadAllLines(filePath);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            // Section header
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                if (!sections.ContainsKey(currentSection))
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            // Key-value pair
            else if (currentSection != null)
            {
                int equalsIndex = line.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    sections[currentSection][key] = value;
                }
            }
        }
    }

    public string GetValue(string section, string key, string defaultValue = "")
    {
        if (sections.TryGetValue(section, out Dictionary<string, string> keyValuePairs))
        {
            if (keyValuePairs.TryGetValue(key, out string value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    public int GetIntValue(string section, string key, int defaultValue = 0)
    {
        string strValue = GetValue(section, key);
        if (int.TryParse(strValue, out int result))
            return result;

        return defaultValue;
    }

    public bool GetBoolValue(string section, string key, bool defaultValue = false)
    {
        string strValue = GetValue(section, key).ToLower();
        if (strValue == "true" || strValue == "1" || strValue == "yes" || strValue == "on")
            return true;
        if (strValue == "false" || strValue == "0" || strValue == "no" || strValue == "off")
            return false;

        return defaultValue;
    }

    public float GetFloatValue(string section, string key, float defaultValue = 0.0f)
    {
        string strValue = GetValue(section, key);
        if (float.TryParse(strValue, out float result))
            return result;

        return defaultValue;
    }

    public void SetValue(string section, string key, string value)
    {
        if (!sections.ContainsKey(section))
            sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        sections[section][key] = value;
    }

    public void Save(string filePath)
    {
        StringBuilder sb = new StringBuilder();

        foreach (KeyValuePair<string, Dictionary<string, string>> section in sections)
        {
            sb.AppendLine($"[{section.Key}]");

            foreach (KeyValuePair<string, string> keyValue in section.Value)
            {
                sb.AppendLine($"{keyValue.Key} = {keyValue.Value}");
            }

            sb.AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public bool HasSection(string section)
    {
        return sections.ContainsKey(section);
    }

    public bool HasKey(string section, string key)
    {
        if (sections.TryGetValue(section, out Dictionary<string, string> keyValuePairs))
        {
            return keyValuePairs.ContainsKey(key);
        }

        return false;
    }

    public IEnumerable<string> GetSections()
    {
        return sections.Keys;
    }

    public IEnumerable<string> GetKeys(string section)
    {
        if (sections.TryGetValue(section, out Dictionary<string, string> keyValuePairs))
        {
            return keyValuePairs.Keys;
        }

        return new string[0];
    }
}
