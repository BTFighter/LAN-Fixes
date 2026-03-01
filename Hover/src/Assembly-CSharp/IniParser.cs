// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// IniParser
using System.Collections.Generic;
using System.IO;

public class IniParser
{
	private Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>();

	private string currentSection;

	public IniParser()
	{
	}

	public IniParser(string filePath)
	{
		Load(filePath);
	}

	public void Load(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return;
		}
		sections.Clear();
		currentSection = null;
		using StreamReader streamReader = new StreamReader(filePath);
		string text;
		while ((text = streamReader.ReadLine()) != null)
		{
			text = text.Trim();
			if (string.IsNullOrEmpty(text) || text.StartsWith(";") || text.StartsWith("#"))
			{
				continue;
			}
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				currentSection = text.Substring(1, text.Length - 2).Trim();
				if (!sections.ContainsKey(currentSection))
				{
					sections.Add(currentSection, new Dictionary<string, string>());
				}
			}
			else if (text.Contains("=") && currentSection != null)
			{
				int num = text.IndexOf('=');
				string key = text.Substring(0, num).Trim();
				string text2 = text.Substring(num + 1).Trim();
				if ((text2.StartsWith("\"") && text2.EndsWith("\"")) || (text2.StartsWith("'") && text2.EndsWith("'")))
				{
					text2 = text2.Substring(1, text2.Length - 2);
				}
				if (sections[currentSection].ContainsKey(key))
				{
					sections[currentSection][key] = text2;
				}
				else
				{
					sections[currentSection].Add(key, text2);
				}
			}
		}
	}

	public void Save(string filePath)
	{
		using StreamWriter streamWriter = new StreamWriter(filePath);
		foreach (KeyValuePair<string, Dictionary<string, string>> section in sections)
		{
			streamWriter.WriteLine("[{0}]", section.Key);
			foreach (KeyValuePair<string, string> item in section.Value)
			{
				streamWriter.WriteLine("{0}={1}", item.Key, item.Value);
			}
			streamWriter.WriteLine();
		}
	}

	public string GetValue(string section, string key, string defaultValue = "")
	{
		if (sections.ContainsKey(section) && sections[section].ContainsKey(key))
		{
			return sections[section][key];
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

	public float GetFloatValue(string section, string key, float defaultValue = 0f)
	{
		if (float.TryParse(GetValue(section, key, defaultValue.ToString()), out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public bool GetBoolValue(string section, string key, bool defaultValue = false)
	{
		string text = GetValue(section, key, defaultValue.ToString()).ToLower();
		if (!(text == "true") && !(text == "1"))
		{
			return text == "yes";
		}
		return true;
	}

	public void SetValue(string section, string key, string value)
	{
		if (!sections.ContainsKey(section))
		{
			sections.Add(section, new Dictionary<string, string>());
		}
		if (sections[section].ContainsKey(key))
		{
			sections[section][key] = value;
		}
		else
		{
			sections[section].Add(key, value);
		}
	}

	public bool HasSection(string section)
	{
		return sections.ContainsKey(section);
	}

	public bool HasKey(string section, string key)
	{
		if (sections.ContainsKey(section))
		{
			return sections[section].ContainsKey(key);
		}
		return false;
	}

	public Dictionary<string, string> GetSection(string section)
	{
		if (sections.ContainsKey(section))
		{
			return new Dictionary<string, string>(sections[section]);
		}
		return new Dictionary<string, string>();
	}

	public List<string> GetSections()
	{
		return new List<string>(sections.Keys);
	}
}
