using System;
using System.Collections.Generic;
using System.IO;
using AkoCmn;
using AkoCmn.Network;
using AkoCmn.Utility;
using AkoScn.Battle;
using AkoScn.Result;
using UnityEngine;

namespace AkoScn.MultiMode;

public class AkoMultiModeScData
{
	private AkoBattleRoomConfigData _roomConfig = new AkoBattleRoomConfigData();

	private List<AkoPlayerInfo> _playerList = new List<AkoPlayerInfo>();

	private List<AkoLobbyInfo> _playerLobbyInfoList = new List<AkoLobbyInfo>();

	private List<AkoRoleInfo> _playerRoleInfoList = new List<AkoRoleInfo>();

	private int _syncInitFrame;

	private int _allStandByFrame;

namespace AkoScn.MultiMode;

public class AkoMultiModeScData
{
	private AkoBattleRoomConfigData _roomConfig = new AkoBattleRoomConfigData();

	private List<AkoPlayerInfo> _playerList = new List<AkoPlayerInfo>();

	private List<AkoLobbyInfo> _playerLobbyInfoList = new List<AkoLobbyInfo>();

	private List<AkoRoleInfo> _playerRoleInfoList = new List<AkoRoleInfo>();

	private int _syncInitFrame;

	private int _allStandByFrame;

	private bool IsCoopModeEnabled()
	{
		string iniPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "server_config.ini");
		if (File.Exists(iniPath))
		{
			AkoIniParser iniParser = AkoIniParser.Load(iniPath);
			return iniParser.GetIntValue("AI", "CoopMode", 0) == 1;
		}
		return false;
	}

	public AkoBattleRoomConfigData roomConfig => _roomConfig;

	public List<AkoLobbyInfo> playerLobbyInfoList => _playerLobbyInfoList;

	public List<AkoRoleInfo> playerRoleInfoList => _playerRoleInfoList;

	public int syncInitFrame => _syncInitFrame;

	public int allStandByFrame => _allStandByFrame;

	public bool UpdateCheckPlayerListBySession(IAkoNetworkSessionStatus sessionStatus)
	{
		bool flag = false;
		IAkoNetworkStationInfo[] stationInfoList = sessionStatus.StationInfoList;
		if (_playerList.Count != stationInfoList.Length)
		{
			flag = true;
		}
		else
		{
			for (int i = 0; i < _playerList.Count; i++)
			{
				if (_playerList[i].constantId != stationInfoList[i].ConstantId)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			List<AkoLobbyInfo> list = new List<AkoLobbyInfo>(_playerLobbyInfoList);
			_playerList.Clear();
			_playerLobbyInfoList.Clear();
			byte b = 1;
			IAkoNetworkStationInfo[] array = stationInfoList;
			foreach (IAkoNetworkStationInfo akoNetworkStationInfo in array)
			{
				AkoPlayerInfo akoPlayerInfo = new AkoPlayerInfo();
				akoPlayerInfo.uniquId = b;
				akoPlayerInfo.monsterFlag = 0;
				akoPlayerInfo.charaId = 1;
				akoPlayerInfo.costumeId = 0;
				akoPlayerInfo.constantId = akoNetworkStationInfo.ConstantId;
				akoPlayerInfo.npcFlag = 0;
				akoPlayerInfo.randNameIdx = 0;
				_playerList.Add(akoPlayerInfo);
				AkoLobbyInfo akoLobbyInfo = new AkoLobbyInfo(akoNetworkStationInfo.ConstantId);
				for (int k = 0; k < list.Count; k++)
				{
					if (list[k].constantId == akoLobbyInfo.constantId)
					{
						list[k].CopyTo(akoLobbyInfo);
					}
				}
				_playerLobbyInfoList.Add(akoLobbyInfo);
				b++;
			}
		}
		return flag;
	}

	public void CreatePlayerListByLobbyInfo(int playerNumMax, IAkoNetworkSessionStatus sessionStatus)
	{
		_playerList.Clear();
		IAkoNetworkStationInfo[] stationInfoList = sessionStatus.StationInfoList;
		bool coopMode = IsCoopModeEnabled();
		int num = (coopMode ? -1 : LotMonsId(stationInfoList.Length)); // -1 means no human monster
		List<int> list = AkoSingletonMonoBehaviour<AkoGM>.instance.npcNameManager.LotRandomNpcNameIdx(playerNumMax);
		byte b = 1;
		IAkoNetworkStationInfo[] array = stationInfoList;
		foreach (IAkoNetworkStationInfo akoNetworkStationInfo in array)
		{
			foreach (AkoLobbyInfo playerLobbyInfo in _playerLobbyInfoList)
			{
				if (akoNetworkStationInfo.ConstantId == playerLobbyInfo.constantId)
				{
					AkoPlayerInfo akoPlayerInfo = new AkoPlayerInfo();
					akoPlayerInfo.uniquId = b;
					akoPlayerInfo.monsterFlag = (byte)((!coopMode && num == b) ? 1u : 0u);
					akoPlayerInfo.charaId = playerLobbyInfo.charaId;
					akoPlayerInfo.costumeId = playerLobbyInfo.costumeId;
					akoPlayerInfo.constantId = playerLobbyInfo.constantId;
					akoPlayerInfo.npcFlag = 0;
					akoPlayerInfo.randNameIdx = (ushort)list[b - 1];
					_playerList.Add(akoPlayerInfo);
					b++;
				}
			}
		}
		bool monsterSpawned = false;
		for (int j = b; j <= playerNumMax; j++)
		{
			AkoPlayerInfo akoPlayerInfo2 = new AkoPlayerInfo();
			akoPlayerInfo2.uniquId = b;
			// In coop mode, spawn 1 monster AI
			akoPlayerInfo2.monsterFlag = (byte)((coopMode && !monsterSpawned) ? 1u : ((!coopMode && num == b) ? 1u : 0u));
			if (akoPlayerInfo2.monsterFlag == 1)
			{
				monsterSpawned = true;
			}
			int num2;
			bool flag;
			do
			{
				num2 = UnityEngine.Random.Range(0, 4) + 1;
				flag = true;
				foreach (AkoPlayerInfo player in _playerList)
				{
					if (num2 == player.charaId)
					{
						flag = false;
					}
				}
			}
			while (!flag);
			akoPlayerInfo2.charaId = num2;
			akoPlayerInfo2.costumeId = 0;
			akoPlayerInfo2.constantId = 0uL;
			akoPlayerInfo2.npcFlag = 1;
			akoPlayerInfo2.randNameIdx = (ushort)list[b - 1];
			_playerList.Add(akoPlayerInfo2);
			b++;
		}
		if (_roomConfig.mapId == 0)
		{
			_roomConfig.mapId = AkoSingletonMonoBehaviour<AkoGM>.instance.dataBase.GetRandomMapId();
		}
	}

	public void CreatePlayerListByResultParam(int playerNumMax, IAkoNetworkSessionStatus sessionStatus, AkoResultScParam param)
	{
		_playerList.Clear();
		IAkoNetworkStationInfo[] stationInfoList = sessionStatus.StationInfoList;
		bool coopMode = IsCoopModeEnabled();
		int num = (coopMode ? -1 : LotMonsId(stationInfoList.Length)); // -1 means no human monster
		List<int> list = AkoSingletonMonoBehaviour<AkoGM>.instance.npcNameManager.LotRandomNpcNameIdx(playerNumMax);
		byte b = 1;
		IAkoNetworkStationInfo[] array = stationInfoList;
		foreach (IAkoNetworkStationInfo akoNetworkStationInfo in array)
		{
			foreach (AkoResultPlayerData resultPlayerData in param.resultPlayerDataList)
			{
				if (akoNetworkStationInfo.ConstantId == resultPlayerData.constantId)
				{
					AkoPlayerInfo akoPlayerInfo = new AkoPlayerInfo();
					akoPlayerInfo.uniquId = b;
					akoPlayerInfo.monsterFlag = (byte)((!coopMode && num == b) ? 1u : 0u);
					akoPlayerInfo.charaId = resultPlayerData.charaId;
					akoPlayerInfo.costumeId = resultPlayerData.costumeId;
					akoPlayerInfo.constantId = resultPlayerData.constantId;
					akoPlayerInfo.npcFlag = 0;
					akoPlayerInfo.randNameIdx = (ushort)list[b - 1];
					_playerList.Add(akoPlayerInfo);
					b++;
				}
			}
		}
		bool monsterSpawned = false;
		for (int j = b; j <= playerNumMax; j++)
		{
			AkoPlayerInfo akoPlayerInfo2 = new AkoPlayerInfo();
			akoPlayerInfo2.uniquId = b;
			// In coop mode, spawn 1 monster AI
			akoPlayerInfo2.monsterFlag = (byte)((coopMode && !monsterSpawned) ? 1u : ((!coopMode && num == b) ? 1u : 0u));
			if (akoPlayerInfo2.monsterFlag == 1)
			{
				monsterSpawned = true;
			}
			int num2;
			bool flag;
			do
			{
				num2 = UnityEngine.Random.Range(0, 4) + 1;
				flag = true;
				foreach (AkoPlayerInfo player in _playerList)
				{
					if (num2 == player.charaId)
					{
						flag = false;
					}
				}
			}
			while (!flag);
			int num2;
			bool flag;
			do
			{
				num2 = UnityEngine.Random.Range(0, 4) + 1;
				flag = true;
				foreach (AkoPlayerInfo player in _playerList)
				{
					if (num2 == player.charaId)
					{
						flag = false;
					}
				}
			}
			while (!flag);
			akoPlayerInfo2.charaId = num2;
			akoPlayerInfo2.costumeId = 0;
			akoPlayerInfo2.constantId = 0uL;
			akoPlayerInfo2.npcFlag = 1;
			akoPlayerInfo2.randNameIdx = (ushort)list[b - 1];
			_playerList.Add(akoPlayerInfo2);
			b++;
		}
		param.roomConfig.CopyTo(_roomConfig);
	}

	private int LotMonsId(int stationNum)
	{
		return 1 + UnityEngine.Random.Range(0, 40) / 10;
	}

	public void UpdateRoleInfoConnected(IAkoNetworkSessionStatus sessionStatus)
	{
		IAkoNetworkStationInfo[] stationInfoList = sessionStatus.StationInfoList;
		for (int i = 0; i < _playerRoleInfoList.Count; i++)
		{
			byte connectedFlag = 0;
			IAkoNetworkStationInfo[] array = stationInfoList;
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].ConstantId == _playerRoleInfoList[i].constantId)
				{
					connectedFlag = 1;
					break;
				}
			}
			_playerRoleInfoList[i].connectedFlag = connectedFlag;
		}
	}

	public void LoadComplete(ulong constantId, byte loadedFlag)
	{
		for (int i = 0; i < _playerRoleInfoList.Count; i++)
		{
			if (constantId == _playerRoleInfoList[i].constantId)
			{
				_playerRoleInfoList[i].loadedFlag = loadedFlag;
			}
		}
	}

	public bool IsAllLoaded()
	{
		bool result = true;
		foreach (AkoRoleInfo playerRoleInfo in _playerRoleInfoList)
		{
			if (playerRoleInfo.connectedFlag != 0 && playerRoleInfo.loadedFlag == 0)
			{
				result = false;
			}
		}
		return result;
	}

	public void UpdateMapId(int mapId)
	{
		_roomConfig.mapId = mapId;
	}

	public void UpdatePlayChara(ulong constantId, int charaId, int costumeId)
	{
		for (int i = 0; i < _playerLobbyInfoList.Count; i++)
		{
			if (constantId == _playerLobbyInfoList[i].constantId)
			{
				_playerLobbyInfoList[i].ChangeCharaId(charaId, costumeId);
			}
		}
	}

	public void UpdateStandBy(ulong constantId, byte standByFlag)
	{
		for (int i = 0; i < _playerLobbyInfoList.Count; i++)
		{
			if (constantId == _playerLobbyInfoList[i].constantId)
			{
				_playerLobbyInfoList[i].ChangeStandByFlag(standByFlag);
			}
		}
	}

	public void UpdateWinRateShow(ulong constantId, byte winRateShowFlag, double winRateValue)
	{
		for (int i = 0; i < _playerLobbyInfoList.Count; i++)
		{
			if (constantId == _playerLobbyInfoList[i].constantId)
			{
				_playerLobbyInfoList[i].ChangeWinRateShow(winRateShowFlag, winRateValue);
			}
		}
	}

	public void UpdateCloseWait(ulong constantId, byte closeWaitFlag)
	{
		for (int i = 0; i < _playerLobbyInfoList.Count; i++)
		{
			if (constantId == _playerLobbyInfoList[i].constantId)
			{
				_playerLobbyInfoList[i].ChangeCloseWaitFlag(closeWaitFlag);
			}
		}
	}

	public bool IsAllStandBy()
	{
		bool result = true;
		foreach (AkoLobbyInfo playerLobbyInfo in _playerLobbyInfoList)
		{
			if (playerLobbyInfo.standByFlag == 0)
			{
				result = false;
			}
		}
		return result;
	}

	public bool IsStandByCancelable()
	{
		return !IsAllStandBy();
	}

	public bool IsAllCloseWait()
	{
		bool result = true;
		foreach (AkoLobbyInfo playerLobbyInfo in _playerLobbyInfoList)
		{
			if (playerLobbyInfo.closeWaitFlag == 0)
			{
				result = false;
			}
		}
		return result;
	}

	public byte[] LobbyInfoSerialize_v2(ulong constantId, bool isHost, bool isAllData, byte isSyncInit = 0, int initFrame = 0, int allStandByFrame = 0)
	{
		byte[] array = null;
		using MemoryStream memoryStream = new MemoryStream();
		byte value = (byte)(isHost ? 1u : 0u);
		memoryStream.WriteByte(value);
		memoryStream.WriteByte(isSyncInit);
		if (isSyncInit == 1)
		{
			_syncInitFrame = initFrame;
			memoryStream.Write(BitConverter.GetBytes(_syncInitFrame), 0, 4);
		}
		else
		{
			if (_allStandByFrame < allStandByFrame)
			{
				_allStandByFrame = allStandByFrame;
			}
			memoryStream.Write(BitConverter.GetBytes(_allStandByFrame), 0, 4);
		}
		int count = _playerLobbyInfoList.Count;
		byte value2 = (byte)((!isAllData) ? 1u : ((uint)count));
		memoryStream.WriteByte(value2);
		for (int i = 0; i < count; i++)
		{
			if (isAllData || constantId == _playerLobbyInfoList[i].constantId)
			{
				_playerLobbyInfoList[i].SerializeTo(memoryStream);
			}
		}
		if (isHost)
		{
			_roomConfig.SerializeTo(memoryStream);
		}
		return memoryStream.ToArray();
	}

	public byte[] LobbyInfoSerialize(ulong constantId, bool isHost, bool isAllData, int frame = 0, byte isSyncInit = 0)
	{
		byte[] array = null;
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.WriteByte(isSyncInit);
		int value = (isHost ? 1 : 0);
		memoryStream.Write(BitConverter.GetBytes(value), 0, 4);
		if (isSyncInit == 1)
		{
			_syncInitFrame = frame;
			memoryStream.Write(BitConverter.GetBytes(_syncInitFrame), 0, 4);
		}
		else
		{
			int count = _playerLobbyInfoList.Count;
			memoryStream.Write(BitConverter.GetBytes(count), 0, 4);
			int value2 = ((!isAllData) ? 1 : count);
			memoryStream.Write(BitConverter.GetBytes(value2), 0, 4);
			for (int i = 0; i < count; i++)
			{
				if (isAllData || constantId == _playerLobbyInfoList[i].constantId)
				{
					_playerLobbyInfoList[i].SerializeTo(memoryStream);
				}
			}
			_allStandByFrame = frame;
			memoryStream.Write(BitConverter.GetBytes(_allStandByFrame), 0, 4);
			if (isHost)
			{
				_roomConfig.SerializeTo(memoryStream);
			}
		}
		return memoryStream.ToArray();
	}

	public byte[] LoadedSerialize(ulong constantId)
	{
		byte[] array = null;
		using MemoryStream memoryStream = new MemoryStream();
		int value = 1;
		memoryStream.Write(BitConverter.GetBytes(value), 0, 4);
		for (int i = 0; i < _playerRoleInfoList.Count; i++)
		{
			if (constantId == _playerRoleInfoList[i].constantId)
			{
				_playerRoleInfoList[i].SerializeTo(memoryStream);
			}
		}
		return memoryStream.ToArray();
	}

	public byte[] Serialize(ulong constantId)
	{
		byte[] array = null;
		using MemoryStream memoryStream = new MemoryStream();
		int count = _playerList.Count;
		memoryStream.Write(BitConverter.GetBytes(count), 0, 4);
		for (int i = 0; i < count; i++)
		{
			_playerList[i].SerializeTo(memoryStream);
		}
		_roomConfig.SerializeTo(memoryStream);
		return memoryStream.ToArray();
	}

	public bool LobbyInfoDeserialize_v2(byte[] data, int startIdx = 0)
	{
		if (_playerList.Count == 0 || _playerLobbyInfoList.Count == 0)
		{
			UpdateCheckPlayerListBySession(AkoNetworkManager.GetSessionStatus());
		}
		byte num = data[startIdx];
		startIdx++;
		bool flag = ((num != 0) ? true : false);
		byte b = data[startIdx];
		startIdx++;
		if (b == 1)
		{
			_syncInitFrame = BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
		}
		else
		{
			int num2 = BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
			if (_allStandByFrame < num2)
			{
				_allStandByFrame = num2;
			}
		}
		byte b2 = data[startIdx];
		startIdx++;
		for (int i = 0; i < b2; i++)
		{
			AkoLobbyInfo akoLobbyInfo = new AkoLobbyInfo(0uL);
			startIdx = akoLobbyInfo.Deserialize(data, startIdx);
			for (int j = 0; j < _playerLobbyInfoList.Count; j++)
			{
				if (akoLobbyInfo.constantId == _playerLobbyInfoList[j].constantId)
				{
					akoLobbyInfo.CopyTo(_playerLobbyInfoList[j]);
				}
			}
		}
		if (flag)
		{
			startIdx = _roomConfig.Deserialize(data, startIdx);
		}
		if (b != 0)
		{
			return true;
		}
		return false;
	}

	public bool LobbyInfoDeserialize(byte[] data, int startIdx = 0)
	{
		if (_playerList.Count == 0 || _playerLobbyInfoList.Count == 0)
		{
			UpdateCheckPlayerListBySession(AkoNetworkManager.GetSessionStatus());
		}
		byte b = data[startIdx];
		startIdx++;
		int num = BitConverter.ToInt32(data, startIdx);
		startIdx += 4;
		bool flag = ((num != 0) ? true : false);
		if (b == 1)
		{
			_syncInitFrame = BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
		}
		else
		{
			BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
			int num2 = BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
			for (int i = 0; i < num2; i++)
			{
				AkoLobbyInfo akoLobbyInfo = new AkoLobbyInfo(0uL);
				startIdx = akoLobbyInfo.Deserialize(data, startIdx);
				for (int j = 0; j < _playerLobbyInfoList.Count; j++)
				{
					if (akoLobbyInfo.constantId == _playerLobbyInfoList[j].constantId)
					{
						akoLobbyInfo.CopyTo(_playerLobbyInfoList[j]);
					}
				}
			}
			_allStandByFrame = BitConverter.ToInt32(data, startIdx);
			startIdx += 4;
			if (flag)
			{
				startIdx = _roomConfig.Deserialize(data, startIdx);
			}
		}
		if (b != 0)
		{
			return true;
		}
		return false;
	}

	public int LoadedDeserialize(byte[] data, int startIdx = 0)
	{
		int num = BitConverter.ToInt32(data, startIdx);
		startIdx += 4;
		for (int i = 0; i < num; i++)
		{
			AkoRoleInfo akoRoleInfo = new AkoRoleInfo(0uL, 0);
			startIdx = akoRoleInfo.Deserialize(data, startIdx);
			for (int j = 0; j < _playerRoleInfoList.Count; j++)
			{
				if (akoRoleInfo.constantId == _playerRoleInfoList[j].constantId)
				{
					_playerRoleInfoList[j].loadedFlag = akoRoleInfo.loadedFlag;
				}
			}
		}
		return startIdx;
	}

	public int Deserialize(byte[] data, int startIdx = 0)
	{
		_playerList.Clear();
		int num = BitConverter.ToInt32(data, startIdx);
		startIdx += 4;
		for (int i = 0; i < num; i++)
		{
			AkoPlayerInfo akoPlayerInfo = new AkoPlayerInfo();
			startIdx = akoPlayerInfo.Deserialize(data, startIdx);
			_playerList.Add(akoPlayerInfo);
		}
		startIdx = _roomConfig.Deserialize(data, startIdx);
		return startIdx;
	}

	public void SetRoleScParam(AkoMultiModeScParam param)
	{
		_playerList.Clear();
		_playerRoleInfoList.Clear();
		foreach (AkoPlayerInfo player in param.playerList)
		{
			AkoPlayerInfo akoPlayerInfo = new AkoPlayerInfo();
			player.CopyTo(akoPlayerInfo);
			_playerList.Add(akoPlayerInfo);
			AkoRoleInfo item = new AkoRoleInfo(player.constantId, (byte)((player.npcFlag != 0) ? 1 : 0));
			_playerRoleInfoList.Add(item);
		}
		param.roomConfig.CopyTo(_roomConfig);
	}

	public AkoMultiModeScParam ConvertToRoleScParam(AkoMultiModeScParam param)
	{
		AkoMultiModeScParam akoMultiModeScParam = new AkoMultiModeScParam();
		akoMultiModeScParam.runMode = param.runMode;
		for (int i = 0; i < _playerList.Count; i++)
		{
			akoMultiModeScParam.playerList.Add(_playerList[i]);
		}
		akoMultiModeScParam.roomConfig = _roomConfig;
		return akoMultiModeScParam;
	}

	public AkoMultiModeScParam ConvertResultToRoleScParam(AkoResultScParam param)
	{
		AkoMultiModeScParam akoMultiModeScParam = new AkoMultiModeScParam();
		akoMultiModeScParam.runMode = param.runMode;
		for (int i = 0; i < _playerList.Count; i++)
		{
			akoMultiModeScParam.playerList.Add(_playerList[i]);
		}
		akoMultiModeScParam.roomConfig = _roomConfig;
		return akoMultiModeScParam;
	}

	public AkoBattleScParam ConvertToBattleScParam(AkoMultiModeScParam param)
	{
		AkoBattleScParam akoBattleScParam = new AkoBattleScParam();
		akoBattleScParam.runMode = param.runMode;
		for (int i = 0; i < _playerList.Count; i++)
		{
			AkoPlayerInfo akoPlayerInfo = _playerList[i];
			AkoBattlePlayerData akoBattlePlayerData = new AkoBattlePlayerData();
			akoBattlePlayerData.playerId = akoPlayerInfo.uniquId;
			akoBattlePlayerData.charaId = akoPlayerInfo.charaId;
			akoBattlePlayerData.costumeId = akoPlayerInfo.costumeId;
			akoBattlePlayerData.constantId = akoPlayerInfo.constantId;
			if (akoPlayerInfo.npcFlag == 0)
			{
				akoBattlePlayerData.name = AkoNetworkManager.GetSessionPlayerName(akoPlayerInfo.constantId);
				akoBattlePlayerData.type = ((akoPlayerInfo.constantId == AkoNetworkManager.GetSelfStationConstantId()) ? AkoBattlePlayerType.Self : AkoBattlePlayerType.Other);
			}
			else
			{
				akoBattlePlayerData.name = AkoBattlePlayerData.GetDefaultName(akoPlayerInfo.randNameIdx);
				akoBattlePlayerData.type = AkoBattlePlayerType.Npc;
			}
			akoBattlePlayerData.bStartMonster = akoPlayerInfo.monsterFlag == 1;
			akoBattleScParam.playerList.Add(akoBattlePlayerData);
		}
		akoBattleScParam.roomConfig = _roomConfig;
		return akoBattleScParam;
	}
}
