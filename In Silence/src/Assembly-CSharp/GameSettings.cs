using System;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings
{
	public static bool STEAM_ACTIVATED;

	public static bool DEBUG_ACTIVATED;

	public static string GAME_VERSION;

	public static string URL_STEAM;

	public static string URL_FACEBOOK;

	public static string URL_TWITTER;

	public static string URL_DISCORD;

	public static bool IsTestButtonsOn;

	public static float LOADING_FADETIMER;

	public static bool isOffline;

	public static string NICKNAME;

	public static int AudibleDistance;

	public static int ConversationalDistance;

	public static float AudioFadeIntensityByDistance;

	public static int REMATCHING_COUNTER;

	public static float LastPosSaveTimer;

	public static float SignalTimer;

	public static float RepairingTimer;

	public static float EnteringTimer;

	public static float EscapingTimer;

	public static float ExitingTimer;

	public static float FlashShotTimer;

	public static float ExplodeFlashTimer;

	public static float FlashbangUIAnimatingTimer;

	public static float EventLongTimer;

	public static float EventShortTimer;

	public static float ActivableTimers;

	public static float DestroyTimer;

	public static float RadioTVSignalTimer;

	public static float MuzzleTimer;

	public static float WorldChatTimer;

	public static float StoneUITimer;

	public static float StoneAddTimer;

	public static float CatchTimer;

	public static float SurvivorEndingTimer;

	public static float CreatureEndingTimer;

	public static int CreatureDeathEndingTimer;

	public static float CreatureKillTimer;

	public static float AISprintTime;

	public static float RushTimer;

	public static float PlacementWaitTimer;

	public static float TruckSurvivorCheckTimer;

	public static float SpeechSignalWaitTimer;

	public static float RatSqueakTimer;

	public static float MIN_OWL_TIMER;

	public static float MAX_OWL_TIMER;

	public static float FlashCreatureStunTimer;

	public static float BearTrapStunTimer;

	public static KeyCode Key_TakeUseOpenEscape;

	public static KeyCode Key_DropItem;

	public static KeyCode Key_ExitCar;

	public static KeyCode Key_Flashlight;

	public static KeyCode Key_FlashBomb;

	public static KeyCode Key_TurnOnOff;

	public static KeyCode Key_Destroy;

	public static KeyCode Key_StoneThrow;

	public static KeyCode Key_Chat;

	public static KeyCode Key_RotatePlacer;

	public static KeyCode Key_PlaceCancel;

	public static int FlashbangShotAmount;

	public static float FlashbangRadius;

	public static float FlashbangDistance;

	public static float FlashbangEnergyNeed;

	public static float FlashbangMaxEnergy;

	public static float JackInBoxRange;

	public static float RushDistanceLimit;

	public static float DropRadius;

	public static float CrowFlockDisableRate;

	public static float BatteryenergyAmount;

	public static int MaxStoneCount;

	public static int CatchRate_Default;

	public static int CatchRate_Negative_Camuflage;

	public static int CatchRate_Negative_Crouch;

	public static int CatchRate_Positive_Flashlight;

	public static int CatchRate_Positive_Moving;

	public static int CatchRate_Positive_Standing;

	public static float AIStoneCheckMeter;

	public static float EnrageKillTimer;

	public static float StandartKillTimer;

	public static float low_far_dist;

	public static float medium_far_dist;

	public static float high_far_dist;

	public static float ultra_far_dist;

	public static string PhotonServerAddress;

	public static int PhotonServerPort;

	public static int PhotonServerVersion;

	public static void SetKeys()
	{
		EncryptedPlayerPrefs.keys = new string[5];
		EncryptedPlayerPrefs.keys[0] = "25WruJrb";
		EncryptedPlayerPrefs.keys[1] = "SD9DuHHz";
		EncryptedPlayerPrefs.keys[2] = "frX5rbS2";
		EncryptedPlayerPrefs.keys[3] = "tHaf2tpt";
		EncryptedPlayerPrefs.keys[4] = "jaw5zDAj";
	}

	public static List<Resolution> GetResolutions()
	{
		Resolution[] resolutions = Screen.resolutions;
		HashSet<Tuple<int, int>> hashSet = new HashSet<Tuple<int, int>>();
		Dictionary<Tuple<int, int>, int> dictionary = new Dictionary<Tuple<int, int>, int>();
		for (int i = 0; i < resolutions.GetLength(0); i++)
		{
			Tuple<int, int> tuple = new Tuple<int, int>(resolutions[i].width, resolutions[i].height);
			hashSet.Add(tuple);
			if (!dictionary.ContainsKey(tuple))
			{
				dictionary.Add(tuple, resolutions[i].refreshRate);
			}
			else
			{
				dictionary[tuple] = resolutions[i].refreshRate;
			}
		}
		List<Resolution> list = new List<Resolution>(hashSet.Count);
		foreach (Tuple<int, int> item2 in hashSet)
		{
			Resolution item = new Resolution
			{
				width = item2.Item1,
				height = item2.Item2
			};
			if (dictionary.TryGetValue(item2, out var value))
			{
				item.refreshRate = value;
			}
			list.Add(item);
		}
		return list;
	}

	static GameSettings()
	{
		STEAM_ACTIVATED = true;
		DEBUG_ACTIVATED = false;
		GAME_VERSION = "v1.01";
		URL_STEAM = "https://store.steampowered.com/app/1361000/In_Silence/";
		URL_FACEBOOK = "https://www.facebook.com/insilencegame";
		URL_TWITTER = "https://twitter.com/InSilence_Game";
		URL_DISCORD = "https://discord.gg/VHhyrnN";
		IsTestButtonsOn = false;
		LOADING_FADETIMER = 1f;
		isOffline = false;
		NICKNAME = "";
		AudibleDistance = 42;
		ConversationalDistance = 2;
		AudioFadeIntensityByDistance = 1f;
		REMATCHING_COUNTER = 15;
		LastPosSaveTimer = 2f;
		SignalTimer = 4f;
		RepairingTimer = 3f;
		EnteringTimer = 2f;
		EscapingTimer = 2f;
		ExitingTimer = 2f;
		FlashShotTimer = 2f;
		ExplodeFlashTimer = 1.6f;
		FlashbangUIAnimatingTimer = 1f;
		EventLongTimer = 3.5f;
		EventShortTimer = 2f;
		ActivableTimers = 1f;
		DestroyTimer = 1f;
		RadioTVSignalTimer = 2f;
		MuzzleTimer = 0.2f;
		WorldChatTimer = 5f;
		StoneUITimer = 2f;
		StoneAddTimer = 45f;
		CatchTimer = 0.35f;
		SurvivorEndingTimer = 5f;
		CreatureEndingTimer = 20f;
		CreatureDeathEndingTimer = 15;
		CreatureKillTimer = 2f;
		AISprintTime = 2f;
		RushTimer = 5f;
		PlacementWaitTimer = 0.2f;
		TruckSurvivorCheckTimer = 5f;
		SpeechSignalWaitTimer = 2f;
		RatSqueakTimer = 10f;
		MIN_OWL_TIMER = 15f;
		MAX_OWL_TIMER = 40f;
		FlashCreatureStunTimer = 3f;
		BearTrapStunTimer = 3f;
		Key_TakeUseOpenEscape = KeyCode.E;
		Key_DropItem = KeyCode.G;
		Key_ExitCar = KeyCode.Q;
		Key_Flashlight = KeyCode.F;
		Key_FlashBomb = KeyCode.B;
		Key_TurnOnOff = KeyCode.Q;
		Key_Destroy = KeyCode.F;
		Key_StoneThrow = KeyCode.T;
		Key_Chat = KeyCode.Return;
		Key_RotatePlacer = KeyCode.Q;
		Key_PlaceCancel = KeyCode.E;
		FlashbangShotAmount = 4;
		FlashbangRadius = 15f;
		FlashbangDistance = 100f;
		FlashbangEnergyNeed = 60f;
		FlashbangMaxEnergy = 100f;
		JackInBoxRange = 2f;
		RushDistanceLimit = 20f;
		DropRadius = 2f;
		CrowFlockDisableRate = 50f;
		BatteryenergyAmount = 40f;
		MaxStoneCount = 2;
		CatchRate_Default = 30;
		CatchRate_Negative_Camuflage = 10;
		CatchRate_Negative_Crouch = 10;
		CatchRate_Positive_Flashlight = 20;
		CatchRate_Positive_Moving = 100;
		CatchRate_Positive_Standing = 20;
		AIStoneCheckMeter = 50f;
		EnrageKillTimer = 4.5f;
		StandartKillTimer = 6.5f;
		low_far_dist = 40f;
		medium_far_dist = 80f;
		high_far_dist = 120f;
		ultra_far_dist = 150f;
		PhotonServerAddress = "";
		PhotonServerPort = 5055;
		PhotonServerVersion = 4;
	}
}
