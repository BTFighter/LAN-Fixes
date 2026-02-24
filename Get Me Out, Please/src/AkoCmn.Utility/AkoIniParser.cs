using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AkoCmn.Utility;

public class AkoIniParser
{
	private readonly Dictionary<string, Dictionary<string, string>> _sections;

	public AkoIniParser()
	{
		_sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
	}

	public static AkoIniParser Load(string filePath)
	{
		AkoIniParser akoIniParser = new AkoIniParser();
		if (File.Exists(filePath))
		{
			string[] lines = File.ReadAllLines(filePath);
			akoIniParser.Parse(lines);
		}
		return akoIniParser;
	}

	public void Parse(string[] lines)
	{
		string key = "DEFAULT";
		for (int i = 0; i < lines.Length; i++)
		{
			string text = lines[i].Trim();
			if (string.IsNullOrEmpty(text) || text.StartsWith(";") || text.StartsWith("#"))
			{
				continue;
			}
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				key = text.Substring(1, text.Length - 2).Trim();
				if (!_sections.ContainsKey(key))
				{
					_sections[key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				continue;
			}
			int num = text.IndexOf('=');
			if (num > 0)
			{
				string key2 = text.Substring(0, num).Trim();
				string value = text.Substring(num + 1).Trim();
				if (!_sections.ContainsKey(key))
				{
					_sections[key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				_sections[key][key2] = value;
			}
		}
	}

	public string GetValue(string section, string key, string defaultValue = "")
	{
		if (_sections.TryGetValue(section, out var value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		return defaultValue;
	}

	public int GetIntValue(string section, string key, int defaultValue = 0)
	{
		if (int.TryParse(GetValue(section, key, defaultValue.ToString()), out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public bool GetBoolValue(string section, string key, bool defaultValue = false)
	{
		string text = GetValue(section, key, defaultValue.ToString()).ToLower().Trim();
		switch (text)
		{
		default:
			return text == "on";
		case "true":
		case "1":
		case "yes":
			return true;
		}
	}

	public void SetValue(string section, string key, string value)
	{
		if (!_sections.ContainsKey(section))
		{
			_sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		_sections[section][key] = value;
	}

	public void Save(string filePath)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, Dictionary<string, string>> section in _sections)
		{
			if (section.Key != "DEFAULT")
			{
				stringBuilder.AppendLine("[" + section.Key + "]");
			}
			foreach (KeyValuePair<string, string> item in section.Value)
			{
				stringBuilder.AppendLine(item.Key + "=" + item.Value);
			}
			if (section.Key != "DEFAULT")
			{
				stringBuilder.AppendLine();
			}
		}
		File.WriteAllText(filePath, stringBuilder.ToString());
	}

	public bool HasSection(string section)
	{
		return _sections.ContainsKey(section);
	}

	public bool HasKey(string section, string key)
	{
		if (_sections.TryGetValue(section, out var value))
		{
			return value.ContainsKey(key);
		}
		return false;
	}

	public IEnumerable<string> GetSections()
	{
		return _sections.Keys;
	}

	public IEnumerable<string> GetKeys(string section)
	{
		if (_sections.TryGetValue(section, out var value))
		{
			return value.Keys;
		}
		return new string[0];
	}
}
