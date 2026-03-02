using System.Collections;
using System.IO;
using System.Net;
using I2.Loc;
using UnityEngine;

public class FirstSceneManager : MonoBehaviour
{
	public LanguageManager languageManager;

	[Header("Screens")]
	[SerializeField]
	private GameObject noInternetScreen;

	[SerializeField]
	private GameObject updateScreen;

	[SerializeField]
	private GameObject languageScreen;

	public GameObject loadingScreen;

	[SerializeField]
	private GameObject openerScreen;

	[SerializeField]
	private GameObject tutorialPop;

	[SerializeField]
	private GameObject guideScreen;

	[SerializeField]
	private GameGuideManager guideManager;

	[Header("Test Bools")]
	public bool resetFirstOptions;

	[SerializeField]
	private bool resetLanguageCrypt;

	[SerializeField]
	private bool resetTutorial;

	[SerializeField]
	private bool passOpenerAndTutorial;

	private void Start()
	{
		GameSettings.SetKeys();
		if (passOpenerAndTutorial)
		{
			openerScreen.SetActive(value: false);
			CheckInternet();
		}
		else
		{
			if (resetTutorial)
			{
				EncryptedPlayerPrefs.DeleteKey("CompletedGuide");
			}
			openerScreen.SetActive(value: true);
		}
		if (!EncryptedPlayerPrefs.HasKey("NightmareMode"))
		{
			EncryptedPlayerPrefs.SetInt("NightmareMode", 0);
		}
		if (!EncryptedPlayerPrefs.HasKey("BeAMonster"))
		{
			EncryptedPlayerPrefs.SetInt("BeAMonster", 1);
		}
	}

	public void OpenerEnd()
	{
		StartCoroutine("OpenerEndWaiter");
	}

	private IEnumerator OpenerEndWaiter()
	{
		yield return new WaitForSeconds(1f);
		openerScreen.SetActive(value: false);
		if (EncryptedPlayerPrefs.GetInt("CompletedGuide") == 1)
		{
			CheckInternet();
			yield break;
		}
		if (resetLanguageCrypt)
		{
			EncryptedPlayerPrefs.DeleteKey("Language");
		}
		if (!EncryptedPlayerPrefs.HasKey("Language"))
		{
			ShowLanguage();
		}
		else
		{
			ShowTutorialPopUp();
		}
	}

	private void ShowTutorialPopUp()
	{
		tutorialPop.SetActive(value: true);
	}

	public void act_ShowGuide()
	{
		tutorialPop.SetActive(value: false);
		ShowGuide();
	}

	public void act_SkipGuide()
	{
		tutorialPop.SetActive(value: false);
		guideManager.CompleteGuide();
	}

	private void ShowGuide()
	{
		guideScreen.SetActive(value: true);
		guideManager.ShowGuide();
	}

	public void CompleteGuide()
	{
		EncryptedPlayerPrefs.SetInt("CompletedGuide", 1);
		guideScreen.SetActive(value: false);
		CheckInternet();
	}

	private void CheckInternet()
	{
		loadingScreen.SetActive(value: true);
		LocalizationManager.CurrentLanguage = EncryptedPlayerPrefs.GetString("Language");
		GetComponent<ReadDataManager>().SetupVariables();
	}

	public void act_DoneLanguage()
	{
		ShowTutorialPopUp();
		languageScreen.SetActive(value: false);
	}

	private void StartSingleplayer()
	{
		GetComponent<ReadDataManager>().StartSingleplayer();
	}

	private void ShowLanguage()
	{
		languageManager.SelectLanguage(0, "English");
		loadingScreen.SetActive(value: false);
		languageScreen.SetActive(value: true);
	}

	private void ReadData()
	{
		GetComponent<LoadingScreenManager>().FadeIn(GameSettings.LOADING_FADETIMER);
		GetComponent<ReadDataManager>().ReadDataProcess();
	}

	public void ShowUpdate()
	{
		loadingScreen.SetActive(value: false);
		updateScreen.SetActive(value: true);
	}

	public void act_Update()
	{
		Application.OpenURL(GameSettings.URL_STEAM);
		Application.Quit();
	}

	public void act_Quit()
	{
		Application.Quit();
	}

	private void CheckOffline()
	{
		DebugManager.instance.Add("IsOFFLINE=" + GameSettings.isOffline);
		GetComponent<ReadDataManager>().SetupVariables();
	}

	public string GetHtmlFromUri(string resource)
	{
		string text = string.Empty;
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(resource);
		try
		{
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			if (httpWebResponse.StatusCode < (HttpStatusCode)299 && httpWebResponse.StatusCode >= HttpStatusCode.OK)
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
				char[] array = new char[80];
				streamReader.Read(array, 0, array.Length);
				char[] array2 = array;
				foreach (char c in array2)
				{
					text += c;
				}
			}
		}
		catch
		{
			return "";
		}
		return text;
	}

	public void PlayStudioLogoSound()
	{
		GeneralUISounds.instance.PlayStudioLogoSound();
	}

	public void PlayGameLogoSound()
	{
		GeneralUISounds.instance.PlayGameLogoSound();
	}
}
