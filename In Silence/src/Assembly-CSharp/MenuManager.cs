using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
	public GameObject screen_Menu;

	public GameObject screen_Main;

	public GameObject screen_Multiplayer;

	public GameObject screen_Options;

	public GameObject screen_Credits;

	public GameObject screen_Singleplayer;

	public List<GameObject> menuScreens;

	public static Camera mainCamera;

	[SerializeField]
	private Camera s_mainCamera;

	public VivoxLobbyManager lobbyManager;

	public GameObject screen_hint;

	public GameGuideManager guideManager;

	public GameObject guideScreen;

	public static MenuManager menuManager;

	private bool isForest;

	public List<KeyBinding> keyBindings;

	[SerializeField]
	private ToggleMenuButton btn_map_forest;

	[SerializeField]
	private ToggleMenuButton btn_map_winterland;

	private void Awake()
	{
		mainCamera = s_mainCamera;
		menuManager = this;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		PlaySoundtrack();
		foreach (KeyBinding keyBinding in keyBindings)
		{
			keyBinding.SetKeybindings();
		}
	}

	public void act_Multiplayer(bool directJoin = false)
	{
		screen_Main.SetActive(value: false);
		screen_Multiplayer.SetActive(value: true);
	}

	public void act_Singleplayer()
	{
		screen_Singleplayer.SetActive(value: true);
		act_Single_Forest();
	}

	public void act_Back_Singleplayer()
	{
		screen_Singleplayer.SetActive(value: false);
		screen_Main.SetActive(value: true);
	}

	public void act_Single_Forest()
	{
		GetComponent<LobbyGameSettings>().IsForest = true;
		btn_map_winterland.Deactivate(Color.white);
		btn_map_forest.Activate();
	}

	public void act_Single_Winterland()
	{
		GetComponent<LobbyGameSettings>().IsForest = false;
		btn_map_forest.Deactivate(Color.white);
		btn_map_winterland.Activate();
	}

	public void act_Play_Singleplayer()
	{
		GetComponent<LobbyManager>().act_SinglePlayer();
	}

	public void act_Options()
	{
		screen_Menu.SetActive(value: false);
		screen_Options.SetActive(value: true);
		screen_hint.SetActive(value: false);
	}

	public void act_BackFromOptions()
	{
		screen_Menu.SetActive(value: true);
		screen_Options.SetActive(value: false);
		screen_hint.SetActive(value: true);
	}

	public void act_Credits()
	{
		screen_Main.SetActive(value: false);
		StartCoroutine("CreditsProcess");
	}

	public void act_GameGuide()
	{
		guideScreen.SetActive(value: true);
		guideManager.ShowGuide();
	}

	public void CompleteGuide()
	{
		EncryptedPlayerPrefs.SetInt("CompletedGuide", 1);
		guideScreen.SetActive(value: false);
	}

	private IEnumerator CreditsProcess()
	{
		LoadingScreenManager loadingScreen = GetComponent<LoadingScreenManager>();
		loadingScreen.FadeIn(0.5f);
		yield return new WaitForSeconds(1f);
		screen_Credits.SetActive(value: true);
		loadingScreen.loadingScreen.SetActive(value: false);
		yield return new WaitForSeconds(10f);
		loadingScreen.loadingScreen.SetActive(value: true);
		screen_Main.SetActive(value: true);
		screen_Credits.SetActive(value: false);
		loadingScreen.FadeOut(0.5f);
	}

	public void act_Quit()
	{
		Application.Quit();
	}

	public void ReturnToMain()
	{
		foreach (GameObject menuScreen in menuScreens)
		{
			menuScreen.SetActive(value: false);
			screen_Main.SetActive(value: true);
		}
	}

	public void act_Community()
	{
		Application.OpenURL(GameSettings.URL_DISCORD);
	}

	public void act_Twitter()
	{
		Application.OpenURL(GameSettings.URL_TWITTER);
	}

	public void PlaySoundtrack()
	{
		GeneralUISounds.instance.PlaySoundTrack(isFromMenu: true);
	}
}
