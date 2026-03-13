using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Afterengine.Game;
using AfterShockUnity;
using AftershockUnity.Steam;
using ExitGames.Client.Photon;
using Steamworks;
using Trapland.Game;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
	private static NetworkManager Instance;

	private string gameVersion = "v.01";

	protected string roomName = "someroom";

	public static bool IsNew;

	public static float Time;

	public int PlayerCount;

	public Action<RoomInfo[]> GetRoomList;

	public Action<Room> JoinedRoom;

	public Action<PhotonPlayer> PlayerConnected;

	public Action PlayerDissconetedFromPhoton;

	public static Action ConnectedToMaster;

	public static int IsConnectedToMasterState;

	private bool triedToReconnect;

	private float addDelay;

	public Callback<GetAuthSessionTicketResponse_t> _OnGetAuthSessionTicketResponse;

	private static byte[] m_Ticket;

	private static uint m_pcbTicket;

	private static HAuthTicket m_HAuthTicket;

	private static bool isAuthed;

	private bool DebugNeedFix = true;

	private static Hashtable hashInfo;

	public static Action OnRoomCreated;

	private ClientSync mySyncHandler;

	private const string ConfigFileName = "LANSettings.ini";

	public static bool IsOnline => PhotonNetwork.inRoom;

	public static bool IsServer
	{
		get
		{
			if (PhotonNetwork.inRoom)
			{
				return PhotonNetwork.player.IsMasterClient;
			}
			return false;
		}
	}

	public static bool IsMaster
	{
		get
		{
			if (PhotonNetwork.inRoom)
			{
				return PhotonNetwork.player.IsMasterClient;
			}
			return true;
		}
	}

	public static bool IsClient
	{
		get
		{
			if (PhotonNetwork.inRoom)
			{
				return !PhotonNetwork.player.IsMasterClient;
			}
			return false;
		}
	}

	private void Start()
	{
		Instance = this;
		ApplyNetworkConfiguration();
		Connect();
	}

	public static void Connect()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			_Timer.AddDelegate(delegate
			{
				BeingAuth();
			});
		}
	}

	private void OnCustomAuthenticationFailed()
	{
		_Timer.AddDelegate(delegate
		{
			addDelay += 2f;
			BeingAuth();
		}, 5f + addDelay);
	}

	public static void BeingAuth()
	{
		byte[] array = new byte[1024];
		m_HAuthTicket = SteamUser.GetAuthSessionTicket(array, array.Length, out var pcbTicket);
		Array.Resize(ref array, (int)pcbTicket);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < pcbTicket; i++)
		{
			stringBuilder.AppendFormat("{0:x2}", array[i]);
		}
		Instance.SetupServerAuth(stringBuilder.ToString());
	}

	protected virtual void OnConnectedToServer()
	{
		Debug.Log((object)"Server");
	}

	private void SetupServerAuth(string authkey)
	{
		string text = null;
		if (PhotonNetwork.AuthValues != null)
		{
			text = PhotonNetwork.AuthValues.UserId;
		}
		if (Application.isEditor)
		{
			PhotonNetwork.AuthValues = new AuthenticationValues();
			PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.Custom;
			PhotonNetwork.AuthValues.AddAuthParameter("key", "faret");
			PhotonNetwork.AuthValues.AddAuthParameter("data", "blaah");
			if (SteamManager.Instance != null)
			{
				PhotonNetwork.AuthValues.AddAuthParameter("userid", SteamManager.Instance.m_AccountID.ToString() + Random.Range(0, 9999));
			}
			else
			{
				PhotonNetwork.AuthValues.AddAuthParameter("userid", Random.Range(0, 999999).ToString());
			}
		}
		else
		{
			PhotonNetwork.AuthValues = new AuthenticationValues();
			PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.Steam;
			PhotonNetwork.AuthValues.AddAuthParameter("ticket", authkey);
		}
		if (!string.IsNullOrEmpty(text))
		{
			PhotonNetwork.AuthValues.UserId = text;
			if (PhotonNetwork.player != null)
			{
				PhotonNetwork.player.UserId = text;
			}
		}
		Debug.Log((object)("[NetworkManager] SetupServerAuth UserId: " + PhotonNetwork.AuthValues.UserId));
		PhotonNetwork.ConnectUsingSettings(Instance.gameVersion);
	}

	protected virtual void JoinLobby()
	{
		PhotonNetwork.JoinLobby();
	}

	protected virtual void JoinCustom()
	{
		PhotonNetwork.JoinLobby();
	}

	protected virtual void OnJoinedLobby()
	{
		RoomInfo[] roomList = PhotonNetwork.GetRoomList();
		if (GetRoomList != null)
		{
			GetRoomList(roomList);
		}
		if (DebugNeedFix)
		{
			PhotonNetwork.JoinLobby();
			DebugNeedFix = false;
		}
	}

	protected virtual void OnConnectedToMaster()
	{
		if (ConnectedToMaster != null)
		{
			ConnectedToMaster();
		}
		Debug.LogWarning((object)"Connected To Master");
		if (PhotonNetwork.AuthValues != null && PhotonNetwork.AuthValues.Token != null && PhotonNetwork.AuthValues.Token.Length > 10)
		{
			isAuthed = true;
			Debug.Log((object)("token: " + PhotonNetwork.AuthValues.Token));
		}
		IsConnectedToMasterState = 1;
		SteamUser.CancelAuthTicket(m_HAuthTicket);
		LobbyListCreater.ForceUpdateRegionlabel(PhotonNetwork.CloudRegion);
	}

	public static void ChangeCostumProperties(string H, string Value)
	{
		if (PhotonNetwork.room != null && IsServer)
		{
			if (!((Dictionary<object, object>)(object)hashInfo).ContainsKey((object)H))
			{
				((Dictionary<object, object>)(object)hashInfo).Add((object)H, (object)Value);
			}
			else
			{
				hashInfo[(object)H] = Value;
			}
			PhotonNetwork.room.SetCustomProperties(hashInfo);
		}
	}

	private void OnCreatedRoom()
	{
		if (OnRoomCreated != null)
		{
			OnRoomCreated();
		}
		OnRoomCreated = null;
	}

	private void OnMasterClientSwitched(PhotonPlayer newMasterClient)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		if (PhotonNetwork.player == newMasterClient)
		{
			new RoomOptions
			{
				IsVisible = true,
				MaxPlayers = 4,
				CustomRoomPropertiesForLobby = new string[5] { "count", "gameStatus", "gameName", "private", "ping" }
			};
			hashInfo = new Hashtable();
			((Dictionary<object, object>)(object)hashInfo).Add((object)"count", PhotonNetwork.room.CustomProperties[(object)"count"]);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"level", PhotonNetwork.room.CustomProperties[(object)"level"]);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"mode", PhotonNetwork.room.CustomProperties[(object)"mode"]);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"gameStatus", PhotonNetwork.room.CustomProperties[(object)"gameStatus"]);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"gameVersion", PhotonNetwork.room.CustomProperties[(object)"gameVersion"]);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"gameName", (object)(PhotonNetwork.playerName + "'s game"));
			((Dictionary<object, object>)(object)hashInfo).Add((object)"Checkpoints", (object)CheckPoint.UseCheckpoints);
			((Dictionary<object, object>)(object)hashInfo).Add((object)"ping", (object)PhotonNetwork.GetPing());
			if (CreateRoom.Private)
			{
				((Dictionary<object, object>)(object)hashInfo).Add((object)"private", PhotonNetwork.room.CustomProperties[(object)"private"]);
			}
			else
			{
				((Dictionary<object, object>)(object)hashInfo).Add((object)"private", PhotonNetwork.room.CustomProperties[(object)"private"]);
			}
			PhotonNetwork.room.SetCustomProperties(hashInfo);
		}
	}

	public virtual void CreateARoom(string RoomName)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		MessageHandler.ShowMessage("Info", PhotonNetwork.networkingPeer.CloudRegion.ToString(), 2f);
		hashInfo = new Hashtable();
		((Dictionary<object, object>)(object)hashInfo).Add((object)"count", (object)"0");
		((Dictionary<object, object>)(object)hashInfo).Add((object)"level", (object)string.Empty);
		((Dictionary<object, object>)(object)hashInfo).Add((object)"mode", (object)"0");
		((Dictionary<object, object>)(object)hashInfo).Add((object)"gameStatus", (object)"Lobby");
		((Dictionary<object, object>)(object)hashInfo).Add((object)"gameVersion", (object)GameDebugHandler.VersionInUse);
		((Dictionary<object, object>)(object)hashInfo).Add((object)"gameName", (object)(PhotonNetwork.playerName + "'s game"));
		((Dictionary<object, object>)(object)hashInfo).Add((object)"ping", (object)PhotonNetwork.GetPing());
		((Dictionary<object, object>)(object)hashInfo).Add((object)"Checkpoints", (object)CheckPoint.UseCheckpoints);
		if (CreateRoom.Private)
		{
			((Dictionary<object, object>)(object)hashInfo).Add((object)"private", (object)"1");
		}
		else
		{
			((Dictionary<object, object>)(object)hashInfo).Add((object)"private", (object)"0");
		}
		RoomOptions roomOptions = new RoomOptions
		{
			IsVisible = true,
			MaxPlayers = 4
		};
		roomOptions.CustomRoomPropertiesForLobby = new string[5] { "count", "gameStatus", "gameName", "private", "ping" };
		if (!PhotonNetwork.CreateRoom(RoomName, roomOptions, TypedLobby.Default))
		{
			FailedToCreateRoom();
			Debug.LogWarning((object)"Photon: Failed to created room");
		}
		else
		{
			Debug.LogWarning((object)"Photon: Created room");
			CreateRoom.ShowRoom();
		}
	}

	protected virtual void FailedToCreateRoom()
	{
	}

	protected virtual void JoinRoom(string RoomName)
	{
		PhotonNetwork.JoinRoom(RoomName);
	}

	protected virtual void ExitRoom()
	{
		ExitRoomInternal();
	}

	public static string LevelName()
	{
		if (PhotonNetwork.room == null)
		{
			return "Disconnected from Master";
		}
		return PhotonNetwork.room.CustomProperties[(object)"gameStatus"].ToString();
	}

	private void CheckJoinedGameState()
	{
		if (IsServer)
		{
			return;
		}
		Debug.Log((object)("hi " + PhotonNetwork.room.CustomProperties[(object)"gameStatus"]));
		string text = PhotonNetwork.room.CustomProperties[(object)"gameStatus"].ToString();
		if (text == "Worldmap")
		{
			CreateTransition.Instance.Show();
			SessionData.SetData("GameMode", "1");
			SceneFlowHandler.Instance.ChangeScene(Scenes.WorldMap, 1);
			InGameNetworkManager.Instance.Begin();
		}
		else if (!(text == "Lobby"))
		{
			string text2 = PhotonNetwork.room.CustomProperties[(object)"mode"].ToString();
			if (text2 == "0")
			{
				StoryModeHandler.SetLevel(PhotonNetwork.room.CustomProperties[(object)"level"].ToString());
				SessionData.SetData("GameMode", "1");
			}
			else
			{
				SessionData.SetData("GameMode", text2);
				SessionData.SetData("LevelToLoad", text);
				Debug.Log((object)text);
			}
			CreateTransition.Instance.Show();
			SceneFlowHandler.Instance.ChangeScene(Scenes.Game, 1);
			InGameNetworkManager.Instance.Begin();
			IsNew = true;
		}
	}

	protected virtual void OnJoinedRoom()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!PhotonNetwork.isMasterClient)
		{
			if ((Object)(object)mySyncHandler != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)mySyncHandler).gameObject);
			}
			GameObject val = PhotonNetwork.Instantiate("ClientSync", Vector3.zero, Quaternion.identity, 0);
			mySyncHandler = val.GetComponent<ClientSync>();
			val.transform.SetParent(((Component)this).transform);
		}
		if (PhotonNetwork.room == null)
		{
			Debug.Log((object)"isNull");
		}
		if (PhotonNetwork.room.CustomProperties[(object)"gameVersion"].ToString() != GameDebugHandler.VersionInUse)
		{
			Debug.Log((object)"Not same game version Update plz");
			MessageHandler.ShowMessage("Online", "not same game version", 3f);
			ExitRoom();
			return;
		}
		Debug.Log((object)("Server version " + PhotonNetwork.room.CustomProperties[(object)"gameVersion"].ToString()));
		MessageHandler.ShowMessage("Online", "Joined " + PhotonNetwork.room.CustomProperties[(object)"gameName"].ToString(), 1.5f);
		Debug.LogWarning((object)"Photon: Joined Room");
		CheckJoinedGameState();
		if (JoinedRoom != null)
		{
			JoinedRoom(PhotonNetwork.room);
		}
		SyncCheckpoint();
	}

	public static void SyncCheckpoint()
	{
		bool result = false;
		if (IsOnline && !IsMaster && ((Dictionary<object, object>)(object)PhotonNetwork.room.CustomProperties).ContainsKey((object)"Checkpoint"))
		{
			bool.TryParse(PhotonNetwork.room.CustomProperties[(object)"Checkpoint"].ToString(), out result);
			GameDifficulty.ForceChangeCheckpointState(result);
		}
	}

	private void OnPhotonPlayerConnected()
	{
		Debug.Log((object)"p connected");
		if (JoinedRoom != null)
		{
			JoinedRoom(PhotonNetwork.room);
		}
	}

	private void OnPhotonPlayerConnected(PhotonPlayer player)
	{
		Debug.LogWarning((object)("playerJoined " + PhotonNetwork.room.PlayerCount));
		PlayerCount = PhotonNetwork.room.PlayerCount;
		if (PlayerConnected != null)
		{
			PlayerConnected(player);
		}
	}

	private void OnDisconnectedFromPhoton()
	{
		if (PlayerDissconetedFromPhoton != null)
		{
			PlayerDissconetedFromPhoton();
		}
		IsConnectedToMasterState = -1;
		ApplyNetworkConfiguration();
		BeingAuth();
		Debug.Log((object)"Disconnected from photon, reconnecting");
	}

	private void OnLeftLobby()
	{
		SyncHandler.Instance.Clean();
	}

	static NetworkManager()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		hashInfo = new Hashtable();
	}

	private void ApplyNetworkConfiguration()
	{
		string text = string.Empty;
		int serverPort = 5055;
		try
		{
			string text2 = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
			if (!File.Exists(text2))
			{
				text2 = Path.Combine(Application.persistentDataPath, "LANSettings.ini");
			}
			if (!File.Exists(text2))
			{
				text2 = Path.Combine(Environment.CurrentDirectory, "LANSettings.ini");
			}
			if (File.Exists(text2))
			{
				IniParser iniParser = new IniParser();
				iniParser.Load(text2);
				text = iniParser.GetValue("Server", "ServerAddress", string.Empty);
				serverPort = iniParser.GetIntValue("Server", "ServerPort", 5055);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[NetworkManager] Error loading config: " + ex.Message));
		}
		if (!string.IsNullOrEmpty(text))
		{
			PhotonNetwork.PhotonServerSettings.UseMyServer(text, serverPort, "master");
			if (PhotonNetwork.AuthValues == null)
			{
				PhotonNetwork.AuthValues = new AuthenticationValues();
			}
			if (string.IsNullOrEmpty(PhotonNetwork.AuthValues.UserId))
			{
				string userId = Guid.NewGuid().ToString();
				PhotonNetwork.AuthValues.UserId = userId;
				if (PhotonNetwork.player != null)
				{
					PhotonNetwork.player.UserId = userId;
				}
			}
			Debug.Log((object)("[NetworkManager] Using self-hosted server: " + text + ":" + serverPort + " UserId: " + PhotonNetwork.AuthValues.UserId));
		}
		else
		{
			PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.BestRegion;
			Debug.Log((object)"[NetworkManager] Using Photon Cloud (Best Region)");
		}
	}

	public static void ApplyNetworkConfigurationStatic()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			Instance.ApplyNetworkConfiguration();
		}
	}

	public static void JoinRoomByName(string roomName)
	{
		if ((Object)(object)Instance != (Object)null)
		{
			Instance.JoinRoom(roomName);
		}
	}

	public static void LeaveCurrentRoom()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			Instance.ExitRoomInternal();
		}
	}

	private void ExitRoomInternal()
	{
		PhotonNetwork.LeaveRoom();
	}
}
