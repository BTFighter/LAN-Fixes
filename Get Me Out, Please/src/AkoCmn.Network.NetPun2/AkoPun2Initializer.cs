using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AkoCmn.Network.NetCore;
using AkoCmn.Utility;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AkoCmn.Network.NetPun2;

public class AkoPun2Initializer
{
	private AkoPun2NetStateHolder _netState;

	private AkoPun2NetDataReceiver _dataReceiver;

	private AkoPun2NetClock _netClock;

	private AkoNetCallbacks _netCallbacks;

	private bool _bFrameworkInited;

	private bool _bEnterNetwork;

	private ulong _selfConstantId;

	private List<AkoPun2NetSessionProperty> _searchedRoomList;

	private AkoPun2NetPlayerInfoHolder _roomPlayerInfoHolder;

	private AkoPun2NetPunCallbacks _punCallbacks;

	private TypedLobby _typedLobby;

	private AkoNetworkSessionSearchData _currentSearchData;

	public AkoPun2NetStateHolder netState => _netState;

	public AkoPun2NetDataReceiver dataReceiver => _dataReceiver;

	public AkoPun2NetClock netClock => _netClock;

	public bool bFrameworkInited => _bFrameworkInited;

	public ulong selfConstantId => _selfConstantId;

	public AkoPun2NetPunCallbacks punCallbacks => _punCallbacks;

	public AkoPun2Initializer()
	{
		_netState = new AkoPun2NetStateHolder();
		_dataReceiver = new AkoPun2NetDataReceiver();
		_dataReceiver.RegisterCbReceive(OnReceiveData);
		_netClock = new AkoPun2NetClock();
	}

	~AkoPun2Initializer()
	{
		_netState = null;
		if (_dataReceiver != null)
		{
			_dataReceiver.UnregisterCbReceive(OnReceiveData);
			_dataReceiver.Final();
			_dataReceiver = null;
		}
		_netClock = null;
		_netCallbacks = null;
	}

	public void SetNetCallbaks(AkoNetCallbacks netCallbacks)
	{
		_netCallbacks = netCallbacks;
	}

	public AkoPun2NetResult Pun2Init()
	{
		if (_bFrameworkInited)
		{
			return AkoPun2NetResult.okResult;
		}
		AkoPun2NetResult akoPun2NetResult = InitializeFramework();
		if (!akoPun2NetResult.IsSuccess())
		{
			Pun2Final();
			return akoPun2NetResult;
		}
		akoPun2NetResult = InitializeCloud();
		if (!akoPun2NetResult.IsSuccess())
		{
			Pun2Final();
			return akoPun2NetResult;
		}
		_bFrameworkInited = true;
		_netState.SetNetworkState(AkoNetworkState.MemoryAllocated);
		return AkoPun2NetResult.okResult;
	}

	public void Pun2Final()
	{
		if (_bFrameworkInited)
		{
			if (PhotonNetwork.IsConnectedAndReady)
			{
				PhotonNetwork.Disconnect();
			}
			EndNetwork();
		}
		FinalizeFramework();
		_bFrameworkInited = false;
		_netState.SetNetworkState(AkoNetworkState.Initial);
	}

	public AkoPun2NetResult StartNetwork()
	{
		if (_bFrameworkInited)
		{
			_ = _bEnterNetwork;
			return AkoPun2NetResult.okResult;
		}
		return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_NotInitFramework);
	}

	public void EndNetwork()
	{
		if (_bEnterNetwork)
		{
			_bEnterNetwork = false;
		}
	}

	private AkoPun2NetResult InitializeFramework()
	{
		_searchedRoomList = new List<AkoPun2NetSessionProperty>();
		_roomPlayerInfoHolder = new AkoPun2NetPlayerInfoHolder();
		_punCallbacks = AkoPun2NetPunCallbacks.Generate();
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks = _punCallbacks;
		akoPun2NetPunCallbacks.cbOnRoomListUpdate = (Action<List<RoomInfo>>)Delegate.Combine(akoPun2NetPunCallbacks.cbOnRoomListUpdate, new Action<List<RoomInfo>>(OnRoomListUpdate));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks2 = _punCallbacks;
		akoPun2NetPunCallbacks2.cbOnJoinedRoom = (Action)Delegate.Combine(akoPun2NetPunCallbacks2.cbOnJoinedRoom, new Action(OnJoinedRoom));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks3 = _punCallbacks;
		akoPun2NetPunCallbacks3.cbOnLeftRoom = (Action)Delegate.Combine(akoPun2NetPunCallbacks3.cbOnLeftRoom, new Action(OnLeftRoom));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks4 = _punCallbacks;
		akoPun2NetPunCallbacks4.cbOnMasterClientSwitched = (Action<Player>)Delegate.Combine(akoPun2NetPunCallbacks4.cbOnMasterClientSwitched, new Action<Player>(OnMasterClientSwitched));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks5 = _punCallbacks;
		akoPun2NetPunCallbacks5.cbOnPlayerEnteredRoom = (Action<Player>)Delegate.Combine(akoPun2NetPunCallbacks5.cbOnPlayerEnteredRoom, new Action<Player>(OnPlayerEnteredRoom));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks6 = _punCallbacks;
		akoPun2NetPunCallbacks6.cbOnPlayerLeftRoom = (Action<Player>)Delegate.Combine(akoPun2NetPunCallbacks6.cbOnPlayerLeftRoom, new Action<Player>(OnPlayerLeftRoom));
		AkoPun2NetPunCallbacks akoPun2NetPunCallbacks7 = _punCallbacks;
		akoPun2NetPunCallbacks7.cbOnRaiseEvent = (Action<EventData>)Delegate.Combine(akoPun2NetPunCallbacks7.cbOnRaiseEvent, new Action<EventData>(OnRaiseEvent));
		return AkoPun2NetResult.okResult;
	}

	private void FinalizeFramework()
	{
		if (_punCallbacks != null)
		{
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks = _punCallbacks;
			akoPun2NetPunCallbacks.cbOnRoomListUpdate = (Action<List<RoomInfo>>)Delegate.Remove(akoPun2NetPunCallbacks.cbOnRoomListUpdate, new Action<List<RoomInfo>>(OnRoomListUpdate));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks2 = _punCallbacks;
			akoPun2NetPunCallbacks2.cbOnJoinedRoom = (Action)Delegate.Remove(akoPun2NetPunCallbacks2.cbOnJoinedRoom, new Action(OnJoinedRoom));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks3 = _punCallbacks;
			akoPun2NetPunCallbacks3.cbOnLeftRoom = (Action)Delegate.Remove(akoPun2NetPunCallbacks3.cbOnLeftRoom, new Action(OnLeftRoom));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks4 = _punCallbacks;
			akoPun2NetPunCallbacks4.cbOnMasterClientSwitched = (Action<Player>)Delegate.Remove(akoPun2NetPunCallbacks4.cbOnMasterClientSwitched, new Action<Player>(OnMasterClientSwitched));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks5 = _punCallbacks;
			akoPun2NetPunCallbacks5.cbOnPlayerEnteredRoom = (Action<Player>)Delegate.Remove(akoPun2NetPunCallbacks5.cbOnPlayerEnteredRoom, new Action<Player>(OnPlayerEnteredRoom));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks6 = _punCallbacks;
			akoPun2NetPunCallbacks6.cbOnPlayerLeftRoom = (Action<Player>)Delegate.Remove(akoPun2NetPunCallbacks6.cbOnPlayerLeftRoom, new Action<Player>(OnPlayerLeftRoom));
			AkoPun2NetPunCallbacks akoPun2NetPunCallbacks7 = _punCallbacks;
			akoPun2NetPunCallbacks7.cbOnRaiseEvent = (Action<EventData>)Delegate.Remove(akoPun2NetPunCallbacks7.cbOnRaiseEvent, new Action<EventData>(OnRaiseEvent));
			UnityEngine.Object.Destroy(_punCallbacks.gameObject);
			_punCallbacks = null;
		}
		if (_searchedRoomList != null)
		{
			_searchedRoomList.Clear();
			_searchedRoomList = null;
		}
		if (_roomPlayerInfoHolder != null)
		{
			_roomPlayerInfoHolder.Clear();
			_roomPlayerInfoHolder = null;
		}
	}

	private AkoPun2NetResult InitializeCloud()
	{
		PhotonNetwork.AutomaticallySyncScene = false;
		PhotonNetwork.KeepAliveInBackground = 10f;
		_typedLobby = new TypedLobby("customLobby", LobbyType.Default);
		return AkoPun2NetResult.okResult;
	}

	private void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		_searchedRoomList.Clear();
		string filteringQuery = (_currentSearchData != null) ? GetRoomFilteringQuerySrc(_currentSearchData) : null;
		
		foreach (RoomInfo room in roomList)
		{
			if (IsRoomMatchingQuery(room, filteringQuery))
			{
				AkoPun2NetSessionProperty item = new AkoPun2NetSessionProperty(room);
				_searchedRoomList.Add(item);
			}
		}
	}
	
	private bool IsRoomMatchingQuery(RoomInfo room, string querySrc)
	{
		if (string.IsNullOrEmpty(querySrc))
		{
			return true;
		}
		
		// For now, we support simple queries like "C9 = 1234" where C9 is the matchmake key
		string[] parts = querySrc.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2)
		{
			return true;
		}
		
		string propertyKey = parts[0];
		int expectedValue;
		if (int.TryParse(parts[1], out expectedValue))
		{
			object actualValue;
			if (room.CustomProperties.TryGetValue(propertyKey, out actualValue) && actualValue is int)
			{
				return (int)actualValue == expectedValue;
			}
		}
		
		return false;
	}

	private void OnJoinedRoom()
	{
		_selfConstantId = AkoPun2NetPlayerInfoHolder.GetConstantIdBySelfActorNumber();
		_roomPlayerInfoHolder.UpdatePlayer();
		_netClock.SetStartTime();
	}

	private void OnLeftRoom()
	{
		_roomPlayerInfoHolder.Clear();
	}

	private void OnMasterClientSwitched(Player newMasterClient)
	{
		_netClock.SetStartTime();
		_netCallbacks?.InvokeHostChanged();
	}

	private void OnPlayerEnteredRoom(Player other)
	{
		_roomPlayerInfoHolder.UpdatePlayer();
		_netCallbacks?.InvokeJoinPlayer();
	}

	private void OnPlayerLeftRoom(Player other)
	{
		_roomPlayerInfoHolder.UpdatePlayer();
		_netCallbacks?.InvokeLeavePlayer();
	}

	private void OnRaiseEvent(EventData photonEvent)
	{
		_dataReceiver.OnRaiseEvent(photonEvent);
	}

	private void OnReceiveData(ushort port, ulong constantId, byte[] data, uint size)
	{
		_netCallbacks?.InvokeReceive(port, constantId, data, size);
	}

	internal AppSettings GetConnectSettings()
	{
		AppSettings appSettings = new AppSettings();
		PhotonNetwork.PhotonServerSettings.AppSettings.CopyTo(appSettings);
		appSettings.AppIdRealtime = AkoCloudAccessInfo.ApplicationId;
		appSettings.AppVersion = AkoCloudAccessInfo.applicationCommunicationVersion.ToString();
		appSettings.FixedRegion = AkoNetworkRegion.GetRegion(AkoSingletonMonoBehaviour<AkoGM>.instance.dataBase.optionData.serverIdx);
		Debug.Log("AppVer=" + appSettings.AppVersion + " リージョン=" + appSettings.FixedRegion);
		string text = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
		if (File.Exists(text))
		{
			AkoIniParser akoIniParser = AkoIniParser.Load(text);
			string value = akoIniParser.GetValue("Server", "ServerAddress");
			int intValue = akoIniParser.GetIntValue("Server", "Port", 5055);
			if (!string.IsNullOrEmpty(value))
			{
				appSettings.Server = value;
				appSettings.Port = intValue;
				appSettings.UseNameServer = false;
				Debug.Log("Using custom Photon server: " + value + ":" + intValue);
			}
		}
		return appSettings;
	}

	internal int GetSearchdRoomNum()
	{
		return _searchedRoomList.Count;
	}

	internal AkoPun2NetSessionProperty GetSearchdRoomInfo(int i)
	{
		return _searchedRoomList[i];
	}

	internal void GetSessionProperty(ref AkoPun2NetSessionProperty dst)
	{
		if (PhotonNetwork.InRoom)
		{
			dst.SetPropertyByRoomInfo(PhotonNetwork.CurrentRoom);
		}
		else
		{
			dst.SetProperty(0uL, 0uL, 0);
		}
	}

	internal void GetSessionStatus(ref AkoPun2NetSessionStatus dst)
	{
		if (PhotonNetwork.InRoom)
		{
			dst.SetPropertyByRoomInfo(PhotonNetwork.CurrentRoom);
			AkoPun2NetSessionStatus obj = dst;
			IAkoNetworkStationInfo[] aStationInfo = _roomPlayerInfoHolder.aStationInfo;
			obj.SetStationInfoList(aStationInfo);
		}
		else
		{
			dst.SetProperty(0uL, 0uL, 0);
			dst.SetStationInfoList(new IAkoNetworkStationInfo[0]);
		}
	}

	internal string GetSessionPlayerNameByCache(ulong constantId)
	{
		IAkoNetworkStationInfo cachedStationInfo = _roomPlayerInfoHolder.GetCachedStationInfo(constantId);
		if (cachedStationInfo == null)
		{
			return "";
		}
		return cachedStationInfo.GetPlayerName();
	}

	internal AkoPun2NetResult JoinCustomLobby()
	{
		if (!PhotonNetwork.JoinLobby(_typedLobby))
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_JoinLobbyFailed);
		}
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult JoinRandomOrCreateRoom(AkoNetworkSessionSearchData searchData, AkoNetworkSessionCreateData createData)
	{
		if (createData == null || searchData == null)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_RoomParamError);
		}
		string roomName = null;
		RoomOptions roomOptions = GenerateRoomOptions(createData.matchmakeKey, (byte)createData.playerNumMax, createData.aCustomAttribute);
		Debug.Log("ルームプロパティ");
		foreach (DictionaryEntry customRoomProperty in roomOptions.CustomRoomProperties)
		{
			Debug.Log("[" + customRoomProperty.Key?.ToString() + "]" + customRoomProperty.Value);
		}
		string roomFilteringQuerySrc = GetRoomFilteringQuerySrc(searchData);
		ExitGames.Client.Photon.Hashtable hashtable = GenerateExpectedCustomRoomProperties(roomFilteringQuerySrc);
		byte expectedMaxPlayers = (byte)searchData.playerNumMax;
		Debug.Log("検索プロパティ maxPlayer=" + expectedMaxPlayers);
		foreach (DictionaryEntry item in hashtable)
		{
			Debug.Log("[" + item.Key?.ToString() + "]" + item.Value);
		}
		roomFilteringQuerySrc = RetouchQuery(roomFilteringQuerySrc);
		Debug.Log("検索クエリ=" + roomFilteringQuerySrc);
		MatchmakingMode matchingType = MatchmakingMode.SerialMatching;
		if (!PhotonNetwork.JoinRandomOrCreateRoom(hashtable, expectedMaxPlayers, matchingType, null, roomFilteringQuerySrc, roomName, roomOptions))
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_CreateRoomFailed);
		}
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult SearchRoom(AkoNetworkSessionSearchData searchData)
	{
		if (searchData == null)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_RoomParamError);
		}
		
		// Store the current search data for filtering in OnRoomListUpdate
		_currentSearchData = searchData;
		_searchedRoomList.Clear();
		
		Debug.Log("Searching rooms with query: " + GetRoomFilteringQuerySrc(searchData));
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult CreateRoom(AkoNetworkSessionCreateData createData)
	{
		if (createData == null)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_RoomParamError);
		}
		string text = null;
		Debug.Log("作成ルーム名=" + text);
		RoomOptions roomOptions = GenerateRoomOptions(createData.matchmakeKey, (byte)createData.playerNumMax, createData.aCustomAttribute);
		Debug.Log("作成ルームプロパティ");
		foreach (DictionaryEntry customRoomProperty in roomOptions.CustomRoomProperties)
		{
			Debug.Log("[" + customRoomProperty.Key?.ToString() + "]" + customRoomProperty.Value);
		}
		if (!PhotonNetwork.CreateRoom(text, roomOptions, _typedLobby))
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_CreateRoomFailed);
		}
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult JoinRoom(string roomName)
	{
		if (string.IsNullOrEmpty(roomName))
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_RoomParamError);
		}
		if (!PhotonNetwork.JoinRoom(roomName))
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_JoinRoomFailed);
		}
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult LeaveRoom()
	{
		if (!PhotonNetwork.InRoom)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_RoomParamError);
		}
		if (!PhotonNetwork.LeaveRoom())
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_LeaveRoomFailed);
		}
		return AkoPun2NetResult.okResult;
	}

	internal AkoPun2NetResult CloseRoom()
	{
		if (!PhotonNetwork.InRoom)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PG_NotRoomClosable);
		}
		PhotonNetwork.CurrentRoom.IsOpen = false;
		return AkoPun2NetResult.okResult;
	}

	private RoomOptions GenerateRoomOptions(ulong matchmakeKey, byte playerNumMax, AkoNetworkSessionCustomAttribute[] aCustomAttribute)
	{
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = playerNumMax;
		roomOptions.PlayerTtl = 1000;
		roomOptions.EmptyRoomTtl = 2000;
		roomOptions.SuppressRoomEvents = false;
		roomOptions.SuppressPlayerInfo = false;
		roomOptions.PublishUserId = true;
		List<string> list = new List<string>();
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		string[] fixPropertyKeys = AkoPun2NetConst.FixPropertyKeys;
		for (int i = 0; i < fixPropertyKeys.Length; i++)
		{
			list.Add(fixPropertyKeys[i]);
			hashtable.Add(fixPropertyKeys[i], null);
		}
		foreach (AkoNetworkSessionCustomAttribute akoNetworkSessionCustomAttribute in aCustomAttribute)
		{
			if (hashtable.ContainsKey(akoNetworkSessionCustomAttribute.key))
			{
				if (hashtable[akoNetworkSessionCustomAttribute.key] == null)
				{
					hashtable[akoNetworkSessionCustomAttribute.key] = (int)akoNetworkSessionCustomAttribute.value;
				}
			}
			else
			{
				list.Add(akoNetworkSessionCustomAttribute.key);
				hashtable.Add(akoNetworkSessionCustomAttribute.key, (int)akoNetworkSessionCustomAttribute.value);
			}
		}
		hashtable[AkoPun2NetConst.FixPropertyKey9] = (int)matchmakeKey;
		roomOptions.CustomRoomPropertiesForLobby = list.ToArray();
		roomOptions.CustomRoomProperties = hashtable;
		return roomOptions;
	}

	private string GetRoomFilteringQuerySrc(AkoNetworkSessionSearchData searchData)
	{
		string result = null;
		switch (searchData.searchAttributeId)
		{
		case AkoNetworkSearchAttributeId.CustomAttribute_0:
			result = searchData.customAttributeFilteringQuery;
			break;
		case AkoNetworkSearchAttributeId.MatchmakeKey:
			result = "C9 = " + (int)searchData.matchmakeKey;
			break;
		}
		return result;
	}

	private ExitGames.Client.Photon.Hashtable GenerateExpectedCustomRoomProperties(string querySrc)
	{
		ExitGames.Client.Photon.Hashtable hashtable = null;
		if (!string.IsNullOrEmpty(querySrc))
		{
			string[] array = querySrc.Split(" ");
			string key = null;
			string text = null;
			bool flag = true;
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2 == "and")
				{
					key = null;
					text = null;
					flag = true;
					continue;
				}
				if (text2 == "=")
				{
					flag = false;
					continue;
				}
				if (flag)
				{
					key = text2;
					continue;
				}
				text = text2;
				if (hashtable == null)
				{
					hashtable = new ExitGames.Client.Photon.Hashtable();
				}
				hashtable.Add(key, int.Parse(text));
			}
		}
		return hashtable;
	}

	private string RetouchQuery(string querySrc)
	{
		if (!string.IsNullOrEmpty(querySrc))
		{
			querySrc = querySrc.Replace("and", "AND");
		}
		return querySrc;
	}

	internal AkoPun2NetResult RaiseEvent(byte eventCode, object eventContent, SendOptions sendOptions)
	{
		RaiseEventOptions raiseEventOptions = RaiseEventOptions.Default;
		if (!PhotonNetwork.RaiseEvent(eventCode, eventContent, raiseEventOptions, sendOptions) && sendOptions.Reliability)
		{
			return new AkoPun2NetResult(AkoNetworkResultCode.ERROR, AkoPun2NetErrorCode.PUN_SendRaizeEvent);
		}
		return AkoPun2NetResult.okResult;
	}
}
