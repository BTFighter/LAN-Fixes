using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using ExitGames.Client.Photon;
using I2.Loc;
using UnityEngine;

public class OnlineMenu : MonoBehaviour
{
	private enum JoinRandomStage
	{
		JoinLobby,
		JoinRunning,
		Done
	}

	public enum XboxMatchmakingState
	{
		None,
		MatchmakingRootMenu,
		ReadyToSearchInParty,
		CreatingMatchmakingSession,
		ReadyToSearch,
		SearchingForGame,
		SearchingForGameInParty,
		JoiningMatch,
		FoundMatch
	}

	private static OnlineMenu _instance;

	public string gameVersion = "0.0.3";

	public string playerVisibleGameVersion = string.Empty;

	public GameObject lobbyOptions;

	public UILabel titleLabel;

	public UILabel versionLabel;

	public static bool useMultiplayer;

	public bool testEditor;

	private LobbyBrowser _lobbyBrowser;

	public UIButton lobbyBrowserButton;

	public UIButton matchmakingButton;

	public UIButton createRoomButton;

	public GameObject buttonTitles;

	public UIButton hostLANButton;

	public UIButton joinLANButton;

	public Font ipFont;

	public GameObject selectOnStart;

	private LobbyGameInfo _lobbyGameInfo;

	private LobbyChat _lobbyChat;

	public QuadrantNotification notification;

	private bool _createdRoom;

	public bool creatingRoom;

	public static int lobbySize;

	public string conventionTitle = "screencheat";

	public OnlinePasswordEntry passwordEntry;

	public bool attemptedMatchmaking;

	public bool attemptingMatchmaking;

	private JoinRandomStage _joinRandomStage;

	public GameObject joinSlotLabel;

	public UILabel backButtonLabel;

	private static string _roomBannedFromName;

	public GameObject consoleSocialButtons;

	public UILabel partyAppLabel;

	public UILabel inviteFriendsLabel;

	public static bool IsConsole;

	public static bool IsXbox;

	public static bool IsPS4;

	public UIButton xboxMatchmakingButtton;

	public bool inXboxMatchmakingMenu;

	public static bool openInMatchmaking;

	public static bool openInCustomGame;

	public static bool MatchmakingOnlyAllowOneInSession;

	private bool _stopUpdatingButtons;

	public XboxMatchmakingState xboxMatchmakingState;

	private static Action _completePreJoin;

	public static bool photonIsReady;

	public static bool createAsFriendsOnly;

	private static float _prejoinLobbyStartTime;

	public static string[] Friends { get; private set; }

	public static float BannedTimer { get; private set; }

	public static bool ShowLobbySettings => (!IsPS4 && !LobbyController.trainingMode) || (!LobbyController.trainingMode && IsPS4 && !useMultiplayer);

	public static bool MatchmakingLockedSlots
	{
		get
		{
			if (Instance == null)
			{
				return false;
			}
			return NetworkManager.IsMatchmakingLockedProfiles || Instance.xboxMatchmakingState == XboxMatchmakingState.SearchingForGame || Instance.xboxMatchmakingState == XboxMatchmakingState.SearchingForGameInParty || Instance.xboxMatchmakingState == XboxMatchmakingState.FoundMatch || Instance.xboxMatchmakingState == XboxMatchmakingState.JoiningMatch;
		}
	}

	public static bool XboxAllowJoinGame
	{
		get
		{
			if (!IsConsole)
			{
				return true;
			}
			return !useMultiplayer || (!MatchmakingLockedSlots && (_instance.inXboxMatchmakingMenu || LobbyController.Instance.CurrentSubMenu == LobbyController.SubMenu.None || InPreJoinLobby));
		}
	}

	public static bool InPreJoinLobby { get; set; }

	public GameObject KickedReturnDefaultbutton
	{
		get
		{
			if (IsConsole)
			{
				return matchmakingButton.gameObject;
			}
			return lobbyBrowserButton.gameObject;
		}
	}

	public static OnlineMenu Instance => _instance;

	static OnlineMenu()
	{
		lobbySize = 4;
		GA_GameObjectManager.onQuit = (Action)Delegate.Combine(GA_GameObjectManager.onQuit, new Action(OnQuit));
	}

	private static void OnQuit()
	{
		if (PhotonNetwork.connected)
		{
			PhotonNetwork.Disconnect();
		}
	}

	public static void OpenPreJoinLobby(Action onComplete)
	{
		MonoBehaviour.print("open pre join lobby!");
		useMultiplayer = true;
		InPreJoinLobby = true;
		openInMatchmaking = false;
		openInCustomGame = false;
		_completePreJoin = onComplete;
		_prejoinLobbyStartTime = Time.time;
		InputManagerController.allowAny = false;
		PlatformUtils.LoadLevel("Lobby");
		NetworkHandler.CloseProgressSpinner();
	}

	public static void CompletePreJoin()
	{
		InPreJoinLobby = false;
		if (_completePreJoin != null)
		{
			_completePreJoin();
		}
		_completePreJoin = null;
	}

	protected void Awake()
	{
		_lobbyGameInfo = UnityEngine.Object.FindObjectOfType<LobbyGameInfo>();
		_lobbyBrowser = UnityEngine.Object.FindObjectOfType<LobbyBrowser>();
		_lobbyChat = UnityEngine.Object.FindObjectOfType<LobbyChat>();
		_lobbyBrowser.gameObject.SetActive(value: false);
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		_instance = this;
		if (testEditor && Application.isEditor)
		{
			useMultiplayer = true;
		}
		if (!useMultiplayer || (NetworkManager.connected && NetworkManager.InRoom))
		{
			base.gameObject.SetActive(value: false);
		}
		if (!NetworkManager.InRoom && _lobbyChat != null)
		{
			_lobbyChat.gameObject.SetActive(value: false);
		}
		creatingRoom = false;
		consoleSocialButtons.SetActive(value: false);
		NetworkManager.OnJoinedRoom += OnGenericJoinedRoom;
		NetworkManager.OnAboutToJoinRoom += OnGenericAboutToJoinRoom;
		xboxMatchmakingState = XboxMatchmakingState.None;
		SetOnlineLobbyLabels();
		UIWidget component = GetComponent<UIWidget>();
		component.topAnchor.absolute = -35;
		photonIsReady = false;
	}

	public void Start()
	{
		MonoBehaviour.print("         Online menu start");
		lobbyOptions.SetActive(value: false);
		_lobbyGameInfo.OnlineMenuBanner();
		ShowPlayerCount();
		LobbyController.Instance.OnSlotChange += ShowPlayerCount;
		ConnectToNetwork();
	}

	protected void OnJoinedLobby()
	{
		MonoBehaviour.print("Go find friends");
		if (Friends != null)
		{
			PhotonNetwork.FindFriends(Friends);
		}
		creatingRoom = false;
	}

	public void SetOnlineLobbyLabels()
	{
		versionLabel.text = ScriptLocalization.Menu_Lobby_Version + " " + ((!string.IsNullOrEmpty(playerVisibleGameVersion)) ? playerVisibleGameVersion : gameVersion);
		if (LobbyController.trainingMode)
		{
			titleLabel.text = ((!LocalizationManager.IsEnglish) ? ScriptLocalization.Menu_Training_TimeTrials : ScriptLocalization.Menu_Training_TimeTrials.ToLower());
			return;
		}
		if (useMultiplayer)
		{
			titleLabel.text = ScriptLocalization.Menu_Lobby_Title_Online;
			string menu_Lobby_JoinRandom = ScriptLocalization.Menu_Lobby_JoinRandom;
			string menu_Lobby_CreateLobby = ScriptLocalization.Menu_Lobby_CreateLobby;
			if (matchmakingButton.gameObject.activeInHierarchy)
			{
				matchmakingButton.GetComponentInChildren<UILabel>().text = menu_Lobby_JoinRandom;
			}
			if (createRoomButton.gameObject.activeInHierarchy)
			{
				createRoomButton.GetComponentInChildren<UILabel>().text = menu_Lobby_CreateLobby;
			}
			if (!NetworkManager.UsePhoton && NetworkManager.InRoom)
			{
				titleLabel.text = RoomSetting.GetProperty(RoomSetting.Setting.LanIpAddressAndPort, ScriptLocalization.Menu_Lobby_Title_LAN);
				if (titleLabel.text != ScriptLocalization.Menu_Lobby_Title_LAN)
				{
					titleLabel.trueTypeFont = ipFont;
				}
			}
			if (attemptingMatchmaking)
			{
				_lobbyGameInfo.OnlineTitle(ScriptLocalization.Menu_Lobby_Online_Searching);
			}
		}
		else if (!Application.CanStreamedLevelBeLoaded("MainMenu"))
		{
			titleLabel.text = conventionTitle;
		}
		else
		{
			titleLabel.text = ScriptLocalization.Menu_Lobby_Title_Local;
		}
		if (!LocalizationManager.IsEnglish)
		{
			titleLabel.text = titleLabel.text.ToUpper();
		}
	}

	protected void OnDestroy()
	{
		NetworkManager.OnJoinedRoom -= OnGenericJoinedRoom;
		NetworkManager.OnAboutToJoinRoom -= OnGenericAboutToJoinRoom;
	}

	private void ShowPlayerCount()
	{
		if (!IsConsole && !NetworkManager.InRoom)
		{
			string format = ((LobbyController.PlayersReady != 1) ? ScriptLocalization.Menu_Lobby_PlayersReadyPlural : ScriptLocalization.Menu_Lobby_PlayersReadySingle);
			_lobbyGameInfo.OnlineTitle(string.Format(format, LobbyController.PlayersReady));
			_lobbyGameInfo.OnlineSubTitle(string.Empty);
		}
		HasPlayerSlotted();
	}

	public void HasPlayerSlotted()
	{
		if (LobbyController.PlayersReady > 0 || IsConsole)
		{
			if (!IsConsole)
			{
				lobbyBrowserButton.gameObject.SetActive(value: true);
			}
			matchmakingButton.gameObject.SetActive(value: true);
			if (!InPreJoinLobby)
			{
				createRoomButton.gameObject.SetActive(value: true);
			}
			if (!IsConsole)
			{
				joinLANButton.gameObject.SetActive(value: true);
				hostLANButton.gameObject.SetActive(value: true);
				buttonTitles.SetActive(value: true);
			}
			joinSlotLabel.SetActive(value: false);
		}
		else
		{
			lobbyBrowserButton.gameObject.SetActive(value: false);
			matchmakingButton.gameObject.SetActive(value: false);
			createRoomButton.gameObject.SetActive(value: false);
			joinLANButton.gameObject.SetActive(value: false);
			hostLANButton.gameObject.SetActive(value: false);
			buttonTitles.SetActive(value: false);
			joinSlotLabel.SetActive(value: true);
		}
	}

	private IEnumerator HoverSelectedButton()
	{
		yield return null;
		LobbyOptionButton option = UICamera.selectedObject.GetComponent<LobbyOptionButton>();
		if (option != null)
		{
			option.SelectUponClosingSubMenu();
		}
	}

	public void SetOnlineMenuEnabled(bool isEnabled)
	{
		lobbyBrowserButton.isEnabled = isEnabled;
		matchmakingButton.isEnabled = isEnabled;
		createRoomButton.isEnabled = isEnabled;
		lobbyBrowserButton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
		matchmakingButton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
		createRoomButton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
		if (IsConsole)
		{
			xboxMatchmakingButtton.isEnabled = isEnabled;
			xboxMatchmakingButtton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
		}
		joinLANButton.isEnabled = isEnabled;
		hostLANButton.isEnabled = isEnabled;
		joinLANButton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
		hostLANButton.GetComponent<UIKeyNavigation>().enabled = isEnabled;
	}

	public void CreateRoomMenu()
	{
		OnlineOptions.Instance.ActivateFromOnlineMenu();
	}

	public void BrowseServers()
	{
		_lobbyBrowser.Activate();
	}

	public void AttemptCancelMatchmakingSearch()
	{
		if (xboxMatchmakingState != XboxMatchmakingState.ReadyToSearchInParty && xboxMatchmakingState != XboxMatchmakingState.SearchingForGameInParty && xboxMatchmakingState != XboxMatchmakingState.JoiningMatch)
		{
			ConfirmPrompt.ShowPrompt(ScriptLocalization.Menu_Lobby_OnlineMenu_CancelSearchPrompt, ScriptLocalization.Menu_Generic_ConfirmPromptYes, ScriptLocalization.Menu_Generic_ConfirmPromptNo, CancelMatchmakingSearch, null, selectYes: false);
		}
	}

	public void CancelMatchmakingSearch()
	{
	}

	public void CancelMatchmakingSearchCleanup()
	{
		xboxMatchmakingButtton.GetComponentInChildren<UILabel>().text = ScriptLocalization.Menu_Lobby_OnlineMenu_XboxMatchmaking_FindGame;
		_lobbyGameInfo.ShowOnlineSpinner(on: false, string.Empty);
		backButtonLabel.text = ControllerEmoteButton.PlatformModifier() + ScriptLocalization.Menu_Generic_Back;
		xboxMatchmakingState = ((xboxMatchmakingState != XboxMatchmakingState.SearchingForGameInParty) ? XboxMatchmakingState.ReadyToSearch : XboxMatchmakingState.ReadyToSearchInParty);
		if (xboxMatchmakingState == XboxMatchmakingState.ReadyToSearchInParty)
		{
			_lobbyGameInfo.OnlineTitle(ScriptLocalization.Matchmaking_Party_WaitingForLeader);
		}
	}

	public void XboxStartMatchmaking()
	{
	}

	public void ConsoleStartMatchmakingSearchForPartyMember()
	{
		_lobbyGameInfo.OnlineTitle(string.Empty);
		_lobbyGameInfo.ShowOnlineSpinner(on: true, ScriptLocalization.Menu_Lobby_Online_Searching);
		xboxMatchmakingState = XboxMatchmakingState.SearchingForGameInParty;
		consoleSocialButtons.SetActive(value: false);
	}

	public void XboxMatchmakingExpired()
	{
		xboxMatchmakingButtton.GetComponentInChildren<UILabel>().text = ScriptLocalization.Menu_Lobby_OnlineMenu_XboxMatchmaking_FindGame;
	}

	public void CreatingMatchmakingSessionLock()
	{
		StartCoroutine(CreatingMatchmakingSessionLockCoroutine());
	}

	private IEnumerator CreatingMatchmakingSessionLockCoroutine()
	{
		creatingRoom = true;
		notification.ShowProgressSpinner(ScriptLocalization.Menu_Lobby_OnlineMenu_Matchmaking_CreatingSession);
		LobbyOptionButton mmButton = xboxMatchmakingButtton.GetComponent<LobbyOptionButton>();
		mmButton.Select(selected: false);
		UICamera.selectedObject = null;
		xboxMatchmakingState = XboxMatchmakingState.CreatingMatchmakingSession;
		float time = Time.time;
		while (!NetworkManager.IsMatchmaking)
		{
			yield return null;
			if (Time.time - time > 20f)
			{
				notification.ClosePriorityNotification();
				yield break;
			}
		}
		creatingRoom = false;
		notification.ClosePriorityNotification(ScriptLocalization.Menu_Lobby_OnlineMenu_Matchmaking_SessionReady);
		consoleSocialButtons.SetActive(value: true);
		mmButton.SetSelected();
		xboxMatchmakingState = XboxMatchmakingState.ReadyToSearch;
		yield return null;
		LobbyController.SetMatchmakingPartyLeader();
		LobbyController.Instance.lobbySlots.EnableKeyNavs(on: true);
		xboxMatchmakingButtton.GetComponent<UIKeyNavigation>().onRight = LobbyController.Instance.lobbySlots.GetFirstSlot();
	}

	public void ActivateXboxMatchmakingMenu(bool on)
	{
		matchmakingButton.gameObject.SetActive(!on);
		if (!InPreJoinLobby)
		{
			createRoomButton.gameObject.SetActive(!on);
		}
		xboxMatchmakingButtton.gameObject.SetActive(on);
		inXboxMatchmakingMenu = on;
		StartCoroutine(SetAllowAnyNextFrame(!on));
		SetOnlineLobbyLabels();
		_lobbyGameInfo.ShowOnlineSpinner(on: false, string.Empty);
		if (on)
		{
			UICamera.selectedObject = null;
		}
		else
		{
			UICamera.selectedObject = matchmakingButton.gameObject;
		}
	}

	public void ConsoleMatchmakingBecomePartyLeader()
	{
	}

	public void ActivateConsoleMatchmakingMenuForJoiningUser()
	{
	}

	private IEnumerator SetAllowAnyNextFrame(bool on)
	{
		yield return null;
		InputManagerController.allowAny = on;
	}

	private void JoinGameFromPreJoinLobby()
	{
		_stopUpdatingButtons = true;
		CompletePreJoin();
		InPreJoinLobby = false;
	}

	public void ConnectToNetwork()
	{
		NetworkManager.ConnectUsingSettings(gameVersion);
	}

	public void Matchmaking()
	{
		ConnectToNetwork();
		if (BannedTimer == -1f)
		{
			LobbyController.Instance.OnSlotChange -= ShowPlayerCount;
			_lobbyGameInfo.OnlineTitle(ScriptLocalization.Menu_Lobby_Online_Searching);
			_joinRandomStage = JoinRandomStage.JoinLobby;
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
			hashtable.Add("f", 0);
			hashtable.Add("s", 0);
			hashtable.Add("pw", string.Empty);
			ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = hashtable;
			PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
			attemptedMatchmaking = true;
			attemptingMatchmaking = true;
		}
		else
		{
			ConfirmPrompt.ShowInfo(ScriptLocalization.Menu_Lobby_Browser_MatchmakingKickedError, KickedReturnDefaultbutton);
		}
	}

	public void CreateRoom(string roomName, bool friendsOnly, string password, int mmStatus = 0)
	{
		notification.ShowProgressSpinner(ScriptLocalization.Menu_Lobby_Online_CreatingRoom);
		if (!NetworkManager.UsePhoton)
		{
			CreateNonPhotonRoom(password);
			return;
		}
		if (!photonIsReady || !PhotonNetwork.connected)
		{
			NetworkHandler.Instance.StartCoroutine(CreateRoomDelayed(roomName, friendsOnly, password, mmStatus));
			return;
		}
		MutatorManager.UpdateMutatorsEnabled(0);
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("m", 0);
		hashtable.Add("g", 0);
		hashtable.Add("f", friendsOnly ? 1 : 0);
		hashtable.Add("s", 0);
		hashtable.Add("p", 0);
		hashtable.Add("pw", password);
		hashtable.Add("bc", 0);
		hashtable.Add("t", 0);
		ExitGames.Client.Photon.Hashtable customRoomProperties = hashtable;
		string[] propsToListInLobby = new string[11]
		{
			"m", "g", "f", "s", "p", "hp", "pw", "pm", "rm", "bc",
			"t"
		};
		PhotonNetwork.CreateRoom(roomName, isVisible: true, isOpen: true, lobbySize, customRoomProperties, propsToListInLobby);
		LobbyController.Instance.Initialise();
		_createdRoom = true;
		creatingRoom = true;
	}

	private IEnumerator CreateRoomDelayed(string roomName, bool friendsOnly, string password, int mmStatus = 0)
	{
		while (!photonIsReady || !PhotonNetwork.connected)
		{
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		CreateRoom(roomName, friendsOnly, password, mmStatus);
	}

	private string GetLocalIPAddress()
	{
		try
		{
			foreach (System.Net.NetworkInformation.NetworkInterface ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
				{
					foreach (System.Net.NetworkInformation.UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
					{
						if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							return ip.Address.ToString();
						}
					}
				}
			}
		}
		catch
		{
		}
		
		return "127.0.0.1";
	}

	public void CreateNonPhotonRoom(string portStr)
	{
		if (!int.TryParse(portStr, out var result))
		{
			result = 2500;
		}
		Network.InitializeServer(8, result, useNat: false);
		RoomSetting.SetProperty(RoomSetting.Setting.LanIpAddressAndPort, $"{GetLocalIPAddress()} : {result}");
		LobbyController.Instance.Initialise();
		LobbyController.BlockClosingCurrentSubMenu = false;
		_createdRoom = true;
		SetOnlineLobbyLabels();
	}

	protected void OnPhotonRandomJoinFailed()
	{
		if (_joinRandomStage == JoinRandomStage.JoinLobby)
		{
			_joinRandomStage = JoinRandomStage.JoinRunning;
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
			hashtable.Add("f", 0);
			hashtable.Add("pw", string.Empty);
			ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = hashtable;
			PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0, MatchmakingMode.RandomMatching);
		}
		else
		{
			StartCoroutine(FailedJoinServerDelayed());
		}
	}

	private void ErrorMessage(string message)
	{
		SetOnlineMenuEnabled(isEnabled: false);
		ConfirmPrompt.ShowPrompt(message, ScriptLocalization.Menu_Lobby_Online_ErrorReturnToMain, null, delegate
		{
			PlatformUtils.LoadLevel("MainMenu");
		}, null);
		creatingRoom = false;
	}

	protected void OnPhotonJoinRoomFailed()
	{
		attemptingMatchmaking = false;
		creatingRoom = false;
		notification.ClosePriorityNotification(null, keepFade: true);
		ConfirmPrompt.ShowInfo(ScriptLocalization.Menu_Lobby_Online_JoiningFailed);
	}

	protected void OnConnectionFail(DisconnectCause cause)
	{
		ErrorMessage(string.Concat(ScriptLocalization.Menu_Lobby_Online_ConnectionLost, " (", cause, ")"));
	}

	protected void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		if (LobbyController.Instance != null && (LobbyController.Instance.CurrentSubMenu == LobbyController.SubMenu.LobbyBrowser || LobbyController.Instance.CurrentSubMenu == LobbyController.SubMenu.OnlineOptions || attemptedMatchmaking))
		{
			ErrorMessage(string.Concat(ScriptLocalization.Menu_Lobby_Online_FailedToConnect, " (", cause, ")"));
		}
	}

	private IEnumerator FailedJoinServerDelayed()
	{
		yield return new WaitForSeconds(0.8f);
		ConfirmPrompt.ShowInfo(ScriptLocalization.Menu_Lobby_Online_FailedToFind, createRoomButton.gameObject);
		LobbyController.Instance.OnSlotChange += ShowPlayerCount;
		attemptingMatchmaking = false;
		creatingRoom = false;
	}

	public static void JoinRoomNotification()
	{
		_instance._createdRoom = false;
		_instance.creatingRoom = false;
		_instance.notification.ShowPriorityNotification(ScriptLocalization.Menu_Lobby_Online_JoiningRoom);
	}

	protected void OnGenericAboutToJoinRoom()
	{
		LobbyController.Instance.JoinRoomResetPlayers();
	}

	protected void OnGenericJoinedRoom()
	{
		if (!NetworkManager.UsePhoton && LobbyJoinLAN.Instance.gameObject.activeSelf)
		{
			LobbyJoinLAN.Instance.CloseOnJoin();
			LobbyController.Instance.SetLANTitleDelayed();
		}
		if (RoomSetting.GetProperty(RoomSetting.Setting.Started, 0) == 0)
		{
			MonoBehaviour.print("Joined a room; it's not running");
			if (LobbyController.Instance.GameMode != null)
			{
				LobbyController.Instance.GameMode.LobbySelected(LobbyController.Instance, LobbyController.Instance.GameMode.info.useTeams);
			}
			LobbyController.Instance.OnSlotChange -= ShowPlayerCount;
			lobbyOptions.SetActive(value: true);
			base.gameObject.SetActive(value: false);
			LobbyController.Instance.allowJoinGame = true;
			LobbyController.Instance.SetMainMenuEnabled(isEnabled: true);
			UICamera.selectedObject = LobbyController.Instance.selectMap.gameObject;
			SpecialXPDayController.Instance.ShowMessage(on: false);
			if (ConfirmPrompt.IsActive)
			{
				ConfirmPrompt.HidePrompt();
			}
			if (_lobbyChat != null)
			{
				_lobbyChat.gameObject.SetActive(value: true);
			}
			LobbyController.Instance.UpdatePlayerCount(NetworkManager.maxPlayers);
			BlackDefocus.Out();
			if (_createdRoom)
			{
				notification.ClosePriorityNotification(ScriptLocalization.Menu_Lobby_Online_CreateSuccess);
			}
			else
			{
				notification.ClosePriorityNotification(ScriptLocalization.Menu_Lobby_Online_JoinSuccess);
			}
			_createdRoom = false;
			if (NetworkManager.IsMatchmaking && NetworkManager.IsAuthority)
			{
				LobbyController.StartMatchmakingRoutine();
			}
		}
		else
		{
			MonoBehaviour.print("Joined a running room; hopping straight into the level");
			LobbyController.Instance.UpdatePlayerCount(NetworkManager.maxPlayers);
			LobbyController.Instance.JoinRunning();
		}
		attemptedMatchmaking = false;
		attemptingMatchmaking = false;
		creatingRoom = false;
		_joinRandomStage = JoinRandomStage.Done;
		SetOnlineLobbyLabels();
	}

	public void BanFromRoom()
	{
		if (PhotonNetwork.room != null)
		{
			BannedTimer = 120f;
			_roomBannedFromName = PhotonNetwork.room.name;
		}
	}

	public bool IsBannedFromRoom(string roomName)
	{
		return BannedTimer != -1f && roomName == _roomBannedFromName;
	}

	protected void Update()
	{
		if (BannedTimer != -1f)
		{
			BannedTimer -= Time.deltaTime;
			if (BannedTimer <= 0f)
			{
				BannedTimer = -1f;
				_roomBannedFromName = null;
			}
		}
		if (InPreJoinLobby && Time.time - _prejoinLobbyStartTime > 80f && _prejoinLobbyStartTime > 0f)
		{
			NetworkHandler.ResetOnline(ScreencheatErrorCode.PrejoinTimedOut);
			_prejoinLobbyStartTime = -1f;
		}
	}

	public void OnApplicationQuit()
	{
		_instance = null;
		BannedTimer = -1f;
		_roomBannedFromName = null;
	}
}
