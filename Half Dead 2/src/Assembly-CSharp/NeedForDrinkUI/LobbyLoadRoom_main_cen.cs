using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using MaterialUI;
using Photon;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace NeedForDrinkUI;

public class LobbyLoadRoom_main_cen : Photon.MonoBehaviour
{
	private RoomInfo[] rooms;

	[SerializeField]
	private Button randomRoomButton;

	private string userName;

	private int userCharacter;

	public string roomName;

	public string roomName1;

	private string roomNameToPassword;

	private bool joined;

	private bool _joinRoom;

	private PropsLibrary_un lib;

	private DialogProgress dProgress;

	private ListOfStringsLocalization_un dialogsLocalization;

	[SerializeField]
	private bool onGuiEnabled;

	private void Start()
	{
		lib = GlobalVariablesAbstractScript_un.scriptsObjectRootTr.GetComponentInChildren<PropsLibrary_un>();
		EventManagerGui_un.StartListening("CreateRoomYes", CreateRoomYes);
		EventManagerGui_un.StartListening("JoinRoom", JoinRoom);
		if (randomRoomButton != null)
		{
			randomRoomButton.onClick.AddListener(JoinRandRoom);
		}
		dialogsLocalization = GlobalVariablesAbstractScript_un.getSplittedList("Dialogs_un");
	}

	public void JoinRoom()
	{
		JoinRoom(passwordSystem: true, roomName);
	}

	public void JoinRoom(bool passwordSystem, string roomName)
	{
		rooms = PhotonNetwork.GetRoomList();
		if (_joinRoom)
		{
			return;
		}
		_joinRoom = true;
		roomNameToPassword = string.Empty;
		UniversalBeforeCheckingErrors();
		Debug.Log("JoinRoom " + roomName);
		RoomInfo checkingRoom = currentRoom(roomName);
		if (PhotonNetwork.connectionStateDetailed != ClientState.JoinedLobby)
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.FailedToJoinTheGame);
			_joinRoom = false;
			return;
		}
		if (!NameChecking(userName))
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.InvalidUsername);
			_joinRoom = false;
			return;
		}
		if (!RoomsChecking(checkingRoom) || !NameChecking(roomName))
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.TheGameLobbyDoesNotExist);
			Debug.Log(roomName);
			_joinRoom = false;
			return;
		}
		if (!CountPlayersInRoomChecking(checkingRoom))
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.TheMaximumNumberOfPlayersHasBeenReached);
			_joinRoom = false;
			return;
		}
		if (!RoomStateChecking(checkingRoom))
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.TheLobbyIsNotAvailable);
			_joinRoom = false;
			return;
		}
		bool passwordIsRequired = false;
		if (passwordSystem)
		{
			PasswordAndStateCheck(out passwordIsRequired);
		}
		else
		{
			PhotonNetwork.JoinRoom(roomName);
		}
		if (!passwordIsRequired)
		{
			NeedForDrinkEscSystem_un.enableUpdate = false;
		}
		SetUserCustProps();
		if (!passwordIsRequired)
		{
			ShowProgress();
		}
		_joinRoom = false;
	}

	private void ShowProgress()
	{
		if (dProgress == null)
		{
			NeedForDrinkEscSystem_un.enableUpdate = false;
			dProgress = DialogManager.ShowProgressCircular(dialogsLocalization.getStringValue("loading"));
			StopCoroutine(_HideDProgress());
			StartCoroutine(_HideDProgress());
		}
	}

	public void JoinRandRoom()
	{
		Debug.Log("JoinRoom");
		UniversalBeforeCheckingErrors();
		SetUserCustProps();
		if (PhotonNetwork.GetRoomList().Length == 0)
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.FailedToJoinTheGame);
			return;
		}
		ShowProgress();
		if (PlayerPrefs.HasKey("YouConnectToTheRoomInGame"))
		{
			PlayerPrefs.DeleteKey("YouConnectToTheRoomInGame");
		}
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		hashtable.Add("roomState", "0");
		hashtable.Add("password", string.Empty);
		PhotonNetwork.JoinRandomRoom(hashtable, (byte)GlobalVariablesAbstractScript_un.maxPlayersInRoom);
	}

	public void JoinRandRoom(Dictionary<string, string> lobbyRequirements, int maxPlayersInRoom)
	{
		Debug.Log("JoinRoom");
		UniversalBeforeCheckingErrors();
		SetUserCustProps();
		if (PhotonNetwork.GetRoomList().Length == 0)
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.FailedToJoinTheGame);
			return;
		}
		ShowProgress();
		if (PlayerPrefs.HasKey("YouConnectToTheRoomInGame"))
		{
			PlayerPrefs.DeleteKey("YouConnectToTheRoomInGame");
		}
		PhotonNetwork.JoinRandomRoom(DictionaryToHashtable(lobbyRequirements), (byte)maxPlayersInRoom);
	}

	public void CreateRoomYesTest(bool connectToDifferentRooms, string correctionRoom)
	{
		PhotonNetwork.offlineMode = false;
		rooms = PhotonNetwork.GetRoomList();
		userName = userNameFromPref();
		SetUserCustProps();
		int num = -1;
		while (!joined)
		{
			num++;
			roomName = "test_" + num + "_" + correctionRoom;
			if (connectToDifferentRooms)
			{
				roomName += "multi";
			}
			else
			{
				roomName += "sinle";
			}
			RoomInfo roomInfo = currentRoom(roomName);
			if (RoomsChecking(roomInfo))
			{
				if (CountPlayersInRoomChecking(roomInfo) && !connectToDifferentRooms)
				{
					JoinTestInGame(roomInfo);
					break;
				}
			}
			else
			{
				CreateTestInGame();
			}
		}
	}

	private void CreateTestInGame()
	{
		try
		{
			PhotonNetwork.CreateRoom(roomName, GetRoomOptions(16), TypedLobby.Default);
			joined = true;
		}
		catch (Exception ex)
		{
			Debug.LogFormat("CreateTestInGame Error: {0}", ex);
		}
	}

	private void JoinTestInGame(RoomInfo curRoom)
	{
		try
		{
			SetUserCustProps();
			PhotonNetwork.JoinRoom(roomName);
			joined = true;
		}
		catch (Exception ex)
		{
			Debug.LogFormat("JoinTestInGame Error: {0}", ex);
		}
	}

	public void CreateRoomYes()
	{
		CreateRoomYes(lib.roomName, GlobalVariablesAbstractScript_un.maxPlayersInRoom);
	}

	public void CreateRoomYes(string roomName, int maxPlayersInRoom, Dictionary<string, string> lobbyParams = null, bool offlineMode = false)
	{
		if (!offlineMode)
		{
			rooms = PhotonNetwork.GetRoomList();
			userName = userNameFromPref();
			RoomInfo checkingRoom = currentRoom(roomName);
			if (PhotonNetwork.connectionStateDetailed != ClientState.JoinedLobby)
			{
				ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.FailedToJoinTheGame);
				return;
			}
			if (!NameChecking(userName))
			{
				ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.InvalidUsername);
				return;
			}
			if (RoomsChecking(checkingRoom) || !NameChecking(roomName))
			{
				ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.TheLobbyNameIsNotUnique);
				return;
			}
		}
		else
		{
			GlobalVariablesAbstractScript_un.DisconnectByScript();
			PhotonNetwork.offlineMode = offlineMode;
		}
		if (PlayerPrefs.HasKey("YouConnectToTheRoomInGame"))
		{
			PlayerPrefs.DeleteKey("YouConnectToTheRoomInGame");
		}
		SetUserCustProps();
		RoomOptions roomOptions = GetRoomOptions(maxPlayersInRoom, lobbyParams);
		PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
	}

	private RoomOptions GetRoomOptions(int maxPlayersInRoom, Dictionary<string, string> dict = null)
	{
		ExitGames.Client.Photon.Hashtable hashtable = DictionaryToHashtable(dict);
		string[] customRoomPropertiesForLobby = new string[0];
		if (dict != null)
		{
			customRoomPropertiesForLobby = dict.Keys.ToArray();
		}
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.IsVisible = true;
		roomOptions.MaxPlayers = (byte)maxPlayersInRoom;
		roomOptions.CleanupCacheOnLeave = true;
		roomOptions.PublishUserId = true; // Ensure UserId is published in the room
		if (hashtable != null)
		{
			roomOptions.CustomRoomPropertiesForLobby = customRoomPropertiesForLobby;
			roomOptions.CustomRoomProperties = hashtable;
		}
		return roomOptions;
	}

	private ExitGames.Client.Photon.Hashtable DictionaryToHashtable(Dictionary<string, string> lobbyParams)
	{
		if (lobbyParams == null)
		{
			return null;
		}
		ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
		foreach (KeyValuePair<string, string> item in lobbyParams.ToList())
		{
			hashtable.Add(item.Key, item.Value);
		}
		return hashtable;
	}

	private void PasswordAndStateCheck(out bool passwordIsRequired)
	{
		bool flag = false;
		for (int i = 0; i < rooms.Length; i++)
		{
			if (!(rooms[i].name == roomName))
			{
				continue;
			}
			if (rooms[i].customProperties["password"].ToString() == string.Empty)
			{
				if (int.Parse((string)rooms[i].customProperties["roomState"]) == 0)
				{
					if (PlayerPrefs.HasKey("YouConnectToTheRoomInGame"))
					{
						PlayerPrefs.DeleteKey("YouConnectToTheRoomInGame");
					}
				}
				else
				{
					PlayerPrefs.SetInt("YouConnectToTheRoomInGame", 1);
				}
				PhotonNetwork.JoinRoom(roomName);
			}
			else
			{
				DialogPrompt prompt = DialogManager.ShowPrompt(dialogsLocalization.getStringValue("password"), delegate(string inputFieldValue)
				{
					PasswordToJoinTheGame(inputFieldValue);
				}, dialogsLocalization.getStringValue("ok"), dialogsLocalization.getStringValue("lobbyisprotected"), null, delegate
				{
				}, dialogsLocalization.getStringValue("cancel"));
				UnityEngine.MonoBehaviour.print("passwPrompt " + roomNameToPassword);
				NeedForDrinkEscSystem_un.DialogPromptEscSystemFix(prompt);
				roomNameToPassword = roomName;
				flag = true;
			}
			break;
		}
		passwordIsRequired = flag;
	}

	private IEnumerator _HideDProgress()
	{
		yield return new WaitForSeconds(2f);
		dProgress.Hide();
		NeedForDrinkEscSystem_un.enableUpdate = true;
	}

	private void ForExtraLobbiesRoomNameToPassword()
	{
		roomNameToPassword = "room710room710room710!!!";
	}

	public void PasswordToJoinTheGame(string inputFieldValue)
	{
		string text = roomNameToPassword;
		if (text == "room710room710room710!!!")
		{
			ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.PasswordIncorrect);
			return;
		}
		for (int i = 0; i < rooms.Length; i++)
		{
			if (rooms[i].name == text)
			{
				if (inputFieldValue == rooms[i].customProperties["password"].ToString())
				{
					PhotonNetwork.JoinRoom(text);
				}
				else
				{
					ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.PasswordIncorrect);
				}
				return;
			}
		}
		ErrorSystemMaterialUI_un.ErrorShow(ErrorSystemMaterialUI_un.Error.TheGameLobbyDoesNotExist);
	}

	private void SetUserCustProps()
	{
		PhotonNetwork.player.name = userName;
		PhotonNetwork.automaticallySyncScene = false;

		// Ensure UserId is always set
		if (string.IsNullOrEmpty(PhotonNetwork.AuthValues?.UserId))
		{
			string userId = GenerateUniqueUserId();
			if (PhotonNetwork.AuthValues == null)
			{
				PhotonNetwork.AuthValues = new AuthenticationValues();
			}
			PhotonNetwork.AuthValues.UserId = userId;
		}
	}

	private string GenerateUniqueUserId()
	{
		if (SteamManager.Initialized)
		{
			return SteamUser.GetSteamID().ToString();
		}
		// Fallback to a random unique ID if Steam is not available
		return "Player_" + UnityEngine.Random.Range(1000, 9999);
	}

	private void UniversalBeforeCheckingErrors()
	{
		userName = userNameFromPref();
		roomName = PlayerPrefs.GetString("CreateRoomInputRoomNameJoin");
		PlayerPrefs.DeleteKey("CreateRoomInputRoomNameJoin");
		rooms = PhotonNetwork.GetRoomList();
	}

	private string userNameFromPref()
	{
		if (SteamManager.Initialized)
		{
			return SteamFriends.GetPersonaName();
		}
		return "Player";
	}

	public RoomInfo currentRoom(string checkingRoom)
	{
		rooms = PhotonNetwork.GetRoomList();
		if (rooms != null)
		{
			RoomInfo[] array = rooms;
			foreach (RoomInfo roomInfo in array)
			{
				if (roomInfo.name == checkingRoom)
				{
					return roomInfo;
				}
			}
			return null;
		}
		return null;
	}

	public bool RoomExists(string roomName)
	{
		if (currentRoom(roomName) != null)
		{
			return true;
		}
		return false;
	}

	private bool RoomsChecking(RoomInfo checkingRoom)
	{
		if (checkingRoom != null)
		{
			return true;
		}
		return false;
	}

	private bool CountPlayersInRoomChecking(RoomInfo checkingRoom)
	{
		if (checkingRoom.maxPlayers - checkingRoom.playerCount > 0)
		{
			return true;
		}
		return false;
	}

	private bool NameChecking(string checkingUser)
	{
		if (checkingUser != string.Empty)
		{
			return true;
		}
		return false;
	}

	private bool RoomStateChecking(RoomInfo checkingRoom)
	{
		string text = checkingRoom.CustomProperties["roomState"].ToString();
		if (text == "0")
		{
			return true;
		}
		return false;
	}

	private void OnGUI()
	{
		if (onGuiEnabled)
		{
			GUILayout.BeginArea(new Rect(0f, 0f, 400f, 2000f));
			if (GUILayout.Button("CreateRoomYes(test 1, 12, dictionary, false);"))
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("key1", "value1");
				dictionary.Add("key2", "value2");
				dictionary.Add("key3", "value3");
				CreateRoomYes("test 1", 12, dictionary);
			}
			if (GUILayout.Button("CreateRoomYes(test 2, 12, dictionary, true);"))
			{
				Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
				dictionary2.Add("ey1", "alue1");
				dictionary2.Add("ey2", "alue2");
				dictionary2.Add("ey3", "alue3");
				CreateRoomYes("test 2", 12, dictionary2, offlineMode: true);
			}
			if (GUILayout.Button("CreateRoomYes(test 3, 12, null, false);"))
			{
				CreateRoomYes("test 3", 12);
			}
			if (GUILayout.Button("CreateRoomYes(test 4, 12, null, true);"))
			{
				CreateRoomYes("test 4", 12, null, offlineMode: true);
			}
			GUILayout.EndArea();
		}
	}
}
