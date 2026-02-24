using System;
using System.Collections.Generic;
using AkoCmn.DataBase;
using AkoScn.Battle.Internal;
using AkoScn.Result;

namespace AkoScn.Battle.Data;

public class AkoBattleGameStatus
{
	private AkoBattleCharaPersonalManager _cpManager;

	private AkoBattleObjectPersonalManager _opManager;

	private AkoBattleBreadDataBuffer _breadBuf;

	private AkoBattleStageRule _stageRule;

	private ConclusionMode _conclusionMode;

	private ConclusionFactor _conclusionFactor;

	private AkoBattleWinLoseState _winLoseState;

	private AkoBattleGameStartState _gameStartState;

	private List<AkoBattlePlayerData> _goalPlayerList;

	private bool _bGoalSelf;

	private int _missionCount;

	private int _baseCount;

	private int _oopartsCount;

	private int _totalClearMission;

	private int _totalRoleChangeCount;

	private int _totalRespawnCount;

	private AkoBattleKakeraGetStatus _kakeraGetStatus;

	private float _boostEndTimeHumanDetect;

	private Action _cbOnOopartsCountChange;

	private Action _cbOnGoalPlayerChange;

	private Action _cbOnConclusion;

	private Action _cbOnTotalRespawnCountChange;

	public int svDebugCounter;

	public ConclusionFactor conclusionFactor => _conclusionFactor;

	public bool bConclusion => _conclusionFactor != ConclusionFactor.None;

	public AkoBattleWinLoseState winLoseState => _winLoseState;

	public AkoBattleGameStartState gameStartState => _gameStartState;

	public List<AkoBattlePlayerData> goalPlayerList => _goalPlayerList;

	public bool bGoalSelf => _bGoalSelf;

	public int missionCount => _missionCount;

	public int oopartsCount => _oopartsCount + _baseCount;

	public int totalClearMission => _totalClearMission;

	public int totalRoleChangeCount => _totalRoleChangeCount;

	public int totalRespawnCount => _totalRespawnCount;

	public AkoBattleKakeraGetStatus kakeraGetStatus => _kakeraGetStatus;

	public List<AkoKakeraData> kakeraList => _kakeraGetStatus.kakeraList;

	public AkoItemPointGettedFlags itemPointGettedFlags => _kakeraGetStatus.itemPointGettedFlags;

	public bool IsUnlocableOopartsCount()
	{
		return oopartsCount >= _missionCount;
	}

	public bool IsFinalOopartsCount()
	{
		return oopartsCount == _missionCount - 1;
	}

	public void IncSvDebugCounter()
	{
		svDebugCounter++;
	}

	public AkoBattleGameStatus(AkoBattleCharaPersonalManager cpManager, AkoBattleObjectPersonalManager opManager, AkoBattleBreadDataBuffer breadBuf, bool bStartRoom, AkoBattleStageRule stageRule, ConclusionMode conclusionMode = ConclusionMode.AllUserGoal)
	{
		_cpManager = cpManager;
		_opManager = opManager;
		_breadBuf = breadBuf;
		_stageRule = stageRule;
		_conclusionMode = conclusionMode;
		_conclusionFactor = ConclusionFactor.None;
		_winLoseState = AkoBattleWinLoseState.None;
		_gameStartState = new AkoBattleGameStartState(bStartRoom, cpManager.GetStartMonsterCharaPersonal());
		_goalPlayerList = new List<AkoBattlePlayerData>();
		_missionCount = stageRule.requireUnlockNum;
		_baseCount = 0;
		_oopartsCount = 0;
		_totalClearMission = 0;
		_totalRoleChangeCount = 0;
		_totalRespawnCount = 0;
		_kakeraGetStatus = new AkoBattleKakeraGetStatus();
		_boostEndTimeHumanDetect = 0f;
		_opManager.RegisterCbOopartsGetStateChange(CbOnOopartsGetStateChange);
		_stageRule.RegisterCbTreatLifeZeroChange(CheckConclusion);
		if (AkoBattleServiceManager.gameClockRt.IsGameTimeLimitEnable())
		{
			AkoBattleServiceManager.gameClockRt.AddGameTimeEvent(AkoBattleServiceManager.gameClockRt.GetGameTimeLimit(), CheckConclusion);
		}
	}

	~AkoBattleGameStatus()
	{
		_opManager.UnregisterCbOopartsGetStateChange(CbOnOopartsGetStateChange);
		_stageRule.UnregisterCbTreatLifeZeroChange(CheckConclusion);
		_cpManager = null;
		_opManager = null;
		_breadBuf = null;
		_stageRule = null;
		_goalPlayerList.Clear();
		_goalPlayerList = null;
		_kakeraGetStatus = null;
	}

	public void RegisterCbOopartsCountChange(Action cb)
	{
		_cbOnOopartsCountChange = (Action)Delegate.Combine(_cbOnOopartsCountChange, cb);
	}

	public void UnregisterCbOopartsCountChange(Action cb)
	{
		_cbOnOopartsCountChange = (Action)Delegate.Remove(_cbOnOopartsCountChange, cb);
	}

	public void RegisterCbGoalPlayerChange(Action cb)
	{
		_cbOnGoalPlayerChange = (Action)Delegate.Combine(_cbOnGoalPlayerChange, cb);
	}

	public void UnregisterCbGoalPlayerChange(Action cb)
	{
		_cbOnGoalPlayerChange = (Action)Delegate.Remove(_cbOnGoalPlayerChange, cb);
	}

	public void RegisterCbConclusion(Action cb)
	{
		_cbOnConclusion = (Action)Delegate.Combine(_cbOnConclusion, cb);
	}

	public void UnregisterCbConclusion(Action cb)
	{
		_cbOnConclusion = (Action)Delegate.Remove(_cbOnConclusion, cb);
	}

	public void RegisterCbTotalRespawnCountChange(Action cb)
	{
		_cbOnTotalRespawnCountChange = (Action)Delegate.Combine(_cbOnTotalRespawnCountChange, cb);
	}

	public void UnregisterCbTotalRespawnCountChange(Action cb)
	{
		_cbOnTotalRespawnCountChange = (Action)Delegate.Remove(_cbOnTotalRespawnCountChange, cb);
	}

	public void AddGoalPlayer(AkoBattlePlayerData player)
	{
		if (player != null && GetPlayerGoalIdx(player) < 0)
		{
			_goalPlayerList.Add(player);
			_cpManager.GetCharaPersonal(player).SetGoalRank(_goalPlayerList.Count);
			if (player.type == AkoBattlePlayerType.Self)
			{
				_bGoalSelf = true;
			}
			_cbOnGoalPlayerChange?.Invoke();
			CheckConclusion();
		}
	}

	public int GetPlayerGoalIdx(AkoBattlePlayerData player)
	{
		int result = -1;
		if (player != null)
		{
			for (int i = 0; i < _goalPlayerList.Count; i++)
			{
				if (_goalPlayerList[i].playerId == player.playerId)
				{
					result = i;
					break;
				}
			}
		}
		return result;
	}

	public void CheckConclusion()
	{
		if (_cpManager != null && _conclusionFactor == ConclusionFactor.None)
		{
			if (_conclusionFactor == ConclusionFactor.None && _cpManager.IsAllHumanGoaledOrTrapped())
			{
				_conclusionFactor = ConclusionFactor.Goal;
			}
			if (_conclusionFactor == ConclusionFactor.None && AkoBattleServiceManager.gameClockRt.IsPassedGameEndTime())
			{
				_conclusionFactor = ConclusionFactor.TimeUp;
			}
			if (_conclusionFactor == ConclusionFactor.None && _totalRespawnCount >= 2)
			{
				_conclusionFactor = ConclusionFactor.RespawnOver;
			}
			if (bConclusion)
			{
				AkoBattleResultDataBuilder akoBattleResultDataBuilder = new AkoBattleResultDataBuilder(_cpManager);
				_winLoseState = akoBattleResultDataBuilder.CalcWinLoseState(_cpManager.GetSelfCharaPersonal());
				_cbOnConclusion?.Invoke();
			}
		}
	}

	public void InitBaseUnlockCount()
	{
		int num = 0;
		List<AkoBattleMissionObjectPersonal> missionObjPersonalList = _opManager.missionObjPersonalList;
		int count = missionObjPersonalList.Count;
		for (int i = 0; i < count; i++)
		{
			if (missionObjPersonalList[i].bGoalTarget)
			{
				num++;
			}
		}
		if (num < _missionCount)
		{
			_baseCount = _missionCount - num;
		}
		else
		{
			_baseCount = 0;
		}
	}

	public int GetRemainMissionCount()
	{
		int num = _missionCount - oopartsCount;
		if (num < 0)
		{
			num = 0;
		}
		return num;
	}

	private void CalcOopartsCount()
	{
		int num = 0;
		int num2 = 0;
		List<AkoBattleMissionObjectPersonal> missionObjPersonalList = _opManager.missionObjPersonalList;
		int count = missionObjPersonalList.Count;
		for (int i = 0; i < count; i++)
		{
			AkoBattleMissionObjectPersonal akoBattleMissionObjectPersonal = missionObjPersonalList[i];
			if (akoBattleMissionObjectPersonal.bOopartsGetState)
			{
				num2++;
				if (akoBattleMissionObjectPersonal.bGoalTarget)
				{
					num++;
				}
			}
		}
		_oopartsCount = num;
		_totalClearMission = num2;
	}

	private void CbOnOopartsGetStateChange()
	{
		int num = _oopartsCount;
		CalcOopartsCount();
		if (_oopartsCount == num)
		{
			return;
		}
		_cbOnOopartsCountChange?.Invoke();
		if (IsUnlocableOopartsCount())
		{
			AkoBattleGoalGateObjectPersonal goalGate = _opManager.GetGoalGate1();
			if (goalGate != null && !goalGate.bUnlocked)
			{
				goalGate.SetUnlockState(on_off: true);
				AkoBattleServiceManager.btlTutorial.NoticeMainGoalGateUnlock();
				AkoBattleServiceManager.btlTutorial.CallTutorialEvent(AkoTutorialEvent.MainGateUnlocked);
			}
			UnlockGoalGate2();
		}
	}

	public void SetTimeEventBackGateUnlock(float gameTime)
	{
		AkoBattleGoalGateObjectPersonal goalGate = _opManager.GetGoalGate2();
		if (goalGate != null && !goalGate.bUnlocked)
		{
			goalGate.SetUnlockEventTime(gameTime);
			AkoBattleServiceManager.gameClockRt.AddGameTimeEvent(gameTime, UnlockGoalGate2);
		}
	}

	private void UnlockGoalGate2()
	{
		AkoBattleGoalGateObjectPersonal akoBattleGoalGateObjectPersonal = ((_opManager != null) ? _opManager.GetGoalGate2() : null);
		if (akoBattleGoalGateObjectPersonal != null && !akoBattleGoalGateObjectPersonal.bUnlocked)
		{
			akoBattleGoalGateObjectPersonal.SetUnlockState(on_off: true);
			AkoBattleServiceManager.btlTutorial.NoticeBackGoalGateUnlock();
			AkoBattleServiceManager.btlTutorial.CallTutorialEvent(AkoTutorialEvent.BackGateUnlocked);
		}
	}

	public void DebugUnlock()
	{
		List<AkoBattleMissionObjectPersonal> missionObjPersonalList = _opManager.missionObjPersonalList;
		for (int i = 0; i < missionObjPersonalList.Count; i++)
		{
			missionObjPersonalList[i].TakeOutOoparts();
		}
	}

	public void AddRoleChangeCount()
	{
		_totalRoleChangeCount++;
		_cbOnTotalRespawnCountChange?.Invoke();
	}

	public void SetTotalRespawnCount(int num)
	{
		if (_totalRespawnCount != num)
		{
			_totalRespawnCount = num;
			_cbOnTotalRespawnCountChange?.Invoke();
			CheckConclusion();
		}
	}

	public int GetUnacquiredKakeraNo(AkoKakeraID type)
	{
		return _kakeraGetStatus.GetUnacquiredKakeraNo(type);
	}

	public bool AddKakera(AkoKakeraID type, int no, int kakeraPointId)
	{
		return _kakeraGetStatus.AddKakera(type, no, kakeraPointId);
	}

	public int GetTotalKakeraCount()
	{
		return _kakeraGetStatus.GetTotalKakeraCount();
	}

	public int GetTotalKakeraMax()
	{
		return _kakeraGetStatus.GetTotalKakeraMax();
	}

	public int GetKakeraCount(AkoKakeraID type)
	{
		return _kakeraGetStatus.GetKakeraCount(type);
	}

	public bool IsKakeraCountMax(AkoKakeraID type)
	{
		return _kakeraGetStatus.IsKakeraCountMax(type);
	}

	public AkoKakeraData GetLastKakera()
	{
		return _kakeraGetStatus.GetLastKakera();
	}

	public void SetBoostHumanDetect(float endTime)
	{
		_boostEndTimeHumanDetect = endTime;
	}

	public bool IsBoostHumanDetect()
	{
		return !AkoBattleServiceManager.gameClockRt.IsPassedGameTime(_boostEndTimeHumanDetect);
	}
}
