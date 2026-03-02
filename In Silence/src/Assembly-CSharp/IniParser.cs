using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class IniParser
{
	private Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

	private string currentSection;

	public IniParser()
	{
	}

	public IniParser(string filePath)
	{
		LoadFromFile(filePath);
	}

	public void LoadFromFile(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return;
		}
		string[] array = File.ReadAllLines(filePath);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (string.IsNullOrEmpty(text) || text.StartsWith(";") || text.StartsWith("#"))
			{
				continue;
			}
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				currentSection = text.Substring(1, text.Length - 2).Trim();
				if (!sections.ContainsKey(currentSection))
				{
					sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				continue;
			}
			int num = text.IndexOf('=');
			if (num <= 0)
			{
				continue;
			}
			string key = text.Substring(0, num).Trim();
			string text2 = text.Substring(num + 1).Trim();
			if ((text2.StartsWith("\"") && text2.EndsWith("\"")) || (text2.StartsWith("'") && text2.EndsWith("'")))
			{
				text2 = text2.Substring(1, text2.Length - 2);
			}
			if (!string.IsNullOrEmpty(currentSection))
			{
				sections[currentSection][key] = text2;
				continue;
			}
			if (!sections.ContainsKey(""))
			{
				sections[""] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
			sections[""][key] = text2;
		}
	}

	public void SaveToFile(string filePath)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (sections.ContainsKey("") && sections[""].Count > 0)
		{
			foreach (KeyValuePair<string, string> item in sections[""])
			{
				stringBuilder.AppendLine(item.Key + "=" + item.Value);
			}
			stringBuilder.AppendLine();
		}
		foreach (KeyValuePair<string, Dictionary<string, string>> section in sections)
		{
			if (string.IsNullOrEmpty(section.Key))
			{
				continue;
			}
			stringBuilder.AppendLine("[" + section.Key + "]");
			foreach (KeyValuePair<string, string> item2 in section.Value)
			{
				stringBuilder.AppendLine(item2.Key + "=" + item2.Value);
			}
			stringBuilder.AppendLine();
		}
		File.WriteAllText(filePath, stringBuilder.ToString());
	}

	public string GetValue(string section, string key, string defaultValue = "")
	{
		if (sections.TryGetValue(section, out var value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		return defaultValue;
	}

	public int GetIntValue(string section, string key, int defaultValue = 0)
	{
		if (int.TryParse(GetValue(section, key), out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public float GetFloatValue(string section, string key, float defaultValue = 0f)
	{
		if (float.TryParse(GetValue(section, key), out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public bool GetBoolValue(string section, string key, bool defaultValue = false)
	{
		switch (GetValue(section, key).ToLower())
		{
		case "true":
		case "1":
		case "yes":
		case "on":
			return true;
		case "false":
		case "0":
		case "no":
		case "off":
			return false;
		default:
			return defaultValue;
		}
	}

	public void SetValue(string section, string key, string value)
	{
		if (!sections.ContainsKey(section))
		{
			sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		sections[section][key] = value;
	}

	public void SetValue(string section, string key, int value)
	{
		SetValue(section, key, value.ToString());
	}

	public void SetValue(string section, string key, float value)
	{
		SetValue(section, key, value.ToString());
	}

	public void SetValue(string section, string key, bool value)
	{
		SetValue(section, key, value ? "true" : "false");
	}

	public bool HasSection(string section)
	{
		return sections.ContainsKey(section);
	}

	public bool HasKey(string section, string key)
	{
		if (sections.TryGetValue(section, out var value))
		{
			return value.ContainsKey(key);
		}
		return false;
	}

	public List<string> GetSections()
	{
		List<string> list = new List<string>();
		foreach (string key in sections.Keys)
		{
			if (!string.IsNullOrEmpty(key))
			{
				list.Add(key);
			}
		}
		return list;
	}

	public List<string> GetKeys(string section)
	{
		List<string> list = new List<string>();
		if (sections.TryGetValue(section, out var value))
		{
			foreach (string key in value.Keys)
			{
				list.Add(key);
			}
		}
		return list;
	}

	public void RemoveSection(string section)
	{
		if (sections.ContainsKey(section))
		{
			sections.Remove(section);
		}
	}

	public void RemoveKey(string section, string key)
	{
		if (sections.TryGetValue(section, out var value) && value.ContainsKey(key))
		{
			value.Remove(key);
		}
	}
}
