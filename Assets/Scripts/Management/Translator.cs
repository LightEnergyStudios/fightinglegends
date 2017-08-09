using System.Collections;
using System.Xml;
using UnityEngine;

public class Translator
{
	private static string defaultLanguage = "English";
	private static Translator instance = null;
	private static readonly object padlock = new object();

	private string language = defaultLanguage;

	private Hashtable DefaultTranslations;		// English - built from xml Translations resource file for fast lookup
	private Hashtable Translations;				// system language - built from xml Translations resource file for fast lookup

	private Translator()
	{
		language = Application.systemLanguage.ToString();

		// load translations xml file
		TextAsset translations = (TextAsset) Resources.Load("Translations", typeof(TextAsset));	
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(translations.text);

		// check if language exists in translations file
		if (xmlDoc.DocumentElement[language] == null)
		{
			// system language isn't present, so use the default
			language = defaultLanguage;
		}

		SetLanguage(xmlDoc, language);
	}

	public static Translator Instance
	{
		get
		{
			lock (padlock)
			{
				if (instance == null)
					instance = new Translator();

				return instance;
			}
		}
	}
		

	private void SetLanguage(XmlDocument xmlDoc, string language)
	{
		Translations = new Hashtable();
		var element = xmlDoc.DocumentElement[language];

		if (element != null)
		{
			var elemEnum = element.GetEnumerator();
			while (elemEnum.MoveNext())
			{
				var xmlItem = (XmlElement)elemEnum.Current;
				string key = xmlItem.GetAttribute("name");

				if (Translations.ContainsKey(key))
					Debug.LogError("Translation duplicate key '" + key + "'!");
				else
					Translations.Add(key, xmlItem.InnerText);
			}
		}
		else
		{
			Debug.LogError("Language '" + language + "' does not exist!");
		}
	}

	public string LookupString(string name, bool wrap = false, bool exclaim = false, bool toUpper = false)
	{
		if (!Translations.ContainsKey(name))
		{
			Debug.LogError("Translation string '" + name + "' not present in language " + language);
			return name + "?";		// TODO: lookup from default language
		}

		var translation = (string)Translations[name];
//		Debug.Log("LookupString: " + name + " / Translation = " + translation);

		// insert valid newlines and remove surrounding spaces
		translation = translation.Replace("\\n", "\n");
		translation = translation.Replace(" \n ", "\n");

		if (wrap)
		{
			translation = translation.Replace("-", "\n");
			translation = translation.Replace(" ", "\n");
		}

		if (exclaim)
			translation += "!";

		if (toUpper)
			translation = translation.ToUpper();

		return translation;
	}
}
