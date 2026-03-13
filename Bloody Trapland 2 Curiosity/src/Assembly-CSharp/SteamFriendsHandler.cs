using System;
using System.Collections.Generic;
using System.Text;
using AftershockUnity.Steam;
using Steamworks;
using UnityEngine;

public class SteamFriendsHandler
{
	public class SteamGameLobby
	{
		public CSteamID LobbyId;
	}

	private Dictionary<string, CSteamID> FriendsList;

	private static SteamFriendsHandler m_instance;

	public SteamGameLobby CurrentLobby;

	private string[] StartingArguments;

	private CSteamID currentLobbyID;

	private static bool didCreateLobby;

	private LobbyInvite_t invData;

	private FriendGameInfo_t fInfo;

	public string CreatedName;

	public string Region;

	public ulong m_ulSteamIDLobby;

	protected Callback<LobbyEnter_t> m_LobbyEnter;

	protected Callback<LobbyInvite_t> m_LobbyInvite;

	protected Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequeste;

	protected Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;

	protected Callback<LobbyChatMsg_t> m_LobbyChatMsg;

	public static SteamFriendsHandler Instance => m_instance;

	public SteamFriendsHandler()
	{
		if (m_instance == null)
		{
			init();
			m_instance = this;
			StartingArguments = Environment.GetCommandLineArgs();
			m_LobbyEnter = Callback<LobbyEnter_t>.Create(LobbyEnterCallback);
			m_LobbyInvite = Callback<LobbyInvite_t>.Create(playerWasInvitedToLobbyCallback);
			m_GameLobbyJoinRequeste = Callback<GameLobbyJoinRequested_t>.Create(onGameLobbyJoinRequest);
			int num = 0;
			if (SteamManager.Initialized)
			{
				num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll);
			}
			FriendsList = new Dictionary<string, CSteamID>();
			for (int i = 0; i < num; i++)
			{
				CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagAll);
				FriendsList.Add(friendByIndex.m_SteamID.ToString(), friendByIndex);
			}
		}
	}

	public static void CreateLobby()
	{
		didCreateLobby = true;
		SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 20);
	}

	public static void JoinLobby(CSteamID lobbyID)
	{
		didCreateLobby = false;
		SteamMatchmaking.JoinLobby(lobbyID);
	}

	public static void InviteToLobbyOverlay()
	{
		SteamFriends.ActivateGameOverlayInviteDialog(Instance.currentLobbyID);
	}

	public static void InviteToGame(string id)
	{
		ulong result = 0uL;
		if (ulong.TryParse(id, out result))
		{
			SteamFriends.InviteUserToGame(new CSteamID(result), "-lobby lobbyname");
		}
	}

	public static void GotoSteamWorkshop()
	{
		SteamFriends.ActivateGameOverlayToWebPage("http://steamcommunity.com/workshop/browse?appid=579840");
	}

	public static void GotoSteamWorkshopLegalagreement()
	{
		SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/workshop/workshoplegalagreement");
	}

	private void playerWasInvitedToLobbyCallback(LobbyInvite_t data)
	{
		if (data.m_ulGameID != 0 && data.m_ulGameID != SteamManager.Instance.m_GameID.m_GameID)
		{
			Debug.Log((object)"Invite was for another game");
			return;
		}
		SteamManager.Instance.RequestPersona(data.m_ulSteamIDUser, OnGetPersonaSteamInviteName);
		invData = data;
		Debug.Log((object)invData.m_ulSteamIDLobby);
	}

	private void OnGetPersonaSteamInviteName(string Name, ulong id)
	{
		MessageHandler.ShowDialog(DialogeDone, "Game Invitation", "You were invited by " + Name + ", Join?");
	}

	public void Game()
	{
	}

	private void DialogeDone(bool done)
	{
		if (done)
		{
			JoinLobby(new CSteamID(invData.m_ulSteamIDLobby));
		}
		Debug.Log((object)invData.m_ulSteamIDLobby);
	}

	private void LobbyEnterCallback(LobbyEnter_t data)
	{
		m_ulSteamIDLobby = data.m_ulSteamIDLobby;
		Instance.currentLobbyID = new CSteamID(data.m_ulSteamIDLobby);
		if (didCreateLobby)
		{
			CreatedName = data.m_ulSteamIDLobby.ToString();
			GameNetworkManager.Instance.CreateARoom(CreatedName);
			Region = PhotonNetwork.networkingPeer.CloudRegion.ToString();
		}
		else
		{
			SendLobbyMessage("join");
		}
		didCreateLobby = false;
	}

	public void SendJoinRequest()
	{
		SendLobbyMessage("join");
	}

	private void onGameLobbyJoinRequest(GameLobbyJoinRequested_t data)
	{
		CSteamID steamIDLobby = data.m_steamIDLobby;
		Debug.Log((object)("[SteamFriends] Game lobby join requested: " + steamIDLobby.ToString()));
		didCreateLobby = false;
		invData = new LobbyInvite_t
		{
			m_ulSteamIDLobby = data.m_steamIDLobby.m_SteamID
		};
	}

	public static string GetName()
	{
		return "Player";
	}

	public void PhotonEvent(byte eventCode, object content, int senderId)
	{
	}

	private void init()
	{
		m_LobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
		m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
	}

	private void SendServerDetails()
	{
		string text = "none";
		if (PhotonNetwork.networkingPeer != null)
		{
			text = PhotonNetwork.networkingPeer.CloudRegion.ToString();
		}
		if (string.IsNullOrEmpty(text) || text.ToLower() == "none")
		{
			text = "selfhosted";
		}
		if (PhotonNetwork.room != null)
		{
			CreatedName = PhotonNetwork.room.Name;
		}
		string text2 = "serverdata:" + Region + " - " + CreatedName;
		Debug.Log((object)("[SteamFriends] Sending server details: " + text2));
		SendLobbyMessage(text2);
	}

	private void OnLobbyChatMsg(LobbyChatMsg_t pCallback)
	{
		Debug.Log((object)("[" + 507 + " - LobbyChatMsg] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDUser + " -- " + pCallback.m_eChatEntryType + " -- " + pCallback.m_iChatID));
		byte[] array = new byte[4096];
		CSteamID pSteamIDUser;
		EChatEntryType peChatEntryType;
		int lobbyChatEntry = SteamMatchmaking.GetLobbyChatEntry((CSteamID)pCallback.m_ulSteamIDLobby, (int)pCallback.m_iChatID, out pSteamIDUser, array, array.Length, out peChatEntryType);
		string text = Encoding.UTF8.GetString(array, 0, lobbyChatEntry).Trim();
		Debug.Log((object)string.Concat("GetLobbyChatEntry(", (CSteamID)pCallback.m_ulSteamIDLobby, ", ", (int)pCallback.m_iChatID, ", out SteamIDUser, Data, Data.Length, out ChatEntryType) : ", lobbyChatEntry, " -- ", pSteamIDUser, " -- ", text, " -- ", peChatEntryType));
		if (text == "join")
		{
			CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(m_ulSteamIDLobby));
			CSteamID steamID = SteamUser.GetSteamID();
			if (lobbyOwner == steamID)
			{
				Debug.Log((object)"[SteamFriends] Host responding to join request with server details");
				SendServerDetails();
				return;
			}
		}
		if (NetworkManager.IsOnline)
		{
			Debug.Log((object)("[SteamFriends] Already in game, ignoring " + text));
		}
		else
		{
			if (NetworkManager.IsServer || !text.Contains("serverdata:"))
			{
				return;
			}
			text = text.Replace("serverdata:", string.Empty);
			string[] array2 = text.Split('-');
			if (array2.Length > 1)
			{
				string text2 = array2[0].Trim();
				string text3 = array2[1].Trim();
				Debug.Log((object)("[SteamFriends] Server region: " + text2 + ", Room: " + text3));
				if (string.IsNullOrEmpty(text2) || text2.ToLower() == "none" || text2.ToLower() == "selfhosted")
				{
					Debug.Log((object)"[SteamFriends] Self-hosted server detected, joining directly");
					NetworkManager.ApplyNetworkConfigurationStatic();
					NetworkManager.JoinRoomByName(text3);
				}
				else
				{
					LobbyListCreater.ChangeRegionJoinGame(text2, text3);
				}
			}
			else
			{
				Debug.LogError((object)("[SteamFriends] Serverinfo was invalid: " + text));
			}
		}
	}

	private void SendLobbyMessage(string send)
	{
		CSteamID steamIDLobby = new CSteamID(m_ulSteamIDLobby);
		byte[] bytes = Encoding.UTF8.GetBytes(send);
		bool flag = SteamMatchmaking.SendLobbyChatMsg(steamIDLobby, bytes, bytes.Length);
	}

	private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
	{
		if (pCallback.m_rgfChatMemberStateChange == 1)
		{
			SendServerDetails();
		}
	}
}
