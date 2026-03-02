using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
	public static bool isChangingRegion;

	public static bool directMenu;

	private string targetRegionCode = "";

	public GameObject lobbyRefreshing;

	private List<RoomInfo> updatedRoms;

	[SerializeField]
	private LoadingScreenManager loadingScreen;

	[SerializeField]
	private GameObject go_multiplayerButton;

	[SerializeField]
	private MenuManager menuManager;

	[SerializeField]
	private SteamLobbyController steamLobby;

	[Header("Create Room")]
	[SerializeField]
	private GameObject screen_NotInRoom_CreateRoom;

	[SerializeField]
	private GameObject screen_InRoom;

	[SerializeField]
	private GameObject screen_NotInRoom;

	[SerializeField]
	private InputField if_CreateRoom_Name;

	[SerializeField]
	private Text txt_CreateRoom_Language;

	[SerializeField]
	private Text txt_CreateRoom_Region;

	[SerializeField]
	private InputField if_CreateRoom_Password;

	[SerializeField]
	private RegionManager regionManager;

	public List<string> lobbyLanguages;

	private int currentLobbyLanguage;

	private string creatingRoom_Name = "";

	private string creatingRoom_Password = "";

	[SerializeField]
	private GameObject go_CreateLobby_lobbyNameError;

	[SerializeField]
	private GameObject go_CreateLobby_Create;

	[SerializeField]
	private GameObject go_CreateLobby_Cancel;

	[SerializeField]
	private GameObject go_CreateLobby_PleaseWait;

	private int lobbyMaxPlayer = 5;

	[SerializeField]
	private List<ToggleMenuButton> maxPlayerButtons;

	private string targetRoomName = "";

	private string targetRoomPass = "";

	[SerializeField]
	private GameObject go_enterPass_screen;

	[SerializeField]
	private GameObject go_enterPass_error;

	[SerializeField]
	private InputField if_roomPass;

	[SerializeField]
	private Text txt_PasswordLobbyName;

	[SerializeField]
	private ToggleMenuButton toggle_cryptid_player;

	[SerializeField]
	private ToggleMenuButton toggle_cryptid_ai;

	[SerializeField]
	private Color disabledCrpytidModeColor;

	private bool isAI;

	[SerializeField]
	private ToggleMenuButton toggle_lobbyType_public;

	[SerializeField]
	private ToggleMenuButton toggle_lobbyType_private;

	[SerializeField]
	private Color disabledLobbyTypeColor;

	private bool isPublic;

	[Header("In Room")]
	[SerializeField]
	private Text txt_Room_Name;

	[SerializeField]
	private Text txt_Room_Language;

	[SerializeField]
	private Text txt_Room_Server;

	[SerializeField]
	private Text txt_Room_Players;

	[SerializeField]
	private Text txt_CryptidMode;

	public List<LobbyPlayerPoint> spawnPoints;

	[SerializeField]
	private GameObject prefab_roomPlayer;

	[SerializeField]
	private List<GameObject> cachedRoomPlayers;

	private bool isRestartingMenu;

	[SerializeField]
	private GameObject noRoomScreen;

	[SerializeField]
	private GameObject go_error_screen;

	[SerializeField]
	private GameObject go_error_gamefull;

	[SerializeField]
	private GameObject go_error_unexpected;

	[SerializeField]
	private GameObject go_error_kicked;

	[SerializeField]
	private GameObject go_error_notOnSameRegion;

	[SerializeField]
	private GameObject go_error_inRoom;

	private bool regionTextSetuped;

	private bool firstOpenSetuped;

	private bool isCheckingPing;

	private bool inviteJoin;

	[SerializeField]
	private ChatManager chatManager;

	[SerializeField]
	private VivoxLobbyManager vivoxLobbyManager;

	[SerializeField]
	private GameObject screen_PhotonError;

	[SerializeField]
	private OptionsManager optionsManager;

	[SerializeField]
	private GameObject go_kick_screen;

	[SerializeField]
	private Text txt_kickingPlayerName;

	[SerializeField]
	private VotingManager votingManager;

	[SerializeField]
	private string kickingPlayerName;

	private Dictionary<string, RoomInfo> cachedRoomList;

	[SerializeField]
	private GameObject prefab_lobbyItem;

	[SerializeField]
	private GameObject go_lobbyContent;

	[SerializeField]
	private GameObject go_ready;

	[SerializeField]
	private GameObject go_readyCancel;

	[SerializeField]
	private GameObject go_start;

	[SerializeField]
	private LobbyGameSettings lobbyGameSettings;

	private bool isSingleplayer;

	private bool isSingleTest;

	public bool isBeMonster = true;

	[SerializeField]
	private Text txt_error_targetRegion;

	private void Start()
	{
		Application.targetFrameRate = 300;
		GameSettings.SetKeys();
		isBeMonster = EncryptedPlayerPrefs.GetInt("BeAMonster") == 1;
		if (!GameSettings.isOffline && !PhotonNetwork.IsConnected)
		{
			ApplyPhotonSettings();
			PhotonNetwork.ConnectUsingSettings();
			if (!firstOpenSetuped)
			{
				firstOpenSetuped = true;
				loadingScreen.FadeOut(GameSettings.LOADING_FADETIMER);
			}
		}
		else if (GameSettings.isOffline)
		{
			Debug.LogError("İnternet yok");
			if (!firstOpenSetuped)
			{
				firstOpenSetuped = true;
				loadingScreen.FadeOut(GameSettings.LOADING_FADETIMER);
			}
		}
		else if (PhotonNetwork.IsConnected)
		{
			if (PhotonNetwork.InRoom)
			{
				DirectJoinLobby();
				directMenu = true;
				return;
			}
			OnConnectedToMaster();
		}
		else
		{
			Debug.LogError("Enteresan işler");
		}
		directMenu = false;
	}

	private void DirectJoinLobby()
	{
		PhotonNetwork.IsMessageQueueRunning = true;
		PhotonNetwork.CurrentRoom.IsVisible = true;
		PhotonNetwork.CurrentRoom.IsOpen = true;
		steamLobby.ChangeJoinableState(isJoinable: true);
		loadingScreen.FadeOut(0.1f);
		menuManager.act_Multiplayer(directJoin: true);
		cachedRoomList.Clear();
		ClearRoomListView();
		ResetReadyState();
		screen_NotInRoom.SetActive(value: false);
		screen_NotInRoom_CreateRoom.SetActive(value: false);
		screen_InRoom.SetActive(value: true);
		MonoBehaviour.print("On Joined Room, Direct Join Server=" + PhotonNetwork.CloudRegion);
		regionManager.SetDirectRegion(PhotonNetwork.CloudRegion);
		GetRoomDatas();
		SetMyPing();
		GetPlayerList();
		go_enterPass_screen.SetActive(value: false);
		vivoxLobbyManager.DirectJoinChannel();
		lobbyGameSettings.GetDirectDatas();
	}

	public void ChangeRegion(string code)
	{
		cachedRoomList.Clear();
		ClearRoomListView();
		PhotonNetwork.Disconnect();
		isChangingRegion = true;
		targetRegionCode = code;
		lobbyRefreshing.SetActive(value: true);
		noRoomScreen.SetActive(value: false);
	}

	public void act_CreateRoom()
	{
		if (!isChangingRegion)
		{
			currentLobbyLanguage = EncryptedPlayerPrefs.GetInt("LanguageID");
			screen_NotInRoom_CreateRoom.SetActive(value: true);
			creatingRoom_Name = "Lobby" + UnityEngine.Random.Range(0, 99999);
			creatingRoom_Password = "";
			if_CreateRoom_Password.text = "";
			if_CreateRoom_Name.text = creatingRoom_Name;
			txt_CreateRoom_Region.text = TermManager.GetDataFromTerms(regionManager.regions[regionManager.currentRegion].termCode).ToUpper();
			txt_CreateRoom_Language.text = StringExtensions.FirstLetterUpper(lobbyLanguages[currentLobbyLanguage]);
			go_CreateLobby_Cancel.SetActive(value: true);
			go_CreateLobby_Create.SetActive(value: true);
			go_CreateLobby_PleaseWait.SetActive(value: false);
			act_CreateRoom_SelectMaxPlayerCount(5);
			act_Cryptid_Player();
			act_LobbyType_Public();
		}
	}

	public void act_CreateRoom_NextLanguage()
	{
		currentLobbyLanguage++;
		if (currentLobbyLanguage > lobbyLanguages.Count - 1)
		{
			currentLobbyLanguage = 0;
		}
		txt_CreateRoom_Language.text = StringExtensions.FirstLetterUpper(lobbyLanguages[currentLobbyLanguage]);
	}

	public void act_CreateRoom_PrevLanguage()
	{
		currentLobbyLanguage--;
		if (currentLobbyLanguage < 0)
		{
			currentLobbyLanguage = lobbyLanguages.Count - 1;
		}
		txt_CreateRoom_Language.text = StringExtensions.FirstLetterUpper(lobbyLanguages[currentLobbyLanguage]);
	}

	public void act_CreateRoom_NameInput()
	{
		creatingRoom_Name = if_CreateRoom_Name.text;
		go_CreateLobby_lobbyNameError.SetActive(value: false);
	}

	public void act_CreateRoom_PasswordInput()
	{
		creatingRoom_Password = if_CreateRoom_Password.text;
	}

	public void act_CreateRoom_Cancel()
	{
		screen_NotInRoom_CreateRoom.SetActive(value: false);
	}

	public void act_CreateRoom_SelectMaxPlayerCount(int count)
	{
		lobbyMaxPlayer = count;
		foreach (ToggleMenuButton maxPlayerButton in maxPlayerButtons)
		{
			maxPlayerButton.Deactivate(disabledCrpytidModeColor);
		}
		maxPlayerButtons[count - 2].Activate();
	}

	public void act_CreateRoom_Create()
	{
		isSingleplayer = false;
		if (creatingRoom_Name.Length <= 0)
		{
			Debug.LogError("No Room Name");
			return;
		}
		bool flag = !string.IsNullOrEmpty(creatingRoom_Password);
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.IsVisible = isPublic;
		roomOptions.MaxPlayers = (byte)lobbyMaxPlayer;
		roomOptions.IsOpen = true;
		string[] array = new string[4];
		roomOptions.CustomRoomPropertiesForLobby = new string[4] { "Password", "Language", "CryptidMode", "MapName" };
		if (flag)
		{
			array[0] = creatingRoom_Password;
		}
		else
		{
			array[0] = "";
		}
		if (isAI)
		{
			array[2] = "AI";
		}
		else
		{
			array[2] = "PLAYER";
		}
		array[3] = "Forest";
		array[1] = txt_CreateRoom_Language.text;
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("Password", array[0]);
		hashtable.Add("Language", array[1]);
		hashtable.Add("CryptidMode", array[2]);
		hashtable.Add("MapName", array[3]);
		roomOptions.CustomRoomProperties = hashtable;
		go_CreateLobby_Cancel.SetActive(value: false);
		go_CreateLobby_Create.SetActive(value: false);
		go_CreateLobby_PleaseWait.SetActive(value: true);
		ExitGames.Client.Photon.Hashtable hashtable2 = new ExitGames.Client.Photon.Hashtable();
		hashtable2.Add("TargetRoomPass", creatingRoom_Password);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable2);
		CreateRoom(creatingRoom_Name, roomOptions);
	}

	private void CreateRoom(string _name, RoomOptions _ro)
	{
		if (cachedRoomList.Count > 0)
		{
			foreach (RoomInfo value in cachedRoomList.Values)
			{
				if (value.Name == _name)
				{
					LobbyNameError();
					return;
				}
			}
		}
		loadingScreen.FadeIn(0.2f);
		PhotonNetwork.CreateRoom(_name, _ro);
	}

	public void TryPasswordJoinRoom(string roomName, string roomPass)
	{
		txt_PasswordLobbyName.text = roomName;
		if_roomPass.text = "";
		targetRoomName = roomName;
		targetRoomPass = roomPass;
		go_enterPass_error.SetActive(value: false);
		go_enterPass_screen.SetActive(value: true);
	}

	public void act_Password_Enter()
	{
		if (if_roomPass.text == targetRoomPass)
		{
			TryJoinRoom(targetRoomName);
		}
		else
		{
			go_enterPass_error.SetActive(value: true);
		}
	}

	public void act_Pasword_Cancel()
	{
		go_enterPass_screen.SetActive(value: false);
		go_enterPass_error.SetActive(value: false);
		if_roomPass.text = "";
	}

	public void act_Cryptid_Player()
	{
		isAI = false;
		toggle_cryptid_ai.Deactivate(disabledCrpytidModeColor);
		toggle_cryptid_player.Activate();
	}

	public void act_Cryptid_AI()
	{
		isAI = true;
		toggle_cryptid_player.Deactivate(disabledCrpytidModeColor);
		toggle_cryptid_ai.Activate();
	}

	public void act_LobbyType_Public()
	{
		isPublic = true;
		toggle_lobbyType_private.Deactivate(disabledLobbyTypeColor);
		toggle_lobbyType_public.Activate();
	}

	public void act_LobbyType_Private()
	{
		isPublic = false;
		toggle_lobbyType_public.Deactivate(disabledLobbyTypeColor);
		toggle_lobbyType_private.Activate();
	}

	private void GetRoomDatas()
	{
		txt_Room_Name.text = PhotonNetwork.CurrentRoom.Name;
		txt_Room_Language.text = PhotonNetwork.CurrentRoom.CustomProperties["Language"].ToString();
		txt_Room_Server.text = TermManager.GetDataFromTerms(regionManager.regions[regionManager.currentRegion].termCode).ToUpper();
		txt_Room_Players.text = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
		if (PhotonNetwork.CurrentRoom.CustomProperties["CryptidMode"].ToString() == "AI")
		{
			txt_CryptidMode.text = TermManager.GetDataFromTerms(LocalizeDatabase.GAME_SETTINGS_AI);
		}
		else
		{
			txt_CryptidMode.text = TermManager.GetDataFromTerms(LocalizeDatabase.GAME_SETTINGS_Player);
		}
	}

	private void LobbyNameError()
	{
		go_CreateLobby_lobbyNameError.SetActive(value: true);
		go_CreateLobby_Cancel.SetActive(value: true);
		go_CreateLobby_Create.SetActive(value: true);
		go_CreateLobby_PleaseWait.SetActive(value: false);
	}

	private void SetMyPing()
	{
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("Ping", PhotonNetwork.GetPing());
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
		if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
		{
			base.photonView.RPC("RefreshPings", RpcTarget.All, null);
		}
	}

	[PunRPC]
	public void RefreshPings()
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (!spawnPoints[i].isEmpty)
			{
				spawnPoints[i].roomPlayer.RefreshPing();
			}
		}
	}

	private void GetPlayerList()
	{
		cachedRoomPlayers.Clear();
		List<Player> list = PhotonNetwork.PlayerList.OrderBy((Player player2) => player2.ActorNumber).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			Player player = list[num];
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab_roomPlayer);
			gameObject.name = player.NickName;
			LobbyPlayerPoint lobbyPlayerPoint = spawnPoints[num];
			RoomPlayer component = gameObject.GetComponent<RoomPlayer>();
			MonoBehaviour.print("Set Player -> Get Player List");
			lobbyPlayerPoint.SetPlayer(player, component);
			MonoBehaviour.print("LPPNAME=" + lobbyPlayerPoint.roomPlayer.gameObject.name);
			int ping = (int)player.CustomProperties["Ping"];
			bool isMasterClient = player.IsMasterClient;
			bool isMonster = (bool)player.CustomProperties["MonsterState"];
			cachedRoomPlayers.Add(gameObject);
			component.Setup(player.NickName, ping, isMasterClient, player, this, isMonster);
		}
		if (PhotonNetwork.IsMasterClient)
		{
			base.photonView.RPC("StateRefreshed", RpcTarget.All, null);
		}
	}

	private void AddPlayerToList(Player _player)
	{
		if (PhotonNetwork.InRoom)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab_roomPlayer);
			LobbyPlayerPoint emptyPoint = GetEmptyPoint();
			RoomPlayer component = gameObject.GetComponent<RoomPlayer>();
			MonoBehaviour.print("Set Player -> Add Player To List");
			emptyPoint.SetPlayer(_player, component);
			gameObject.name = _player.NickName;
			int ping = (int)_player.CustomProperties["Ping"];
			bool isMasterClient = _player.IsMasterClient;
			bool isMonster = (bool)_player.CustomProperties["MonsterState"];
			cachedRoomPlayers.Add(gameObject);
			component.Setup(_player.NickName, ping, isMasterClient, _player, this, isMonster);
			GetRoomDatas();
			if (PhotonNetwork.IsMasterClient)
			{
				base.photonView.RPC("RefreshGameSettings", RpcTarget.All, null);
				base.photonView.RPC("StateRefreshed", RpcTarget.All, null);
				RefreshSkinArrays(isToAll: false, _player);
			}
		}
	}

	private void RemovePlayerFromList(int actorNumber)
	{
		GetPlayerPoint(actorNumber).RemovePlayer();
	}

	[PunRPC]
	public void RemovePlayerFromVivox(string nickName)
	{
		vivoxLobbyManager.RemoveRoster(GetRoster(nickName));
	}

	private LobbyPlayerPoint GetPlayerPoint(int actorNumber)
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (spawnPoints[i].actorNumber == actorNumber)
			{
				return spawnPoints[i];
			}
		}
		return null;
	}

	private LobbyPlayerPoint GetEmptyPoint()
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (spawnPoints[i].isEmpty)
			{
				return spawnPoints[i];
			}
		}
		return null;
	}

	public RosterItem GetRoster(string name)
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (!spawnPoints[i].isEmpty && spawnPoints[i].currentPlayer.NickName == name)
			{
				return spawnPoints[i].roomPlayer.GetComponent<RosterItem>();
			}
		}
		return null;
	}

	public void act_LobbyBack()
	{
		LeaveFromDirectMenu();
	}

	public void LeaveFromDirectMenu()
	{
		StartCoroutine("ExitToLobby");
	}

	private IEnumerator ExitToLobby()
	{
		loadingScreen.FadeIn(0.1f);
		steamLobby.LeaveRoom();
		vivoxLobbyManager.DisconnectFromDirectMenu();
		yield return new WaitForSeconds(0.1f);
		MonoBehaviour.print("Disconnect Try");
		isChangingRegion = false;
		isRestartingMenu = true;
		PhotonNetwork.Disconnect();
	}

	public void act_Error_OK()
	{
		if (go_error_kicked.activeSelf)
		{
			act_Kicked_Ok();
		}
		go_error_screen.SetActive(value: false);
		go_error_unexpected.SetActive(value: false);
		go_error_gamefull.SetActive(value: false);
		go_error_notOnSameRegion.SetActive(value: false);
		go_error_inRoom.SetActive(value: false);
	}

	public void PingReset()
	{
		Application.Quit();
	}

	private IEnumerator lobbyBack(bool isKicked)
	{
		base.photonView.RPC("RemovePlayerFromVivox", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName);
		loadingScreen.FadeIn(0.1f);
		yield return new WaitForSeconds(0.5f);
		foreach (GameObject cachedRoomPlayer in cachedRoomPlayers)
		{
			UnityEngine.Object.Destroy(cachedRoomPlayer);
		}
		foreach (LobbyPlayerPoint spawnPoint in spawnPoints)
		{
			spawnPoint.ResetPoint();
		}
		for (int i = 0; i < go_lobbyContent.transform.childCount; i++)
		{
			UnityEngine.Object.Destroy(go_lobbyContent.transform.GetChild(i).gameObject);
		}
		yield return new WaitForSeconds(0.5f);
		if (isKicked)
		{
			inviteJoin = false;
			go_error_screen.SetActive(value: true);
			go_error_kicked.SetActive(value: true);
		}
		go_error_inRoom.SetActive(value: false);
		MonoBehaviour.print("Kicked");
		steamLobby.LeaveRoom();
		PhotonNetwork.LeaveRoom();
	}

	private IEnumerator CheckPing()
	{
		isCheckingPing = true;
		yield return new WaitForSeconds(3f);
		SetMyPing();
		MonoBehaviour.print("CheckPing");
		StartCoroutine("CheckPing");
	}

	private IEnumerator lobbyBackRefresh()
	{
		yield return new WaitForSeconds(0.5f);
		regionManager.act_RefreshRegion();
		loadingScreen.FadeOut(0.1f);
	}

	private IEnumerator AddingProcess(Player newPlayer)
	{
		yield return new WaitForSeconds(0.1f);
		AddPlayerToList(newPlayer);
	}

	private IEnumerator JoinChannelProcess()
	{
		yield return new WaitForSeconds(1f);
		vivoxLobbyManager.JoinChannel();
		loadingScreen.FadeOut(0.1f);
	}

	private void RefreshPlayerList()
	{
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			if (!spawnPoints[i].isEmpty)
			{
				spawnPoints[i].roomPlayer.Refresh();
			}
		}
		if (PhotonNetwork.IsMasterClient)
		{
			base.photonView.RPC("StateRefreshed", RpcTarget.All, null);
		}
	}

	public override void OnCreatedRoom()
	{
		if (!isSingleplayer)
		{
			MonoBehaviour.print("Created Room");
			steamLobby.ChangeJoinableState(isJoinable: true);
			steamLobby.CreateRoom(PhotonNetwork.CurrentRoom.Name, regionManager.regions[regionManager.currentRegion].code, PhotonNetwork.CurrentRoom.MaxPlayers);
		}
	}

	public override void OnConnectedToMaster()
	{
		isChangingRegion = false;
		MonoBehaviour.print("Connected to Master=" + PhotonNetwork.CloudRegion);
		DebugManager.instance.Add("ConnectedToMaster");
		if (!regionTextSetuped)
		{
			MonoBehaviour.print("Not RegionTextSetuped");
			regionTextSetuped = true;
			GetComponent<RegionManager>().SetupRegionText();
		}
		if (!isCheckingPing)
		{
			MonoBehaviour.print("Not Checking Ping");
			StartCoroutine("CheckPing");
		}
		PhotonNetwork.JoinLobby();
	}

	public void FirstOpenSetup()
	{
		VivoxVoiceManager.Instance.AudioInputDevices.Muted = false;
		if (!firstOpenSetuped)
		{
			firstOpenSetuped = true;
			MonoBehaviour.print("REMOVE THIS TEST");
			if (FirstOptionsManager.isDirectSingleTest)
			{
				act_SinglePlayer();
			}
			if (SteamGameStarter.IS_INVITED)
			{
				DebugManager.instance.Add("INVITED");
				SteamGameStarter.IS_INVITED = false;
				inviteJoin = true;
				regionManager.act_DirectChangeRegion(SteamGameStarter.LOBBY_REGION);
			}
			else
			{
				loadingScreen.FadeOut(GameSettings.LOADING_FADETIMER);
			}
		}
	}

	public void TryJoinRoom(string roomName)
	{
		loadingScreen.FadeIn(0.1f);
		targetRoomPass = if_roomPass.text;
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("TargetRoomPass", targetRoomPass);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
		PhotonNetwork.JoinRoom(roomName);
	}

	public override void OnLeftRoom()
	{
		MonoBehaviour.print("On Left Room");
		go_error_inRoom.SetActive(value: false);
		screen_NotInRoom.SetActive(value: true);
		screen_InRoom.SetActive(value: false);
		vivoxLobbyManager.DisconnectChannel();
		votingManager.Close();
		StartCoroutine("lobbyBackRefresh");
	}

	public override void OnJoinedLobby()
	{
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("ReadyState", false);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
		MonoBehaviour.print("Joined Lobby");
		if (isSingleplayer)
		{
			MonoBehaviour.print("Single Player");
			GameSettings.isOffline = true;
		}
		PhotonNetwork.LocalPlayer.NickName = GameSettings.NICKNAME;
		if (!GameSettings.isOffline)
		{
			MonoBehaviour.print("Not Offline, setup vivox");
			vivoxLobbyManager.Setup(GameSettings.NICKNAME);
		}
		if (inviteJoin)
		{
			MonoBehaviour.print("Invite Join");
			StartCoroutine("WaitForInviteJoin");
		}
		SetMonsterState(isBeMonster);
		StartCoroutine("CheckPhotonConnection");
	}

	private IEnumerator WaitForInviteJoin()
	{
		SetMyPing();
		yield return new WaitForSeconds(1f);
		menuManager.act_Multiplayer(directJoin: true);
		TryJoinRoom(SteamGameStarter.LOBBY_NAME);
	}

	private IEnumerator CheckPhotonConnection()
	{
		yield return new WaitForSeconds(1f);
		if (PhotonNetwork.IsConnected)
		{
			StartCoroutine("CheckPhotonConnection");
		}
		else
		{
			screen_PhotonError.SetActive(value: true);
		}
	}

	public override void OnJoinedRoom()
	{
		MonoBehaviour.print("On Joined Room");
		DebugManager.instance.Add("OnJoinedRoom");
		if (isSingleplayer)
		{
			StartSingle();
			return;
		}
		optionsManager.CloseLanguageChanger();
		cachedRoomList.Clear();
		ClearRoomListView();
		ResetReadyState();
		screen_NotInRoom.SetActive(value: false);
		screen_NotInRoom_CreateRoom.SetActive(value: false);
		screen_InRoom.SetActive(value: true);
		GetRoomDatas();
		SetMyPing();
		GetPlayerList();
		go_enterPass_screen.SetActive(value: false);
		StartCoroutine("JoinChannelProcess");
		steamLobby.ChangeJoinableState(isJoinable: true);
		if (PhotonNetwork.IsMasterClient)
		{
			lobbyGameSettings.act_SetTemplate();
		}
		else if (!SteamLobbyController.InLobby)
		{
			ulong num = Convert.ToUInt64((string)PhotonNetwork.CurrentRoom.CustomProperties["SteamLobbyID"]);
			steamLobby.JoinRoom((CSteamID)num);
		}
	}

	public override void OnLeftLobby()
	{
		MonoBehaviour.print("LEFT LOBBY");
		cachedRoomList.Clear();
		ResetReadyState();
		ClearRoomListView();
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		MonoBehaviour.print("On Player Entered Room");
		StartCoroutine("AddingProcess", newPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		MonoBehaviour.print("On Player Left Room");
		RemovePlayerFromList(otherPlayer.ActorNumber);
		votingManager.CheckLeftPlayer(otherPlayer.NickName);
		GetRoomDatas();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		MonoBehaviour.print("On Master Client Switched");
		lobbyGameSettings.DeactivateGameSettingsUI();
		RefreshPlayerList();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		MonoBehaviour.print("Room List Update");
		noRoomScreen.SetActive(value: false);
		ClearRoomListView();
		MonoBehaviour.print("Room List Count=" + roomList.Count);
		MonoBehaviour.print("Room List All Rooms=" + PhotonNetwork.CountOfRooms);
		UpdateCachedRoomList(roomList);
		ShowRooms();
		isChangingRegion = false;
		lobbyRefreshing.SetActive(value: false);
		if (cachedRoomList.Count < 1)
		{
			noRoomScreen.SetActive(value: true);
		}
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		loadingScreen.FadeOut(0.1f);
		go_enterPass_error.SetActive(value: false);
		go_enterPass_screen.SetActive(value: false);
		go_error_screen.SetActive(value: true);
		inviteJoin = false;
		regionManager.act_RefreshRegion();
		if (returnCode == 32765)
		{
			go_error_gamefull.SetActive(value: true);
			return;
		}
		Debug.LogError("ReturnCode=" + returnCode + " Message=" + message);
		go_error_unexpected.SetActive(value: true);
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		Debug.Log("Disconnected from Photon: " + cause);
		if (isChangingRegion)
		{
			PhotonNetwork.ConnectToRegion(targetRegionCode);
		}
		else if (isRestartingMenu)
		{
			SceneManager.LoadScene(2);
		}
		else if (!GameSettings.isOffline)
		{
			ApplyPhotonSettings();
			PhotonNetwork.ConnectUsingSettings();
		}
	}

	public void KickProcess(Player _player)
	{
		go_kick_screen.SetActive(value: true);
		kickingPlayerName = _player.NickName;
		txt_kickingPlayerName.text = "\" " + _player.NickName + " \"";
	}

	public void act_Kick_Yes()
	{
		go_kick_screen.SetActive(value: false);
		if (PhotonNetwork.IsMasterClient && !votingManager.isKicking)
		{
			base.photonView.RPC("StartKickVoting", RpcTarget.All, kickingPlayerName);
		}
	}

	[PunRPC]
	public void StartKickVoting(string _kickingPlayerName)
	{
		if (!votingManager.isKicking)
		{
			votingManager.OpenKickUI(_kickingPlayerName);
			if (PhotonNetwork.IsMasterClient)
			{
				votingManager.act_Kick_Yes();
			}
		}
	}

	public void KickMe()
	{
		StartCoroutine("lobbyBack", true);
	}

	public void act_Kicked_Ok()
	{
		loadingScreen.FadeIn(0.1f);
		isChangingRegion = false;
		isRestartingMenu = true;
		PhotonNetwork.Disconnect();
	}

	private void Awake()
	{
		cachedRoomList = new Dictionary<string, RoomInfo>();
	}

	private void ClearRoomListView()
	{
		for (int i = 0; i < go_lobbyContent.transform.childCount; i++)
		{
			UnityEngine.Object.Destroy(go_lobbyContent.transform.GetChild(i).gameObject);
		}
	}

	private void UpdateCachedRoomList(List<RoomInfo> roomList)
	{
		foreach (RoomInfo room in roomList)
		{
			if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
			{
				if (cachedRoomList.ContainsKey(room.Name))
				{
					cachedRoomList.Remove(room.Name);
				}
			}
			else if (cachedRoomList.ContainsKey(room.Name))
			{
				cachedRoomList[room.Name] = room;
			}
			else
			{
				cachedRoomList.Add(room.Name, room);
			}
		}
	}

	private void ShowRooms()
	{
		foreach (RoomInfo value in cachedRoomList.Values)
		{
			if (!value.IsOpen || !value.IsVisible || value.RemovedFromList || value.PlayerCount == 0)
			{
				if (cachedRoomList.ContainsKey(value.Name))
				{
					cachedRoomList.Remove(value.Name);
				}
				continue;
			}
			GameObject obj = UnityEngine.Object.Instantiate(prefab_lobbyItem);
			obj.transform.SetParent(go_lobbyContent.transform, worldPositionStays: false);
			LobbyListItem component = obj.GetComponent<LobbyListItem>();
			string password = (string)value.CustomProperties["Password"];
			string language = (string)value.CustomProperties["Language"];
			string text = (string)value.CustomProperties["CryptidMode"];
			component.Setup(map: (string)value.CustomProperties["MapName"], name: value.Name, players: value.PlayerCount, language: language, password: password, isAI: text == "AI", _lobbyManager: this, maxPlayers: value.MaxPlayers);
		}
	}

	public void act_Ready()
	{
		SetPlayerReadyState(isReady: true);
		StartCoroutine("ReadyWaiter");
	}

	private IEnumerator ReadyWaiter()
	{
		go_ready.SetActive(value: false);
		yield return new WaitForSeconds(1f);
		go_readyCancel.SetActive(value: true);
	}

	public void act_ReadyCancel()
	{
		SetPlayerReadyState(isReady: false);
		StartCoroutine("ReadyCancelWaiter");
	}

	private IEnumerator ReadyCancelWaiter()
	{
		go_readyCancel.SetActive(value: false);
		yield return new WaitForSeconds(1f);
		go_ready.SetActive(value: true);
	}

	private void SetPlayerReadyState(bool isReady)
	{
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("ReadyState", isReady);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
		base.photonView.RPC("StateRefreshed", RpcTarget.All, null);
	}

	private void ResetReadyState()
	{
		go_ready.SetActive(value: true);
		go_readyCancel.SetActive(value: false);
		go_start.SetActive(value: false);
		chatManager.Clear();
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("ReadyState", false);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
	}

	[PunRPC]
	public void StateRefreshed()
	{
		foreach (LobbyPlayerPoint spawnPoint in spawnPoints)
		{
			if (!spawnPoint.isEmpty)
			{
				spawnPoint.roomPlayer.CheckState();
			}
		}
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<int, Player> player in PhotonNetwork.CurrentRoom.Players)
		{
			if ((bool)player.Value.CustomProperties["ReadyState"])
			{
				num++;
			}
		}
		MonoBehaviour.print("readyPlayer=" + num + " currentRoomPLayerCount=" + PhotonNetwork.CurrentRoom.PlayerCount);
		go_start.SetActive(PhotonNetwork.CurrentRoom.PlayerCount == num && num != 1);
		lobbyGameSettings.ActivateGameSettingsUI();
	}

	public void act_StartGame()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.CurrentRoom.IsVisible = false;
			PhotonNetwork.CurrentRoom.IsOpen = false;
			steamLobby.ChangeJoinableState(isJoinable: false);
			ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
			hashtable.Add("StartedPlayerCount", PhotonNetwork.CurrentRoom.PlayerCount);
			hashtable.Add("Template", lobbyGameSettings.currentGameSettingTemplate);
			PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
			base.photonView.RPC("StartGame", RpcTarget.All, lobbyGameSettings.IsForest);
		}
	}

	[PunRPC]
	public void StartGame(bool _isForest)
	{
		if (!isSingleplayer)
		{
			Player[] playerList = PhotonNetwork.PlayerList;
			for (int i = 0; i < playerList.Length; i++)
			{
				if (!(bool)playerList[i].CustomProperties["ReadyState"])
				{
					return;
				}
			}
		}
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("ReadyState", false);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
		StartCoroutine("StartingProcess", _isForest);
	}

	private IEnumerator StartingProcess(bool _isForest)
	{
		loadingScreen.FadeIn(0.2f, tip: true);
		if (!GameSettings.isOffline)
		{
			vivoxLobbyManager.RemoveEvents();
		}
		yield return new WaitForSeconds(0.2f);
		PhotonNetwork.IsMessageQueueRunning = false;
		MonoBehaviour.print("IsForest=" + _isForest);
		if (isSingleplayer)
		{
			if (isSingleTest)
			{
				SceneManager.LoadScene("NEW_Winterland");
			}
			else if (_isForest)
			{
				SceneManager.LoadScene("Forest");
			}
			else
			{
				SceneManager.LoadScene("NEW_Winterland");
			}
		}
		else if (_isForest)
		{
			SceneManager.LoadScene("Forest");
		}
		else
		{
			SceneManager.LoadScene("NEW_Winterland");
		}
	}

	public void act_SinglePlayer()
	{
		if (PhotonNetwork.IsConnected)
		{
			loadingScreen.FadeIn(0.2f, tip: true);
			isSingleplayer = true;
			RoomOptions roomOptions = new RoomOptions();
			roomOptions.IsVisible = false;
			roomOptions.IsOpen = false;
			roomOptions.MaxPlayers = 1;
			PhotonNetwork.CreateRoom("SinglePlayer" + UnityEngine.Random.Range(0, 99999), roomOptions);
		}
	}

	public void act_SingleTest()
	{
		if (PhotonNetwork.IsConnected)
		{
			isSingleTest = true;
			loadingScreen.FadeIn(0.2f);
			isSingleplayer = true;
			RoomOptions roomOptions = new RoomOptions();
			roomOptions.IsVisible = false;
			roomOptions.IsOpen = false;
			roomOptions.MaxPlayers = 1;
			PhotonNetwork.CreateRoom("SinglePlayer" + UnityEngine.Random.Range(0, 99999), roomOptions);
		}
	}

	private void StartSingle()
	{
		PhotonNetwork.CurrentRoom.IsVisible = false;
		PhotonNetwork.CurrentRoom.IsOpen = false;
		steamLobby.ChangeJoinableState(isJoinable: false);
		lobbyGameSettings.act_SetTemplate(changeMap: false);
		if (FirstOptionsManager.isNightmareMode)
		{
			lobbyGameSettings.IsNightmare = true;
		}
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("StartedPlayerCount", PhotonNetwork.CurrentRoom.PlayerCount);
		hashtable.Add("CryptidMode", "AI");
		PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
		MonoBehaviour.print("IsForestStartSingle=" + lobbyGameSettings.IsForest);
		base.photonView.RPC("StartGame", RpcTarget.All, lobbyGameSettings.IsForest);
	}

	[PunRPC]
	public void ShowSkinChange(int skinID, string playerName, int actorNumber)
	{
		GetPlayerPoint(actorNumber).skinID = skinID;
		GetRoster(playerName).GetComponent<CharacterChooser>().Show(skinID);
		if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
		{
			RefreshSkinArrays(isToAll: true);
		}
	}

	private void RefreshSkinArrays(bool isToAll, Player _player = null)
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		List<string> list3 = new List<string>();
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			LobbyPlayerPoint lobbyPlayerPoint = spawnPoints[i];
			if (!lobbyPlayerPoint.isEmpty)
			{
				list.Add(lobbyPlayerPoint.currentPlayer.ActorNumber);
				list2.Add(lobbyPlayerPoint.skinID);
				list3.Add(lobbyPlayerPoint.currentPlayer.NickName);
			}
		}
		int[] array = list.ToArray();
		int[] array2 = list2.ToArray();
		string[] array3 = list3.ToArray();
		if (isToAll)
		{
			base.photonView.RPC("SetSkinsToAll", RpcTarget.All, array, array2);
		}
		else
		{
			base.photonView.RPC("SetSkinToNewPlayer", _player, array, array2, array3);
		}
	}

	[PunRPC]
	public void SetSkinsToAll(int[] actorNumbers, int[] skinIDs)
	{
		for (int i = 0; i < actorNumbers.Length; i++)
		{
			GetPlayerPoint(actorNumbers[i]).skinID = skinIDs[i];
		}
	}

	[PunRPC]
	public void SetSkinToNewPlayer(int[] actorNumbers, int[] skinIDs, string[] names)
	{
		for (int i = 0; i < actorNumbers.Length; i++)
		{
			GetPlayerPoint(actorNumbers[i]).skinID = skinIDs[i];
			GetRoster(names[i]).GetComponent<CharacterChooser>().Show(skinIDs[i]);
		}
	}

	public void act_BeAMonster()
	{
		isBeMonster = !isBeMonster;
		if (isBeMonster)
		{
			EncryptedPlayerPrefs.SetInt("BeAMonster", 1);
		}
		else
		{
			EncryptedPlayerPrefs.SetInt("BeAMonster", 0);
		}
		SetMonsterState(isBeMonster);
		base.photonView.RPC("RefreshMonsterStates", RpcTarget.All, null);
	}

	private void SetMonsterState(bool isMonster)
	{
		MonoBehaviour.print("Set Monster State");
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("MonsterState", isMonster);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
	}

	[PunRPC]
	public void RefreshMonsterStates()
	{
		foreach (LobbyPlayerPoint spawnPoint in spawnPoints)
		{
			if (!spawnPoint.isEmpty)
			{
				spawnPoint.roomPlayer.RefreshMonster();
			}
		}
	}

	public void DifferentRegionError(string targetRegionTermCode)
	{
		if (!menuManager.screen_Multiplayer.activeSelf)
		{
			menuManager.act_Multiplayer();
		}
		go_error_screen.SetActive(value: true);
		go_error_notOnSameRegion.SetActive(value: true);
		txt_error_targetRegion.text = TermManager.GetDataFromTerms("TargetRegion") + " " + TermManager.GetDataFromTerms(targetRegionTermCode).ToUpper();
	}

	public void InRoomError()
	{
		go_error_inRoom.SetActive(value: true);
	}

	public void act_CloseRoomError()
	{
		go_error_inRoom.SetActive(value: false);
	}

	static LobbyManager()
	{
	}

	private void ApplyPhotonSettings()
	{
		string text = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
		if (!File.Exists(text))
		{
			Debug.Log("[PhotonLANFix] Server config file not found, using default settings");
			return;
		}
		IniParser iniParser = new IniParser(text);
		GameSettings.PhotonServerAddress = iniParser.GetValue("Server", "ServerAddress", GameSettings.PhotonServerAddress);
		GameSettings.PhotonServerPort = iniParser.GetIntValue("Server", "ServerPort", GameSettings.PhotonServerPort);
		GameSettings.PhotonServerVersion = iniParser.GetIntValue("Server", "ServerVersion", GameSettings.PhotonServerVersion);
		if (!string.IsNullOrEmpty(GameSettings.PhotonServerAddress))
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.Server = GameSettings.PhotonServerAddress;
			Debug.Log("[PhotonLANFix] Changed Server Address: " + PhotonNetwork.PhotonServerSettings.AppSettings.Server);
			if (GameSettings.PhotonServerVersion == 4)
			{
				PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = false;
				PhotonNetwork.NetworkingClient.LoadBalancingPeer.SerializationProtocolType = SerializationProtocol.GpBinaryV16;
				PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
			}
			else
			{
				PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
				PhotonNetwork.NetworkingClient.LoadBalancingPeer.SerializationProtocolType = SerializationProtocol.GpBinaryV18;
			}
			if (GameSettings.PhotonServerVersion == 5)
			{
				PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "EU";
			}
			PhotonNetwork.NetworkingClient.ExpectedProtocol = ConnectionProtocol.Udp;
			if (GameSettings.PhotonServerPort > 0)
			{
				PhotonNetwork.PhotonServerSettings.AppSettings.Port = GameSettings.PhotonServerPort;
				Debug.Log($"[PhotonLANFix] Changed Server Port: {PhotonNetwork.PhotonServerSettings.AppSettings.Port}");
			}
			PhotonNetwork.PhotonServerSettings.AppSettings.Protocol = ConnectionProtocol.Udp;
			Debug.Log("[PhotonLANFix] Photon server settings applied successfully");
		}
		else
		{
			Debug.Log("[PhotonLANFix] No Photon server address configured, using default settings");
		}
	}
}
