using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Viveport;
using VRTK;

public class MainManager : MonoBehaviour
{
	public static MainManager instance;

	public Camera sceneCamera;

	public List<Transform> spawns = new List<Transform>();

	public GameObject vrPlayerModel;

	public GameObject pcPlayerModel;

	private bool ranOnce;

	[SerializeField]
	private GameObject serverObject;

	[SerializeField]
	private VRTK_UICanvas vrtkCanvas;

	public PCManager pcManager;

	[SerializeField]
	private MyAudioManager audioManager;

	[SerializeField]
	private StoreSDKManager storeSDKManager;

	public ControlsManager controlsManager;

	public ServerManager serverManager;

	[SerializeField]
	private GameObject RewardScreen;

	[SerializeField]
	private GameObject FailureScreen;

	[SerializeField]
	private GameObject TrainingScreen;

	[SerializeField]
	private GameObject serverScreen;

	[SerializeField]
	private GameObject ErrorScreen;

	[SerializeField]
	private GameObject PhotoWarningScreen;

	[SerializeField]
	private Text trainingGhostTypeText;

	[SerializeField]
	private Button serverLobbyButton;

	[SerializeField]
	private Text serverLobbyText;

	[SerializeField]
	private Text serverVersionText;

	[HideInInspector]
	public Player localPlayer;

	private int connectionAttempts;

	[SerializeField]
	private SteamAuth steamAuth;

	private void Awake()
	{
		instance = this;
		((Behaviour)vrtkCanvas).enabled = XRDevice.isPresent;
	}

	public IEnumerator Start()
	{
		if (!PhotonNetwork.connected && !PhotonNetwork.offlineMode)
		{
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
		}
		serverVersionText.text = LocalisationSystem.GetLocalisedValue("Menu_ServerVersion") + ": " + storeSDKManager.serverVersion;
		yield return (object)new WaitForSeconds(0.5f);
		if (!XRDevice.isPresent)
		{
			for (int i = 0; i < spawns.Count; i++)
			{
				spawns[i].Translate(Vector3.up);
			}
		}
		if (!PhotonNetwork.connected || PhotonNetwork.offlineMode)
		{
			if (PhotonNetwork.offlineMode)
			{
				PhotonNetwork.offlineMode = false;
			}
			if (storeSDKManager.storeSDKType == StoreSDKManager.StoreSDKType.steam && !HasLANSettings())
			{
				steamAuth.ConnectViaSteamAuthenticator();
			}
			else if (HasLANSettings())
			{
				PhotonNetwork.AuthValues = null;
				PhotonNetwork.ConnectUsingSettings(storeSDKManager.serverVersion + storeSDKManager.storeBranchType);
			}
			if (XRDevice.isPresent)
			{
				localPlayer = Object.Instantiate<GameObject>(vrPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
			}
			else
			{
				localPlayer = Object.Instantiate<GameObject>(pcPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
				pcManager.SetValues();
			}
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
			LoadRewardScreens();
		}
		else if (FileBasedPrefs.GetInt("StayInServerRoom", 0) == 1)
		{
			PhotonNetwork.LeaveRoom();
		}
		else
		{
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
			PhotonNetwork.LeaveRoom();
		}
		if (FileBasedPrefs.GetInt("completedTraining", 0) == 0 && !Application.isEditor)
		{
			((Selectable)serverLobbyButton).interactable = false;
			((Graphic)serverLobbyText).color = Color32.op_Implicit(new Color32((byte)50, (byte)50, (byte)50, (byte)119));
		}
		FileBasedPrefs.SetInt("isTutorial", 0);
		trainingGhostTypeText.text = LocalisationSystem.GetLocalisedValue("Reward_Ghost") + " " + FileBasedPrefs.GetString("GhostType", "");
		SetScreenResolution();
		if (FileBasedPrefs.GetInt("myTotalExp", 0) < 100)
		{
			FileBasedPrefs.SetInt("myTotalExp", 100);
		}
		if (FileBasedPrefs.GetInt("myTotalExp", 0) < 100)
		{
			FileBasedPrefs.SetInt("myTotalExp", 100);
		}
	}

	private void OnDisconnectedFromPhoton()
	{
		FileBasedPrefs.SetInt("StayInServerRoom", 0);
		PhotonNetwork.offlineMode = true;
		Debug.Log((object)"Photon is now in Offline Mode: Disconnected");
	}

	private void OnConnectionFail(DisconnectCause cause)
	{
		FileBasedPrefs.SetInt("StayInServerRoom", 0);
		PhotonNetwork.offlineMode = true;
		Debug.Log((object)("Photon is now in Offline Mode: " + cause));
		PlayerPrefs.SetString("ErrorMessage", "Connection Failed: " + cause);
		ErrorScreen.SetActive(true);
		((Component)this).gameObject.SetActive(false);
	}

	private void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		FileBasedPrefs.SetInt("StayInServerRoom", 0);
		PhotonNetwork.offlineMode = true;
		Debug.Log((object)("Photon is now in Offline Mode: " + cause));
		PlayerPrefs.SetString("ErrorMessage", "Failed To Connect: " + cause);
		ErrorScreen.SetActive(true);
		((Component)this).gameObject.SetActive(false);
	}

	private void OnPhotonMaxCccuReached()
	{
		FileBasedPrefs.SetInt("StayInServerRoom", 0);
		PhotonNetwork.offlineMode = true;
		Debug.Log((object)"Photon is now in Offline Mode due to too many players on the server!");
		PlayerPrefs.SetString("ErrorMessage", "Disconnected: Server player limit reached. Please let the developer know as soon as possible.");
		ResetSettings(resetSetup: true);
		ErrorScreen.SetActive(true);
		((Component)this).gameObject.SetActive(false);
	}

	private void OnConnectedToMaster()
	{
		if (!PhotonNetwork.offlineMode)
		{
			PhotonNetwork.JoinLobby();
		}
		else
		{
			OnJoinedLobby();
		}
	}

	private void OnLeftRoom()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (FileBasedPrefs.GetInt("StayInServerRoom", 0) == 0)
		{
			if (XRDevice.isPresent)
			{
				localPlayer = Object.Instantiate<GameObject>(vrPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
			}
			else
			{
				localPlayer = Object.Instantiate<GameObject>(pcPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
				pcManager.SetValues();
			}
		}
		if (FileBasedPrefs.GetInt("MissionStatus", 0) == 3)
		{
			LoadRewardScreens();
		}
	}

	private void OnJoinedRoom()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (XRDevice.isPresent)
		{
			localPlayer = PhotonNetwork.Instantiate("VRPlayer", spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity, 0).GetComponent<Player>();
		}
		else
		{
			localPlayer = PhotonNetwork.Instantiate("PCPlayer", spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity, 0).GetComponent<Player>();
			pcManager.SetValues();
		}
		LoadRewardScreens();
	}

	public void SetPlayerName()
	{
		string text = "Unkwown";
		if (storeSDKManager.storeSDKType == StoreSDKManager.StoreSDKType.steam)
		{
			text = (SteamManager.Initialized ? SteamFriends.GetPersonaName() : "Unkwown");
		}
		else if (storeSDKManager.storeSDKType == StoreSDKManager.StoreSDKType.viveport)
		{
			text = (ViveportInitialiser.Initialized ? User.GetUserName() : "Unkwown");
		}
		if (text == "Goldberg")
		{
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
			PhotonNetwork.offlineMode = true;
			text = "I pirated the game";
			Debug.Log((object)"I pirated the game");
		}
		PhotonNetwork.playerName = text;
	}

	private void OnJoinedLobby()
	{
		if (FileBasedPrefs.GetInt("StayInServerRoom", 0) == 1)
		{
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
			RoomOptions roomOptions = new RoomOptions
			{
				IsOpen = true,
				IsVisible = (PlayerPrefs.GetInt("isPublicServer") == 1),
				MaxPlayers = 4,
				PlayerTtl = 2000
			};
			PhotonNetwork.JoinOrCreateRoom(PlayerPrefs.GetString("ServerName"), roomOptions, TypedLobby.Default);
		}
		else
		{
			LoadRewardScreens();
		}
	}

	private void OnPhotonCreateRoomFailed()
	{
		RoomOptions roomOptions = new RoomOptions
		{
			IsOpen = true,
			IsVisible = (PlayerPrefs.GetInt("isPublicServer") == 1),
			MaxPlayers = 4,
			PlayerTtl = 2000
		};
		PhotonNetwork.JoinOrCreateRoom(PlayerPrefs.GetString("ServerName"), roomOptions, TypedLobby.Default);
	}

	private void OnPhotonJoinRoomFailed()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (connectionAttempts == 6)
		{
			if (XRDevice.isPresent)
			{
				localPlayer = Object.Instantiate<GameObject>(vrPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
			}
			else
			{
				localPlayer = Object.Instantiate<GameObject>(pcPlayerModel, spawns[Random.Range(0, spawns.Count)].position, Quaternion.identity).GetComponent<Player>();
				pcManager.SetValues();
			}
			LoadRewardScreens();
			FileBasedPrefs.SetInt("StayInServerRoom", 0);
		}
		else
		{
			connectionAttempts++;
			((MonoBehaviour)this).StartCoroutine(AttemptToJoinRoomAfterDelay());
		}
	}

	private IEnumerator AttemptToJoinRoomAfterDelay()
	{
		yield return (object)new WaitForSeconds(2f);
		RoomOptions roomOptions = new RoomOptions
		{
			IsOpen = true,
			IsVisible = (PlayerPrefs.GetInt("isPublicServer") == 1),
			MaxPlayers = 4,
			PlayerTtl = 2000
		};
		PhotonNetwork.JoinOrCreateRoom(PlayerPrefs.GetString("ServerName"), roomOptions, TypedLobby.Default);
	}

	public void AcceptPhotoWarning()
	{
		PlayerPrefs.SetInt("PhotoSensitivityWarning", 3);
		PhotoWarningScreen.SetActive(false);
		((Component)this).gameObject.SetActive(true);
	}

	private void LoadRewardScreens()
	{
		if (!ranOnce)
		{
			ranOnce = true;
			if (PlayerPrefs.GetString("ErrorMessage") != string.Empty)
			{
				Debug.LogError((object)("Disconnect Error: " + PlayerPrefs.GetString("ErrorMessage")));
				ResetSettings(resetSetup: true);
				ErrorScreen.SetActive(true);
				((Component)this).gameObject.SetActive(false);
			}
			else if (PlayerPrefs.GetInt("PhotoSensitivityWarning") != 3)
			{
				ResetSettings(resetSetup: true);
				PhotoWarningScreen.SetActive(true);
				((Component)this).gameObject.SetActive(false);
			}
			else if (FileBasedPrefs.GetInt("MissionStatus", 0) == 1)
			{
				ResetSettings(resetSetup: true);
				RewardScreen.SetActive(true);
				((Component)this).gameObject.SetActive(false);
			}
			else if (FileBasedPrefs.GetInt("MissionStatus", 0) == 3)
			{
				ResetSettings(resetSetup: true);
				TrainingScreen.SetActive(true);
				((Component)this).gameObject.SetActive(false);
			}
			else if (FileBasedPrefs.GetInt("MissionStatus", 0) == 2)
			{
				ResetSettings(resetSetup: false);
				FailureScreen.SetActive(true);
				((Component)this).gameObject.SetActive(false);
			}
			if (PhotonNetwork.inRoom)
			{
				serverScreen.SetActive(true);
				serverManager.EnableMasks(active: false);
			}
		}
	}

	private void ResetSettings(bool resetSetup)
	{
		PlayerPrefs.SetInt("isInGame", 0);
		FileBasedPrefs.SetInt("isTutorial", 0);
		FileBasedPrefs.SetInt("MissionStatus", 0);
		if (resetSetup)
		{
			FileBasedPrefs.SetInt("setupPhase", 0);
		}
	}

	private void SetScreenResolution()
	{
		if (!XRDevice.isPresent)
		{
			if (PlayerPrefs.GetInt("resolutionValue") > Screen.resolutions.Length - 1)
			{
				PlayerPrefs.SetInt("resolutionValue", Screen.resolutions.Length - 1);
			}
			else if (PlayerPrefs.GetInt("resolutionValue") < 0)
			{
				PlayerPrefs.SetInt("resolutionValue", Screen.resolutions.Length - 1);
			}
			if (PlayerPrefs.GetInt("resolutionValue") == 0)
			{
				PlayerPrefs.SetInt("resolutionValue", Screen.resolutions.Length - 1);
				Screen.SetResolution(((Resolution)(ref Screen.resolutions[Screen.resolutions.Length - 1])).width, ((Resolution)(ref Screen.resolutions[Screen.resolutions.Length - 1])).height, true);
			}
			else
			{
				Screen.SetResolution(((Resolution)(ref Screen.resolutions[PlayerPrefs.GetInt("resolutionValue")])).width, ((Resolution)(ref Screen.resolutions[PlayerPrefs.GetInt("resolutionValue")])).height, PlayerPrefs.GetInt("fullscreenType") == 1);
			}
		}
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	private bool HasLANSettings()
	{
		string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", "LANSettings.ini"));
		if (!File.Exists(fullPath))
		{
			return false;
		}
		try
		{
			string[] array = File.ReadAllLines(fullPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.StartsWith("ServerAddress=", StringComparison.OrdinalIgnoreCase))
				{
					return !string.IsNullOrEmpty(text.Substring("ServerAddress=".Length).Trim());
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
