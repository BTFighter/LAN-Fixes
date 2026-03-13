using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : AbstractUIScreen
{
	public FrameUI frameUI;

	private bool showLobbies;

	public ImageToggle matchMakingBulb;

	public ImageToggle roomListBulb;

	public ImageToggle matchMakingswitch;

	public ServerListElement serverlistElementPrefab;

	public ScrollRect serverListScrollrect;

	private List<ServerListElement> activeServerListItems = new List<ServerListElement>();

	private float lastClickTime;

	public GameObject noRoomsFilterLabel;

	public GameObject serverListHeader;

	public LobbyHostDialog hostDialog;

	public Button hostRoomButton;

	public Button joinRoomButton;

	public Button filterRoomButton;

	public GameObject matchMakingPanel;

	public GameObject startMatchMakingButton;

	public GameObject cancelMatchMakingButton;

	public TMP_Text matchmakingTimerText;

	private float matchmakeLabelFlashTimer;

	private Coroutine matchmakingRoutine;

	private float matchMakingTimer;

	public TMP_Text selectedMatchmakingMode;

	public TMP_Text selectedMatchmakingModeDescription;

	public Image selectedMatchmakingModeImage;

	public ColoredToggle ffaToggle;

	public ColoredToggle tdmToggle;

	public ColoredToggle ltsToggle;

	public ColoredToggle ctfToggle;

	public ColoredToggle coopAssaultToggle;

	public ColoredToggle territoryToggle;

	public ColoredToggle customMapToggle;

	private int selectedModesMask;

	private GameStyleID shownGameModeInfo = GameStyleID.Cooperative;

	public Sprite[] modeImages;

	private List<RoomInfo> failedRooms = new List<RoomInfo>();

	private RoomInfo[] netServerList = new RoomInfo[0];

	private List<RoomInfo> filteredServerList = new List<RoomInfo>();

	private Dictionary<string, List<Ping>> serverPings = new Dictionary<string, List<Ping>>();

	private float lastRefreshTime = -1f;

	public const int refreshInterval = 1;

	private int serverSelection = -1;

	private RoomInfo selectedServer;

	private string serverName = "My game, come and play!";

	private string serverComment = "";

	private bool hiddenRoom;

	private int maxPlayers = 32;

	private bool connecting;

	private RoomInfo connectingTo;

	internal MapData lateJoinMapData;

	public List<Image> sortingButtons;

	public Sprite[] sortingStateSprites;

	private SortState[] sortingStates;

	private string[] roomNameFilter;

	private string[] mapNameFilter;

	private List<GameStyleID> gameStyleFilter = new List<GameStyleID>();

	private bool filterOpenRooms;

	private bool filterRanks;

	private bool filterFriends;

	public GameObject filterPanel;

	public Toggle allToggle;

	public Toggle[] gamestyleFilterToggles;

	public Toggle openRoomFilterToggle;

	public Toggle rankFilterToggle;

	public Toggle friendFilterToggle;

	public TMP_InputField roomNameFilterInput;

	public TMP_InputField mapNameFilterInput;

	private bool rankLoaded;

	public Sprite[] rankImagesLarge;

	public Image currentRankImage;

	public Image currentRankProgress;

	public TMP_Text currentRankText;

	public UnlockItemListElement unlockItemListElementPrefab;

	public ScrollRect unlockItemListScrollrect;

	private List<UnlockItemListElement> activeunlockItemListElements = new List<UnlockItemListElement>();

	public MissionListElement missionListElementPrefab;

	private List<MissionListElement> activeMissionListElements = new List<MissionListElement>();

	private CallResult<LobbyCreated_t> OnLobbyCreatedCallResult;

	private CallResult<LobbyEnter_t> OnLobbyEnterCallResult;

	protected Callback<LobbyInvite_t> m_LobbyInvite;

	protected Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;

	public TMP_Text leaderboard_FFA_RankText;

	public TMP_Text leaderboard_TDM_RankText;

	public TMP_Text leaderboard_LTS_RankText;

	public TMP_Text leaderboard_Territory_RankText;

	public TMP_Text leaderboard_CTG_RankText;

	public TMP_Text leaderboard_TM_RankText;

	private int leaderboard_FFA_Rank;

	private int leaderboard_TDM_Rank;

	private int leaderboard_LTS_Rank;

	private int leaderboard_Territory_Rank;

	private int leaderboard_CTG_Rank;

	private int leaderboard_TM_Rank;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_FFA_Callresult;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_TDM_Callresult;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_LTS_Callresult;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_Territory_Callresult;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_CTG_Callresult;

	private CallResult<LeaderboardScoresDownloaded_t> OnLeaderBoardDownload_TM_Callresult;

	private bool updateWarningTriggered;

	internal static List<ulong> kickedByPlayer = new List<ulong>();

	private Coroutine roomWatchRoutine;

	private IEnumerator mapLoadRoutine;

	public void OnEnable()
	{
		OnLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
		OnLobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
		m_LobbyInvite = Callback<LobbyInvite_t>.Create(OnLobbyInvite);
		m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
		OnLeaderBoardDownload_FFA_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_FFA);
		OnLeaderBoardDownload_TDM_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_TDM);
		OnLeaderBoardDownload_LTS_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_LTS);
		OnLeaderBoardDownload_Territory_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_Territory);
		OnLeaderBoardDownload_CTG_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_CTG_);
		OnLeaderBoardDownload_TM_Callresult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderBoardDownloaded_TM);
	}

	public override bool Close(GUIManager.GUIScreen nextScreen, bool forceClose)
	{
		if (forceClose)
		{
			CloseInternal(nextScreen);
			return true;
		}
		if (GameManager.GetInstance() != null && nextScreen != GUIManager.GUIScreen.mapSelection && nextScreen != GUIManager.GUIScreen.squadPicker && nextScreen != GUIManager.GUIScreen.loadScreen && nextScreen != GUIManager.GUIScreen.lobby && nextScreen != GUIManager.GUIScreen.room && nextScreen != GUIManager.GUIScreen.ingame)
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.LeaveRoomWarning"), DialogType.YesNoMessage, delegate(bool res, object val)
			{
				if (res)
				{
					CloseInternal(nextScreen);
					DisconnectIfRequired(nextScreen);
					GUIManager.GetInstance().CloseDialog();
					GUIManager.GetInstance().OpenNextScreen(getScreenEnum(), nextScreen);
				}
			});
			return false;
		}
		DisconnectIfRequired(nextScreen);
		CloseInternal(nextScreen);
		return true;
	}

	public void DisconnectIfRequired(GUIManager.GUIScreen nextScreen)
	{
		bool flag = true;
		if (nextScreen == GUIManager.GUIScreen.loadScreen)
		{
			flag = false;
		}
		if (nextScreen == GUIManager.GUIScreen.mapSelection)
		{
			flag = false;
		}
		if (nextScreen == GUIManager.GUIScreen.squadPicker)
		{
			flag = false;
		}
		if (nextScreen == GUIManager.GUIScreen.room)
		{
			flag = false;
		}
		if (nextScreen == GUIManager.GUIScreen.lobby)
		{
			flag = false;
		}
		if (nextScreen == GUIManager.GUIScreen.ingame)
		{
			flag = false;
		}
		if (flag)
		{
			Debug.Log("RoomGUI Close() - Disconnecting, status: " + PhotonNetwork.connectionState);
			GameManager.Disconnect();
			GUIManager.GetInstance().CloseDialog();
		}
	}

	private void CloseInternal(GUIManager.GUIScreen nextScreen)
	{
		base.gameObject.SetActivePerf(active: false);
		frameUI.gameObject.SetActivePerf(active: false);
		frameUI.UpdateFrameButtons(nextScreen);
		AbstractUIScreen.ClearDynamicList(activeServerListItems);
		AbstractUIScreen.ClearDynamicList(activeunlockItemListElements);
		AbstractUIScreen.ClearDynamicList(activeMissionListElements);
		if (matchmakingRoutine != null)
		{
			StopCoroutine(matchmakingRoutine);
			matchmakingRoutine = null;
		}
		SaveFilterSettings();
	}

	public override GUIManager.GUIScreen getScreenEnum()
	{
		return GUIManager.GUIScreen.lobby;
	}

	public override void Open(GUIManager.GUIScreen lastScreen)
	{
		Debug.Log("LobbyUI Open");
		frameUI.gameObject.SetActivePerf(active: true);
		frameUI.UpdateFrameButtons(getScreenEnum());
		int num = PlayerPrefs.GetInt("PhotonRegion", 1);
		GUIManager.GetInstance().currentPhotonRegion = frameUI.availableRegions[(num >= 0 && num < frameUI.availableRegions.Length) ? num : 0];
		if (!PhotonNetwork.connected)
		{
			PhotonNetwork.sendRate = 10;
			PhotonNetwork.sendRateOnSerialize = 10;
			PhotonNetwork.MaxResendsBeforeDisconnect = 10;
			PhotonNetwork.QuickResends = 3;
			PhotonNetwork.NetworkStatisticsEnabled = true;
			PhotonNetwork.CrcCheckEnabled = true;
			PhotonNetwork.autoCleanUpPlayerObjects = false;
		}
		TryConnectToPhotonIfNecessary();
		base.gameObject.SetActivePerf(active: true);
		if (serverlistElementPrefab != null)
		{
			serverlistElementPrefab.gameObject.SetActivePerf(active: false);
		}
		if (unlockItemListElementPrefab != null)
		{
			unlockItemListElementPrefab.gameObject.SetActivePerf(active: false);
		}
		missionListElementPrefab.gameObject.SetActivePerf(active: false);
		noRoomsFilterLabel.SetActivePerf(active: false);
		if (hostDialog != null)
		{
			hostDialog.gameObject.SetActivePerf(active: false);
		}
		filterPanel.SetActivePerf(active: false);
		selectedServer = null;
		rankLoaded = false;
		connecting = false;
		connectingTo = null;
		GUIManager.GetInstance().SetCursor(GameCursor.Normal);
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		GUIManager.GetInstance().ResumeMenuMusic();
		sortingStates = new SortState[sortingButtons.Count];
		sortingStates[1] = SortState.Ascending;
		gameStyleFilter = Utilities.StringDeSerializeObject<List<GameStyleID>>(PlayerPrefs.GetString("gameStyleFilter"));
		if (gameStyleFilter == null)
		{
			gameStyleFilter = new List<GameStyleID>();
		}
		filterOpenRooms = PlayerPrefs.GetInt("filterOpenRooms", 0) > 0;
		filterRanks = PlayerPrefs.GetInt("filterRanks", 0) > 0;
		filterFriends = PlayerPrefs.GetInt("filterFriends", 0) > 0;
		roomNameFilterInput.text = "";
		mapNameFilterInput.text = "";
		int defaultValue = -1;
		selectedModesMask = PlayerPrefs.GetInt("MatchmakingModeMask", defaultValue);
		if (!PlayerPrefs.HasKey("TerritoryUpdateFilterReset"))
		{
			selectedModesMask = -1;
			PlayerPrefs.SetString("TerritoryUpdateFilterReset", "");
			PlayerPrefs.Save();
		}
		if (!SteamManager.Initialized)
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Generic.SteamMissing"), DialogType.BlockingOKOnlyMessage, delegate
			{
				GUIManager.GetInstance().QuitGame();
			});
		}
		leaderboard_FFA_Rank = -1;
		leaderboard_TDM_Rank = -1;
		leaderboard_LTS_Rank = -1;
		leaderboard_Territory_Rank = -1;
		leaderboard_CTG_Rank = -1;
		leaderboard_TM_Rank = -1;
		GetLeaderboardRanks();
		ScriptEngine.debug = false;
	}

	private void SaveFilterSettings()
	{
		PlayerPrefs.SetString("gameStyleFilter", Utilities.StringSerializeObject(gameStyleFilter));
		PlayerPrefs.SetInt("filterOpenRooms", filterOpenRooms ? 1 : 0);
		PlayerPrefs.SetInt("filterRanks", filterRanks ? 1 : 0);
		PlayerPrefs.SetInt("filterFriends", filterFriends ? 1 : 0);
		PlayerPrefs.Save();
	}

	public void SelectServer(ServerListElement server)
	{
		bool flag = selectedServer == server.roomInfo;
		selectedServer = server.roomInfo;
		serverSelection = filteredServerList.IndexOf(server.roomInfo);
		for (int i = 0; i < activeServerListItems.Count; i++)
		{
			activeServerListItems[i].UpdateUI(activeServerListItems[i].roomInfo, serverSelection == i);
		}
		if (flag && !connecting && Time.time - lastClickTime < 0.3f)
		{
			JoinSelectedServer();
		}
		lastClickTime = Time.time;
	}

	public void JoinSelectedOnClick()
	{
		if (!connecting)
		{
			JoinSelectedServer();
		}
	}

	public static void TryConnectToPhotonIfNecessary()
	{
		if (IsOfflineMode())
		{
			PhotonNetwork.offlineMode = true;
		}
		else if (!PhotonNetwork.connected && !PhotonNetwork.connecting && !PhotonNetwork.offlineMode && SteamManager.Initialized)
		{
			// Check for custom server IP in INI file
			string customServerIP = GetCustomServerIP();
			int customServerPort = GetCustomServerPort();
			
			if (!string.IsNullOrEmpty(customServerIP))
			{
				// Connect to custom self-hosted Photon server
				ConnectToCustomServer(customServerIP, customServerPort);
				return;
			}
			
			int num = Array.IndexOf(GUIManager.GetInstance().lobby.frameUI.availableRegions, GUIManager.GetInstance().currentPhotonRegion);
			if (num == -1)
			{
				num = 0;
				GUIManager.GetInstance().currentPhotonRegion = GUIManager.GetInstance().lobby.frameUI.availableRegions[0];
			}
			PlayerPrefs.SetInt("PhotonRegion", num);
			Debug.Log("Connecting to Photon " + GUIManager.GetInstance().currentPhotonRegion);
			string pchName = "";
			if (!SteamApps.GetCurrentBetaName(out pchName, 256))
			{
				pchName = "stable";
			}
			if (Application.isEditor)
			{
				pchName = "bleeding";
			}
			Debug.Log("branch: " + pchName);
			PhotonNetwork.SwitchToProtocol(PhotonNetwork.PhotonServerSettings.Protocol);
			PhotonNetwork.ConnectToRegion(GUIManager.GetInstance().currentPhotonRegion, pchName);
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
			if (SteamManager.Initialized)
			{
				hashtable.Add("SteamID", (long)SteamUser.GetSteamID().m_SteamID);
			}
			PhotonNetwork.player.SetCustomProperties(hashtable);
			if (SteamManager.Initialized)
			{
				PhotonNetwork.player.NickName = SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID());
			}
		}
	}

	private static string GetCustomServerIP()
	{
		try
		{
			string iniPath = Path.Combine(Application.dataPath, "..", "CustomServer.ini");
			
			if (File.Exists(iniPath))
			{
				IniParser ini = new IniParser();
				ini.Load(iniPath);
				
				string serverIP = ini.GetValue("Server", "IP", "");
				
				if (!string.IsNullOrEmpty(serverIP))
				{
					Debug.Log("Custom server IP configured: " + serverIP);
					return serverIP;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Error reading custom server config: " + ex.Message);
		}
		
		return string.Empty;
	}

	private static int GetCustomServerPort()
	{
		try
		{
			string iniPath = Path.Combine(Application.dataPath, "..", "CustomServer.ini");
			
			if (File.Exists(iniPath))
			{
				IniParser ini = new IniParser();
				ini.Load(iniPath);
				
				return ini.GetIntValue("Server", "Port", 4530);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Error reading custom server port config: " + ex.Message);
		}
		
		return 5055; // Default Photon master server port
	}

	private static ConnectionProtocol GetCustomServerProtocol()
	{
		try
		{
			string iniPath = Path.Combine(Application.dataPath, "..", "CustomServer.ini");
			
			if (File.Exists(iniPath))
			{
				return ConnectionProtocol.Tcp;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Error reading custom server protocol config: " + ex.Message);
		}
		
		return ConnectionProtocol.Tcp; // Default to TCP
	}

	private static void ConnectToCustomServer(string serverIP, int serverPort)
	{
		Debug.Log("Connecting to custom Photon server: " + serverIP + ":" + serverPort);
		
		// Get the protocol from config
		ConnectionProtocol protocol = GetCustomServerProtocol();
		Debug.Log("Using protocol: " + protocol);
		
		PhotonNetwork.SwitchToProtocol(protocol);
		
		string appID = PhotonNetwork.PhotonServerSettings.AppID;
		string gameVersion = PhotonNetwork.gameVersion;
		
		// Connect to the custom master server
		PhotonNetwork.ConnectToMaster(serverIP, serverPort, appID, gameVersion);
		
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		if (SteamManager.Initialized)
		{
			hashtable.Add("SteamID", (long)SteamUser.GetSteamID().m_SteamID);
		}
		PhotonNetwork.player.SetCustomProperties(hashtable);
		if (SteamManager.Initialized)
		{
			PhotonNetwork.player.NickName = SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID());
		}
	}

	private void OnFailedToConnectToPhoton(DisconnectCause info)
	{
		Debug.Log("Could not connect to Photon server: " + info);
		GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.PhotonConnectError"), DialogType.YesNoMessage, delegate(bool res, object val)
		{
			if (res)
			{
				TryConnectToPhotonIfNecessary();
			}
		});
	}

	private void OnConnectionFail()
	{
		Debug.Log("OnConnectionFail");
		if (matchmakingRoutine != null)
		{
			CancelMatchmaking();
		}
	}

	private static bool IsOfflineMode()
	{
		return Array.IndexOf(Environment.GetCommandLineArgs(), "+OfflineMode") > -1;
	}

	public void OnConnectedToPhoton()
	{
		Debug.Log("Photon connected");
		SupportClass.StartBackgroundCalls(FallbackSendAckThread);
	}

	public static bool FallbackSendAckThread()
	{
		if (PhotonNetwork.connected && !PhotonNetwork.offlineMode)
		{
			PhotonNetwork.networkingPeer.SendAcksOnly();
		}
		return true;
	}

	private void ClearServerData()
	{
		new List<List<Ping>>().AddRange(serverPings.Values);
		serverPings.Clear();
		netServerList = new RoomInfo[0];
		filteredServerList = new List<RoomInfo>();
	}

	public void Update()
	{
		if (GUIManager.GetInstance().GetCurrentScreen() == GUIManager.GUIScreen.lobby && PhotonNetwork.connected && Time.time > lastRefreshTime + 1f)
		{
			lastRefreshTime = Time.time;
			netServerList = PhotonNetwork.GetRoomList();
			if (showLobbies)
			{
				UpdateServerList();
			}
			noRoomsFilterLabel.SetActivePerf(showLobbies && PhotonNetwork.connectedAndReady && netServerList.Length != 0 && filteredServerList.Count == 0 && PhotonNetwork.room == null);
			for (int i = 0; i < netServerList.Length; i++)
			{
				if (!updateWarningTriggered && !Application.isEditor && SettingsHolder.currentRevision < (int)netServerList[i].CustomProperties["revision"])
				{
					updateWarningTriggered = true;
					GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Generic.SteamUpdateRequired"), DialogType.BlockingOKOnlyMessage, delegate
					{
						GUIManager.GetInstance().QuitGame();
					});
					break;
				}
			}
		}
		if (showLobbies)
		{
			matchMakingPanel.gameObject.SetActivePerf(active: false);
			serverListScrollrect.gameObject.SetActivePerf(active: true);
			hostRoomButton.gameObject.SetActivePerf(PhotonNetwork.room == null && !connecting);
			joinRoomButton.gameObject.SetActivePerf(PhotonNetwork.room == null && !connecting && selectedServer != null);
			filterRoomButton.gameObject.SetActivePerf(PhotonNetwork.room == null && !connecting);
			serverListHeader.gameObject.SetActivePerf(active: true);
			if (filterPanel.activeInHierarchy)
			{
				for (int num = 0; num < gamestyleFilterToggles.Length; num++)
				{
					gamestyleFilterToggles[num].isOn = gameStyleFilter.Count == 0 || gameStyleFilter.Contains((GameStyleID)num);
				}
				allToggle.isOn = gameStyleFilter.Count == 0;
				openRoomFilterToggle.isOn = filterOpenRooms;
				rankFilterToggle.isOn = filterRanks;
				friendFilterToggle.isOn = filterFriends;
			}
		}
		else
		{
			noRoomsFilterLabel.gameObject.SetActivePerf(active: false);
			joinRoomButton.gameObject.SetActivePerf(active: false);
			filterRoomButton.gameObject.SetActivePerf(active: false);
			serverListScrollrect.gameObject.SetActivePerf(active: false);
			serverListHeader.gameObject.SetActivePerf(active: false);
			matchMakingPanel.gameObject.SetActivePerf(active: true);
			startMatchMakingButton.gameObject.SetActivePerf(matchmakingRoutine == null);
			cancelMatchMakingButton.gameObject.SetActivePerf(!startMatchMakingButton.activeInHierarchy);
			matchmakingTimerText.gameObject.SetActivePerf(!startMatchMakingButton.activeInHierarchy);
			matchmakingTimerText.text = Texts.GetText("UI.Lobby.MatchmakingTimerLabel").Replace("{TIMELEFT}", Utilities.SecToTime((int)matchMakingTimer));
			Color color = matchmakingTimerText.color;
			color.a = Mathf.Cos(matchmakeLabelFlashTimer * MathF.PI);
			matchmakingTimerText.color = color;
			if (matchmakeLabelFlashTimer > 0f)
			{
				matchmakeLabelFlashTimer -= Time.deltaTime * 3f;
			}
			else
			{
				matchmakeLabelFlashTimer = 0f;
			}
			selectedMatchmakingModeImage.sprite = modeImages[(int)shownGameModeInfo];
			selectedMatchmakingModeDescription.text = Texts.GetText("UI.MapPicker.GameModeDescriptions." + shownGameModeInfo);
			selectedMatchmakingMode.text = Texts.GetText("UI.Generic.GameStyleID." + shownGameModeInfo).ToUpper();
			ffaToggle.isOn = (selectedModesMask & 2) > 0;
			tdmToggle.isOn = (selectedModesMask & 4) > 0;
			ltsToggle.isOn = (selectedModesMask & 0x80) > 0;
			ctfToggle.isOn = (selectedModesMask & 8) > 0;
			coopAssaultToggle.isOn = (selectedModesMask & 0x10) > 0;
			territoryToggle.isOn = (selectedModesMask & 0x40) > 0;
			customMapToggle.isOn = (selectedModesMask & 1) > 0;
			ffaToggle.interactive = matchmakingRoutine == null;
			tdmToggle.interactive = matchmakingRoutine == null;
			ltsToggle.interactive = matchmakingRoutine == null;
			ctfToggle.interactive = matchmakingRoutine == null;
			coopAssaultToggle.interactive = matchmakingRoutine == null;
			territoryToggle.interactive = matchmakingRoutine == null;
			customMapToggle.interactive = matchmakingRoutine == null;
			ffaToggle.UpdateUI();
			tdmToggle.UpdateUI();
			ltsToggle.UpdateUI();
			ctfToggle.UpdateUI();
			coopAssaultToggle.UpdateUI();
			territoryToggle.UpdateUI();
			customMapToggle.UpdateUI();
		}
		matchMakingBulb.SetOnState(!showLobbies);
		matchMakingswitch.SetOnState(!showLobbies);
		roomListBulb.SetOnState(showLobbies);
		if (!rankLoaded && SteamAchievements.GetInstance().statsValid)
		{
			AssetDB.LoadItemPrefabs();
			rankLoaded = true;
			int num2 = (int)SteamAchievements.GetInstance().GetSteamStatValue(SteamStatID.Rank);
			currentRankImage.sprite = rankImagesLarge[Mathf.Clamp(num2, 0, 19)];
			if (num2 >= 19)
			{
				currentRankText.text = Texts.GetText("UI.Generic.RankNames.Rank_19").Replace("{RANK}", (num2 + 1).ToString());
			}
			else
			{
				currentRankText.text = Texts.GetText("UI.Generic.RankNames.Rank_" + num2);
			}
			int num3 = (int)SteamAchievements.GetInstance().GetSteamStatValue(SteamStatID.Progression);
			int levelUpCost = AssetDB.GetInstance().balanceData.GetLevelUpCost(num2);
			currentRankProgress.fillAmount = Mathf.InverseLerp(0f, levelUpCost, num3);
			List<Item> list = new List<Item>();
			ItemRef[] progressionUnlockItems = AssetDB.GetInstance().balanceData.progressionUnlockItems;
			foreach (ItemRef itemRef in progressionUnlockItems)
			{
				list.Add(Utilities.LoadItem(itemRef.itemID, isLiveGame: false));
			}
			AbstractUIScreen.UpdateDynamicList(unlockItemListElementPrefab, activeunlockItemListElements, list, (Item)null);
			LayoutRebuilder.ForceRebuildLayoutImmediate(unlockItemListElementPrefab.transform.parent as RectTransform);
			if (num2 > 2)
			{
				UpdateScrollToSelectedY(unlockItemListScrollrect, activeunlockItemListElements[Mathf.Clamp(num2 - 1, 0, 18)].transform as RectTransform);
			}
			if (num2 >= 18)
			{
				unlockItemListScrollrect.verticalNormalizedPosition = 0f;
			}
		}
		AbstractUIScreen.UpdateDynamicList(missionListElementPrefab, activeMissionListElements, MissionManager.GetInstance().GetCurrentMissions(), (MissionData)null);
		leaderboard_FFA_RankText.text = ((leaderboard_FFA_Rank > 0) ? leaderboard_FFA_Rank.ToString() : ((leaderboard_FFA_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
		leaderboard_TDM_RankText.text = ((leaderboard_TDM_Rank > 0) ? leaderboard_TDM_Rank.ToString() : ((leaderboard_TDM_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
		leaderboard_LTS_RankText.text = ((leaderboard_LTS_Rank > 0) ? leaderboard_LTS_Rank.ToString() : ((leaderboard_LTS_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
		leaderboard_Territory_RankText.text = ((leaderboard_Territory_Rank > 0) ? leaderboard_Territory_Rank.ToString() : ((leaderboard_Territory_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
		leaderboard_CTG_RankText.text = ((leaderboard_CTG_Rank > 0) ? leaderboard_CTG_Rank.ToString() : ((leaderboard_CTG_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
		leaderboard_TM_RankText.text = ((leaderboard_TM_Rank > 0) ? leaderboard_TM_Rank.ToString() : ((leaderboard_TM_Rank == -1) ? "?" : Texts.GetText("UI.Lobby.LeaderboardUnranked")));
	}

	public void SetSteamStatus(string status)
	{
		if (SteamManager.Initialized)
		{
			if (status == "online")
			{
				SteamFriends.SetRichPresence("status", "In the main lobby.");
			}
			if (status == "ingame")
			{
				SteamFriends.SetRichPresence("status", "Is playing.");
			}
		}
	}

	private void UpdateServerList()
	{
		serverSelection = -1;
		filteredServerList = SortAndFilter();
		for (int i = 0; i < filteredServerList.Count; i++)
		{
			if (selectedServer != null && selectedServer.Name == filteredServerList[i].Name)
			{
				serverSelection = i;
				selectedServer = filteredServerList[i];
			}
		}
		if (serverSelection == -1)
		{
			selectedServer = null;
		}
		AbstractUIScreen.UpdateDynamicList(serverlistElementPrefab, activeServerListItems, filteredServerList, selectedServer);
		if (serverSelection > -1)
		{
			ScrollViewPortToElement(serverListScrollrect, activeServerListItems[serverSelection].transform);
		}
	}

	public List<RoomInfo> SortAndFilter()
	{
		List<RoomInfo> list = new List<RoomInfo>(netServerList);
		if (sortingStates[0] != SortState.Off)
		{
			list.Sort((RoomInfo x, RoomInfo y) => (sortingStates[0] == SortState.Ascending) ? ((GameState)x.CustomProperties["state"]/*cast due to .constrained prefix*/).CompareTo((GameState)y.CustomProperties["state"]) : ((GameState)y.CustomProperties["state"]/*cast due to .constrained prefix*/).CompareTo((GameState)x.CustomProperties["state"]));
		}
		if (sortingStates[1] != SortState.Off)
		{
			list.Sort((RoomInfo x, RoomInfo y) => (sortingStates[1] == SortState.Ascending) ? ((string)x.CustomProperties["name"]).CompareTo(y.CustomProperties["name"]) : ((string)y.CustomProperties["name"]).CompareTo(x.CustomProperties["name"]));
		}
		if (sortingStates[2] != SortState.Off)
		{
			list.Sort((RoomInfo x, RoomInfo y) => (sortingStates[2] == SortState.Ascending) ? ((GameStyleID)x.CustomProperties["gameStyle"]/*cast due to .constrained prefix*/).CompareTo((GameStyleID)y.CustomProperties["gameStyle"]) : ((GameStyleID)y.CustomProperties["gameStyle"]/*cast due to .constrained prefix*/).CompareTo((GameStyleID)x.CustomProperties["gameStyle"]));
		}
		if (sortingStates[3] != SortState.Off)
		{
			list.Sort((RoomInfo x, RoomInfo y) => (sortingStates[3] == SortState.Ascending) ? x.PlayerCount.CompareTo(y.PlayerCount) : y.PlayerCount.CompareTo(x.PlayerCount));
		}
		List<CSteamID> list2 = new List<CSteamID>();
		if (filterFriends)
		{
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			for (int num = 0; num < friendCount; num++)
			{
				if (SteamFriends.GetFriendGamePlayed(SteamFriends.GetFriendByIndex(num, EFriendFlags.k_EFriendFlagImmediate), out var pFriendGameInfo) && pFriendGameInfo.m_gameID.AppID() == SteamUtils.GetAppID() && pFriendGameInfo.m_steamIDLobby.IsValid())
				{
					list2.Add(pFriendGameInfo.m_steamIDLobby);
				}
			}
		}
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			if (roomNameFilter != null && roomNameFilter.Length != 0)
			{
				bool flag = false;
				for (int num3 = 0; num3 < roomNameFilter.Length; num3++)
				{
					flag |= ((string)list[num2].CustomProperties["name"]).IndexOf(roomNameFilter[num2], StringComparison.InvariantCultureIgnoreCase) > -1;
				}
				if (!flag)
				{
					list.RemoveAt(num2);
					num2--;
					continue;
				}
			}
			if (mapNameFilter != null && mapNameFilter.Length != 0)
			{
				bool flag2 = false;
				for (int num4 = 0; num4 < mapNameFilter.Length; num4++)
				{
					flag2 |= ((string)list[num2].CustomProperties["mapName"]).IndexOf(mapNameFilter[num2], StringComparison.InvariantCultureIgnoreCase) > -1;
				}
				if (!flag2)
				{
					list.RemoveAt(num2);
					num2--;
					continue;
				}
			}
			if (gameStyleFilter != null && gameStyleFilter.Count > 0 && !gameStyleFilter.Contains((GameStyleID)list[num2].CustomProperties["gameStyle"]))
			{
				list.RemoveAt(num2);
				num2--;
			}
			else if (filterOpenRooms && (GameState)list[num2].CustomProperties["state"] != GameState.InLobby && !(bool)list[num2].CustomProperties["lateJoinAllowed"])
			{
				list.RemoveAt(num2);
				num2--;
			}
			else if (filterFriends && !list2.Contains((CSteamID)(ulong)(long)list[num2].CustomProperties["SteamLobbyID"]))
			{
				list.RemoveAt(num2);
				num2--;
			}
		}
		return list;
	}

	public void HostServer()
	{
		Debug.Log("Host");
		GameManager.lateJoining = false;
		LoadingScreenUI.isSP = false;
		if (!PhotonNetwork.connected)
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.PhotonNotConnected"), DialogType.OkMessage, null);
			return;
		}
		if (matchmakingRoutine == null)
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.HostingPopup"), DialogType.BlockingMessage, null);
			serverName = hostDialog.roomName.text;
			serverComment = hostDialog.comment.text;
			hiddenRoom = hostDialog.hiddenRoom.isOn;
			int.TryParse(hostDialog.maxPlayers.text, out maxPlayers);
			maxPlayers = Mathf.Clamp(maxPlayers, 1, 12);
			PlayerPrefs.SetString("LastServerName", hostDialog.roomName.text);
			PlayerPrefs.SetString("LastServerComment", hostDialog.comment.text);
			PlayerPrefs.SetString("LastServerPlayerLimit", hostDialog.maxPlayers.text);
			PlayerPrefs.SetInt("LastServerHidden", hostDialog.hiddenRoom.isOn ? 1 : 0);
			PlayerPrefs.Save();
		}
		else
		{
			serverName = Texts.GetText("UI.Lobby.DefaultRoomName", "My room, come and play!");
			serverComment = "";
			hiddenRoom = false;
			maxPlayers = 12;
		}
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = (byte)maxPlayers;
		roomOptions.IsVisible = false;
		roomOptions.CustomRoomPropertiesForLobby = new string[13]
		{
			"name", "comment", "state", "hostname", "hostID", "mapName", "mapID", "gameStyle", "lateJoinAllowed", "ping",
			"customSettings", "SteamLobbyID", "revision"
		};
		if (!PhotonNetwork.CreateRoom("", roomOptions, TypedLobby.Default))
		{
			GUIManager.GetInstance().OpenMessageDialog("Could not create room, Photon network not ready. Re-establishing connection\n Try joining again, or restart application if the problem persists!", DialogType.BlockingOKOnlyMessage, null);
			connecting = false;
			connectingTo = null;
			CancelMatchmaking();
			FrameUI.reconnectToPhoton = true;
			frameUI.ChangePhotonRegion(Array.IndexOf(frameUI.availableRegions, GUIManager.GetInstance().currentPhotonRegion));
		}
		hostDialog.gameObject.SetActivePerf(active: false);
	}

	public void OnCreatedRoom()
	{
		GUIManager.GetInstance().CloseDialog();
		if (base.enabled && GUIManager.GetInstance().GetCurrentScreen() == GUIManager.GUIScreen.lobby)
		{
			Debug.Log("GUI: Server initialized");
			PhotonNetwork.Instantiate("GameManager", Vector3.zero, Quaternion.identity, 0);
			GameManager instance = GameManager.GetInstance();
			instance.serverName = serverName;
			instance.serverComment = serverComment;
			instance.hiddenRoom = hiddenRoom;
			if (matchmakingRoutine == null)
			{
				GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.room);
			}
			SteamAPICall_t hAPICall = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
			OnLobbyCreatedCallResult.Set(hAPICall);
		}
	}

	private void OnPhotonCreateRoomFailed(object[] codeAndMsg)
	{
		if (base.enabled)
		{
			Debug.Log("GUI: host fail callback");
			if (connecting)
			{
				GUIManager.GetInstance().ShowMessage("Could not create room:\n" + codeAndMsg[1].ToString());
			}
		}
	}

	private void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
	{
		CSteamID cSteamID = (CSteamID)pCallback.m_ulSteamIDLobby;
		if (PhotonNetwork.room != null)
		{
			GameManager.lobbyID = cSteamID;
			SteamMatchmaking.SetLobbyData(cSteamID, "PhotonRoomRegion", GUIManager.GetInstance().currentPhotonRegion.ToString());
			SteamMatchmaking.SetLobbyData(cSteamID, "PhotonRoomID", PhotonNetwork.room.Name);
			SteamMatchmaking.SetLobbyData(cSteamID, "state", 0.ToString() ?? "");
			if (GameManager.GetInstance() != null)
			{
				GameManager.GetInstance().UpdateServerInfo();
			}
			Debug.Log("Steam lobby created");
		}
		else
		{
			Debug.LogError("Joined a lobby but room is null!");
			SteamMatchmaking.LeaveLobby(cSteamID);
		}
	}

	public void JoinRoombyID(CSteamID lobbyID)
	{
		SteamAPICall_t hAPICall = SteamMatchmaking.JoinLobby(lobbyID);
		OnLobbyEnterCallResult.Set(hAPICall);
	}

	private void JoinSelectedServer()
	{
		if (selectedServer == null)
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.NoserverSelected"), DialogType.OkMessage, null);
		}
		else if (SettingsHolder.currentRevision < (int)selectedServer.CustomProperties["revision"])
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.CantJoinUpdateRequired"), DialogType.OkMessage, null);
		}
		else if (SettingsHolder.currentRevision > (int)selectedServer.CustomProperties["revision"])
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.CantJoinOldVersion"), DialogType.OkMessage, null);
		}
		else if ((GameState)selectedServer.CustomProperties["state"] == GameState.InLobby || (bool)selectedServer.CustomProperties["lateJoinAllowed"])
		{
			_ = (GameState)selectedServer.CustomProperties["state"];
			JoinOrLateJoin(selectedServer);
		}
		else
		{
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.RoomAlreadyLaunched"), DialogType.OkMessage, null);
		}
	}

	private void JoinOrLateJoin(RoomInfo server)
	{
		Debug.Log("JoinOrLateJoin");
		if (roomWatchRoutine != null)
		{
			GUIManager.GetInstance().StopCoroutine(roomWatchRoutine);
			roomWatchRoutine = null;
		}
		GameState gameState = (GameState)server.CustomProperties["state"];
		if (gameState == GameState.InLobby || gameState == GameState.GameOver)
		{
			GameManager.lateJoining = false;
			NetConnect(server);
			return;
		}
		ulong num = (ulong)((server.CustomProperties["mapID"] == null) ? 0 : ((long)server.CustomProperties["mapID"]));
		if (num == 0L)
		{
			return;
		}
		GameManager.lateJoining = true;
		GameManager.lateJoinSynching = true;
		if (GUIManager.GetInstance().mapCache.IsUGCMapAvailable(num) || num < MapCache.OFFICIAL_MAP_THRESHOLD)
		{
			LoadingScreenUI.isSP = false;
			GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.loadScreen);
			lateJoinMapData = GUIManager.GetInstance().mapCache.ReadMapData((num < MapCache.OFFICIAL_MAP_THRESHOLD) ? MapDataLocation.OfficialMaps : MapDataLocation.UGC, num, "");
			LoadingScreenUI.message = Texts.GetText("UI.LoadingScreen.LateJoin_LoadMap");
			GUIManager.GetInstance().mapCache.OpenMap(lateJoinMapData, isLiveGame: true, delegate
			{
				GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.lobby);
				NetConnect(server);
			});
			roomWatchRoutine = GUIManager.GetInstance().StartCoroutine(RoomWatchRoutine(server));
			return;
		}
		mapLoadRoutine = GUIManager.GetInstance().mapCache.DownloadMap((ulong)(long)selectedServer.CustomProperties["mapID"], delegate(MapData map)
		{
			if (map == null)
			{
				GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.MapNotAvailableError"), DialogType.OkMessage, null);
			}
			else
			{
				JoinOrLateJoin(server);
			}
		});
		StartCoroutine(mapLoadRoutine);
		GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.LateJoinMapDownload"), DialogType.BlockingCancelOnlyMessage, delegate
		{
			StopCoroutine(mapLoadRoutine);
			mapLoadRoutine = null;
			FrameUI.reconnectToPhoton = true;
			PhotonNetwork.Disconnect();
			connecting = false;
			connectingTo = null;
			GameManager.lateJoining = false;
		});
	}

	private void NetConnect(RoomInfo server)
	{
		if (roomWatchRoutine != null)
		{
			GUIManager.GetInstance().StopCoroutine(roomWatchRoutine);
			roomWatchRoutine = null;
		}
		connecting = true;
		connectingTo = server;
		if (!PhotonNetwork.JoinRoom(connectingTo.Name))
		{
			GUIManager.GetInstance().OpenMessageDialog("Could not join room, Photon network not ready. Re-establishing connection\n Try joining again, or restart application if the problem persists!", DialogType.BlockingOKOnlyMessage, null);
			connecting = false;
			connectingTo = null;
			FrameUI.reconnectToPhoton = true;
			frameUI.ChangePhotonRegion(Array.IndexOf(frameUI.availableRegions, GUIManager.GetInstance().currentPhotonRegion));
			return;
		}
		GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.Connecting"), DialogType.BlockingCancelOnlyMessage, delegate
		{
			FrameUI.reconnectToPhoton = true;
			PhotonNetwork.Disconnect();
			connecting = false;
			connectingTo = null;
			GameManager.lateJoining = false;
			lateJoinMapData = null;
		});
	}

	public void OnPhotonJoinRoomFailed(object[] codeAndMsg)
	{
		Debug.Log("GUI: Connect fail callback");
		GUIManager.GetInstance().CloseDialog();
		if (matchmakingRoutine == null)
		{
			if (connecting)
			{
				GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.lobby);
				GUIManager.GetInstance().ShowMessage("Could not join room:\n" + codeAndMsg[1].ToString());
				if (GameManager.lobbyID != CSteamID.Nil)
				{
					SteamMatchmaking.LeaveLobby(GameManager.lobbyID);
					GameManager.lobbyID = CSteamID.Nil;
				}
			}
		}
		else
		{
			failedRooms.Add(connectingTo);
		}
		connecting = false;
		connectingTo = null;
		GameManager.lateJoining = false;
	}

	public void OnJoinedRoom()
	{
		Debug.Log("GUI: Connect callback");
		if (PhotonNetwork.isNonMasterClientInRoom && GameManager.lobbyID == CSteamID.Nil && PhotonNetwork.room.CustomProperties["SteamLobbyID"] != null)
		{
			SteamAPICall_t hAPICall = SteamMatchmaking.JoinLobby((CSteamID)(ulong)(long)PhotonNetwork.room.CustomProperties["SteamLobbyID"]);
			OnLobbyEnterCallResult.Set(hAPICall);
		}
		connecting = false;
		GUIManager.GetInstance().CloseDialog();
	}

	private void OnLobbyEnter(LobbyEnter_t pCallback, bool bIOFailure)
	{
		GameManager.lobbyID = (CSteamID)pCallback.m_ulSteamIDLobby;
		Debug.Log("Steam lobby joined");
		if (PhotonNetwork.room == null)
		{
			string lobbyData = SteamMatchmaking.GetLobbyData((CSteamID)pCallback.m_ulSteamIDLobby, "PhotonRoomRegion");
			string lobbyData2 = SteamMatchmaking.GetLobbyData((CSteamID)pCallback.m_ulSteamIDLobby, "PhotonRoomID");
			GameState gameState = (GameState)int.Parse(SteamMatchmaking.GetLobbyData((CSteamID)pCallback.m_ulSteamIDLobby, "state"));
			long num = long.Parse(SteamMatchmaking.GetLobbyData((CSteamID)pCallback.m_ulSteamIDLobby, "mapID"));
			bool flag = bool.Parse(SteamMatchmaking.GetLobbyData((CSteamID)pCallback.m_ulSteamIDLobby, "lateJoinAllowed"));
			if (lobbyData != GUIManager.GetInstance().currentPhotonRegion.ToString())
			{
				GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.WrongRegion").Replace("{REGION}", lobbyData), DialogType.OkMessage, null);
				SteamMatchmaking.LeaveLobby((CSteamID)pCallback.m_ulSteamIDLobby);
				GameManager.lobbyID = CSteamID.Nil;
			}
			else if (gameState == GameState.InLobby || flag)
			{
				ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
				hashtable.Add("state", gameState);
				hashtable.Add("mapID", num);
				hashtable.Add("lateJoinAllowed", flag);
				RoomInfo server = new RoomInfo(lobbyData2, hashtable);
				JoinOrLateJoin(server);
			}
			else
			{
				GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.RoomAlreadyLaunched"), DialogType.OkMessage, null);
				SteamMatchmaking.LeaveLobby((CSteamID)pCallback.m_ulSteamIDLobby);
				GameManager.lobbyID = CSteamID.Nil;
			}
		}
	}

	public void Kicked()
	{
		connecting = false;
		connectingTo = null;
	}

	private IEnumerator RoomWatchRoutine(RoomInfo server)
	{
		RoomInfo roomInfo;
		while (true)
		{
			Debug.Log("RoomWatchRoutine fetching update");
			roomInfo = null;
			RoomInfo[] roomList = PhotonNetwork.GetRoomList();
			for (int i = 0; i < roomList.Length; i++)
			{
				if (roomList[i].Name.Equals(server.Name))
				{
					roomInfo = roomList[i];
					break;
				}
			}
			if (roomInfo != null)
			{
				GameState gameState = (GameState)roomInfo.CustomProperties["state"];
				Debug.Log("LateJoin server state: " + gameState);
				if (gameState == GameState.InLobby)
				{
					Debug.Log("RoomWatchRoutine match ended");
					GUIManager.GetInstance().mapCache.CancelOpenMap();
					if (mapLoadRoutine != null)
					{
						StopCoroutine(mapLoadRoutine);
						mapLoadRoutine = null;
					}
					SceneManager.LoadSceneAsync("Menu");
					GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.lobby);
					roomWatchRoutine = null;
					JoinOrLateJoin(roomInfo);
					yield break;
				}
				if ((long)server.CustomProperties["mapID"] != (long)roomInfo.CustomProperties["mapID"])
				{
					break;
				}
			}
			yield return new WaitForSecondsRealtime(1f);
		}
		Debug.Log("RoomWatchRoutine map changed");
		GUIManager.GetInstance().mapCache.CancelOpenMap();
		if (mapLoadRoutine != null)
		{
			StopCoroutine(mapLoadRoutine);
			mapLoadRoutine = null;
		}
		SceneManager.LoadSceneAsync("Menu");
		GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.lobby);
		roomWatchRoutine = null;
		JoinOrLateJoin(roomInfo);
	}

	public Color GetBlendedPingColor(int percentage)
	{
		if (percentage < 50)
		{
			return Color.Lerp(AbstractGUIScreen.brightGreen, Color.yellow, (float)percentage / 50f);
		}
		return Color.Lerp(Color.yellow, AbstractGUIScreen.brightRed, (float)(percentage - 50) / 50f);
	}

	public void ClearSortingStates()
	{
		for (int i = 0; i < sortingStates.Length; i++)
		{
			sortingStates[i] = SortState.Off;
		}
	}

	protected SortState RollSortState(SortState state, System.Action clearSortingButtons)
	{
		clearSortingButtons();
		switch (state)
		{
		case SortState.Off:
			state = SortState.Ascending;
			break;
		case SortState.Ascending:
			state = SortState.Descending;
			break;
		case SortState.Descending:
			state = SortState.Ascending;
			break;
		}
		return state;
	}

	public void SortBy(int index)
	{
		sortingStates[index] = RollSortState(sortingStates[index], ClearSortingStates);
		for (int i = 0; i < sortingButtons.Count; i++)
		{
			sortingButtons[i].sprite = sortingStateSprites[(int)sortingStates[i]];
		}
		UpdateServerList();
	}

	public void ShowRoom()
	{
		GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.room);
	}

	public void ToggleFilterPanel()
	{
		filterPanel.SetActivePerf(!filterPanel.activeInHierarchy);
		if (!filterPanel.activeInHierarchy)
		{
			SaveFilterSettings();
		}
	}

	public void ToggleGameStyleFilter(int style)
	{
		if (style == -1)
		{
			gameStyleFilter.Clear();
		}
		else if (gameStyleFilter.Contains((GameStyleID)style))
		{
			gameStyleFilter.Remove((GameStyleID)style);
		}
		else
		{
			gameStyleFilter.Add((GameStyleID)style);
		}
		UpdateServerList();
	}

	public void SetRoomNameFilter(string filterText)
	{
		roomNameFilter = filterText.Split(new char[2] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
		UpdateServerList();
	}

	public void SetMapNameFilter(string filterText)
	{
		mapNameFilter = filterText.Split(new char[2] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
		UpdateServerList();
	}

	public void ToggleOpenRoomFilter()
	{
		filterOpenRooms = !filterOpenRooms;
		UpdateServerList();
	}

	public void ToggleMatchingRankFilter()
	{
		filterRanks = !filterRanks;
		UpdateServerList();
	}

	public void ToggleFilterFriends()
	{
		filterFriends = !filterFriends;
		UpdateServerList();
	}

	private void OnLobbyInvite(LobbyInvite_t pCallback)
	{
		if (!base.gameObject.activeInHierarchy || GameManager.lobbyID == (CSteamID)pCallback.m_ulSteamIDLobby)
		{
			return;
		}
		GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.InvitedToRoom").Replace("{PLAYERNAME}", SteamFriends.GetFriendPersonaName((CSteamID)pCallback.m_ulSteamIDUser)), DialogType.YesNoMessage, delegate(bool res, object val)
		{
			if (res)
			{
				JoinRoombyID((CSteamID)pCallback.m_ulSteamIDLobby);
			}
		});
	}

	private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
	{
		if (base.gameObject.activeInHierarchy)
		{
			JoinRoombyID(pCallback.m_steamIDLobby);
		}
		else
		{
			GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Lobby.AlreadyInARoom"));
		}
	}

	public void OpenHostDialog()
	{
		if (PhotonNetwork.room == null)
		{
			hostDialog.gameObject.SetActivePerf(active: true);
			hostDialog.roomName.text = PlayerPrefs.GetString("LastServerName", Texts.GetText("UI.Lobby.DefaultRoomName", "My room, come and play!"));
			hostDialog.comment.text = PlayerPrefs.GetString("LastServerComment", hostDialog.comment.text);
			hostDialog.maxPlayers.text = PlayerPrefs.GetString("LastServerPlayerLimit", hostDialog.maxPlayers.text);
			hostDialog.hiddenRoom.isOn = PlayerPrefs.GetInt("LastServerHidden", 0) > 0;
			if (!PlayerPrefs.HasKey("ShownHostWarningAlready"))
			{
				PlayerPrefs.SetString("ShownHostWarningAlready", "");
				PlayerPrefs.Save();
				GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Room.FirstHostWarning"));
			}
			GameManager.matchMadeRoom = false;
		}
		else
		{
			GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Lobby.AlreadyInARoom"));
		}
		CancelMatchmaking();
	}

	public void ToggleMatchMaking()
	{
		showLobbies = !showLobbies;
		if (showLobbies)
		{
			CancelMatchmaking();
		}
		else
		{
			filterPanel.SetActivePerf(active: false);
			if (hostDialog != null)
			{
				hostDialog.gameObject.SetActivePerf(active: false);
			}
		}
		if (showLobbies && !PlayerPrefs.HasKey("ShownLobbiesWarningAlready"))
		{
			showLobbies = false;
			PlayerPrefs.SetString("ShownLobbiesWarningAlready", "");
			PlayerPrefs.Save();
			GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Lobby.ShowRoomsWarning"));
		}
	}

	public void ToggleMatchmakingMode(int gameModeIndex)
	{
		selectedModesMask ^= 1 << gameModeIndex;
		PlayerPrefs.SetInt("MatchmakingModeMask", selectedModesMask);
	}

	public void ShowMatchmakingModeInfo(int gameModeIndex)
	{
		shownGameModeInfo = (GameStyleID)gameModeIndex;
	}

	public void StartMatchmaking(float time)
	{
		if (PhotonNetwork.connected)
		{
			if (selectedModesMask != 0)
			{
				matchMakingTimer = time;
				matchmakingRoutine = this.StartThrowingCoroutine(MatchmakingRoutine(), delegate(Exception e)
				{
					Debug.LogException(e);
					CancelMatchmaking();
					StartMatchmaking(time);
				});
				GameManager.matchMadeRoom = true;
				showLobbies = false;
				matchmakeLabelFlashTimer = 6f;
				failedRooms.Clear();
			}
			else
			{
				GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Lobby.MatchmakingCannotStartNoModesSelected"));
			}
		}
		else
		{
			GUIManager.GetInstance().ShowMessage(Texts.GetText("UI.Lobby.MatchmakingCannotStartPhotonOffline"));
		}
	}

	public void CancelMatchmaking()
	{
		if (matchmakingRoutine != null)
		{
			StopCoroutine(matchmakingRoutine);
			matchmakingRoutine = null;
			GameManager.Disconnect();
		}
	}

	private IEnumerator MatchmakingRoutine()
	{
		Debug.Log("Searching for a match...");
		float num = matchMakingTimer;
		float halfTimer = num / 2f;
		yield return new WaitForSecondsRealtime(1.5f);
		while (matchMakingTimer > 0f)
		{
			if (halfTimer > 0f)
			{
				List<RoomInfo> list = (from x in new List<RoomInfo>(PhotonNetwork.GetRoomList())
					where x.PlayerCount < x.MaxPlayers && ((GameState)x.CustomProperties["state"] == GameState.InLobby || (bool)x.CustomProperties["lateJoinAllowed"]) && (selectedModesMask & (1 << (int)x.CustomProperties["gameStyle"])) > 0 && !failedRooms.Contains(x) && !kickedByPlayer.Contains((ulong)(long)x.CustomProperties["hostID"]) && x.CustomProperties["mapID"] != null && (long)x.CustomProperties["mapID"] != 0
					orderby x.PlayerCount
					select x).ToList();
				Debug.Log("Found " + list.Count + " matching rooms");
				if (!connecting && list.Count > 0)
				{
					try
					{
						JoinOrLateJoin(list[0]);
					}
					catch (Exception exception)
					{
						failedRooms.Add(list[0]);
						Debug.LogException(exception);
					}
				}
				if (PhotonNetwork.room != null)
				{
					break;
				}
				if (halfTimer <= 1f)
				{
					halfTimer = 0f;
					Debug.Log("No matches, self hosting");
					List<GameStyleID> list2 = new List<GameStyleID>();
					if (ffaToggle.isOn)
					{
						list2.Add(GameStyleID.FreeForAll);
					}
					if (tdmToggle.isOn)
					{
						list2.Add(GameStyleID.TeamDeathMatch);
					}
					if (ltsToggle.isOn)
					{
						list2.Add(GameStyleID.LastTeamStanding);
					}
					if (ctfToggle.isOn)
					{
						list2.Add(GameStyleID.CaptureTheGas);
					}
					if (coopAssaultToggle.isOn)
					{
						list2.Add(GameStyleID.Cooperative);
					}
					if (territoryToggle.isOn)
					{
						list2.Add(GameStyleID.Territory);
					}
					GameStyleID randomStyle = list2[UnityEngine.Random.Range(0, list2.Count)];
					HostServer();
					while (GameManager.GetInstance() == null && matchMakingTimer > 0f)
					{
						yield return new WaitForSecondsRealtime(1f);
						halfTimer -= 1f;
						matchMakingTimer -= 1f;
					}
					MapData[] availableOfficialMaps = GUIManager.GetInstance().mapCache.GetAvailableOfficialMaps(randomStyle);
					ulong publishedFileID = availableOfficialMaps[UnityEngine.Random.Range(0, availableOfficialMaps.Length)].publishedFileID;
					ServerConfig defaultSettingsForMode = ServerConfig.GetDefaultSettingsForMode(randomStyle);
					GameManager.GetInstance().SetServerConfig(defaultSettingsForMode);
					GameManager.GetInstance().SetCurrentMap(MapDataLocation.OfficialMaps, publishedFileID);
					GameManager.GetInstance().serverMapCrc = 0u;
					GameManager.GetInstance().UpdateServerInfo();
				}
				else
				{
					halfTimer -= 1f;
				}
			}
			else if (PhotonNetwork.room != null && PhotonNetwork.room.PlayerCount > 1)
			{
				break;
			}
			matchMakingTimer -= 1f;
			yield return new WaitForSecondsRealtime(1f);
		}
		if (GameManager.GetInstance() != null && PhotonNetwork.isMasterClient)
		{
			GUIManager.GetInstance().SetCurrentScreen(GUIManager.GUIScreen.room);
			GUIManager.GetInstance().OpenMessageDialog(Texts.GetText("UI.Lobby.MatchMakingHostAssignmentNotification"), DialogType.OkMessage, null);
		}
		Debug.Log("matchmaking ends!");
		matchmakingRoutine = null;
	}

	private void GetLeaderboardRanks()
	{
		leaderboard_FFA_Rank = -1;
		leaderboard_TDM_Rank = -1;
		leaderboard_LTS_Rank = -1;
		leaderboard_Territory_Rank = -1;
		leaderboard_CTG_Rank = -1;
		leaderboard_TM_Rank = -1;
		SteamAPICall_t hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_FFA, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_FFA_Callresult.Set(hAPICall);
		hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_TDM, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_TDM_Callresult.Set(hAPICall);
		hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_LTS, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_LTS_Callresult.Set(hAPICall);
		hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_Territory, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_Territory_Callresult.Set(hAPICall);
		hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_CTG, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_CTG_Callresult.Set(hAPICall);
		hAPICall = SteamUserStats.DownloadLeaderboardEntries(SteamAchievements.leaderboard_Tasks, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
		OnLeaderBoardDownload_TM_Callresult.Set(hAPICall);
	}

	private void OnLeaderBoardDownloaded_TM(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_TM_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_TM_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	private void OnLeaderBoardDownloaded_CTG_(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_CTG_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_CTG_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	private void OnLeaderBoardDownloaded_Territory(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_Territory_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_Territory_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	private void OnLeaderBoardDownloaded_LTS(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_LTS_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_LTS_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	private void OnLeaderBoardDownloaded_TDM(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_TDM_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_TDM_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	private void OnLeaderBoardDownloaded_FFA(LeaderboardScoresDownloaded_t param, bool bIOFailure)
	{
		if (!bIOFailure)
		{
			if (param.m_cEntryCount == 0)
			{
				leaderboard_FFA_Rank = 0;
				return;
			}
			LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
			int[] pDetails = new int[4];
			SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, 0, out pLeaderboardEntry, pDetails, 4);
			leaderboard_FFA_Rank = pLeaderboardEntry.m_nGlobalRank;
		}
	}

	public void LanguageChanged()
	{
		int num = (int)SteamAchievements.GetInstance().GetSteamStatValue(SteamStatID.Rank);
		currentRankImage.sprite = rankImagesLarge[Mathf.Clamp(num, 0, 19)];
		if (num >= 19)
		{
			currentRankText.text = Texts.GetText("UI.Generic.RankNames.Rank_19").Replace("{RANK}", (num + 1).ToString());
		}
		else
		{
			currentRankText.text = Texts.GetText("UI.Generic.RankNames.Rank_" + num);
		}
	}
}
