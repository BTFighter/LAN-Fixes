using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReadDataManager : MonoBehaviour
{
	private string url_datas = "http://www.mertcorekci.com/cryptids/data.txt";

	private string url_updateChecker = "http://www.mertcorekci.com/cryptids/updatechecker.txt";

	private string url_gameVersion = "http://www.mertcorekci.com/cryptids/version.txt";

	private string url_testButtons = "http://www.mertcorekci.com/cryptids/test.txt";

	private string[] words;

	private void Awake()
	{
		words = new string[1];
	}

	public void ReadDataProcess()
	{
		StartCoroutine("UpdateChecker");
	}

	private IEnumerator UpdateChecker()
	{
		using WWW www = new WWW(url_updateChecker);
		yield return www;
		int num = int.Parse(www.text);
		MonoBehaviour.print("UpdateChecker");
		if (num == 1)
		{
			StartCoroutine("GetGameVersion");
		}
		else
		{
			StartCoroutine("ReadDataURL");
		}
	}

	private IEnumerator GetGameVersion()
	{
		using WWW www = new WWW(url_gameVersion);
		yield return www;
		string text = www.text;
		MonoBehaviour.print("GetGameVersion=" + text + "-" + GameSettings.GAME_VERSION);
		if (text == GameSettings.GAME_VERSION)
		{
			StartCoroutine("ReadDataURL");
		}
		else
		{
			GetComponent<FirstSceneManager>().ShowUpdate();
		}
	}

	private IEnumerator ReadDataURL()
	{
		using (WWW www = new WWW(url_datas))
		{
			yield return www;
			MonoBehaviour.print("ReadDataURL");
			string text = www.text;
			words = text.Split('\n');
			GameSettings.IsTestButtonsOn = words[0] == "1";
		}
		yield return new WaitForSeconds(1f);
		SetupVariables();
	}

	public void SetupVariables()
	{
		GetComponent<FirstSceneManager>().loadingScreen.SetActive(value: true);
		SceneManager.LoadSceneAsync(2);
	}

	public void StartSingleplayer()
	{
		StartCoroutine("SingleplayerProcess");
	}

	private IEnumerator SingleplayerProcess()
	{
		GetComponent<LoadingScreenManager>().FadeIn(GameSettings.LOADING_FADETIMER);
		yield return new WaitForSeconds(1f);
		GetComponent<FirstSceneManager>().loadingScreen.SetActive(value: true);
		SceneManager.LoadSceneAsync(2);
	}
}
