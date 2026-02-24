using System.Collections.Generic;
using System.IO;
using AkoCmn;
using AkoCmn.Utility;
using AkoScn.Battle.AI;
using AkoScn.Battle.Data;
using UnityEngine;

namespace AkoScn.Battle.Internal;

public class AkoBattleCharaPersonalManager
{
	private List<AkoBattleCharaPersonal> _charaPersonalList;

	private List<AkoBattleCharaPersonal> _enemyCharaPersonalList;

	public List<AkoBattleCharaPersonal> charaPersonalList => _charaPersonalList;

	public List<AkoBattleCharaPersonal> enemyCpList => _enemyCharaPersonalList;

	public List<AkoBattleCharaPersonal> allCpList
	{
		get
		{
			List<AkoBattleCharaPersonal> list = new List<AkoBattleCharaPersonal>(_charaPersonalList);
			list.AddRange(_enemyCharaPersonalList);
			return list;
		}
	}

	public AkoBattleCharaPersonalManager(AkoBattleScView scView, AkoBattleRoomConfigData roomConfig, List<AkoBattlePlayerData> playerList, List<AkoBattlePlayerData> enemyPlayerList, bool bInitRole)
	{
		LoadAISettings(out var overrideDifficulty, out var monsterBotDifficulty, out var survivorBotDifficulty);
		AkoBattleScParam obj = AkoSingletonMonoBehaviour<AkoGM>.instance.sceneManager.scParam as AkoBattleScParam;
		bool isSinglePlayer = obj != null && obj.runMode == AkoBattleRunMode.Solo;
		int entryPointNum = scView.GetEntryPointNum();
		_charaPersonalList = new List<AkoBattleCharaPersonal>();
		for (int i = 0; i < playerList.Count; i++)
		{
			AkoBattlePlayerData akoBattlePlayerData = playerList[i];
			if (akoBattlePlayerData.charaId != 0)
			{
				if (entryPointNum <= _charaPersonalList.Count)
				{
					break;
				}
				AkoBattleCharaRole akoBattleCharaRole = AkoBattleCharaRole.Human;
				if (bInitRole && akoBattlePlayerData.bStartMonster)
				{
					akoBattleCharaRole = AkoBattleCharaRole.Monster;
				}
				AkoBattleAIType aILevelForRole = GetAILevelForRole(roomConfig.aiLevel, akoBattleCharaRole, overrideDifficulty, monsterBotDifficulty, survivorBotDifficulty, isSinglePlayer);
				AkoBattleCharaPersonal item = new AkoBattleCharaPersonal(akoBattlePlayerData, aILevelForRole, akoBattleCharaRole);
				_charaPersonalList.Add(item);
			}
		}
		AkoBattleAIType defaultLevel = (scView.HasTutorialArea() ? AkoBattleAIType.Tutorial : roomConfig.aiLevel);
		int enemyPointNum = scView.GetEnemyPointNum();
		_enemyCharaPersonalList = new List<AkoBattleCharaPersonal>();
		for (int j = 0; j < enemyPlayerList.Count; j++)
		{
			AkoBattlePlayerData akoBattlePlayerData2 = enemyPlayerList[j];
			if (akoBattlePlayerData2.charaId != 0)
			{
				if (enemyPointNum <= _enemyCharaPersonalList.Count)
				{
					break;
				}
				AkoBattleAIType aILevelForRole2 = GetAILevelForRole(defaultLevel, AkoBattleCharaRole.Monster, overrideDifficulty, monsterBotDifficulty, survivorBotDifficulty, isSinglePlayer);
				AkoBattleCharaPersonal item2 = new AkoBattleCharaPersonal(akoBattlePlayerData2, aILevelForRole2, AkoBattleCharaRole.Monster, bEnemy: true);
				_enemyCharaPersonalList.Add(item2);
			}
		}
	}

	public AkoBattleCharaPersonal GetSelfCharaPersonal()
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			if (_charaPersonalList[i].playerType == AkoBattlePlayerType.Self)
			{
				return _charaPersonalList[i];
			}
		}
		return null;
	}

	public AkoBattleCharaPersonal GetCharaPersonal(AkoBattlePlayerData player)
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			if (_charaPersonalList[i].playerData == player)
			{
				return _charaPersonalList[i];
			}
		}
		return null;
	}

	public AkoBattleCharaPersonal GetCharaPersonal(int playerId)
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			if (_charaPersonalList[i].playerData.playerId == playerId)
			{
				return _charaPersonalList[i];
			}
		}
		return null;
	}

	public AkoBattleCharaPersonal GetStartMonsterCharaPersonal()
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			if (_charaPersonalList[i].playerData.bStartMonster)
			{
				return _charaPersonalList[i];
			}
		}
		return null;
	}

	public AkoBattlePersonalBase GetPersonalBase(int uniqueId)
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			if (_charaPersonalList[i].uniqueId == uniqueId)
			{
				return _charaPersonalList[i];
			}
		}
		for (int j = 0; j < _enemyCharaPersonalList.Count; j++)
		{
			if (_enemyCharaPersonalList[j].uniqueId == uniqueId)
			{
				return _enemyCharaPersonalList[j];
			}
		}
		return null;
	}

	public void StartSequenceAllChara()
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			_charaPersonalList[i].SetGameStart();
		}
		for (int j = 0; j < _enemyCharaPersonalList.Count; j++)
		{
			_enemyCharaPersonalList[j].SetGameStart();
		}
	}

	public bool IsAllHumanGoaledOrTrapped()
	{
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			AkoBattleCharaPersonal akoBattleCharaPersonal = _charaPersonalList[i];
			if (akoBattleCharaPersonal.goalRank <= 0 && akoBattleCharaPersonal.role == AkoBattleCharaRole.Human && !akoBattleCharaPersonal.restarState.IsTrapped())
			{
				return false;
			}
		}
		return true;
	}

	public bool IsAllHumanGoaled()
	{
		bool flag = false;
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			AkoBattleCharaPersonal akoBattleCharaPersonal = _charaPersonalList[i];
			if (akoBattleCharaPersonal.goalRank <= 0 && akoBattleCharaPersonal.role == AkoBattleCharaRole.Human && !akoBattleCharaPersonal.restarState.IsTrapped())
			{
				flag = true;
				break;
			}
		}
		return !flag;
	}

	public bool IsAllUserHumanGoaled()
	{
		bool flag = false;
		bool flag2 = true;
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			AkoBattleCharaPersonal akoBattleCharaPersonal = _charaPersonalList[i];
			if (akoBattleCharaPersonal.playerData.type != AkoBattlePlayerType.Npc && akoBattleCharaPersonal.role == AkoBattleCharaRole.Human)
			{
				flag = true;
				if (akoBattleCharaPersonal.goalRank <= 0)
				{
					flag2 = false;
					break;
				}
			}
		}
		return flag && flag2;
	}

	public bool IsAllUserCharaGoaled()
	{
		bool flag = false;
		bool flag2 = true;
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			AkoBattleCharaPersonal akoBattleCharaPersonal = _charaPersonalList[i];
			if (akoBattleCharaPersonal.playerData.type != AkoBattlePlayerType.Npc)
			{
				flag = true;
				if (akoBattleCharaPersonal.goalRank <= 0)
				{
					flag2 = false;
					break;
				}
			}
		}
		return flag && flag2;
	}

	public bool IsAllUserCharaMonster()
	{
		bool flag = false;
		bool flag2 = true;
		for (int i = 0; i < _charaPersonalList.Count; i++)
		{
			AkoBattleCharaPersonal akoBattleCharaPersonal = _charaPersonalList[i];
			if (akoBattleCharaPersonal.goalRank <= 0 && akoBattleCharaPersonal.playerData.type != AkoBattlePlayerType.Npc)
			{
				flag = true;
				if (akoBattleCharaPersonal.role == AkoBattleCharaRole.Human)
				{
					flag2 = false;
					break;
				}
			}
		}
		return flag && flag2;
	}

	private AkoBattleAIType GetAILevelForRole(AkoBattleAIType defaultLevel, AkoBattleCharaRole role, int overrideDifficulty, int monsterBotDifficulty, int survivorBotDifficulty, bool isSinglePlayer)
	{
		bool flag = false;
		if (overrideDifficulty == 1 && !isSinglePlayer)
		{
			flag = true;
		}
		else if (overrideDifficulty == 2 && isSinglePlayer)
		{
			flag = true;
		}
		else if (overrideDifficulty == 3)
		{
			flag = true;
		}
		if (!flag)
		{
			return defaultLevel;
		}
		if (role == AkoBattleCharaRole.Monster && monsterBotDifficulty >= 1 && monsterBotDifficulty <= 3)
		{
			return (AkoBattleAIType)monsterBotDifficulty;
		}
		if (role == AkoBattleCharaRole.Human && survivorBotDifficulty >= 1 && survivorBotDifficulty <= 3)
		{
			return (AkoBattleAIType)survivorBotDifficulty;
		}
		return defaultLevel;
	}

	private void LoadAISettings(out int overrideDifficulty, out int monsterBotDifficulty, out int survivorBotDifficulty)
	{
		overrideDifficulty = 0;
		monsterBotDifficulty = 0;
		survivorBotDifficulty = 0;
		string text = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
		if (File.Exists(text))
		{
			AkoIniParser akoIniParser = AkoIniParser.Load(text);
			overrideDifficulty = akoIniParser.GetIntValue("AI", "OverrideDifficulty");
			monsterBotDifficulty = akoIniParser.GetIntValue("AI", "MonsterBotDifficulty");
			survivorBotDifficulty = akoIniParser.GetIntValue("AI", "SurvivorBotDifficulty");
		}
	}
}
