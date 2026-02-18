using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Token: 0x02001608 RID: 5640
public class IniParser
{
	// Token: 0x06009440 RID: 37952 RVA: 0x0008BB4B File Offset: 0x00089D4B
	public IniParser()
	{
		this.currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		this.sections[""] = this.currentSection;
	}

	// Token: 0x06009441 RID: 37953 RVA: 0x0008BB89 File Offset: 0x00089D89
	public IniParser(string filePath)
		: this()
	{
		this.Load(filePath);
	}

	// Token: 0x06009442 RID: 37954 RVA: 0x002C6D90 File Offset: 0x002C4F90
	public void Load(string filePath)
	{
		if (!File.Exists(filePath))
		{
			Debug.LogWarning("INI file not found: " + filePath);
			return;
		}
		try
		{
			string[] array = File.ReadAllLines(filePath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (!string.IsNullOrEmpty(text) && !text.StartsWith(";") && !text.StartsWith("#"))
				{
					if (text.StartsWith("[") && text.EndsWith("]"))
					{
						string text2 = text.Substring(1, text.Length - 2).Trim();
						if (!this.sections.ContainsKey(text2))
						{
							this.currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							this.sections[text2] = this.currentSection;
						}
						else
						{
							this.currentSection = this.sections[text2];
						}
					}
					else
					{
						int num = text.IndexOf('=');
						if (num > 0)
						{
							string text3 = text.Substring(0, num).Trim();
							string text4 = text.Substring(num + 1).Trim();
							if (text4.StartsWith("\"") && text4.EndsWith("\""))
							{
								text4 = text4.Substring(1, text4.Length - 2);
							}
							if (this.currentSection.ContainsKey(text3))
							{
								this.currentSection[text3] = text4;
							}
							else
							{
								this.currentSection.Add(text3, text4);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error loading INI file: " + ex.Message);
		}
	}

	// Token: 0x06009443 RID: 37955 RVA: 0x002C6F48 File Offset: 0x002C5148
	public string GetString(string section, string key, string defaultValue = "")
	{
		Dictionary<string, string> dictionary;
		string text;
		if (this.sections.TryGetValue(section, out dictionary) && dictionary.TryGetValue(key, out text))
		{
			return text;
		}
		return defaultValue;
	}

	// Token: 0x06009444 RID: 37956 RVA: 0x002C6F74 File Offset: 0x002C5174
	public int GetInt(string section, string key, int defaultValue = 0)
	{
		int num;
		if (int.TryParse(this.GetString(section, key, ""), out num))
		{
			return num;
		}
		return defaultValue;
	}

	// Token: 0x06009445 RID: 37957 RVA: 0x002C6F9C File Offset: 0x002C519C
	public bool GetBool(string section, string key, bool defaultValue = false)
	{
		string text = this.GetString(section, key, "").ToLower();
		return text == "true" || text == "1" || text == "yes" || text == "on" || (!(text == "false") && !(text == "0") && !(text == "no") && !(text == "off") && defaultValue);
	}

	// Token: 0x06009446 RID: 37958 RVA: 0x002C702C File Offset: 0x002C522C
	public float GetFloat(string section, string key, float defaultValue = 0f)
	{
		float num;
		if (float.TryParse(this.GetString(section, key, ""), out num))
		{
			return num;
		}
		return defaultValue;
	}

	// Token: 0x06009447 RID: 37959 RVA: 0x002C7054 File Offset: 0x002C5254
	public void SetValue(string section, string key, string value)
	{
		Dictionary<string, string> dictionary;
		if (!this.sections.TryGetValue(section, out dictionary))
		{
			dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			this.sections[section] = dictionary;
		}
		if (dictionary.ContainsKey(key))
		{
			dictionary[key] = value;
			return;
		}
		dictionary.Add(key, value);
	}

	// Token: 0x06009448 RID: 37960 RVA: 0x002C70A4 File Offset: 0x002C52A4
	public void Save(string filePath)
	{
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(filePath))
			{
				foreach (KeyValuePair<string, Dictionary<string, string>> keyValuePair in this.sections)
				{
					if (!string.IsNullOrEmpty(keyValuePair.Key))
					{
						streamWriter.WriteLine("[{0}]", keyValuePair.Key);
					}
					foreach (KeyValuePair<string, string> keyValuePair2 in keyValuePair.Value)
					{
						streamWriter.WriteLine("{0}={1}", keyValuePair2.Key, keyValuePair2.Value);
					}
					if (!string.IsNullOrEmpty(keyValuePair.Key))
					{
						streamWriter.WriteLine();
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error saving INI file: " + ex.Message);
		}
	}

	// Token: 0x040070E4 RID: 28900
	private Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

	// Token: 0x040070E5 RID: 28901
	private Dictionary<string, string> currentSection;
}