using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;

internal class NetworkingPeer : LoadBalancingPeer, IPhotonPeerListener
{
	protected internal string AppId;

	private string tokenCache;

	public AuthModeOption AuthMode;

	public EncryptionMode EncryptionMode;

	public const string NameServerHost = "ns.exitgames.com";

	public const string NameServerHttp = "http://ns.exitgamescloud.com:80/photon/n";

	private static readonly Dictionary<ConnectionProtocol, int> ProtocolToNameServerPort;

	public bool IsInitialConnect;

	public bool insideLobby;

	protected internal List<TypedLobbyInfo> LobbyStatistics = new List<TypedLobbyInfo>();

	public Dictionary<string, RoomInfo> mGameList = new Dictionary<string, RoomInfo>();

	public RoomInfo[] mGameListCopy = new RoomInfo[0];

	private string playername = "";

	private bool mPlayernameHasToBeUpdated;

	private Room currentRoom;

	private JoinType lastJoinType;

	protected internal EnterRoomParams enterRoomParamsCache;

	private bool didAuthenticate;

	private string[] friendListRequested;

	private int friendListTimestamp;

	private bool isFetchingFriendList;

	private string cloudCluster;

	public string CurrentCluster;

	public Dictionary<int, PhotonPlayer> mActors = new Dictionary<int, PhotonPlayer>();

	public PhotonPlayer[] mOtherPlayerListCopy = new PhotonPlayer[0];

	public PhotonPlayer[] mPlayerListCopy = new PhotonPlayer[0];

	public bool hasSwitchedMC;

	private HashSet<byte> allowedReceivingGroups = new HashSet<byte>();

	private HashSet<byte> blockSendingGroups = new HashSet<byte>();

	protected internal Dictionary<int, PhotonView> photonViewList = new Dictionary<int, PhotonView>();

	private readonly PhotonStream readStream = new PhotonStream(write: false, null);

	private readonly PhotonStream pStream = new PhotonStream(write: true, null);

	private readonly Dictionary<int, Hashtable> dataPerGroupReliable = new Dictionary<int, Hashtable>();

	private readonly Dictionary<int, Hashtable> dataPerGroupUnreliable = new Dictionary<int, Hashtable>();

	protected internal short currentLevelPrefix;

	protected internal bool loadingLevelAndPausedNetwork;

	protected internal const string CurrentSceneProperty = "curScn";

	protected internal const string CurrentScenePropertyLoadAsync = "curScnLa";

	public static bool UsePrefabCache;

	internal IPunPrefabPool ObjectPool;

	public static Dictionary<string, GameObject> PrefabCache;

	private Dictionary<Type, List<MethodInfo>> monoRPCMethodsCache = new Dictionary<Type, List<MethodInfo>>();

	private readonly Dictionary<string, int> rpcShortcuts;

	private static readonly string OnPhotonInstantiateString;

	private string cachedServerAddress;

	private string cachedApplicationName;

	private ServerConnection cachedServerType;

	private AsyncOperation _AsyncLevelLoadingOperation;

	private RaiseEventOptions _levelReloadEventOptions = new RaiseEventOptions
	{
		Receivers = ReceiverGroup.Others
	};

	private bool _isReconnecting;

	private readonly Type typePunRPC = typeof(PunRPC);

	private readonly Type typePhotonMessageInfo = typeof(PhotonMessageInfo);

	private readonly object keyByteZero = (byte)0;

	private readonly object keyByteOne = (byte)1;

	private readonly object keyByteTwo = (byte)2;

	private readonly object keyByteThree = (byte)3;

	private readonly object keyByteFour = (byte)4;

	private readonly object keyByteFive = (byte)5;

	private readonly object[] emptyObjectArray = new object[0];

	private readonly Type[] emptyTypeArray = new Type[0];

	private Dictionary<int, object[]> tempInstantiationData = new Dictionary<int, object[]>();

	private readonly Hashtable reusedRpcEvent = new Hashtable();

	public static int ObjectsInOneUpdate;

	private RaiseEventOptions options = new RaiseEventOptions();

	public const int SyncViewId = 0;

	public const int SyncCompressed = 1;

	public const int SyncNullValues = 2;

	public const int SyncFirstValue = 3;

	public bool IsReloadingLevel;

	public bool AsynchLevelLoadCall;

	private const string iniFileName = "Kirigiri.ini";

	protected internal string AppVersion => string.Format("{0}_{1}", PhotonNetwork.gameVersion, "1.103.1");

	public AuthenticationValues AuthValues { get; set; }

	private string TokenForInit
	{
		get
		{
			if (AuthMode == AuthModeOption.Auth)
			{
				return null;
			}
			if (AuthValues == null)
			{
				return null;
			}
			return AuthValues.Token;
		}
	}

	public bool IsUsingNameServer { get; protected internal set; }

	public string NameServerAddress => GetNameServerAddress();

	public string MasterServerAddress { get; protected internal set; }

	public string GameServerAddress { get; protected internal set; }

	protected internal ServerConnection Server { get; private set; }

	public ClientState State { get; internal set; }

	public TypedLobby lobby { get; set; }

	private bool requestLobbyStatistics
	{
		get
		{
			if (PhotonNetwork.EnableLobbyStatistics)
			{
				return Server == ServerConnection.MasterServer;
			}
			return false;
		}
	}

	public string PlayerName
	{
		get
		{
			return playername;
		}
		set
		{
			if (!string.IsNullOrEmpty(value) && !value.Equals(playername))
			{
				if (LocalPlayer != null)
				{
					LocalPlayer.NickName = value;
				}
				playername = value;
				if (CurrentRoom != null)
				{
					SendPlayerName();
				}
			}
		}
	}

	public Room CurrentRoom
	{
		get
		{
			if (currentRoom != null && currentRoom.IsLocalClientInside)
			{
				return currentRoom;
			}
			return null;
		}
		private set
		{
			currentRoom = value;
		}
	}

	public PhotonPlayer LocalPlayer { get; internal set; }

	public int PlayersOnMasterCount { get; internal set; }

	public int PlayersInRoomsCount { get; internal set; }

	public int RoomsCount { get; internal set; }

	protected internal int FriendListAge
	{
		get
		{
			if (!isFetchingFriendList && friendListTimestamp != 0)
			{
				return Environment.TickCount - friendListTimestamp;
			}
			return 0;
		}
	}

	public bool IsAuthorizeSecretAvailable
	{
		get
		{
			if (AuthValues != null)
			{
				return !string.IsNullOrEmpty(AuthValues.Token);
			}
			return false;
		}
	}

	public List<Region> AvailableRegions { get; protected internal set; }

	public CloudRegionCode CloudRegion { get; protected internal set; }

	public int mMasterClientId
	{
		get
		{
			if (PhotonNetwork.offlineMode)
			{
				return LocalPlayer.ID;
			}
			if (CurrentRoom != null)
			{
				return CurrentRoom.MasterClientId;
			}
			return 0;
		}
		private set
		{
			if (CurrentRoom != null)
			{
				CurrentRoom.MasterClientId = value;
			}
		}
	}

	public NetworkingPeer(string playername, ConnectionProtocol connectionProtocol)
		: base(connectionProtocol)
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		((PhotonPeer)this).Listener = (IPhotonPeerListener)(object)this;
		lobby = TypedLobby.Default;
		PlayerName = playername;
		LocalPlayer = new PhotonPlayer(isLocal: true, -1, this.playername);
		AddNewPlayer(LocalPlayer.ID, LocalPlayer);
		rpcShortcuts = new Dictionary<string, int>(PhotonNetwork.PhotonServerSettings.RpcList.Count);
		for (int i = 0; i < PhotonNetwork.PhotonServerSettings.RpcList.Count; i++)
		{
			string key = PhotonNetwork.PhotonServerSettings.RpcList[i];
			rpcShortcuts[key] = i;
		}
		State = ClientState.PeerCreated;
	}

	private string GetNameServerAddress()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		ConnectionProtocol transportProtocol = ((PhotonPeer)this).TransportProtocol;
		int value = 0;
		ProtocolToNameServerPort.TryGetValue(transportProtocol, out value);
		string arg = string.Empty;
		if ((int)transportProtocol == 4)
		{
			arg = "ws://";
		}
		else if ((int)transportProtocol == 5)
		{
			arg = "wss://";
		}
		if (PhotonNetwork.UseAlternativeUdpPorts && (int)((PhotonPeer)this).TransportProtocol == 0)
		{
			value = 27000;
		}
		return string.Format("{0}{1}:{2}", arg, "ns.exitgames.com", value);
	}

	public override bool Connect(string serverAddress, string applicationName)
	{
		Debug.LogError((object)"Avoid using this directly. Thanks.");
		return false;
	}

	public bool ReconnectToMaster()
	{
		if (AuthValues == null)
		{
			Debug.LogWarning((object)"ReconnectToMaster() with AuthValues == null is not correct!");
			AuthValues = new AuthenticationValues();
		}
		AuthValues.Token = tokenCache;
		return Connect(MasterServerAddress, ServerConnection.MasterServer);
	}

	public bool ReconnectAndRejoin()
	{
		if (AuthValues == null)
		{
			Debug.LogWarning((object)"ReconnectAndRejoin() with AuthValues == null is not correct!");
			AuthValues = new AuthenticationValues();
		}
		AuthValues.Token = tokenCache;
		if (!string.IsNullOrEmpty(GameServerAddress) && enterRoomParamsCache != null)
		{
			lastJoinType = JoinType.JoinRoom;
			enterRoomParamsCache.RejoinOnly = true;
			return Connect(GameServerAddress, ServerConnection.GameServer);
		}
		return false;
	}

	public bool Connect(string serverAddress, ServerConnection type)
	{
		if (PhotonHandler.AppQuits)
		{
			Debug.LogWarning((object)"Ignoring Connect() because app gets closed. If this is an error, check PhotonHandler.AppQuits.");
			return false;
		}
		if (State == ClientState.Disconnecting)
		{
			Debug.LogError((object)("Connect() failed. Can't connect while disconnecting (still). Current state: " + PhotonNetwork.connectionStateDetailed));
			return false;
		}
		cachedServerType = type;
		cachedServerAddress = serverAddress;
		cachedApplicationName = string.Empty;
		SetupProtocol(type);
		bool flag = ((PhotonPeer)this).Connect(serverAddress, "", (object)TokenForInit);
		if (flag)
		{
			switch (type)
			{
			case ServerConnection.NameServer:
				State = ClientState.ConnectingToNameServer;
				break;
			case ServerConnection.MasterServer:
				State = ClientState.ConnectingToMasterserver;
				break;
			case ServerConnection.GameServer:
				State = ClientState.ConnectingToGameserver;
				break;
			}
		}
		return flag;
	}

	private bool Reconnect()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		_isReconnecting = true;
		PhotonNetwork.SwitchToProtocol(PhotonNetwork.PhotonServerSettings.Protocol);
		SetupProtocol(cachedServerType);
		bool flag = ((PhotonPeer)this).Connect(cachedServerAddress, cachedApplicationName, (object)TokenForInit);
		if (flag)
		{
			switch (cachedServerType)
			{
			case ServerConnection.NameServer:
				State = ClientState.ConnectingToNameServer;
				break;
			case ServerConnection.MasterServer:
				State = ClientState.ConnectingToMasterserver;
				break;
			case ServerConnection.GameServer:
				State = ClientState.ConnectingToGameserver;
				break;
			}
		}
		return flag;
	}

	public bool ConnectToNameServer()
	{
		if (PhotonHandler.AppQuits)
		{
			Debug.LogWarning((object)"Ignoring Connect() because app gets closed. If this is an error, check PhotonHandler.AppQuits.");
			return false;
		}
		IsUsingNameServer = true;
		CloudRegion = CloudRegionCode.none;
		cloudCluster = null;
		if (State == ClientState.ConnectedToNameServer)
		{
			return true;
		}
		SetupProtocol(ServerConnection.NameServer);
		cachedServerType = ServerConnection.NameServer;
		cachedServerAddress = NameServerAddress;
		cachedApplicationName = "ns";
		if (!((PhotonPeer)this).Connect(NameServerAddress, "ns", (object)TokenForInit))
		{
			return false;
		}
		State = ClientState.ConnectingToNameServer;
		return true;
	}

	public bool ConnectToRegionMaster(CloudRegionCode region, string specificCluster = null)
	{
		if (PhotonHandler.AppQuits)
		{
			Debug.LogWarning((object)"Ignoring Connect() because app gets closed. If this is an error, check PhotonHandler.AppQuits.");
			return false;
		}
		IsUsingNameServer = true;
		CloudRegion = region;
		cloudCluster = specificCluster;
		if (State == ClientState.ConnectedToNameServer)
		{
			return CallAuthenticate();
		}
		cachedServerType = ServerConnection.NameServer;
		cachedServerAddress = NameServerAddress;
		cachedApplicationName = "ns";
		SetupProtocol(ServerConnection.NameServer);
		if (!((PhotonPeer)this).Connect(NameServerAddress, "ns", (object)TokenForInit))
		{
			return false;
		}
		State = ClientState.ConnectingToNameServer;
		return true;
	}

	protected internal void SetupProtocol(ServerConnection serverType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		ConnectionProtocol val = ((PhotonPeer)this).TransportProtocol;
		if (AuthMode == AuthModeOption.AuthOnceWss)
		{
			if (serverType != ServerConnection.NameServer)
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.ErrorsOnly)
				{
					Debug.LogWarning((object)("Using PhotonServerSettings.Protocol when leaving the NameServer (AuthMode is AuthOnceWss): " + PhotonNetwork.PhotonServerSettings.Protocol));
				}
				val = PhotonNetwork.PhotonServerSettings.Protocol;
			}
			else
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.ErrorsOnly)
				{
					Debug.LogWarning((object)"Using WebSocket to connect NameServer (AuthMode is AuthOnceWss).");
				}
				val = (ConnectionProtocol)5;
			}
		}
		Type type = null;
		bool num = ((PhotonPeer)this).SocketImplementationConfig == null || !((PhotonPeer)this).SocketImplementationConfig.ContainsKey((ConnectionProtocol)4) || ((PhotonPeer)this).SocketImplementationConfig[(ConnectionProtocol)4] == null;
		bool flag = ((PhotonPeer)this).SocketImplementationConfig == null || !((PhotonPeer)this).SocketImplementationConfig.ContainsKey((ConnectionProtocol)5) || ((PhotonPeer)this).SocketImplementationConfig[(ConnectionProtocol)5] == null;
		if (num || flag)
		{
			type = Type.GetType("ExitGames.Client.Photon.SocketWebTcp, Assembly-CSharp", throwOnError: false);
			if (type == null)
			{
				type = Type.GetType("ExitGames.Client.Photon.SocketWebTcp, Assembly-CSharp-firstpass", throwOnError: false);
			}
		}
		if (type != null)
		{
			((PhotonPeer)this).SocketImplementationConfig[(ConnectionProtocol)4] = type;
			((PhotonPeer)this).SocketImplementationConfig[(ConnectionProtocol)5] = type;
		}
		if (PhotonHandler.PingImplementation == null)
		{
			PhotonHandler.PingImplementation = typeof(PingMono);
		}
		if (((PhotonPeer)this).TransportProtocol != val)
		{
			if (PhotonNetwork.logLevel >= PhotonLogLevel.ErrorsOnly)
			{
				Debug.LogWarning((object)string.Concat("Protocol switch from: ", ((PhotonPeer)this).TransportProtocol, " to: ", val, "."));
			}
			((PhotonPeer)this).TransportProtocol = val;
		}
	}

	public override void Disconnect()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((PhotonPeer)this).PeerState == 0)
		{
			if (!PhotonHandler.AppQuits)
			{
				Debug.LogWarning((object)$"Can't execute Disconnect() while not connected. Nothing changed. State: {State}");
			}
		}
		else
		{
			State = ClientState.Disconnecting;
			((PhotonPeer)this).Disconnect();
		}
	}

	private bool CallAuthenticate()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		AuthenticationValues authenticationValues = AuthValues ?? new AuthenticationValues
		{
			UserId = PlayerName
		};
		if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.SelfHosted && string.IsNullOrEmpty(authenticationValues.UserId))
		{
			authenticationValues.UserId = Guid.NewGuid().ToString();
		}
		if (string.IsNullOrEmpty(cloudCluster))
		{
			cloudCluster = "*";
		}
		string regionCode = string.Concat(CloudRegion, "/", cloudCluster);
		if (AuthMode == AuthModeOption.Auth)
		{
			return OpAuthenticate(AppId, AppVersion, authenticationValues, regionCode, requestLobbyStatistics);
		}
		return OpAuthenticateOnce(AppId, AppVersion, authenticationValues, regionCode, EncryptionMode, PhotonNetwork.PhotonServerSettings.Protocol);
	}

	private void DisconnectToReconnect()
	{
		switch (Server)
		{
		case ServerConnection.NameServer:
			State = ClientState.DisconnectingFromNameServer;
			((PhotonPeer)this).Disconnect();
			break;
		case ServerConnection.MasterServer:
			State = ClientState.DisconnectingFromMasterserver;
			((PhotonPeer)this).Disconnect();
			break;
		case ServerConnection.GameServer:
			State = ClientState.DisconnectingFromGameserver;
			((PhotonPeer)this).Disconnect();
			break;
		}
	}

	public bool GetRegions()
	{
		if (Server != ServerConnection.NameServer)
		{
			return false;
		}
		bool num = OpGetRegions(AppId);
		if (num)
		{
			AvailableRegions = null;
		}
		return num;
	}

	public override bool OpFindFriends(string[] friendsToFind, FindFriendsOptions findFriendsOptions = null)
	{
		if (isFetchingFriendList)
		{
			return false;
		}
		friendListRequested = friendsToFind;
		isFetchingFriendList = true;
		return base.OpFindFriends(friendsToFind, findFriendsOptions);
	}

	public bool OpCreateGame(EnterRoomParams enterRoomParams)
	{
		bool flag = (enterRoomParams.OnGameServer = Server == ServerConnection.GameServer);
		enterRoomParams.PlayerProperties = GetLocalActorProperties();
		if (!flag)
		{
			enterRoomParamsCache = enterRoomParams;
		}
		lastJoinType = JoinType.CreateRoom;
		return base.OpCreateRoom(enterRoomParams);
	}

	public override bool OpJoinRoom(EnterRoomParams opParams)
	{
		if (!(opParams.OnGameServer = Server == ServerConnection.GameServer))
		{
			enterRoomParamsCache = opParams;
		}
		lastJoinType = ((!opParams.CreateIfNotExists) ? JoinType.JoinRoom : JoinType.JoinOrCreateRoom);
		return base.OpJoinRoom(opParams);
	}

	public override bool OpJoinRandomRoom(OpJoinRandomRoomParams opJoinRandomRoomParams)
	{
		enterRoomParamsCache = new EnterRoomParams();
		enterRoomParamsCache.Lobby = opJoinRandomRoomParams.TypedLobby;
		enterRoomParamsCache.ExpectedUsers = opJoinRandomRoomParams.ExpectedUsers;
		lastJoinType = JoinType.JoinRandomRoom;
		return base.OpJoinRandomRoom(opJoinRandomRoomParams);
	}

	public override bool OpRaiseEvent(byte eventCode, object customEventContent, bool sendReliable, RaiseEventOptions raiseEventOptions)
	{
		if (PhotonNetwork.offlineMode)
		{
			return false;
		}
		return base.OpRaiseEvent(eventCode, customEventContent, sendReliable, raiseEventOptions);
	}

	private void ReadoutProperties(Hashtable gameProperties, Hashtable pActorProperties, int targetActorNr)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		if (pActorProperties != null && ((Dictionary<object, object>)(object)pActorProperties).Count > 0)
		{
			if (targetActorNr > 0)
			{
				PhotonPlayer playerWithId = GetPlayerWithId(targetActorNr);
				if (playerWithId != null)
				{
					Hashtable val = ReadoutPropertiesForActorNr(pActorProperties, targetActorNr);
					playerWithId.InternalCacheProperties(val);
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerPropertiesChanged, playerWithId, val);
				}
			}
			else
			{
				foreach (object key in ((Dictionary<object, object>)(object)pActorProperties).Keys)
				{
					int num = (int)key;
					Hashtable val2 = (Hashtable)pActorProperties[key];
					string name = (string)val2[(object)byte.MaxValue];
					PhotonPlayer photonPlayer = GetPlayerWithId(num);
					if (photonPlayer == null)
					{
						photonPlayer = new PhotonPlayer(isLocal: false, num, name);
						AddNewPlayer(num, photonPlayer);
					}
					photonPlayer.InternalCacheProperties(val2);
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerPropertiesChanged, photonPlayer, val2);
				}
			}
		}
		if (CurrentRoom != null && gameProperties != null)
		{
			CurrentRoom.InternalCacheProperties(gameProperties);
			SendMonoMessage(PhotonNetworkingMessage.OnPhotonCustomRoomPropertiesChanged, gameProperties);
			if (PhotonNetwork.automaticallySyncScene)
			{
				LoadLevelIfSynced();
			}
		}
	}

	private Hashtable ReadoutPropertiesForActorNr(Hashtable actorProperties, int actorNr)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		if (((Dictionary<object, object>)(object)actorProperties).ContainsKey((object)actorNr))
		{
			return (Hashtable)actorProperties[(object)actorNr];
		}
		return actorProperties;
	}

	public void ChangeLocalID(int newID)
	{
		if (LocalPlayer == null)
		{
			Debug.LogWarning((object)$"LocalPlayer is null or not in mActors! LocalPlayer: {LocalPlayer} mActors==null: {mActors == null} newID: {newID}");
		}
		if (mActors.ContainsKey(LocalPlayer.ID))
		{
			mActors.Remove(LocalPlayer.ID);
		}
		LocalPlayer.InternalChangeLocalID(newID);
		mActors[LocalPlayer.ID] = LocalPlayer;
		RebuildPlayerListCopies();
	}

	private void LeftLobbyCleanup()
	{
		mGameList = new Dictionary<string, RoomInfo>();
		mGameListCopy = new RoomInfo[0];
		if (insideLobby)
		{
			insideLobby = false;
			SendMonoMessage(PhotonNetworkingMessage.OnLeftLobby);
		}
	}

	private void LeftRoomCleanup()
	{
		bool num = CurrentRoom != null;
		bool num2 = ((CurrentRoom != null) ? CurrentRoom.AutoCleanUp : PhotonNetwork.autoCleanUpPlayerObjects);
		hasSwitchedMC = false;
		CurrentRoom = null;
		mActors = new Dictionary<int, PhotonPlayer>();
		mPlayerListCopy = new PhotonPlayer[0];
		mOtherPlayerListCopy = new PhotonPlayer[0];
		allowedReceivingGroups = new HashSet<byte>();
		blockSendingGroups = new HashSet<byte>();
		mGameList = new Dictionary<string, RoomInfo>();
		mGameListCopy = new RoomInfo[0];
		isFetchingFriendList = false;
		ChangeLocalID(-1);
		if (num2)
		{
			LocalCleanupAnythingInstantiated(destroyInstantiatedGameObjects: true);
			PhotonNetwork.manuallyAllocatedViewIds = new List<int>();
		}
		if (num)
		{
			SendMonoMessage(PhotonNetworkingMessage.OnLeftRoom);
		}
	}

	protected internal void LocalCleanupAnythingInstantiated(bool destroyInstantiatedGameObjects)
	{
		if (tempInstantiationData.Count > 0)
		{
			Debug.LogWarning((object)"It seems some instantiation is not completed, as instantiation data is used. You should make sure instantiations are paused when calling this method. Cleaning now, despite this.");
		}
		if (destroyInstantiatedGameObjects)
		{
			HashSet<GameObject> hashSet = new HashSet<GameObject>();
			foreach (PhotonView value in photonViewList.Values)
			{
				if (value.isRuntimeInstantiated)
				{
					hashSet.Add(((Component)value).gameObject);
				}
			}
			foreach (GameObject item in hashSet)
			{
				RemoveInstantiatedGO(item, localOnly: true);
			}
		}
		tempInstantiationData.Clear();
		PhotonNetwork.lastUsedViewSubId = 0;
		PhotonNetwork.lastUsedViewSubIdStatic = 0;
	}

	private void GameEnteredOnGameServer(OperationResponse operationResponse)
	{
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected O, but got Unknown
		if (operationResponse.ReturnCode != 0)
		{
			switch (operationResponse.OperationCode)
			{
			case 227:
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
				{
					Debug.Log((object)("Create failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage));
				}
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonCreateRoomFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
				break;
			case 226:
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
				{
					Debug.Log((object)("Join failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage));
					if (operationResponse.ReturnCode == 32758)
					{
						Debug.Log((object)"Most likely the game became empty during the switch to GameServer.");
					}
				}
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonJoinRoomFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
				break;
			case 225:
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
				{
					Debug.Log((object)("Join failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage));
					if (operationResponse.ReturnCode == 32758)
					{
						Debug.Log((object)"Most likely the game became empty during the switch to GameServer.");
					}
				}
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonRandomJoinFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
				break;
			}
			DisconnectToReconnect();
		}
		else
		{
			Room room = new Room(enterRoomParamsCache.RoomName, enterRoomParamsCache.RoomOptions);
			room.IsLocalClientInside = true;
			CurrentRoom = room;
			State = ClientState.Joined;
			if (operationResponse.Parameters.ContainsKey(252))
			{
				int[] actorsInRoom = (int[])operationResponse.Parameters[252];
				UpdatedActorList(actorsInRoom);
			}
			int newID = (int)operationResponse[(byte)254];
			ChangeLocalID(newID);
			Hashtable pActorProperties = (Hashtable)operationResponse[(byte)249];
			Hashtable gameProperties = (Hashtable)operationResponse[(byte)248];
			ReadoutProperties(gameProperties, pActorProperties, 0);
			if (!CurrentRoom.serverSideMasterClient)
			{
				CheckMasterClient(-1);
			}
			if (mPlayernameHasToBeUpdated)
			{
				SendPlayerName();
			}
			byte operationCode = operationResponse.OperationCode;
			if ((uint)(operationCode - 225) > 1u && operationCode == 227)
			{
				SendMonoMessage(PhotonNetworkingMessage.OnCreatedRoom);
			}
		}
	}

	private void AddNewPlayer(int ID, PhotonPlayer player)
	{
		if (!mActors.ContainsKey(ID))
		{
			mActors[ID] = player;
			RebuildPlayerListCopies();
		}
		else
		{
			Debug.LogError((object)("Adding player twice: " + ID));
		}
	}

	private void RemovePlayer(int ID, PhotonPlayer player)
	{
		mActors.Remove(ID);
		if (!player.IsLocal)
		{
			RebuildPlayerListCopies();
		}
	}

	private void RebuildPlayerListCopies()
	{
		mPlayerListCopy = new PhotonPlayer[mActors.Count];
		mActors.Values.CopyTo(mPlayerListCopy, 0);
		List<PhotonPlayer> list = new List<PhotonPlayer>();
		for (int i = 0; i < mPlayerListCopy.Length; i++)
		{
			PhotonPlayer photonPlayer = mPlayerListCopy[i];
			if (!photonPlayer.IsLocal)
			{
				list.Add(photonPlayer);
			}
		}
		mOtherPlayerListCopy = list.ToArray();
	}

	private void ResetPhotonViewsOnSerialize()
	{
		foreach (PhotonView value in photonViewList.Values)
		{
			value.lastOnSerializeDataSent = null;
		}
	}

	private void HandleEventLeave(int actorID, EventData evLeave)
	{
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
		{
			Debug.Log((object)("HandleEventLeave for player ID: " + actorID + " evLeave: " + evLeave.ToStringFull()));
		}
		PhotonPlayer playerWithId = GetPlayerWithId(actorID);
		if (playerWithId == null)
		{
			Debug.LogError((object)$"Received event Leave for unknown player ID: {actorID}");
			return;
		}
		bool isInactive = playerWithId.IsInactive;
		if (evLeave.Parameters.ContainsKey(233))
		{
			playerWithId.IsInactive = (bool)evLeave.Parameters[233];
			if (playerWithId.IsInactive != isInactive)
			{
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerActivityChanged, playerWithId);
			}
			if (playerWithId.IsInactive && isInactive)
			{
				Debug.LogWarning((object)("HandleEventLeave for player ID: " + actorID + " isInactive: " + playerWithId.IsInactive.ToString() + ". Stopping handling if inactive."));
				return;
			}
		}
		if (evLeave.Parameters.ContainsKey(203))
		{
			if ((int)evLeave[(byte)203] != 0)
			{
				mMasterClientId = (int)evLeave[(byte)203];
				UpdateMasterClient();
			}
		}
		else if (!CurrentRoom.serverSideMasterClient)
		{
			CheckMasterClient(actorID);
		}
		if (!playerWithId.IsInactive || isInactive)
		{
			if (CurrentRoom != null && CurrentRoom.AutoCleanUp)
			{
				DestroyPlayerObjects(actorID, localOnly: true);
			}
			RemovePlayer(actorID, playerWithId);
			SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerDisconnected, playerWithId);
		}
	}

	private void CheckMasterClient(int leavingPlayerId)
	{
		bool flag = mMasterClientId == leavingPlayerId;
		bool flag2 = leavingPlayerId > 0;
		if (flag2 && !flag)
		{
			return;
		}
		int num;
		if (mActors.Count <= 1)
		{
			num = LocalPlayer.ID;
		}
		else
		{
			num = int.MaxValue;
			foreach (int key in mActors.Keys)
			{
				if (key < num && key != leavingPlayerId)
				{
					num = key;
				}
			}
		}
		mMasterClientId = num;
		if (flag2)
		{
			SendMonoMessage(PhotonNetworkingMessage.OnMasterClientSwitched, GetPlayerWithId(num));
		}
	}

	protected internal void UpdateMasterClient()
	{
		SendMonoMessage(PhotonNetworkingMessage.OnMasterClientSwitched, PhotonNetwork.masterClient);
	}

	private static int ReturnLowestPlayerId(PhotonPlayer[] players, int playerIdToIgnore)
	{
		if (players == null || players.Length == 0)
		{
			return -1;
		}
		int num = int.MaxValue;
		foreach (PhotonPlayer photonPlayer in players)
		{
			if (photonPlayer.ID != playerIdToIgnore && photonPlayer.ID < num)
			{
				num = photonPlayer.ID;
			}
		}
		return num;
	}

	protected internal bool SetMasterClient(int playerId, bool sync)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_0045: Expected O, but got Unknown
		if (mMasterClientId == playerId || !mActors.ContainsKey(playerId))
		{
			return false;
		}
		if (sync)
		{
			Hashtable val = new Hashtable();
			((Dictionary<object, object>)val).Add((object)(byte)1, (object)playerId);
			if (!OpRaiseEvent(208, (object)val, sendReliable: true, null))
			{
				return false;
			}
		}
		hasSwitchedMC = true;
		CurrentRoom.MasterClientId = playerId;
		SendMonoMessage(PhotonNetworkingMessage.OnMasterClientSwitched, GetPlayerWithId(playerId));
		return true;
	}

	public bool SetMasterClient(int nextMasterId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_001c: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_003d: Expected O, but got Unknown
		Hashtable val = new Hashtable();
		((Dictionary<object, object>)val).Add((object)(byte)248, (object)nextMasterId);
		Hashtable gameProperties = val;
		Hashtable val2 = new Hashtable();
		((Dictionary<object, object>)val2).Add((object)(byte)248, (object)mMasterClientId);
		Hashtable expectedProperties = val2;
		return OpSetPropertiesOfRoom(gameProperties, expectedProperties);
	}

	protected internal PhotonPlayer GetPlayerWithId(int number)
	{
		if (mActors == null)
		{
			return null;
		}
		PhotonPlayer value = null;
		mActors.TryGetValue(number, out value);
		return value;
	}

	private void SendPlayerName()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		if (State == ClientState.Joining)
		{
			mPlayernameHasToBeUpdated = true;
		}
		else if (LocalPlayer != null)
		{
			LocalPlayer.NickName = PlayerName;
			Hashtable val = new Hashtable();
			val[(object)byte.MaxValue] = PlayerName;
			if (LocalPlayer.ID > 0)
			{
				OpSetPropertiesOfActor(LocalPlayer.ID, val);
				mPlayernameHasToBeUpdated = false;
			}
		}
	}

	private Hashtable GetLocalActorProperties()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		if (PhotonNetwork.player != null)
		{
			return PhotonNetwork.player.AllProperties;
		}
		return new Hashtable { [(object)byte.MaxValue] = PlayerName };
	}

	public void DebugReturn(DebugLevel level, string message)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		if ((int)level == 1)
		{
			Debug.LogError((object)message);
		}
		else if ((int)level == 2)
		{
			Debug.LogWarning((object)message);
		}
		else if ((int)level == 3 && PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
		{
			Debug.Log((object)message);
		}
		else if ((int)level == 5 && PhotonNetwork.logLevel == PhotonLogLevel.Full)
		{
			Debug.Log((object)message);
		}
	}

	public void OnOperationResponse(OperationResponse operationResponse)
	{
		//IL_0b77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7d: Expected O, but got Unknown
		//IL_0c4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c53: Expected O, but got Unknown
		//IL_0c5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c65: Expected O, but got Unknown
		//IL_0afa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bba: Expected O, but got Unknown
		//IL_093e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		if (PhotonNetwork.networkingPeer.State == ClientState.Disconnecting)
		{
			if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
			{
				Debug.Log((object)("OperationResponse ignored while disconnecting. Code: " + operationResponse.OperationCode));
			}
			return;
		}
		if (operationResponse.ReturnCode == 0)
		{
			if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
			{
				Debug.Log((object)((object)operationResponse).ToString());
			}
		}
		else if (operationResponse.ReturnCode == -3)
		{
			Debug.LogError((object)("Operation " + operationResponse.OperationCode + " could not be executed (yet). Wait for state JoinedLobby or ConnectedToMaster and their callbacks before calling operations. WebRPCs need a server-side configuration. Enum OperationCode helps identify the operation."));
		}
		else if (operationResponse.ReturnCode == 32752)
		{
			Debug.LogError((object)("Operation " + operationResponse.OperationCode + " failed in a server-side plugin. Check the configuration in the Dashboard. Message from server-plugin: " + operationResponse.DebugMessage));
		}
		else if (operationResponse.ReturnCode == 32760)
		{
			Debug.LogWarning((object)("Operation failed: " + operationResponse.ToStringFull()));
		}
		else
		{
			Debug.LogError((object)("Operation failed: " + operationResponse.ToStringFull() + " Server: " + Server));
		}
		if (operationResponse.Parameters.ContainsKey(221))
		{
			if (AuthValues == null)
			{
				AuthValues = new AuthenticationValues();
			}
			AuthValues.Token = operationResponse[(byte)221] as string;
			tokenCache = AuthValues.Token;
		}
		switch (operationResponse.OperationCode)
		{
		case 230:
		case 231:
			if (operationResponse.ReturnCode != 0)
			{
				if (operationResponse.ReturnCode == -2)
				{
					Debug.LogError((object)string.Format("If you host Photon yourself, make sure to start the 'Instance LoadBalancing' " + ((PhotonPeer)this).ServerAddress));
				}
				else if (operationResponse.ReturnCode == short.MaxValue)
				{
					Debug.LogError((object)$"The appId this client sent is unknown on the server (Cloud). Check settings. If using the Cloud, check account.");
					SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, DisconnectCause.InvalidAuthentication);
				}
				else if (operationResponse.ReturnCode == 32755)
				{
					Debug.LogError((object)$"Custom Authentication failed (either due to user-input or configuration or AuthParameter string format). Calling: OnCustomAuthenticationFailed()");
					SendMonoMessage(PhotonNetworkingMessage.OnCustomAuthenticationFailed, operationResponse.DebugMessage);
				}
				else
				{
					Debug.LogError((object)$"Authentication failed: '{operationResponse.DebugMessage}' Code: {operationResponse.ReturnCode}");
				}
				State = ClientState.Disconnecting;
				((PhotonPeer)this).Disconnect();
				if (operationResponse.ReturnCode == 32757)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.LogWarning((object)$"Currently, the limit of users is reached for this title. Try again later. Disconnecting");
					}
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonMaxCccuReached);
					SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, DisconnectCause.MaxCcuReached);
				}
				else if (operationResponse.ReturnCode == 32756)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.LogError((object)$"The used master server address is not available with the subscription currently used. Got to Photon Cloud Dashboard or change URL. Disconnecting.");
					}
					SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, DisconnectCause.InvalidRegion);
				}
				else if (operationResponse.ReturnCode == 32753)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.LogError((object)$"The authentication ticket expired. You need to connect (and authenticate) again. Disconnecting.");
					}
					SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, DisconnectCause.AuthenticationTicketExpired);
				}
				break;
			}
			if (Server == ServerConnection.NameServer || Server == ServerConnection.MasterServer)
			{
				if (operationResponse.Parameters.ContainsKey(225))
				{
					string text4 = (string)operationResponse.Parameters[225];
					if (!string.IsNullOrEmpty(text4))
					{
						if (AuthValues == null)
						{
							AuthValues = new AuthenticationValues();
						}
						AuthValues.UserId = text4;
						PhotonNetwork.player.UserId = text4;
						if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
						{
							DebugReturn((DebugLevel)3, $"Received your UserID from server. Updating local value to: {text4}");
						}
					}
				}
				if (operationResponse.Parameters.ContainsKey(202))
				{
					PlayerName = (string)operationResponse.Parameters[202];
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						DebugReturn((DebugLevel)3, $"Received your NickName from server. Updating local value to: {playername}");
					}
				}
				if (operationResponse.Parameters.ContainsKey(192))
				{
					SetupEncryption((Dictionary<byte, object>)operationResponse.Parameters[192]);
				}
			}
			if (Server == ServerConnection.NameServer)
			{
				MasterServerAddress = operationResponse[(byte)230] as string;
				if (PhotonNetwork.UseAlternativeUdpPorts && (int)((PhotonPeer)this).TransportProtocol == 0)
				{
					MasterServerAddress = MasterServerAddress.Replace("5058", "27000").Replace("5055", "27001").Replace("5056", "27002");
				}
				string text5 = operationResponse[(byte)196] as string;
				if (!string.IsNullOrEmpty(text5))
				{
					CurrentCluster = text5;
				}
				DisconnectToReconnect();
			}
			else if (Server == ServerConnection.MasterServer)
			{
				if (AuthMode != AuthModeOption.Auth)
				{
					OpSettings(requestLobbyStatistics);
				}
				if (PhotonNetwork.autoJoinLobby)
				{
					State = ClientState.Authenticated;
					OpJoinLobby(lobby);
				}
				else
				{
					State = ClientState.ConnectedToMaster;
					SendMonoMessage(PhotonNetworkingMessage.OnConnectedToMaster);
				}
			}
			else if (Server == ServerConnection.GameServer)
			{
				State = ClientState.Joining;
				enterRoomParamsCache.PlayerProperties = GetLocalActorProperties();
				enterRoomParamsCache.OnGameServer = true;
				if (lastJoinType == JoinType.JoinRoom || lastJoinType == JoinType.JoinRandomRoom || lastJoinType == JoinType.JoinOrCreateRoom)
				{
					OpJoinRoom(enterRoomParamsCache);
				}
				else if (lastJoinType == JoinType.CreateRoom)
				{
					OpCreateGame(enterRoomParamsCache);
				}
			}
			if (operationResponse.Parameters.ContainsKey(245))
			{
				Dictionary<string, object> dictionary = (Dictionary<string, object>)operationResponse.Parameters[245];
				if (dictionary != null)
				{
					SendMonoMessage(PhotonNetworkingMessage.OnCustomAuthenticationResponse, dictionary);
				}
			}
			break;
		case 220:
		{
			if (operationResponse.ReturnCode == short.MaxValue)
			{
				Debug.LogError((object)$"The appId this client sent is unknown on the server (Cloud). Check settings. If using the Cloud, check account.");
				SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, DisconnectCause.InvalidAuthentication);
				State = ClientState.Disconnecting;
				((PhotonPeer)this).Disconnect();
				break;
			}
			if (operationResponse.ReturnCode != 0)
			{
				Debug.LogError((object)("GetRegions failed. Can't provide regions list. Error: " + operationResponse.ReturnCode + ": " + operationResponse.DebugMessage));
				break;
			}
			string[] array3 = operationResponse[(byte)210] as string[];
			string[] array4 = operationResponse[(byte)230] as string[];
			if (array3 == null || array4 == null || array3.Length != array4.Length)
			{
				Debug.LogError((object)("The region arrays from Name Server are not ok. Must be non-null and same length. " + (array3 == null) + " " + (array4 == null) + "\n" + operationResponse.ToStringFull()));
				break;
			}
			AvailableRegions = new List<Region>(array3.Length);
			for (int j = 0; j < array3.Length; j++)
			{
				string text2 = array3[j];
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				text2 = text2.ToLower();
				CloudRegionCode cloudRegionCode = Region.Parse(text2);
				bool flag = true;
				if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.BestRegion && PhotonNetwork.PhotonServerSettings.EnabledRegions != 0)
				{
					CloudRegionFlag cloudRegionFlag = Region.ParseFlag(cloudRegionCode);
					flag = (PhotonNetwork.PhotonServerSettings.EnabledRegions & cloudRegionFlag) != 0;
					if (!flag && PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.Log((object)("Skipping region because it's not in PhotonServerSettings.EnabledRegions: " + cloudRegionCode));
					}
				}
				if (flag)
				{
					AvailableRegions.Add(new Region(cloudRegionCode, text2, array4[j]));
				}
			}
			if (PhotonNetwork.PhotonServerSettings.HostType != ServerSettings.HostingOption.BestRegion)
			{
				break;
			}
			CloudRegionCode bestFromPrefs = PhotonHandler.BestRegionCodeInPreferences;
			if (bestFromPrefs != CloudRegionCode.none && AvailableRegions.Exists((Region x) => x.Code == bestFromPrefs))
			{
				Debug.Log((object)("Best region found in PlayerPrefs. Connecting to: " + bestFromPrefs));
				if (!ConnectToRegionMaster(bestFromPrefs))
				{
					PhotonHandler.PingAvailableRegionsAndConnectToBest();
				}
			}
			else
			{
				PhotonHandler.PingAvailableRegionsAndConnectToBest();
			}
			break;
		}
		case 227:
		{
			if (Server == ServerConnection.GameServer)
			{
				GameEnteredOnGameServer(operationResponse);
				break;
			}
			if (operationResponse.ReturnCode != 0)
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
				{
					Debug.LogWarning((object)$"CreateRoom failed, client stays on masterserver: {operationResponse.ToStringFull()}.");
				}
				State = (insideLobby ? ClientState.JoinedLobby : ClientState.ConnectedToMaster);
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonCreateRoomFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
				break;
			}
			string text3 = (string)operationResponse[byte.MaxValue];
			if (!string.IsNullOrEmpty(text3))
			{
				enterRoomParamsCache.RoomName = text3;
			}
			GameServerAddress = (string)operationResponse[(byte)230];
			if (PhotonNetwork.UseAlternativeUdpPorts && (int)((PhotonPeer)this).TransportProtocol == 0)
			{
				GameServerAddress = GameServerAddress.Replace("5058", "27000").Replace("5055", "27001").Replace("5056", "27002");
			}
			DisconnectToReconnect();
			break;
		}
		case 226:
			if (Server != ServerConnection.GameServer)
			{
				if (operationResponse.ReturnCode != 0)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.Log((object)$"JoinRoom failed (room maybe closed by now). Client stays on masterserver: {operationResponse.ToStringFull()}. State: {State}");
					}
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonJoinRoomFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
				}
				else
				{
					GameServerAddress = (string)operationResponse[(byte)230];
					if (PhotonNetwork.UseAlternativeUdpPorts && (int)((PhotonPeer)this).TransportProtocol == 0)
					{
						GameServerAddress = GameServerAddress.Replace("5058", "27000").Replace("5055", "27001").Replace("5056", "27002");
					}
					DisconnectToReconnect();
				}
			}
			else
			{
				GameEnteredOnGameServer(operationResponse);
			}
			break;
		case 225:
			if (operationResponse.ReturnCode != 0)
			{
				if (operationResponse.ReturnCode == 32760)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
					{
						Debug.Log((object)"JoinRandom failed: No open game. Calling: OnPhotonRandomJoinFailed() and staying on master server.");
					}
				}
				else if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
				{
					Debug.LogWarning((object)$"JoinRandom failed: {operationResponse.ToStringFull()}.");
				}
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonRandomJoinFailed, operationResponse.ReturnCode, operationResponse.DebugMessage);
			}
			else
			{
				string roomName = (string)operationResponse[byte.MaxValue];
				enterRoomParamsCache.RoomName = roomName;
				GameServerAddress = (string)operationResponse[(byte)230];
				if (PhotonNetwork.UseAlternativeUdpPorts && (int)((PhotonPeer)this).TransportProtocol == 0)
				{
					GameServerAddress = GameServerAddress.Replace("5058", "27000").Replace("5055", "27001").Replace("5056", "27002");
				}
				DisconnectToReconnect();
			}
			break;
		case 217:
		{
			if (operationResponse.ReturnCode != 0)
			{
				DebugReturn((DebugLevel)1, "GetGameList failed: " + operationResponse.ToStringFull());
				break;
			}
			mGameList = new Dictionary<string, RoomInfo>();
			Hashtable val = (Hashtable)operationResponse[(byte)222];
			foreach (object key in ((Dictionary<object, object>)(object)val).Keys)
			{
				string text = (string)key;
				mGameList[text] = new RoomInfo(text, (Hashtable)val[key]);
			}
			mGameListCopy = new RoomInfo[mGameList.Count];
			mGameList.Values.CopyTo(mGameListCopy, 0);
			SendMonoMessage(PhotonNetworkingMessage.OnReceivedRoomListUpdate);
			break;
		}
		case 229:
			State = ClientState.JoinedLobby;
			insideLobby = true;
			SendMonoMessage(PhotonNetworkingMessage.OnJoinedLobby);
			break;
		case 228:
			State = ClientState.Authenticated;
			LeftLobbyCleanup();
			break;
		case 254:
			DisconnectToReconnect();
			break;
		case 251:
		{
			Hashtable pActorProperties = (Hashtable)operationResponse[(byte)249];
			Hashtable gameProperties = (Hashtable)operationResponse[(byte)248];
			ReadoutProperties(gameProperties, pActorProperties, 0);
			break;
		}
		case 222:
		{
			bool[] array = operationResponse[(byte)1] as bool[];
			string[] array2 = operationResponse[(byte)2] as string[];
			if (array != null && array2 != null && friendListRequested != null && array.Length == friendListRequested.Length)
			{
				List<FriendInfo> list = new List<FriendInfo>(friendListRequested.Length);
				for (int i = 0; i < friendListRequested.Length; i++)
				{
					FriendInfo friendInfo = new FriendInfo();
					friendInfo.UserId = friendListRequested[i];
					friendInfo.Room = array2[i];
					friendInfo.IsOnline = array[i];
					list.Insert(i, friendInfo);
				}
				PhotonNetwork.Friends = list;
			}
			else
			{
				Debug.LogError((object)"FindFriends failed to apply the result, as a required value wasn't provided or the friend list length differed from result.");
			}
			friendListRequested = null;
			isFetchingFriendList = false;
			friendListTimestamp = Environment.TickCount;
			if (friendListTimestamp == 0)
			{
				friendListTimestamp = 1;
			}
			SendMonoMessage(PhotonNetworkingMessage.OnUpdatedFriendList);
			break;
		}
		case 219:
			SendMonoMessage(PhotonNetworkingMessage.OnWebRpcResponse, operationResponse);
			break;
		default:
			Debug.LogWarning((object)$"OperationResponse unhandled: {((object)operationResponse).ToString()}");
			break;
		case 252:
		case 253:
			break;
		}
	}

	public unsafe void OnStatusChanged(StatusCode statusCode)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected I4, but got Unknown
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected I4, but got Unknown
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Expected I4, but got Unknown
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Expected I4, but got Unknown
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_0552: Expected I4, but got Unknown
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_0538: Expected I4, but got Unknown
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Expected I4, but got Unknown
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Expected I4, but got Unknown
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Expected I4, but got Unknown
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Invalid comparison between Unknown and I4
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Invalid comparison between Unknown and I4
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
		{
			Debug.Log((object)$"OnStatusChanged: {((object)(*(StatusCode*)(&statusCode))/*cast due to .constrained prefix*/).ToString()} current State: {State}");
		}
		switch (statusCode - 1022)
		{
		default:
			switch (statusCode - 1039)
			{
			case 9:
				break;
			case 10:
			{
				Debug.LogError((object)string.Concat("Encryption wasn't established: ", statusCode, ". Going to authenticate anyways."));
				AuthenticationValues authValues = AuthValues ?? new AuthenticationValues
				{
					UserId = PlayerName
				};
				OpAuthenticate(AppId, AppVersion, authValues, CloudRegion.ToString(), requestLobbyStatistics);
				return;
			}
			case 1:
				if (IsInitialConnect)
				{
					if (!_isReconnecting)
					{
						Debug.LogWarning((object)string.Concat(statusCode, " while connecting to: ", ((PhotonPeer)this).ServerAddress, ". Check if the server is available."));
						IsInitialConnect = false;
						DisconnectCause disconnectCause = (DisconnectCause)statusCode;
						SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, disconnectCause);
					}
				}
				else
				{
					DisconnectCause disconnectCause = (DisconnectCause)statusCode;
					SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, disconnectCause);
				}
				if (AuthValues != null)
				{
					AuthValues.Token = null;
				}
				((PhotonPeer)this).Disconnect();
				return;
			case 0:
			case 2:
			case 3:
			case 4:
				goto IL_04f5;
			default:
				goto IL_0583;
			}
			goto IL_0183;
		case 2:
			if (State == ClientState.ConnectingToNameServer)
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
				{
					Debug.Log((object)"Connected to NameServer.");
				}
				Server = ServerConnection.NameServer;
				if (AuthValues != null)
				{
					AuthValues.Token = null;
				}
			}
			if (State == ClientState.ConnectingToGameserver)
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
				{
					Debug.Log((object)"Connected to gameserver.");
				}
				Server = ServerConnection.GameServer;
				State = ClientState.ConnectedToGameserver;
			}
			if (State == ClientState.ConnectingToMasterserver)
			{
				if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
				{
					Debug.Log((object)"Connected to masterserver.");
				}
				Server = ServerConnection.MasterServer;
				State = ClientState.Authenticating;
				if (IsInitialConnect)
				{
					IsInitialConnect = false;
					SendMonoMessage(PhotonNetworkingMessage.OnConnectedToPhoton);
				}
			}
			if (PhotonNetwork.offlineMode)
			{
				break;
			}
			if ((int)((PhotonPeer)this).TransportProtocol != 5)
			{
				if (Server == ServerConnection.NameServer || AuthMode == AuthModeOption.Auth)
				{
					((PhotonPeer)this).EstablishEncryption();
				}
				break;
			}
			if ((int)((PhotonPeer)this).DebugOut == 3)
			{
				Debug.Log((object)"Skipping EstablishEncryption. Protocol is secure.");
			}
			goto IL_0183;
		case 3:
			didAuthenticate = false;
			isFetchingFriendList = false;
			if (Server == ServerConnection.GameServer)
			{
				LeftRoomCleanup();
			}
			if (Server == ServerConnection.MasterServer)
			{
				LeftLobbyCleanup();
			}
			if (State == ClientState.DisconnectingFromMasterserver)
			{
				if (Connect(GameServerAddress, ServerConnection.GameServer))
				{
					State = ClientState.ConnectingToGameserver;
				}
			}
			else if (State == ClientState.DisconnectingFromGameserver || State == ClientState.DisconnectingFromNameServer)
			{
				SetupProtocol(ServerConnection.MasterServer);
				if (Connect(MasterServerAddress, ServerConnection.MasterServer))
				{
					State = ClientState.ConnectingToMasterserver;
				}
			}
			else if (!_isReconnecting)
			{
				if (AuthValues != null)
				{
					AuthValues.Token = null;
				}
				IsInitialConnect = false;
				State = ClientState.PeerCreated;
				SendMonoMessage(PhotonNetworkingMessage.OnDisconnectedFromPhoton);
			}
			break;
		case 0:
		case 1:
		{
			IsInitialConnect = false;
			State = ClientState.PeerCreated;
			if (AuthValues != null)
			{
				AuthValues.Token = null;
			}
			DisconnectCause disconnectCause = (DisconnectCause)statusCode;
			SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, disconnectCause);
			break;
		}
		case 4:
			if (IsInitialConnect)
			{
				Debug.LogError((object)("Exception while connecting to: " + ((PhotonPeer)this).ServerAddress + ". Check if the server is available."));
				if (((PhotonPeer)this).ServerAddress == null || ((PhotonPeer)this).ServerAddress.StartsWith("127.0.0.1"))
				{
					Debug.LogWarning((object)"The server address is 127.0.0.1 (localhost): Make sure the server is running on this machine. Android and iOS emulators have their own localhost.");
					if (((PhotonPeer)this).ServerAddress == GameServerAddress)
					{
						Debug.LogWarning((object)"This might be a misconfiguration in the game server config. You need to edit it to a (public) address.");
					}
				}
				State = ClientState.PeerCreated;
				DisconnectCause disconnectCause = (DisconnectCause)statusCode;
				IsInitialConnect = false;
				SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, disconnectCause);
			}
			else
			{
				State = ClientState.PeerCreated;
				DisconnectCause disconnectCause = (DisconnectCause)statusCode;
				SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, disconnectCause);
			}
			((PhotonPeer)this).Disconnect();
			break;
		case 8:
			goto IL_04f5;
		case 5:
		case 6:
		case 7:
			goto IL_0583;
			IL_04f5:
			if (IsInitialConnect)
			{
				Debug.LogWarning((object)string.Concat(statusCode, " while connecting to: ", ((PhotonPeer)this).ServerAddress, ". Check if the server is available."));
				IsInitialConnect = false;
				DisconnectCause disconnectCause = (DisconnectCause)statusCode;
				SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, disconnectCause);
			}
			else
			{
				DisconnectCause disconnectCause = (DisconnectCause)statusCode;
				SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, disconnectCause);
			}
			if (AuthValues != null)
			{
				AuthValues.Token = null;
			}
			((PhotonPeer)this).Disconnect();
			break;
			IL_0183:
			_isReconnecting = false;
			if (Server == ServerConnection.NameServer)
			{
				State = ClientState.ConnectedToNameServer;
				if (!didAuthenticate && CloudRegion == CloudRegionCode.none)
				{
					OpGetRegions(AppId);
				}
			}
			if (Server != ServerConnection.NameServer && (AuthMode == AuthModeOption.AuthOnce || AuthMode == AuthModeOption.AuthOnceWss))
			{
				Debug.Log((object)("didAuthenticate " + didAuthenticate.ToString() + " AuthMode " + AuthMode));
			}
			else if (!didAuthenticate && (!IsUsingNameServer || CloudRegion != CloudRegionCode.none))
			{
				didAuthenticate = CallAuthenticate();
				if (didAuthenticate)
				{
					State = ClientState.Authenticating;
				}
			}
			break;
			IL_0583:
			Debug.LogError((object)("Received unknown status code: " + statusCode));
			break;
		}
	}

	public void OnEvent(EventData photonEvent)
	{
		//IL_06f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Expected O, but got Unknown
		//IL_078c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0798: Expected O, but got Unknown
		//IL_0834: Unknown result type (might be due to invalid IL or missing references)
		//IL_07dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Expected O, but got Unknown
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Expected O, but got Unknown
		//IL_086f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0875: Invalid comparison between Unknown and I4
		//IL_0804: Unknown result type (might be due to invalid IL or missing references)
		//IL_080a: Invalid comparison between Unknown and I4
		//IL_058f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Expected O, but got Unknown
		//IL_06c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Expected O, but got Unknown
		//IL_06ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Expected O, but got Unknown
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Expected O, but got Unknown
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Expected O, but got Unknown
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
		{
			Debug.Log((object)$"OnEvent: {((object)photonEvent).ToString()}");
		}
		int sender = photonEvent.Sender;
		PhotonPlayer photonPlayer = null;
		photonPlayer = GetPlayerWithId(sender);
		switch (photonEvent.Code)
		{
		case 209:
		{
			int[] obj = (int[])photonEvent.Parameters[245];
			int num = obj[0];
			int num2 = obj[1];
			PhotonView photonView = PhotonView.Find(num);
			if ((Object)(object)photonView == (Object)null)
			{
				Debug.LogWarning((object)("Can't find PhotonView of incoming OwnershipRequest. ViewId not found: " + num));
				break;
			}
			if (PhotonNetwork.logLevel == PhotonLogLevel.Informational)
			{
				Debug.Log((object)string.Concat("Ev OwnershipRequest ", photonView.ownershipTransfer, ". ActorNr: ", sender, " takes from: ", num2, ". local RequestedView.ownerId: ", photonView.ownerId, " isOwnerActive: ", photonView.isOwnerActive.ToString(), ". MasterClient: ", mMasterClientId, ". This client's player: ", PhotonNetwork.player.ToStringFull()));
			}
			switch (photonView.ownershipTransfer)
			{
			case OwnershipOption.Fixed:
				Debug.LogWarning((object)"Ownership mode == fixed. Ignoring request.");
				break;
			case OwnershipOption.Takeover:
				if (num2 == photonView.ownerId || (num2 == 0 && photonView.ownerId == mMasterClientId) || photonView.ownerId == 0)
				{
					photonView.OwnerShipWasTransfered = true;
					int ownerId = photonView.ownerId;
					PhotonPlayer playerWithId = GetPlayerWithId(ownerId);
					photonView.ownerId = sender;
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.LogWarning((object)string.Concat(photonView, " ownership transfered to: ", sender));
					}
					SendMonoMessage(PhotonNetworkingMessage.OnOwnershipTransfered, photonView, photonPlayer, playerWithId);
				}
				break;
			case OwnershipOption.Request:
				if ((num2 == PhotonNetwork.player.ID || PhotonNetwork.player.IsMasterClient) && (photonView.ownerId == PhotonNetwork.player.ID || (PhotonNetwork.player.IsMasterClient && !photonView.isOwnerActive)))
				{
					SendMonoMessage(PhotonNetworkingMessage.OnOwnershipRequest, photonView, photonPlayer);
				}
				break;
			}
			break;
		}
		case 210:
		{
			int[] array = (int[])photonEvent.Parameters[245];
			if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
			{
				Debug.Log((object)("Ev OwnershipTransfer. ViewID " + array[0] + " to: " + array[1] + " Time: " + Environment.TickCount % 1000));
			}
			int viewID = array[0];
			int num3 = array[1];
			PhotonView photonView2 = PhotonView.Find(viewID);
			if ((Object)(object)photonView2 != (Object)null)
			{
				int ownerId2 = photonView2.ownerId;
				photonView2.OwnerShipWasTransfered = true;
				photonView2.ownerId = num3;
				SendMonoMessage(PhotonNetworkingMessage.OnOwnershipTransfered, photonView2, PhotonPlayer.Find(num3), PhotonPlayer.Find(ownerId2));
			}
			break;
		}
		case 230:
		{
			mGameList = new Dictionary<string, RoomInfo>();
			Hashtable val = (Hashtable)photonEvent[(byte)222];
			foreach (object key in ((Dictionary<object, object>)(object)val).Keys)
			{
				string text = (string)key;
				mGameList[text] = new RoomInfo(text, (Hashtable)val[key]);
			}
			mGameListCopy = new RoomInfo[mGameList.Count];
			mGameList.Values.CopyTo(mGameListCopy, 0);
			SendMonoMessage(PhotonNetworkingMessage.OnReceivedRoomListUpdate);
			break;
		}
		case 229:
		{
			Hashtable val2 = (Hashtable)photonEvent[(byte)222];
			foreach (object key2 in ((Dictionary<object, object>)(object)val2).Keys)
			{
				string text2 = (string)key2;
				RoomInfo roomInfo = new RoomInfo(text2, (Hashtable)val2[key2]);
				if (roomInfo.removedFromList)
				{
					mGameList.Remove(text2);
				}
				else
				{
					mGameList[text2] = roomInfo;
				}
			}
			mGameListCopy = new RoomInfo[mGameList.Count];
			mGameList.Values.CopyTo(mGameListCopy, 0);
			SendMonoMessage(PhotonNetworkingMessage.OnReceivedRoomListUpdate);
			break;
		}
		case 226:
			PlayersInRoomsCount = (int)photonEvent[(byte)229];
			PlayersOnMasterCount = (int)photonEvent[(byte)227];
			RoomsCount = (int)photonEvent[(byte)228];
			break;
		case byte.MaxValue:
		{
			bool flag = false;
			Hashtable properties = (Hashtable)photonEvent[(byte)249];
			if (photonPlayer == null)
			{
				bool isLocal = LocalPlayer.ID == sender;
				AddNewPlayer(sender, new PhotonPlayer(isLocal, sender, properties));
				ResetPhotonViewsOnSerialize();
			}
			else
			{
				flag = photonPlayer.IsInactive;
				photonPlayer.InternalCacheProperties(properties);
				photonPlayer.IsInactive = false;
			}
			if (sender == LocalPlayer.ID)
			{
				int[] actorsInRoom = (int[])photonEvent[(byte)252];
				UpdatedActorList(actorsInRoom);
				if (lastJoinType == JoinType.JoinOrCreateRoom && LocalPlayer.ID == 1)
				{
					SendMonoMessage(PhotonNetworkingMessage.OnCreatedRoom);
				}
				SendMonoMessage(PhotonNetworkingMessage.OnJoinedRoom);
			}
			else
			{
				SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerConnected, mActors[sender]);
				if (flag)
				{
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerActivityChanged, mActors[sender]);
				}
			}
			break;
		}
		case 254:
			if (_AsyncLevelLoadingOperation != null)
			{
				_AsyncLevelLoadingOperation = null;
			}
			HandleEventLeave(sender, photonEvent);
			break;
		case 253:
		{
			int num5 = (int)photonEvent[(byte)253];
			Hashtable gameProperties = null;
			Hashtable pActorProperties = null;
			if (num5 == 0)
			{
				gameProperties = (Hashtable)photonEvent[(byte)251];
			}
			else
			{
				pActorProperties = (Hashtable)photonEvent[(byte)251];
			}
			ReadoutProperties(gameProperties, pActorProperties, num5);
			break;
		}
		case 200:
		{
			object obj2 = photonEvent[(byte)245];
			ExecuteRpc((Hashtable)((obj2 is Hashtable) ? obj2 : null), sender);
			break;
		}
		case 201:
		case 206:
		{
			Hashtable val3 = (Hashtable)photonEvent[(byte)245];
			int networkTime = (int)val3[(object)(byte)0];
			short correctPrefix = -1;
			byte b = 10;
			int num6 = 1;
			if (((Dictionary<object, object>)(object)val3).ContainsKey((object)(byte)1))
			{
				correctPrefix = (short)val3[(object)(byte)1];
				num6 = 2;
			}
			byte b2 = b;
			while (b2 - b < ((Dictionary<object, object>)(object)val3).Count - num6)
			{
				OnSerializeRead(val3[(object)b2] as object[], photonPlayer, networkTime, correctPrefix);
				b2++;
			}
			break;
		}
		case 202:
			DoInstantiate((Hashtable)photonEvent[(byte)245], photonPlayer, null);
			break;
		case 203:
			if (photonPlayer == null || !photonPlayer.IsMasterClient)
			{
				Debug.LogError((object)string.Concat("Error: Someone else(", photonPlayer, ") then the masterserver requests a disconnect!"));
				break;
			}
			if (_AsyncLevelLoadingOperation != null)
			{
				_AsyncLevelLoadingOperation = null;
			}
			PhotonNetwork.LeaveRoom(becomeInactive: false);
			break;
		case 207:
		{
			int num7 = (int)((Hashtable)photonEvent[(byte)245])[(object)(byte)0];
			if (num7 >= 0)
			{
				DestroyPlayerObjects(num7, localOnly: true);
				break;
			}
			if ((int)((PhotonPeer)this).DebugOut >= 3)
			{
				Debug.Log((object)("Ev DestroyAll! By PlayerId: " + sender));
			}
			DestroyAll(localOnly: true);
			break;
		}
		case 204:
		{
			int num4 = (int)((Hashtable)photonEvent[(byte)245])[(object)(byte)0];
			PhotonView value = null;
			if (photonViewList.TryGetValue(num4, out value))
			{
				RemoveInstantiatedGO(((Component)value).gameObject, localOnly: true);
			}
			else if ((int)((PhotonPeer)this).DebugOut >= 1)
			{
				Debug.LogError((object)("Ev Destroy Failed. Could not find PhotonView with instantiationId " + num4 + ". Sent by actorNr: " + sender));
			}
			break;
		}
		case 208:
		{
			int playerId = (int)((Hashtable)photonEvent[(byte)245])[(object)(byte)1];
			SetMasterClient(playerId, sync: false);
			break;
		}
		case 224:
		{
			string[] array2 = photonEvent[(byte)213] as string[];
			byte[] array3 = photonEvent[(byte)212] as byte[];
			int[] array4 = photonEvent[(byte)229] as int[];
			int[] array5 = photonEvent[(byte)228] as int[];
			LobbyStatistics.Clear();
			for (int i = 0; i < array2.Length; i++)
			{
				TypedLobbyInfo typedLobbyInfo = new TypedLobbyInfo();
				typedLobbyInfo.Name = array2[i];
				typedLobbyInfo.Type = (LobbyType)array3[i];
				typedLobbyInfo.PlayerCount = array4[i];
				typedLobbyInfo.RoomCount = array5[i];
				LobbyStatistics.Add(typedLobbyInfo);
			}
			SendMonoMessage(PhotonNetworkingMessage.OnLobbyStatisticsUpdate);
			break;
		}
		case 251:
			if (!PhotonNetwork.CallEvent(photonEvent.Code, photonEvent[(byte)218], sender))
			{
				Debug.LogWarning((object)"Warning: Unhandled Event ErrorInfo (251). Set PhotonNetwork.OnEventCall to the method PUN should call for this event.");
			}
			break;
		case 223:
			if (AuthValues == null)
			{
				AuthValues = new AuthenticationValues();
			}
			AuthValues.Token = photonEvent[(byte)221] as string;
			tokenCache = AuthValues.Token;
			break;
		case 212:
			if ((bool)photonEvent.Parameters[245])
			{
				PhotonNetwork.LoadLevelAsync(SceneManagerHelper.ActiveSceneName);
			}
			else
			{
				PhotonNetwork.LoadLevel(SceneManagerHelper.ActiveSceneName);
			}
			break;
		default:
			if (photonEvent.Code < 200 && !PhotonNetwork.CallEvent(photonEvent.Code, photonEvent[(byte)245], sender))
			{
				Debug.LogWarning((object)string.Concat("Warning: Unhandled event ", photonEvent, ". Set PhotonNetwork.OnEventCall."));
			}
			break;
		}
	}

	public void OnMessage(object messages)
	{
	}

	private void SetupEncryption(Dictionary<byte, object> encryptionData)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		if (AuthMode == AuthModeOption.Auth && (int)((PhotonPeer)this).DebugOut == 1)
		{
			Debug.LogWarning((object)("SetupEncryption() called but ignored. Not XB1 compiled. EncryptionData: " + encryptionData.ToStringFull()));
			return;
		}
		if ((int)((PhotonPeer)this).DebugOut == 3)
		{
			Debug.Log((object)("SetupEncryption() got called. " + encryptionData.ToStringFull()));
		}
		switch ((EncryptionMode)(byte)encryptionData[0])
		{
		case EncryptionMode.PayloadEncryption:
		{
			byte[] array3 = (byte[])encryptionData[1];
			((PhotonPeer)this).InitPayloadEncryption(array3);
			break;
		}
		case EncryptionMode.DatagramEncryption:
		{
			byte[] array = (byte[])encryptionData[1];
			byte[] array2 = (byte[])encryptionData[2];
			((PhotonPeer)this).InitDatagramEncryption(array, array2, false, false);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	protected internal void UpdatedActorList(int[] actorsInRoom)
	{
		foreach (int num in actorsInRoom)
		{
			if (LocalPlayer.ID != num && !mActors.ContainsKey(num))
			{
				AddNewPlayer(num, new PhotonPlayer(isLocal: false, num, string.Empty));
			}
		}
	}

	private void SendVacantViewIds()
	{
		Debug.Log((object)"SendVacantViewIds()");
		List<int> list = new List<int>();
		foreach (PhotonView value in photonViewList.Values)
		{
			if (!value.isOwnerActive)
			{
				list.Add(value.viewID);
			}
		}
		Debug.Log((object)("Sending vacant view IDs. Length: " + list.Count));
		OpRaiseEvent(211, list.ToArray(), sendReliable: true, null);
	}

	public static void SendMonoMessage(PhotonNetworkingMessage methodString, params object[] parameters)
	{
		HashSet<GameObject> hashSet = ((PhotonNetwork.SendMonoMessageTargets == null) ? PhotonNetwork.FindGameObjectsWithComponent(PhotonNetwork.SendMonoMessageTargetType) : PhotonNetwork.SendMonoMessageTargets);
		string text = methodString.ToString();
		object obj = ((parameters != null && parameters.Length == 1) ? parameters[0] : parameters);
		foreach (GameObject item in hashSet)
		{
			if ((Object)(object)item != (Object)null)
			{
				item.SendMessage(text, obj, (SendMessageOptions)1);
			}
		}
	}

	protected internal void ExecuteRpc(Hashtable rpcData, int senderID = 0)
	{
		if (rpcData == null || !((Dictionary<object, object>)(object)rpcData).ContainsKey(keyByteZero))
		{
			Debug.LogError((object)("Malformed RPC; this should never occur. Content: " + SupportClass.DictionaryToString((IDictionary)rpcData)));
			return;
		}
		int num = (int)rpcData[keyByteZero];
		int num2 = 0;
		if (((Dictionary<object, object>)(object)rpcData).ContainsKey(keyByteOne))
		{
			num2 = (short)rpcData[keyByteOne];
		}
		string text;
		if (((Dictionary<object, object>)(object)rpcData).ContainsKey(keyByteFive))
		{
			int num3 = (byte)rpcData[keyByteFive];
			if (num3 > PhotonNetwork.PhotonServerSettings.RpcList.Count - 1)
			{
				Debug.LogError((object)("Could not find RPC with index: " + num3 + ". Going to ignore! Check PhotonServerSettings.RpcList"));
				return;
			}
			text = PhotonNetwork.PhotonServerSettings.RpcList[num3];
		}
		else
		{
			text = (string)rpcData[keyByteThree];
		}
		object[] array = emptyObjectArray;
		if (((Dictionary<object, object>)(object)rpcData).ContainsKey(keyByteFour))
		{
			array = (object[])rpcData[keyByteFour];
		}
		PhotonView photonView = GetPhotonView(num);
		if ((Object)(object)photonView == (Object)null)
		{
			int num4 = num / PhotonNetwork.MAX_VIEW_IDS;
			bool flag = num4 == LocalPlayer.ID;
			bool flag2 = num4 == senderID;
			if (flag)
			{
				Debug.LogWarning((object)("Received RPC \"" + text + "\" for viewID " + num + " but this PhotonView does not exist! View was/is ours." + (flag2 ? " Owner called." : " Remote called.") + " By: " + senderID));
			}
			else
			{
				Debug.LogWarning((object)("Received RPC \"" + text + "\" for viewID " + num + " but this PhotonView does not exist! Was remote PV." + (flag2 ? " Owner called." : " Remote called.") + " By: " + senderID + " Maybe GO was destroyed but RPC not cleaned up."));
			}
			return;
		}
		if (photonView.prefix != num2)
		{
			Debug.LogError((object)("Received RPC \"" + text + "\" on viewID " + num + " with a prefix of " + num2 + ", our prefix is " + photonView.prefix + ". The RPC has been ignored."));
			return;
		}
		if (string.IsNullOrEmpty(text))
		{
			Debug.LogError((object)("Malformed RPC; this should never occur. Content: " + SupportClass.DictionaryToString((IDictionary)rpcData)));
			return;
		}
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
		{
			Debug.Log((object)("Received RPC: " + text));
		}
		if (photonView.group != 0 && !allowedReceivingGroups.Contains(photonView.group))
		{
			return;
		}
		Type[] array2 = emptyTypeArray;
		if (array.Length != 0)
		{
			array2 = new Type[array.Length];
			int num5 = 0;
			foreach (object obj in array)
			{
				if (obj == null)
				{
					array2[num5] = null;
				}
				else
				{
					array2[num5] = obj.GetType();
				}
				num5++;
			}
		}
		int num6 = 0;
		int num7 = 0;
		if (!PhotonNetwork.UseRpcMonoBehaviourCache || photonView.RpcMonoBehaviours == null || photonView.RpcMonoBehaviours.Length == 0)
		{
			photonView.RefreshRpcMonoBehaviourCache();
		}
		for (int j = 0; j < photonView.RpcMonoBehaviours.Length; j++)
		{
			MonoBehaviour val = photonView.RpcMonoBehaviours[j];
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogError((object)"ERROR You have missing MonoBehaviours on your gameobjects!");
				continue;
			}
			Type type = ((object)val).GetType();
			List<MethodInfo> value = null;
			if (!monoRPCMethodsCache.TryGetValue(type, out value))
			{
				List<MethodInfo> methods = SupportClass.GetMethods(type, typePunRPC);
				monoRPCMethodsCache[type] = methods;
				value = methods;
			}
			if (value == null)
			{
				continue;
			}
			for (int k = 0; k < value.Count; k++)
			{
				MethodInfo methodInfo = value[k];
				if (!methodInfo.Name.Equals(text))
				{
					continue;
				}
				num7++;
				ParameterInfo[] cachedParemeters = methodInfo.GetCachedParemeters();
				if (cachedParemeters.Length == array2.Length)
				{
					if (CheckTypeMatch(cachedParemeters, array2))
					{
						num6++;
						IEnumerator enumerator = methodInfo.Invoke(val, array) as IEnumerator;
						if (PhotonNetwork.StartRpcsAsCoroutine && enumerator != null)
						{
							val.StartCoroutine(enumerator);
						}
					}
				}
				else if (cachedParemeters.Length == array2.Length + 1)
				{
					if (CheckTypeMatch(cachedParemeters, array2) && cachedParemeters[cachedParemeters.Length - 1].ParameterType == typePhotonMessageInfo)
					{
						int timestamp = (int)rpcData[keyByteTwo];
						PhotonMessageInfo photonMessageInfo = new PhotonMessageInfo(GetPlayerWithId(senderID), timestamp, photonView);
						object[] array3 = new object[array.Length + 1];
						array.CopyTo(array3, 0);
						array3[array3.Length - 1] = photonMessageInfo;
						num6++;
						IEnumerator enumerator2 = methodInfo.Invoke(val, array3) as IEnumerator;
						if (PhotonNetwork.StartRpcsAsCoroutine && enumerator2 != null)
						{
							val.StartCoroutine(enumerator2);
						}
					}
				}
				else if (cachedParemeters.Length == 1 && cachedParemeters[0].ParameterType.IsArray)
				{
					num6++;
					IEnumerator enumerator3 = methodInfo.Invoke(val, new object[1] { array }) as IEnumerator;
					if (PhotonNetwork.StartRpcsAsCoroutine && enumerator3 != null)
					{
						val.StartCoroutine(enumerator3);
					}
				}
			}
		}
		if (num6 == 1)
		{
			return;
		}
		string text2 = string.Empty;
		foreach (Type type2 in array2)
		{
			if (text2 != string.Empty)
			{
				text2 += ", ";
			}
			text2 = ((!(type2 == null)) ? (text2 + type2.Name) : (text2 + "null"));
		}
		if (num6 == 0)
		{
			if (num7 == 0)
			{
				Debug.LogError((object)("PhotonView with ID " + num + " has no method \"" + text + "\" marked with the [PunRPC](C#) or @PunRPC(JS) property! Args: " + text2));
			}
			else
			{
				Debug.LogError((object)("PhotonView with ID " + num + " has no method \"" + text + "\" that takes " + array2.Length + " argument(s): " + text2));
			}
		}
		else
		{
			Debug.LogError((object)("PhotonView with ID " + num + " has " + num6 + " methods \"" + text + "\" that takes " + array2.Length + " argument(s): " + text2 + ". Should be just one?"));
		}
	}

	private bool CheckTypeMatch(ParameterInfo[] methodParameters, Type[] callParameterTypes)
	{
		if (methodParameters.Length < callParameterTypes.Length)
		{
			return false;
		}
		for (int i = 0; i < callParameterTypes.Length; i++)
		{
			Type parameterType = methodParameters[i].ParameterType;
			if (callParameterTypes[i] != null && !parameterType.IsAssignableFrom(callParameterTypes[i]) && (!parameterType.IsEnum || !Enum.GetUnderlyingType(parameterType).IsAssignableFrom(callParameterTypes[i])))
			{
				return false;
			}
		}
		return true;
	}

	internal Hashtable SendInstantiate(string prefabName, Vector3 position, Quaternion rotation, byte group, int[] viewIDs, object[] data, bool isGlobalObject)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		int num = viewIDs[0];
		Hashtable val = new Hashtable();
		val[(object)(byte)0] = prefabName;
		if (position != Vector3.zero)
		{
			val[(object)(byte)1] = position;
		}
		if (rotation != Quaternion.identity)
		{
			val[(object)(byte)2] = rotation;
		}
		if (group != 0)
		{
			val[(object)(byte)3] = group;
		}
		if (viewIDs.Length > 1)
		{
			val[(object)(byte)4] = viewIDs;
		}
		if (data != null)
		{
			val[(object)(byte)5] = data;
		}
		if (currentLevelPrefix > 0)
		{
			val[(object)(byte)8] = currentLevelPrefix;
		}
		val[(object)(byte)6] = PhotonNetwork.ServerTimestamp;
		val[(object)(byte)7] = num;
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.CachingOption = (isGlobalObject ? EventCaching.AddToRoomCacheGlobal : EventCaching.AddToRoomCache);
		OpRaiseEvent(202, val, sendReliable: true, raiseEventOptions);
		return val;
	}

	internal GameObject DoInstantiate(Hashtable evData, PhotonPlayer photonPlayer, GameObject resourceGameObject)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Expected O, but got Unknown
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		string text = (string)evData[(object)(byte)0];
		int timestamp = (int)evData[(object)(byte)6];
		int num = (int)evData[(object)(byte)7];
		Vector3 val = (Vector3)((!((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)1)) ? Vector3.zero : ((Vector3)evData[(object)(byte)1]));
		Quaternion val2 = Quaternion.identity;
		if (((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)2))
		{
			val2 = (Quaternion)evData[(object)(byte)2];
		}
		byte b = 0;
		if (((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)3))
		{
			b = (byte)evData[(object)(byte)3];
		}
		short prefix = 0;
		if (((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)8))
		{
			prefix = (short)evData[(object)(byte)8];
		}
		int[] array = ((!((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)4)) ? new int[1] { num } : ((int[])evData[(object)(byte)4]));
		object[] array2 = ((!((Dictionary<object, object>)(object)evData).ContainsKey((object)(byte)5)) ? null : ((object[])evData[(object)(byte)5]));
		if (b != 0 && !allowedReceivingGroups.Contains(b))
		{
			return null;
		}
		if (ObjectPool != null)
		{
			GameObject val3 = ObjectPool.Instantiate(text, val, val2);
			PhotonView[] photonViewsInChildren = val3.GetPhotonViewsInChildren();
			if (photonViewsInChildren.Length != array.Length)
			{
				throw new Exception("Error in Instantiation! The resource's PhotonView count is not the same as in incoming data.");
			}
			for (int i = 0; i < photonViewsInChildren.Length; i++)
			{
				photonViewsInChildren[i].didAwake = false;
				photonViewsInChildren[i].viewID = 0;
				photonViewsInChildren[i].prefix = prefix;
				photonViewsInChildren[i].instantiationId = num;
				photonViewsInChildren[i].isRuntimeInstantiated = true;
				photonViewsInChildren[i].instantiationDataField = array2;
				photonViewsInChildren[i].didAwake = true;
				photonViewsInChildren[i].viewID = array[i];
			}
			val3.SendMessage(OnPhotonInstantiateString, (object)new PhotonMessageInfo(photonPlayer, timestamp, null), (SendMessageOptions)1);
			return val3;
		}
		if ((Object)(object)resourceGameObject == (Object)null)
		{
			if (!UsePrefabCache || !PrefabCache.TryGetValue(text, out resourceGameObject))
			{
				resourceGameObject = (GameObject)Resources.Load(text, typeof(GameObject));
				if (UsePrefabCache)
				{
					PrefabCache.Add(text, resourceGameObject);
				}
			}
			if ((Object)(object)resourceGameObject == (Object)null)
			{
				Debug.LogError((object)("PhotonNetwork error: Could not Instantiate the prefab [" + text + "]. Please verify you have this gameobject in a Resources folder."));
				return null;
			}
		}
		PhotonView[] photonViewsInChildren2 = resourceGameObject.GetPhotonViewsInChildren();
		if (photonViewsInChildren2.Length != array.Length)
		{
			throw new Exception("Error in Instantiation! The resource's PhotonView count is not the same as in incoming data.");
		}
		for (int j = 0; j < array.Length; j++)
		{
			photonViewsInChildren2[j].viewID = array[j];
			photonViewsInChildren2[j].prefix = prefix;
			photonViewsInChildren2[j].instantiationId = num;
			photonViewsInChildren2[j].isRuntimeInstantiated = true;
		}
		StoreInstantiationData(num, array2);
		GameObject val4 = Object.Instantiate<GameObject>(resourceGameObject, val, val2);
		for (int k = 0; k < array.Length; k++)
		{
			photonViewsInChildren2[k].viewID = 0;
			photonViewsInChildren2[k].prefix = -1;
			photonViewsInChildren2[k].prefixBackup = -1;
			photonViewsInChildren2[k].instantiationId = -1;
			photonViewsInChildren2[k].isRuntimeInstantiated = false;
		}
		RemoveInstantiationData(num);
		val4.SendMessage(OnPhotonInstantiateString, (object)new PhotonMessageInfo(photonPlayer, timestamp, null), (SendMessageOptions)1);
		return val4;
	}

	private void StoreInstantiationData(int instantiationId, object[] instantiationData)
	{
		tempInstantiationData[instantiationId] = instantiationData;
	}

	public object[] FetchInstantiationData(int instantiationId)
	{
		object[] value = null;
		if (instantiationId == 0)
		{
			return null;
		}
		tempInstantiationData.TryGetValue(instantiationId, out value);
		return value;
	}

	private void RemoveInstantiationData(int instantiationId)
	{
		tempInstantiationData.Remove(instantiationId);
	}

	public void DestroyPlayerObjects(int playerId, bool localOnly)
	{
		if (playerId <= 0)
		{
			Debug.LogError((object)("Failed to Destroy objects of playerId: " + playerId));
			return;
		}
		if (!localOnly)
		{
			OpRemoveFromServerInstantiationsOfPlayer(playerId);
			OpCleanRpcBuffer(playerId);
			SendDestroyOfPlayer(playerId);
		}
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (PhotonView value in photonViewList.Values)
		{
			if ((Object)(object)value != (Object)null && value.CreatorActorNr == playerId)
			{
				hashSet.Add(((Component)value).gameObject);
			}
		}
		foreach (GameObject item in hashSet)
		{
			RemoveInstantiatedGO(item, localOnly: true);
		}
		foreach (PhotonView value2 in photonViewList.Values)
		{
			if (value2.ownerId == playerId)
			{
				value2.ownerId = value2.CreatorActorNr;
			}
		}
	}

	public void DestroyAll(bool localOnly)
	{
		if (!localOnly)
		{
			OpRemoveCompleteCache();
			SendDestroyOfAll();
		}
		LocalCleanupAnythingInstantiated(destroyInstantiatedGameObjects: true);
	}

	protected internal void RemoveInstantiatedGO(GameObject go, bool localOnly)
	{
		if ((Object)(object)go == (Object)null)
		{
			Debug.LogError((object)"Failed to 'network-remove' GameObject because it's null.");
			return;
		}
		PhotonView[] componentsInChildren = go.GetComponentsInChildren<PhotonView>(true);
		if (componentsInChildren == null || componentsInChildren.Length == 0)
		{
			Debug.LogError((object)("Failed to 'network-remove' GameObject because has no PhotonView components: " + go));
			return;
		}
		PhotonView photonView = componentsInChildren[0];
		int creatorActorNr = photonView.CreatorActorNr;
		int instantiationId = photonView.instantiationId;
		if (!localOnly)
		{
			if (!photonView.isMine)
			{
				Debug.LogError((object)("Failed to 'network-remove' GameObject. Client is neither owner nor masterClient taking over for owner who left: " + photonView));
				return;
			}
			if (instantiationId < 1)
			{
				Debug.LogError((object)string.Concat("Failed to 'network-remove' GameObject because it is missing a valid InstantiationId on view: ", photonView, ". Not Destroying GameObject or PhotonViews!"));
				return;
			}
		}
		if (!localOnly)
		{
			ServerCleanInstantiateAndDestroy(instantiationId, creatorActorNr, photonView.isRuntimeInstantiated);
		}
		for (int num = componentsInChildren.Length - 1; num >= 0; num--)
		{
			PhotonView photonView2 = componentsInChildren[num];
			if (!((Object)(object)photonView2 == (Object)null))
			{
				if (photonView2.instantiationId >= 1)
				{
					LocalCleanPhotonView(photonView2);
				}
				if (!localOnly)
				{
					OpCleanRpcBuffer(photonView2);
				}
			}
		}
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
		{
			Debug.Log((object)("Network destroy Instantiated GO: " + ((Object)go).name));
		}
		if (ObjectPool != null)
		{
			PhotonView[] photonViewsInChildren = go.GetPhotonViewsInChildren();
			for (int i = 0; i < photonViewsInChildren.Length; i++)
			{
				photonViewsInChildren[i].viewID = 0;
			}
			ObjectPool.Destroy(go);
		}
		else
		{
			Object.Destroy((Object)(object)go);
		}
	}

	private void ServerCleanInstantiateAndDestroy(int instantiateId, int creatorId, bool isRuntimeInstantiated)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		Hashtable val = new Hashtable();
		val[(object)(byte)7] = instantiateId;
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.CachingOption = EventCaching.RemoveFromRoomCache;
		raiseEventOptions.TargetActors = new int[1] { creatorId };
		RaiseEventOptions raiseEventOptions2 = raiseEventOptions;
		OpRaiseEvent(202, val, sendReliable: true, raiseEventOptions2);
		Hashtable val2 = new Hashtable();
		val2[(object)(byte)0] = instantiateId;
		raiseEventOptions2 = null;
		if (!isRuntimeInstantiated)
		{
			raiseEventOptions2 = new RaiseEventOptions();
			raiseEventOptions2.CachingOption = EventCaching.AddToRoomCacheGlobal;
			Debug.Log((object)("Destroying GO as global. ID: " + instantiateId));
		}
		OpRaiseEvent(204, val2, sendReliable: true, raiseEventOptions2);
	}

	private void SendDestroyOfPlayer(int actorNr)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		Hashtable val = new Hashtable();
		val[(object)(byte)0] = actorNr;
		OpRaiseEvent(207, val, sendReliable: true, null);
	}

	private void SendDestroyOfAll()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		Hashtable val = new Hashtable();
		val[(object)(byte)0] = -1;
		OpRaiseEvent(207, val, sendReliable: true, null);
	}

	private void OpRemoveFromServerInstantiationsOfPlayer(int actorNr)
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.CachingOption = EventCaching.RemoveFromRoomCache;
		raiseEventOptions.TargetActors = new int[1] { actorNr };
		RaiseEventOptions raiseEventOptions2 = raiseEventOptions;
		OpRaiseEvent(202, null, sendReliable: true, raiseEventOptions2);
	}

	protected internal void RequestOwnership(int viewID, int fromOwner)
	{
		OpRaiseEvent(209, new int[2] { viewID, fromOwner }, sendReliable: true, new RaiseEventOptions
		{
			Receivers = ReceiverGroup.All
		});
	}

	protected internal void TransferOwnership(int viewID, int playerID)
	{
		Debug.Log((object)("TransferOwnership() view " + viewID + " to: " + playerID + " Time: " + Environment.TickCount % 1000));
		OpRaiseEvent(210, new int[2] { viewID, playerID }, sendReliable: true, new RaiseEventOptions
		{
			Receivers = ReceiverGroup.All
		});
	}

	public bool LocalCleanPhotonView(PhotonView view)
	{
		view.removedFromLocalViewList = true;
		return photonViewList.Remove(view.viewID);
	}

	public PhotonView GetPhotonView(int viewID)
	{
		PhotonView value = null;
		photonViewList.TryGetValue(viewID, out value);
		if ((Object)(object)value == (Object)null)
		{
			PhotonView[] array = Object.FindObjectsOfType(typeof(PhotonView)) as PhotonView[];
			foreach (PhotonView photonView in array)
			{
				if (photonView.viewID == viewID)
				{
					if (photonView.didAwake)
					{
						Debug.LogWarning((object)("Had to lookup view that wasn't in photonViewList: " + photonView));
					}
					return photonView;
				}
			}
		}
		return value;
	}

	public void RegisterPhotonView(PhotonView netView)
	{
		if (!Application.isPlaying)
		{
			photonViewList = new Dictionary<int, PhotonView>();
			return;
		}
		if (netView.viewID == 0)
		{
			Debug.Log((object)("PhotonView register is ignored, because viewID is 0. No id assigned yet to: " + netView));
			return;
		}
		PhotonView value = null;
		if (photonViewList.TryGetValue(netView.viewID, out value))
		{
			if (!((Object)(object)netView != (Object)(object)value))
			{
				return;
			}
			Debug.LogError((object)$"PhotonView ID duplicate found: {netView.viewID}. New: {netView} old: {value}. Maybe one wasn't destroyed on scene load?! Check for 'DontDestroyOnLoad'. Destroying old entry, adding new.");
			RemoveInstantiatedGO(((Component)value).gameObject, localOnly: true);
		}
		photonViewList.Add(netView.viewID, netView);
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
		{
			Debug.Log((object)("Registered PhotonView: " + netView.viewID));
		}
	}

	public void OpCleanRpcBuffer(int actorNumber)
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.CachingOption = EventCaching.RemoveFromRoomCache;
		raiseEventOptions.TargetActors = new int[1] { actorNumber };
		RaiseEventOptions raiseEventOptions2 = raiseEventOptions;
		OpRaiseEvent(200, null, sendReliable: true, raiseEventOptions2);
	}

	public void OpRemoveCompleteCacheOfPlayer(int actorNumber)
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.CachingOption = EventCaching.RemoveFromRoomCache;
		raiseEventOptions.TargetActors = new int[1] { actorNumber };
		RaiseEventOptions raiseEventOptions2 = raiseEventOptions;
		OpRaiseEvent(0, null, sendReliable: true, raiseEventOptions2);
	}

	public void OpRemoveCompleteCache()
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions
		{
			CachingOption = EventCaching.RemoveFromRoomCache,
			Receivers = ReceiverGroup.MasterClient
		};
		OpRaiseEvent(0, null, sendReliable: true, raiseEventOptions);
	}

	private void RemoveCacheOfLeftPlayers()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<byte, object> dictionary = new Dictionary<byte, object>();
		dictionary[244] = (byte)0;
		dictionary[247] = (byte)7;
		((PhotonPeer)this).SendOperation((byte)253, dictionary, SendOptions.SendReliable);
	}

	public void CleanRpcBufferIfMine(PhotonView view)
	{
		if (view.ownerId != LocalPlayer.ID && !LocalPlayer.IsMasterClient)
		{
			Debug.LogError((object)string.Concat("Cannot remove cached RPCs on a PhotonView thats not ours! ", view.owner, " scene: ", view.isSceneView.ToString()));
		}
		else
		{
			OpCleanRpcBuffer(view);
		}
	}

	public void OpCleanRpcBuffer(PhotonView view)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		Hashtable val = new Hashtable();
		val[(object)(byte)0] = view.viewID;
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions
		{
			CachingOption = EventCaching.RemoveFromRoomCache
		};
		OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions);
	}

	public void RemoveRPCsInGroup(int group)
	{
		foreach (PhotonView value in photonViewList.Values)
		{
			if (value.group == group)
			{
				CleanRpcBufferIfMine(value);
			}
		}
	}

	public void SetLevelPrefix(short prefix)
	{
		currentLevelPrefix = prefix;
	}

	internal void RPC(PhotonView view, string methodName, PhotonTargets target, PhotonPlayer player, bool encrypt, params object[] parameters)
	{
		if (blockSendingGroups.Contains(view.group))
		{
			return;
		}
		if (view.viewID < 1)
		{
			Debug.LogError((object)("Illegal view ID:" + view.viewID + " method: " + methodName + " GO:" + ((Object)((Component)view).gameObject).name));
		}
		if (PhotonNetwork.logLevel >= PhotonLogLevel.Full)
		{
			Debug.Log((object)string.Concat("Sending RPC \"", methodName, "\" to target: ", target, " or player:", player, "."));
		}
		((Dictionary<object, object>)(object)reusedRpcEvent).Clear();
		Hashtable val = reusedRpcEvent;
		val[keyByteZero] = view.viewID;
		if (view.prefix > 0)
		{
			val[keyByteOne] = (short)view.prefix;
		}
		val[keyByteTwo] = PhotonNetwork.ServerTimestamp;
		int value = 0;
		if (rpcShortcuts.TryGetValue(methodName, out value))
		{
			val[keyByteFive] = (byte)value;
		}
		else
		{
			val[keyByteThree] = methodName;
		}
		if (parameters != null && parameters.Length != 0)
		{
			val[keyByteFour] = parameters;
		}
		if (player != null)
		{
			if (LocalPlayer.ID == player.ID)
			{
				ExecuteRpc(val, player.ID);
				return;
			}
			RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
			raiseEventOptions.TargetActors = new int[1] { player.ID };
			raiseEventOptions.Encrypt = encrypt;
			RaiseEventOptions raiseEventOptions2 = raiseEventOptions;
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions2);
			return;
		}
		switch (target)
		{
		case PhotonTargets.All:
		{
			RaiseEventOptions raiseEventOptions9 = new RaiseEventOptions
			{
				InterestGroup = view.group,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions9);
			ExecuteRpc(val, LocalPlayer.ID);
			break;
		}
		case PhotonTargets.Others:
		{
			RaiseEventOptions raiseEventOptions8 = new RaiseEventOptions
			{
				InterestGroup = view.group,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions8);
			break;
		}
		case PhotonTargets.AllBuffered:
		{
			RaiseEventOptions raiseEventOptions6 = new RaiseEventOptions
			{
				CachingOption = EventCaching.AddToRoomCache,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions6);
			ExecuteRpc(val, LocalPlayer.ID);
			break;
		}
		case PhotonTargets.OthersBuffered:
		{
			RaiseEventOptions raiseEventOptions4 = new RaiseEventOptions
			{
				CachingOption = EventCaching.AddToRoomCache,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions4);
			break;
		}
		case PhotonTargets.MasterClient:
		{
			if (mMasterClientId == LocalPlayer.ID)
			{
				ExecuteRpc(val, LocalPlayer.ID);
				break;
			}
			RaiseEventOptions raiseEventOptions7 = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.MasterClient,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions7);
			break;
		}
		case PhotonTargets.AllViaServer:
		{
			RaiseEventOptions raiseEventOptions5 = new RaiseEventOptions
			{
				InterestGroup = view.group,
				Receivers = ReceiverGroup.All,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions5);
			if (PhotonNetwork.offlineMode)
			{
				ExecuteRpc(val, LocalPlayer.ID);
			}
			break;
		}
		case PhotonTargets.AllBufferedViaServer:
		{
			RaiseEventOptions raiseEventOptions3 = new RaiseEventOptions
			{
				InterestGroup = view.group,
				Receivers = ReceiverGroup.All,
				CachingOption = EventCaching.AddToRoomCache,
				Encrypt = encrypt
			};
			OpRaiseEvent(200, val, sendReliable: true, raiseEventOptions3);
			if (PhotonNetwork.offlineMode)
			{
				ExecuteRpc(val, LocalPlayer.ID);
			}
			break;
		}
		default:
			Debug.LogError((object)("Unsupported target enum: " + target));
			break;
		}
	}

	public void SetInterestGroups(byte[] disableGroups, byte[] enableGroups)
	{
		if (disableGroups != null)
		{
			if (disableGroups.Length == 0)
			{
				allowedReceivingGroups.Clear();
			}
			else
			{
				foreach (byte b in disableGroups)
				{
					if (b <= 0)
					{
						Debug.LogError((object)("Error: PhotonNetwork.SetInterestGroups was called with an illegal group number: " + b + ". The group number should be at least 1."));
					}
					else if (allowedReceivingGroups.Contains(b))
					{
						allowedReceivingGroups.Remove(b);
					}
				}
			}
		}
		if (enableGroups != null)
		{
			if (enableGroups.Length == 0)
			{
				for (byte b2 = 0; b2 < byte.MaxValue; b2++)
				{
					allowedReceivingGroups.Add(b2);
				}
				allowedReceivingGroups.Add(byte.MaxValue);
			}
			else
			{
				foreach (byte b3 in enableGroups)
				{
					if (b3 <= 0)
					{
						Debug.LogError((object)("Error: PhotonNetwork.SetInterestGroups was called with an illegal group number: " + b3 + ". The group number should be at least 1."));
					}
					else
					{
						allowedReceivingGroups.Add(b3);
					}
				}
			}
		}
		if (!PhotonNetwork.offlineMode)
		{
			OpChangeGroups(disableGroups, enableGroups);
		}
	}

	public void SetSendingEnabled(byte group, bool enabled)
	{
		if (!enabled)
		{
			blockSendingGroups.Add(group);
		}
		else
		{
			blockSendingGroups.Remove(group);
		}
	}

	public void SetSendingEnabled(byte[] disableGroups, byte[] enableGroups)
	{
		if (disableGroups != null)
		{
			foreach (byte item in disableGroups)
			{
				blockSendingGroups.Add(item);
			}
		}
		if (enableGroups != null)
		{
			foreach (byte item2 in enableGroups)
			{
				blockSendingGroups.Remove(item2);
			}
		}
	}

	public void NewSceneLoaded()
	{
		if (loadingLevelAndPausedNetwork)
		{
			loadingLevelAndPausedNetwork = false;
			PhotonNetwork.isMessageQueueRunning = true;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, PhotonView> photonView in photonViewList)
		{
			if ((Object)(object)photonView.Value == (Object)null)
			{
				list.Add(photonView.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			int key = list[i];
			photonViewList.Remove(key);
		}
		if (list.Count > 0 && PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
		{
			Debug.Log((object)("New level loaded. Removed " + list.Count + " scene view IDs from last level."));
		}
	}

	public void RunViewUpdate()
	{
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Expected O, but got Unknown
		if (!PhotonNetwork.connected || PhotonNetwork.offlineMode || mActors == null)
		{
			return;
		}
		if (PhotonNetwork.inRoom && _AsyncLevelLoadingOperation != null && _AsyncLevelLoadingOperation.isDone)
		{
			_AsyncLevelLoadingOperation = null;
			LoadLevelIfSynced();
		}
		if (mActors.Count <= 1)
		{
			return;
		}
		int num = 0;
		options.Reset();
		List<int> list = null;
		Dictionary<int, PhotonView>.Enumerator enumerator = photonViewList.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PhotonView value = enumerator.Current.Value;
			if ((Object)(object)value == (Object)null)
			{
				Debug.LogError((object)$"PhotonView with ID {enumerator.Current.Key} wasn't properly unregistered! Please report this case to developer@photonengine.com");
				if (list == null)
				{
					list = new List<int>(4);
				}
				list.Add(enumerator.Current.Key);
			}
			else
			{
				if (value.synchronization == ViewSynchronization.Off || !value.isMine || !((Component)value).gameObject.activeInHierarchy || blockSendingGroups.Contains(value.group))
				{
					continue;
				}
				object[] array = OnSerializeWrite(value);
				if (array == null)
				{
					continue;
				}
				if (value.synchronization == ViewSynchronization.ReliableDeltaCompressed || value.mixedModeIsReliable)
				{
					Hashtable value2 = null;
					if (!dataPerGroupReliable.TryGetValue(value.group, out value2))
					{
						value2 = new Hashtable(ObjectsInOneUpdate);
						dataPerGroupReliable[value.group] = value2;
					}
					((Dictionary<object, object>)(object)value2).Add((object)(byte)(((Dictionary<object, object>)(object)value2).Count + 10), (object)array);
					num++;
					if (((Dictionary<object, object>)(object)value2).Count >= ObjectsInOneUpdate)
					{
						num -= ((Dictionary<object, object>)(object)value2).Count;
						options.InterestGroup = value.group;
						value2[(object)(byte)0] = PhotonNetwork.ServerTimestamp;
						if (currentLevelPrefix >= 0)
						{
							value2[(object)(byte)1] = currentLevelPrefix;
						}
						OpRaiseEvent(206, value2, sendReliable: true, options);
						((Dictionary<object, object>)(object)value2).Clear();
					}
					continue;
				}
				Hashtable value3 = null;
				if (!dataPerGroupUnreliable.TryGetValue(value.group, out value3))
				{
					value3 = new Hashtable(ObjectsInOneUpdate);
					dataPerGroupUnreliable[value.group] = value3;
				}
				((Dictionary<object, object>)(object)value3).Add((object)(byte)(((Dictionary<object, object>)(object)value3).Count + 10), (object)array);
				num++;
				if (((Dictionary<object, object>)(object)value3).Count >= ObjectsInOneUpdate)
				{
					num -= ((Dictionary<object, object>)(object)value3).Count;
					options.InterestGroup = value.group;
					value3[(object)(byte)0] = PhotonNetwork.ServerTimestamp;
					if (currentLevelPrefix >= 0)
					{
						value3[(object)(byte)1] = currentLevelPrefix;
					}
					OpRaiseEvent(201, value3, sendReliable: false, options);
					((Dictionary<object, object>)(object)value3).Clear();
				}
			}
		}
		if (list != null)
		{
			int i = 0;
			for (int count = list.Count; i < count; i++)
			{
				photonViewList.Remove(list[i]);
			}
		}
		if (num == 0)
		{
			return;
		}
		foreach (int key in dataPerGroupReliable.Keys)
		{
			options.InterestGroup = (byte)key;
			Hashtable val = dataPerGroupReliable[key];
			if (((Dictionary<object, object>)(object)val).Count != 0)
			{
				val[(object)(byte)0] = PhotonNetwork.ServerTimestamp;
				if (currentLevelPrefix >= 0)
				{
					val[(object)(byte)1] = currentLevelPrefix;
				}
				OpRaiseEvent(206, val, sendReliable: true, options);
				((Dictionary<object, object>)(object)val).Clear();
			}
		}
		foreach (int key2 in dataPerGroupUnreliable.Keys)
		{
			options.InterestGroup = (byte)key2;
			Hashtable val2 = dataPerGroupUnreliable[key2];
			if (((Dictionary<object, object>)(object)val2).Count != 0)
			{
				val2[(object)(byte)0] = PhotonNetwork.ServerTimestamp;
				if (currentLevelPrefix >= 0)
				{
					val2[(object)(byte)1] = currentLevelPrefix;
				}
				OpRaiseEvent(201, val2, sendReliable: false, options);
				((Dictionary<object, object>)(object)val2).Clear();
			}
		}
	}

	private object[] OnSerializeWrite(PhotonView view)
	{
		if (view.synchronization == ViewSynchronization.Off)
		{
			return null;
		}
		PhotonMessageInfo info = new PhotonMessageInfo(LocalPlayer, PhotonNetwork.ServerTimestamp, view);
		pStream.ResetWriteStream();
		pStream.SendNext(null);
		pStream.SendNext(null);
		pStream.SendNext(null);
		view.SerializeView(pStream, info);
		if (pStream.Count <= 3)
		{
			return null;
		}
		object[] array = pStream.ToArray();
		array[0] = view.viewID;
		array[1] = false;
		array[2] = null;
		if (view.synchronization == ViewSynchronization.Unreliable)
		{
			return array;
		}
		if (view.synchronization == ViewSynchronization.UnreliableOnChange)
		{
			if (AlmostEquals(array, view.lastOnSerializeDataSent))
			{
				if (view.mixedModeIsReliable)
				{
					return null;
				}
				view.mixedModeIsReliable = true;
				view.lastOnSerializeDataSent = array;
			}
			else
			{
				view.mixedModeIsReliable = false;
				view.lastOnSerializeDataSent = array;
			}
			return array;
		}
		if (view.synchronization == ViewSynchronization.ReliableDeltaCompressed)
		{
			object[] result = DeltaCompressionWrite(view.lastOnSerializeDataSent, array);
			view.lastOnSerializeDataSent = array;
			return result;
		}
		return null;
	}

	private void OnSerializeRead(object[] data, PhotonPlayer sender, int networkTime, short correctPrefix)
	{
		int num = (int)data[0];
		PhotonView photonView = GetPhotonView(num);
		if ((Object)(object)photonView == (Object)null)
		{
			Debug.LogWarning((object)("Received OnSerialization for view ID " + num + ". We have no such PhotonView! Ignored this if you're leaving a room. State: " + State));
		}
		else if (photonView.prefix > 0 && correctPrefix != photonView.prefix)
		{
			Debug.LogError((object)("Received OnSerialization for view ID " + num + " with prefix " + correctPrefix + ". Our prefix is " + photonView.prefix));
		}
		else
		{
			if (photonView.group != 0 && !allowedReceivingGroups.Contains(photonView.group))
			{
				return;
			}
			if (photonView.synchronization == ViewSynchronization.ReliableDeltaCompressed)
			{
				object[] array = DeltaCompressionRead(photonView.lastOnSerializeDataReceived, data);
				if (array == null)
				{
					if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
					{
						Debug.Log((object)("Skipping packet for " + ((Object)photonView).name + " [" + photonView.viewID + "] as we haven't received a full packet for delta compression yet. This is OK if it happens for the first few frames after joining a game."));
					}
					return;
				}
				photonView.lastOnSerializeDataReceived = array;
				data = array;
			}
			if (sender.ID != photonView.ownerId && (!photonView.OwnerShipWasTransfered || photonView.ownerId == 0) && photonView.currentMasterID == -1)
			{
				photonView.ownerId = sender.ID;
			}
			readStream.SetReadStream(data, 3);
			photonView.DeserializeView(info: new PhotonMessageInfo(sender, networkTime, photonView), stream: readStream);
		}
	}

	private object[] DeltaCompressionWrite(object[] previousContent, object[] currentContent)
	{
		if (currentContent == null || previousContent == null || previousContent.Length != currentContent.Length)
		{
			return currentContent;
		}
		if (currentContent.Length <= 3)
		{
			return null;
		}
		previousContent[1] = false;
		int num = 0;
		Queue<int> queue = null;
		for (int i = 3; i < currentContent.Length; i++)
		{
			object obj = currentContent[i];
			object two = previousContent[i];
			if (AlmostEquals(obj, two))
			{
				num++;
				previousContent[i] = null;
				continue;
			}
			previousContent[i] = obj;
			if (obj == null)
			{
				if (queue == null)
				{
					queue = new Queue<int>(currentContent.Length);
				}
				queue.Enqueue(i);
			}
		}
		if (num > 0)
		{
			if (num == currentContent.Length - 3)
			{
				return null;
			}
			previousContent[1] = true;
			if (queue != null)
			{
				previousContent[2] = queue.ToArray();
			}
		}
		previousContent[0] = currentContent[0];
		return previousContent;
	}

	private object[] DeltaCompressionRead(object[] lastOnSerializeDataReceived, object[] incomingData)
	{
		if (!(bool)incomingData[1])
		{
			return incomingData;
		}
		if (lastOnSerializeDataReceived == null)
		{
			return null;
		}
		int[] array = incomingData[2] as int[];
		for (int i = 3; i < incomingData.Length; i++)
		{
			if ((array == null || !array.Contains(i)) && incomingData[i] == null)
			{
				object obj = lastOnSerializeDataReceived[i];
				incomingData[i] = obj;
			}
		}
		return incomingData;
	}

	private bool AlmostEquals(object[] lastData, object[] currentContent)
	{
		if (lastData == null && currentContent == null)
		{
			return true;
		}
		if (lastData == null || currentContent == null || lastData.Length != currentContent.Length)
		{
			return false;
		}
		for (int i = 0; i < currentContent.Length; i++)
		{
			object one = currentContent[i];
			object two = lastData[i];
			if (!AlmostEquals(one, two))
			{
				return false;
			}
		}
		return true;
	}

	private bool AlmostEquals(object one, object two)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (one == null || two == null)
		{
			if (one == null)
			{
				return two == null;
			}
			return false;
		}
		if (!one.Equals(two))
		{
			if (one is _003F target)
			{
				Vector3 second = (Vector3)two;
				if (((Vector3)target).AlmostEquals(second, PhotonNetwork.precisionForVectorSynchronization))
				{
					return true;
				}
			}
			else if (one is _003F target2)
			{
				Vector2 second2 = (Vector2)two;
				if (((Vector2)target2).AlmostEquals(second2, PhotonNetwork.precisionForVectorSynchronization))
				{
					return true;
				}
			}
			else if (one is _003F target3)
			{
				Quaternion second3 = (Quaternion)two;
				if (((Quaternion)target3).AlmostEquals(second3, PhotonNetwork.precisionForQuaternionSynchronization))
				{
					return true;
				}
			}
			else if (one is float target4)
			{
				float second4 = (float)two;
				if (target4.AlmostEquals(second4, PhotonNetwork.precisionForFloatSynchronization))
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	protected internal static bool GetMethod(MonoBehaviour monob, string methodType, out MethodInfo mi)
	{
		mi = null;
		if ((Object)(object)monob == (Object)null || string.IsNullOrEmpty(methodType))
		{
			return false;
		}
		List<MethodInfo> methods = SupportClass.GetMethods(((object)monob).GetType(), (Type)null);
		for (int i = 0; i < methods.Count; i++)
		{
			MethodInfo methodInfo = methods[i];
			if (methodInfo.Name.Equals(methodType))
			{
				mi = methodInfo;
				return true;
			}
		}
		return false;
	}

	protected internal void LoadLevelIfSynced()
	{
		if (!PhotonNetwork.automaticallySyncScene || PhotonNetwork.isMasterClient || PhotonNetwork.room == null)
		{
			return;
		}
		if (_AsyncLevelLoadingOperation != null)
		{
			if (!_AsyncLevelLoadingOperation.isDone)
			{
				return;
			}
			_AsyncLevelLoadingOperation = null;
		}
		if (!((Dictionary<object, object>)(object)PhotonNetwork.room.CustomProperties).ContainsKey((object)"curScn"))
		{
			return;
		}
		bool flag = ((Dictionary<object, object>)(object)PhotonNetwork.room.CustomProperties).ContainsKey((object)"curScnLa");
		object obj = PhotonNetwork.room.CustomProperties[(object)"curScn"];
		if (obj is int)
		{
			if (SceneManagerHelper.ActiveSceneBuildIndex != (int)obj)
			{
				if (flag)
				{
					_AsyncLevelLoadingOperation = PhotonNetwork.LoadLevelAsync((int)obj);
				}
				else
				{
					PhotonNetwork.LoadLevel((int)obj);
				}
			}
		}
		else if (obj is string && SceneManagerHelper.ActiveSceneName != (string)obj)
		{
			if (flag)
			{
				_AsyncLevelLoadingOperation = PhotonNetwork.LoadLevelAsync((string)obj);
			}
			else
			{
				PhotonNetwork.LoadLevel((string)obj);
			}
		}
	}

	protected internal void SetLevelInPropsIfSynced(object levelId, bool initiatingCall, bool asyncLoading = false)
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		if (!PhotonNetwork.automaticallySyncScene || !PhotonNetwork.isMasterClient || PhotonNetwork.room == null)
		{
			return;
		}
		if (levelId == null)
		{
			Debug.LogError((object)"Parameter levelId can't be null!");
			return;
		}
		if (!asyncLoading && ((Dictionary<object, object>)(object)PhotonNetwork.room.CustomProperties).ContainsKey((object)"curScn"))
		{
			object obj = PhotonNetwork.room.CustomProperties[(object)"curScn"];
			if (obj is int && SceneManagerHelper.ActiveSceneBuildIndex == (int)obj)
			{
				SendLevelReloadEvent();
				return;
			}
			if (obj is string && SceneManagerHelper.ActiveSceneName != null && SceneManagerHelper.ActiveSceneName.Equals((string)obj))
			{
				bool flag = false;
				if (!IsReloadingLevel)
				{
					if (levelId is int)
					{
						flag = (int)levelId == SceneManagerHelper.ActiveSceneBuildIndex;
					}
					else if (levelId is string)
					{
						flag = SceneManagerHelper.ActiveSceneName.Equals((string)levelId);
					}
				}
				if (initiatingCall && IsReloadingLevel)
				{
					flag = false;
				}
				if (flag)
				{
					SendLevelReloadEvent();
				}
				return;
			}
		}
		Hashtable val = new Hashtable();
		if (levelId is int)
		{
			val[(object)"curScn"] = (int)levelId;
		}
		else if (levelId is string)
		{
			val[(object)"curScn"] = (string)levelId;
		}
		else
		{
			Debug.LogError((object)"Parameter levelId must be int or string!");
		}
		if (asyncLoading)
		{
			val[(object)"curScnLa"] = true;
		}
		PhotonNetwork.room.SetCustomProperties(val);
		((PhotonPeer)this).SendOutgoingCommands();
	}

	private void SendLevelReloadEvent()
	{
		IsReloadingLevel = true;
		if (PhotonNetwork.inRoom)
		{
			OpRaiseEvent(212, AsynchLevelLoadCall, sendReliable: true, _levelReloadEventOptions);
		}
	}

	public void SetApp(string appId, string gameVersion)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		IniSettings iniSettings = LoadIniSettings();
		if (iniSettings != null)
		{
			if (!string.IsNullOrEmpty(iniSettings.ServerAddress))
			{
				PhotonNetwork.PhotonServerSettings.ServerAddress = iniSettings.ServerAddress;
				PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.SelfHosted;
			}
			if (iniSettings.ServerPort > 0)
			{
				PhotonNetwork.PhotonServerSettings.ServerPort = iniSettings.ServerPort;
			}
			PhotonNetwork.PhotonServerSettings.Protocol = iniSettings.Protocol;
			if (!string.IsNullOrEmpty(iniSettings.ServerVersion))
			{
				PhotonNetwork.gameVersion = iniSettings.ServerVersion;
			}
		}
		AppId = appId?.Trim();
		if (!string.IsNullOrEmpty(gameVersion))
		{
			PhotonNetwork.gameVersion = gameVersion.Trim();
		}
	}

	public bool WebRpc(string uriPath, object parameters)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<byte, object> dictionary = new Dictionary<byte, object>();
		dictionary.Add(209, uriPath);
		dictionary.Add(208, parameters);
		return ((PhotonPeer)this).SendOperation((byte)219, dictionary, SendOptions.SendReliable);
	}

	static NetworkingPeer()
	{
		ProtocolToNameServerPort = new Dictionary<ConnectionProtocol, int>
		{
			{
				(ConnectionProtocol)0,
				5058
			},
			{
				(ConnectionProtocol)1,
				4533
			},
			{
				(ConnectionProtocol)4,
				9093
			},
			{
				(ConnectionProtocol)5,
				19093
			}
		};
		UsePrefabCache = true;
		PrefabCache = new Dictionary<string, GameObject>();
		OnPhotonInstantiateString = PhotonNetworkingMessage.OnPhotonInstantiate.ToString();
		ObjectsInOneUpdate = 10;
	}

	private IniSettings LoadIniSettings()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", "LANSettings.ini"));
		if (!File.Exists(fullPath))
		{
			Debug.LogWarning((object)"'LANSettings.ini' not found in game root.");
			return null;
		}
		string serverAddress = null;
		int result = 0;
		ConnectionProtocol result2 = (ConnectionProtocol)0;
		string serverVersion = null;
		bool flag = false;
		try
		{
			string[] array = File.ReadAllLines(fullPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (string.IsNullOrEmpty(text) || text.StartsWith(";") || text.StartsWith("#"))
				{
					continue;
				}
				if (text.StartsWith("[") && text.EndsWith("]"))
				{
					flag = text.Equals("[Server]", StringComparison.OrdinalIgnoreCase);
				}
				else if (flag)
				{
					if (text.StartsWith("ServerAddress=", StringComparison.OrdinalIgnoreCase))
					{
						serverAddress = text.Substring("ServerAddress=".Length).Trim();
					}
					if (text.StartsWith("ServerPort=", StringComparison.OrdinalIgnoreCase))
					{
						int.TryParse(text.Substring("ServerPort=".Length).Trim(), out result);
					}
					if (text.StartsWith("Protocol=", StringComparison.OrdinalIgnoreCase) && !Enum.TryParse<ConnectionProtocol>(text.Substring("Protocol=".Length).Trim(), true, out result2))
					{
						result2 = (ConnectionProtocol)0;
					}
					if (text.StartsWith("ServerVersion=", StringComparison.OrdinalIgnoreCase))
					{
						serverVersion = text.Substring("ServerVersion=".Length).Trim();
					}
				}
			}
			return new IniSettings
			{
				ServerAddress = serverAddress,
				ServerPort = result,
				Protocol = result2,
				ServerVersion = serverVersion
			};
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("Error reading 'LANSettings.ini': " + ex.Message));
		}
		return null;
	}
}
