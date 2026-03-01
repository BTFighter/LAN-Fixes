// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// HoverPhotonCore
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OrionPlatform.Main;
using OrionPlatform.Multiplayer;
using OrionPlatform.PlatformSpecific.Steam;
using Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HoverPhotonCore : MonoBehaviour
{
	private class boolRef
	{
		public bool b;
	}

	public delegate void DelegateFunc();

	public static HoverPhotonCore instance;

	public static byte HOVER_SERVER_MAX_PLAYERS;

	private bool b_haveUpdatedMaxPlayers;

	public static bool MustReconnectMaster;

	public bool m_bLastDisconnectionIsNormal;

	public OnlinePrivacy clientPrivacy;

	private OnlinePrivacy clientLastPrivacy;

	private float retryConnectionFreezeTime;

	private float retryConnectionWaitTime = 6f;

	private bool isInConnectionSequence;

	private IEnumerator CheckPlatformConnectionUpdateCoroutine;

	private string m_currentLobby;

	public string version = "0.62";

	private HoverLobbyInfo m_currLobbyInfo;

	private bool lastConnectShouldTriggerPhotonStandardError;

	private bool m_lastJoinIsRichPresence;

	private bool m_mustIgnoreFriends;

	private DateTime last_joined_room;

	public static List<string> announcedPlayerList;

	public LocalizedString ls_playerJoinRoom;

	private static float roomConnectionTime;

	private bool disableCrossPlatform
	{
		get
		{
			if (SaveManager.current != null)
			{
				return SaveManager.current.crossPlatform == 1;
			}
			return false;
		}
	}

	private CrossPlatformState crossPlatformState
	{
		get
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Invalid comparison between Unknown and I4
			if (SaveManager.current != null)
			{
				return (CrossPlatformState)SaveManager.current.crossPlatform;
			}
			if ((int)Application.platform == 25)
			{
				return CrossPlatformState.PcPs;
			}
			return CrossPlatformState.PcXboxSwitch;
		}
	}

	private string hoverLobbyName
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			int num;
			if (((Scene)(ref activeScene)).buildIndex == 8)
			{
				num = 1;
			}
			else
			{
				activeScene = SceneManager.GetActiveScene();
				if (((Scene)(ref activeScene)).buildIndex == 9)
				{
					num = 5;
				}
				else
				{
					activeScene = SceneManager.GetActiveScene();
					num = ((Scene)(ref activeScene)).buildIndex;
				}
			}
			int num2 = num;
			return "hoverLobby" + version + ":map:" + num2 + ":privacy:" + clientPrivacy;
		}
	}

	public static event DelegateFunc OnJoinedRoomEvent;

	public static event DelegateFunc OnLeftRoomEvent;

	private void Awake()
	{
		if ((Object)(object)instance != (Object)null)
		{
			((Behaviour)this).enabled = false;
			return;
		}
		instance = this;
		Core.filters_CanAcceptNetworkInvitation = OnFilterMultiplayerInvitations;
		EventManager.OnReturnToMainMenu += OnReturnToMainMenu;
	}

	private void Start()
	{
		PhotonNetwork.autoJoinLobby = false;
	}

	private bool OnFilterMultiplayerInvitations(AccountUserDesc from)
	{
		Core.ServiceUnavailableReason serviceUnavailableReason = Core.LiveServiceAvailables(forceInstant: true);
		switch (serviceUnavailableReason)
		{
		case Core.ServiceUnavailableReason.RequirePatching:
			OrionThreadBridge.instance.MainThreadTask(delegate
			{
				Hover_RequirePatching();
			});
			return false;
		case Core.ServiceUnavailableReason.RequireSystemUpdate:
			OrionThreadBridge.instance.MainThreadTask(delegate
			{
				Hover_RequireSystemUpdate();
			});
			return false;
		default:
			switch (serviceUnavailableReason)
			{
			case Core.ServiceUnavailableReason.RequireSystemUpdate:
				OrionThreadBridge.instance.MainThreadTask(delegate
				{
					Hover_RequireSystemUpdate();
				});
				return false;
			case Core.ServiceUnavailableReason.InvalidAge:
				OrionThreadBridge.instance.MainThreadTask(delegate
				{
					Hover_InvalidAge();
				});
				return false;
			default:
				OrionThreadBridge.instance.MainThreadTask(delegate
				{
					Hover_OnConnectionFail();
				});
				return false;
			case Core.ServiceUnavailableReason.Ok:
				return true;
			}
		}
	}

	public string RichPresenceStringToRoomName(string msg)
	{
		return msg;
	}

	private void OnLevelWasLoaded(int LevelIdx)
	{
		if (!isInConnectionSequence)
		{
			LeaveRoomAndLobby();
			if (SaveManager.current != null)
			{
				SetOnlinePrivacy((OnlinePrivacy)SaveManager.current.onlinePrivacy);
			}
			else
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
		}
	}

	public static OnlinePrivacy GetOnlinePrivacy()
	{
		if ((Object)(object)instance == (Object)null)
		{
			return OnlinePrivacy.Offline;
		}
		return instance.clientPrivacy;
	}

	public static void SetOnlineMode(OnlinePrivacy new_privacy)
	{
		if (!((Object)(object)instance == (Object)null))
		{
			instance.SetOnlinePrivacy(new_privacy);
		}
	}

	public OnlinePrivacy SetOnlinePrivacy(OnlinePrivacy new_privacy)
	{
		if ((Object)(object)Game.current_player_manager != (Object)null && (Object)(object)Game.current_player_manager.current_mission_manager != (Object)null)
		{
			return clientPrivacy;
		}
		if ((Object)(object)ClientSync.instance == (Object)null || Game.speedRun)
		{
			new_privacy = OnlinePrivacy.Offline;
		}
		clientLastPrivacy = clientPrivacy;
		clientPrivacy = new_privacy;
		bool flag = clientLastPrivacy != clientPrivacy;
		if (ClientSync.instance.disableOnlineOnThisScene)
		{
			Disconnect(string.Empty);
		}
		else
		{
			switch (new_privacy)
			{
			case OnlinePrivacy.Offline:
				isInConnectionSequence = false;
				Disconnect();
				Core.SetDetailedRichPresence(GetAdvancedRichPresence());
				Core.ClearMatchmakingStatus();
				break;
			case OnlinePrivacy.FriendsOnly:
			case OnlinePrivacy.Online:
				if ((clientLastPrivacy == OnlinePrivacy.Online && new_privacy == OnlinePrivacy.FriendsOnly) || (clientLastPrivacy == OnlinePrivacy.FriendsOnly && new_privacy == OnlinePrivacy.Online))
				{
					LeaveRoomAndLobby();
				}
				if (flag || !isInConnectionSequence)
				{
					Connection();
				}
				break;
			}
		}
		return clientPrivacy;
	}

	public void Disconnect(string connectionState = "Offline")
	{
		isInConnectionSequence = false;
		((MonoBehaviour)this).StopAllCoroutines();
		LeaveRoomAndLobby();
		if (PhotonNetwork.connected)
		{
			PhotonNetwork.Disconnect();
		}
		GuiConnectionState.SetConnectionInfoText(connectionState, ShowInfoText: false);
	}

	public void Connection()
	{
		if (!PhotonNetwork.connecting && (!PhotonNetwork.connectedAndReady || !((Object)(object)Game.current_player_manager != (Object)null) || !PhotonNetwork.inRoom))
		{
			isInConnectionSequence = false;
			((MonoBehaviour)this).StopAllCoroutines();
			((MonoBehaviour)this).StartCoroutine(ConnectionSequence());
			((MonoBehaviour)this).StartCoroutine(PeriodicNotifyRichPresence());
		}
	}

	private IEnumerator PeriodicNotifyRichPresence()
	{
		while (true)
		{
			if (PhotonNetwork.inRoom)
			{
				Core.SetDetailedRichPresence(GetAdvancedRichPresence());
			}
			yield return (object)new WaitForSecondsRealtime(10f);
		}
	}

	private IEnumerator __SwitchLiveServiceAvail(boolRef isSuccess)
	{
		yield break;
	}

	private IEnumerator ConnectionSequence()
	{
		if (retryConnectionFreezeTime > Time.unscaledTime)
		{
			GuiConnectionState.SetConnectionInfoText("Next connection attempt in " + (retryConnectionFreezeTime - Time.unscaledTime).ToString("##.0") + " sec");
			Debug.LogError((object)("PHOTON : Next connection attempt in " + (retryConnectionFreezeTime - Time.unscaledTime).ToString("##.0") + " sec"));
		}
		m_lastJoinIsRichPresence = false;
		isInConnectionSequence = true;
		yield return null;
		yield return (object)new WaitForSecondsRealtime(0.5f);
		m_bLastDisconnectionIsNormal = false;
		while ((Object)(object)Game.current_player_manager == (Object)null || !Game.can_load_level || GuiGameInterface.currentGuiPageName == GuiGameInterface.GuiPageName.dialog)
		{
			yield return (object)new WaitForSecondsRealtime(1f);
		}
		GuiConnectionState.SetConnectionInfoText("Connection...");
		while (!Core.IsSignedIn())
		{
			yield return (object)new WaitForSecondsRealtime(1f);
		}
		bool bImmediateReturn = false;
		Debug.LogError((object)"BEGIN INTERNETCHECK");
		Core.AsyncBoolResult haveInternetCheck = Core.AsyncLiveServiceAvailables();
		haveInternetCheck.LockResult();
		Debug.LogError((object)"ACQUIRED BEGIN INTERNETCHECK");
		while (!haveInternetCheck.isDone)
		{
			haveInternetCheck.ReleaseResult();
			yield return (object)new WaitForSecondsRealtime(0.25f);
			haveInternetCheck.LockResult();
		}
		haveInternetCheck.ReleaseResult();
		bool haveInternet = haveInternetCheck.result;
		Debug.LogError((object)"END INTERNETCHECK");
		if (!haveInternet)
		{
			if (haveInternetCheck.reason != Core.ServiceUnavailableReason.SignedOut)
			{
				if (haveInternetCheck.reason == Core.ServiceUnavailableReason.RequirePatching)
				{
					Hover_RequirePatching();
					yield break;
				}
				if (haveInternetCheck.reason == Core.ServiceUnavailableReason.RequireSystemUpdate)
				{
					Hover_RequireSystemUpdate();
					yield break;
				}
				if (haveInternetCheck.reason == Core.ServiceUnavailableReason.RequireSystemUpdate)
				{
					Hover_RequireSystemUpdate();
					yield break;
				}
				if (haveInternetCheck.reason == Core.ServiceUnavailableReason.InvalidAge)
				{
					Hover_InvalidAge();
					yield break;
				}
				Debug.LogError((object)"SetOnlinePrivacy from INTERNETCHECK");
				Hover_OnConnectionFail();
				yield break;
			}
			Core.ShowSigninDialog();
		}
		haveInternetCheck = Core.AsyncLiveServiceAvailables(force: true);
		haveInternetCheck.LockResult();
		while (!haveInternetCheck.isDone)
		{
			haveInternetCheck.ReleaseResult();
			yield return (object)new WaitForSecondsRealtime(0.25f);
			haveInternetCheck.LockResult();
		}
		haveInternetCheck.ReleaseResult();
		haveInternet = haveInternetCheck.result;
		if (!haveInternet)
		{
			Debug.LogError((object)"SetOnlinePrivacy from INTERNETCHECK2");
			Hover_OnConnectionFail();
			yield break;
		}
		Debug.LogError((object)"BEGIN MPCHECK");
		Core.AsyncBoolResult haveMPCheck = Core.AsyncAccountHaveMultiplayerRights();
		haveMPCheck.LockResult();
		while (!haveMPCheck.isDone)
		{
			haveMPCheck.ReleaseResult();
			yield return (object)new WaitForSecondsRealtime(0.25f);
			haveMPCheck.LockResult();
		}
		haveMPCheck.ReleaseResult();
		bool result = haveMPCheck.result;
		Debug.LogError((object)"END MPCHECK");
		if (!bImmediateReturn && haveInternet && !result)
		{
			Core.RedirectMultiplayerCommercial();
		}
		if (!result)
		{
			haveMPCheck = Core.AsyncAccountHaveMultiplayerRights();
			haveMPCheck.LockResult();
			while (!haveMPCheck.isDone)
			{
				haveMPCheck.ReleaseResult();
				yield return (object)new WaitForSecondsRealtime(0.25f);
				haveMPCheck.LockResult();
			}
			haveMPCheck.ReleaseResult();
			result = haveMPCheck.result;
		}
		if (!bImmediateReturn && haveInternet && !result)
		{
			Debug.LogError((object)"SetOnlinePrivacy from MULTIPLAYERRIGHTS");
			Hover_OnConnectionFail();
			yield break;
		}
		if (!bImmediateReturn)
		{
			_ = string.Empty;
			string text = "steam[]" + Core.GetOnlineUser().GetStringID();
			if (text != PhotonNetwork.playerName)
			{
				PhotonNetwork.playerName = text;
			}
		}
		if (bImmediateReturn)
		{
			Hover_OnConnectionFail();
			yield break;
		}
		if ((!PhotonNetwork.connected || MustReconnectMaster) && !bImmediateReturn && haveInternet)
		{
			if (retryConnectionFreezeTime < Time.unscaledTime)
			{
				Debug.LogError((object)("PHOTON--------------------------(Connection...)" + PhotonNetwork.connected));
				GuiConnectionState.SetConnectionInfoText("Connection To Master...");
				if (PhotonNetwork.connected)
				{
					PhotonNetwork.Disconnect();
				}
				bool overrideDisableCrossPlatform = disableCrossPlatform;
				CrossPlatformBanList.player_banned_callback_data cbData = new CrossPlatformBanList.player_banned_callback_data();
				yield return CrossPlatformBanList.IsUserBanned(Core.GetOnlineUser().GetStringID(), Game.platform.ToString(), cbData);
				if (cbData.succeed && cbData.isBanned)
				{
					SetOnlinePrivacy(OnlinePrivacy.Offline);
					GuiGameInterface.CreateAdvancedPopup(999, "BanPopup", cbData.banreason, LanguageMgr.GetStringById("GuiGeneral.reconnect"), LanguageMgr.GetStringById("GuiGeneral.stayOffline"), delegate(GuiListButton popupResult, GuiPopUpManager popupScript)
					{
						if ((Object)(object)popupResult != (Object)null && popupResult.popupValidated)
						{
							SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
						}
						else
						{
							SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
						}
					});
					yield break;
				}
				if (!b_haveUpdatedMaxPlayers)
				{
					b_haveUpdatedMaxPlayers = true;
					CrossPlatformGeneralConfig.general_config_int_result res5 = new CrossPlatformGeneralConfig.general_config_int_result();
					yield return CrossPlatformGeneralConfig.GetValue("hover-server-maxplayers", 16, res5);
					HOVER_SERVER_MAX_PLAYERS = (byte)res5.val;
					Debug.LogError((object)("Updated Max Player :: " + HOVER_SERVER_MAX_PLAYERS));
				}
				CrossPlatformGeneralConfig.general_config_result res6 = new CrossPlatformGeneralConfig.general_config_result();
				yield return CrossPlatformGeneralConfig.GetValue(overrideDisableCrossPlatform ? "hover-pc-universe" : ((crossPlatformState != CrossPlatformState.PcPs) ? "hover-ccxbswitch-universe" : "hover-ccpsn-universe"), DefaultValue: true, res6);
				if (!res6.val)
				{
					overrideDisableCrossPlatform = true;
					res6 = new CrossPlatformGeneralConfig.general_config_result();
					yield return CrossPlatformGeneralConfig.GetValue(overrideDisableCrossPlatform ? "hover-pc-universe" : ((crossPlatformState != CrossPlatformState.PcPs) ? "hover-ccxbswitch-universe" : "hover-ccpsn-universe"), DefaultValue: true, res6);
					if (!res6.val)
					{
						SetOnlinePrivacy(OnlinePrivacy.Offline);
						GuiGameInterface.CreateAdvancedPopup(999, "BanPopup", "This service is currently disabled, please try another multiplayer option", LanguageMgr.GetStringById("GuiGeneral.reconnect"), LanguageMgr.GetStringById("GuiGeneral.stayOffline"), delegate(GuiListButton popupResult, GuiPopUpManager popupScript)
						{
							if ((Object)(object)popupResult != (Object)null && popupResult.popupValidated)
							{
								SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
							}
							else
							{
								SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
							}
						});
						yield break;
					}
				}
				string text2 = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
				string text3 = "";
				int port = 5055;
				if (File.Exists(text2))
				{
					IniParser iniParser = new IniParser(text2);
					text3 = iniParser.GetValue("Server", "ServerAddress");
					port = iniParser.GetIntValue("Server", "ServerPort", 5055);
				}
				if (!string.IsNullOrEmpty(text3))
				{
					Debug.LogError((object)("PHOTON: Connecting to custom server: " + text3 + ":" + port + " (AppID: " + "Master" + ")"));
					PhotonNetwork.ConnectToMaster(text3, port, "Master", "hover-pc-universe");
				}
				else if (overrideDisableCrossPlatform)
				{
					PhotonNetwork.ConnectUsingSettings("hover-pc-universe");
				}
				else if (crossPlatformState == CrossPlatformState.PcPs)
				{
					PhotonNetwork.ConnectUsingSettings("hover-ccpsn-universe");
				}
				else
				{
					PhotonNetwork.ConnectUsingSettings("hover-ccxbswitch-universe");
				}
				retryConnectionFreezeTime = Time.time + retryConnectionWaitTime;
				retryConnectionWaitTime = Mathf.Min(retryConnectionWaitTime + 4f, 60f);
			}
			yield return (object)new WaitForSecondsRealtime(1f);
		}
		if (!bImmediateReturn)
		{
			retryConnectionWaitTime = 6f;
			GuiConnectionState.SetConnectionInfoText("Connection To Master...");
			while (!PhotonNetwork.connectedAndReady)
			{
				yield return (object)new WaitForSecondsRealtime(0.5f);
			}
			LeaveRoomAndLobby();
			GuiConnectionState.SetConnectionInfoText("Connected to Master Server");
			JoinLobby();
		}
		isInConnectionSequence = false;
	}

	public void OnCustomAuthenticationFailed()
	{
		Debug.LogError((object)"OnCustomAuthenticationFailed");
	}

	public void OnMasterClientSwitched(PhotonPlayer newMasterClient)
	{
		if (PhotonNetwork.room != null && newMasterClient == PhotonNetwork.player)
		{
			PhotonNetwork.room.MaxPlayers = HOVER_SERVER_MAX_PLAYERS;
		}
	}

	public void OnConnectedToMaster()
	{
		Debug.Log((object)"OnConnectedToMaster");
		if (CheckPlatformConnectionUpdateCoroutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(CheckPlatformConnectionUpdateCoroutine);
		}
		CheckPlatformConnectionUpdateCoroutine = CheckPlatformConnectionUpdate();
		((MonoBehaviour)this).StartCoroutine(CheckPlatformConnectionUpdateCoroutine);
	}

	private IEnumerator CheckPlatformConnectionUpdate()
	{
		yield return (object)new WaitForSecondsRealtime(5f);
		while (PhotonNetwork.connected)
		{
			_ = string.Empty;
			Core.AsyncBoolResult haveInternetCheck = Core.AsyncLiveServiceAvailables();
			haveInternetCheck.LockResult();
			while (!haveInternetCheck.isDone)
			{
				haveInternetCheck.ReleaseResult();
				yield return null;
				haveInternetCheck.LockResult();
			}
			haveInternetCheck.ReleaseResult();
			bool result = haveInternetCheck.result;
			if (!result)
			{
				haveInternetCheck.reason.ToString();
			}
			if (!result)
			{
				Hover_OnDisconnectedFromTheServer();
			}
			else
			{
				Core.PeriodicMultiplayerNotify();
			}
			yield return (object)new WaitForSecondsRealtime(5f);
		}
	}

	public void OnConnectedToPhoton()
	{
		Debug.Log((object)"OnConnectedToPhoton");
	}

	public void OnDisconnectedFromPhoton()
	{
		Debug.LogError((object)"OnDisconnectedFromPhoton");
		Player_Manager.ClearPlayerNickNameCache();
	}

	public virtual void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)("Failed To Connect To Photon Cause: " + cause));
		if (cause == DisconnectCause.DisconnectByClientTimeout || cause == DisconnectCause.DisconnectByServerTimeout || cause == DisconnectCause.AuthenticationTicketExpired)
		{
			Hover_OnDisconnectedFromTheServer();
		}
		else if (!m_bLastDisconnectionIsNormal)
		{
			Hover_OnConnectionFail();
		}
	}

	private void OnConnectionFail(DisconnectCause cause)
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)("On Connection Failed " + cause));
		if (m_bLastDisconnectionIsNormal)
		{
			return;
		}
		GuiGameInterface.CreatePopup(999, "GuiGeneral.popupOnConnectionFail", "GuiGeneral.retryConnect", "GuiGeneral.stayOffline", delegate(GuiListButton result, GuiPopUpManager popupScript)
		{
			if ((Object)(object)result != (Object)null && result.popupValidated)
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
			else
			{
				SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
			}
		});
	}

	private void Hover_RequirePatching()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_RequirePatch");
		GuiGameInterface.CreateAdvancedPopup(999, "GUI.WndMsg", "A new update exist for the game, you need to update your game to play online, you will now switch to offline mode", "Continue", null, delegate
		{
			SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
		});
	}

	private void Hover_RequireSystemUpdate()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_RequireSysUpdate");
		GuiGameInterface.CreateAdvancedPopup(999, "GuiGeneric.requireSysUpdate", "NYI", LanguageMgr.GetStringById("GuiGeneric.continue"), null, delegate
		{
			SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
		});
	}

	private void Hover_InvalidAge()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_InvalidAge");
		GuiGameInterface.CreateAdvancedPopup(999, "GuiGeneral.InvalidAge", LanguageMgr.GetStringById("GuiGeneric.invalidAge"), LanguageMgr.GetStringById("GuiGeneric.continue"), null, delegate
		{
			SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
		});
	}

	private void Hover_OnGameFull()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_GameIsFull");
		GuiGameInterface.CreateAdvancedPopup(999, "GUI.onGameFull", LanguageMgr.GetStringById("GuiGeneric.onGameFull"), LanguageMgr.GetStringById("GuiGeneric.retryConnect"), LanguageMgr.GetStringById("GuiGeneric.stayOffine"), delegate(GuiListButton result, GuiPopUpManager popupScript)
		{
			if ((Object)(object)result != (Object)null && result.popupValidated)
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
			else
			{
				SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
			}
		});
	}

	private void Hover_OnConnectionFail()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_OnConnectionFail");
		GuiGameInterface.CreatePopup(999, "GuiGeneral.popupOnConnectionFail", "GuiGeneral.retryConnect", "GuiGeneral.stayOffline", delegate(GuiListButton result, GuiPopUpManager popupScript)
		{
			if ((Object)(object)result != (Object)null && result.popupValidated)
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
			else
			{
				SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
			}
		});
	}

	private void Hover_OnDisconnectedFromTheServer()
	{
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		Debug.LogError((object)"Hover_OnConnectionFail");
		GuiGameInterface.CreatePopup(999, "GuiGeneral.popupOnConnectionFromTheServer", "GuiGeneral.retryConnect", "GuiGeneral.stayOffline", delegate(GuiListButton result, GuiPopUpManager popupScript)
		{
			if ((Object)(object)result != (Object)null && result.popupValidated)
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
			else
			{
				SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
			}
		});
	}

	public void JoinLobby()
	{
		TypedLobby typedLobby = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
		GuiConnectionState.SetConnectionInfoText("Joining Lobby...");
		if (m_currentLobby == null || m_currentLobby != typedLobby.ToString())
		{
			LeaveRoomAndLobby();
			m_currentLobby = typedLobby.ToString();
			Debug.LogError((object)("Joining Lobby With ID " + PhotonNetwork.playerName));
			PhotonNetwork.JoinLobby(typedLobby);
		}
		else
		{
			OnJoinedLobby();
		}
	}

	public virtual void OnJoinedLobby()
	{
		Debug.Log((object)("OnJoinedLobby() : " + m_currentLobby + " " + PhotonNetwork.lobby.Name));
		((MonoBehaviour)this).StartCoroutine(JoinRoom());
	}

	public virtual void OnLeftLobby()
	{
		Debug.Log((object)"OnLeftLobby");
		if (m_bLastDisconnectionIsNormal || NetworkingPeer.pLatestPhotonOperationResponse == null || NetworkingPeer.pLatestPhotonOperationResponse.ReturnCode != 32746 || NetworkingPeer.pLatestPhotonOperationResponse.DebugMessage == null || !NetworkingPeer.pLatestPhotonOperationResponse.DebugMessage.Contains("already joined the specified game"))
		{
			return;
		}
		SetOnlinePrivacy(OnlinePrivacy.Offline);
		GuiGameInterface.CreatePopup(999, "GuiGeneral.popupOnConnectionFail", "GuiGeneral.retryConnect", "GuiGeneral.stayOffline", delegate(GuiListButton result, GuiPopUpManager popupScript)
		{
			if ((Object)(object)result != (Object)null && result.popupValidated)
			{
				SetOnlinePrivacy(SaveManager.GetCLientOnlinePrivacy());
			}
			else
			{
				SaveManager.SetCLientOnlinePrivacy(OnlinePrivacy.Offline);
			}
		});
	}

	private IEnumerator JoinRoom()
	{
		GuiConnectionState.SetConnectionInfoText("Joining Room...");
		JoinRoomLogic();
		yield return null;
	}

	public string GenerateRoomName()
	{
		HoverLobbyInfo hoverLobbyInfo = GenerateLobbyInfo();
		return hoverLobbyInfo.SceneIndex + ";" + hoverLobbyInfo.PlatformType + ";" + hoverLobbyInfo.RoomUID + ";" + hoverLobbyInfo.PrivacyMode;
	}

	public HoverLobbyInfo GetCurrLobbyInfo()
	{
		return m_currLobbyInfo;
	}

	public HoverLobbyInfo GenerateLobbyInfo(bool save = true)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		HoverLobbyInfo hoverLobbyInfo = new HoverLobbyInfo();
		if (save)
		{
			m_currLobbyInfo = hoverLobbyInfo;
		}
		hoverLobbyInfo.PlatformType = "cchover1";
		hoverLobbyInfo.PrivacyMode = clientPrivacy;
		hoverLobbyInfo.RoomUID = Guid.NewGuid().ToString();
		Scene activeScene = SceneManager.GetActiveScene();
		int sceneIndex;
		if (((Scene)(ref activeScene)).buildIndex == 8)
		{
			sceneIndex = 1;
		}
		else
		{
			activeScene = SceneManager.GetActiveScene();
			if (((Scene)(ref activeScene)).buildIndex == 9)
			{
				sceneIndex = 5;
			}
			else
			{
				activeScene = SceneManager.GetActiveScene();
				sceneIndex = ((Scene)(ref activeScene)).buildIndex;
			}
		}
		hoverLobbyInfo.SceneIndex = sceneIndex;
		return hoverLobbyInfo;
	}

	public HoverLobbyInfo ParseRoomName(string RoomName)
	{
		HoverLobbyInfo hoverLobbyInfo = new HoverLobbyInfo();
		try
		{
			string[] array = RoomName.Split(';');
			hoverLobbyInfo.SceneIndex = int.Parse(array[0]);
			hoverLobbyInfo.PlatformType = array[1];
			hoverLobbyInfo.RoomUID = array[2];
			hoverLobbyInfo.PrivacyMode = ((array[3] == "FriendsOnly") ? OnlinePrivacy.FriendsOnly : ((array[3] == "Online") ? OnlinePrivacy.Online : OnlinePrivacy.Offline));
		}
		catch (Exception)
		{
			hoverLobbyInfo.PrivacyMode = OnlinePrivacy.Offline;
		}
		return hoverLobbyInfo;
	}

	public bool MatchmakingRoom(string RoomName, bool IgnorePrivacyAndMap = false)
	{
		Debug.Log((object)("MatchmakingRoom: " + RoomName));
		HoverLobbyInfo hoverLobbyInfo = ParseRoomName(RoomName);
		HoverLobbyInfo hoverLobbyInfo2 = GenerateLobbyInfo(save: false);
		if (hoverLobbyInfo.PrivacyMode == hoverLobbyInfo2.PrivacyMode || IgnorePrivacyAndMap)
		{
			return hoverLobbyInfo.SceneIndex == hoverLobbyInfo2.SceneIndex || IgnorePrivacyAndMap;
		}
		return false;
	}

	public void JoinRoomLogic()
	{
		if (!PhotonNetwork.insideLobby)
		{
			return;
		}
		if (!Core.PlatformSupportFriendlyNameLookup())
		{
			GuiConnectionState.SetConnectionInfoText("Connection Failed");
			return;
		}
		lastConnectShouldTriggerPhotonStandardError = false;
		Core.GetPartyOrFriends(delegate(AccountUserDesc[] friends)
		{
			if ((friends == null || friends.Length == 0) && clientPrivacy == OnlinePrivacy.FriendsOnly)
			{
				Debug.LogError((object)"Playing in Friends Only but you dont have any friends");
			}
			else if (friends == null || friends.Length == 0)
			{
				Debug.LogError((object)"Playing auto mode, tried to find friends, didnt find any");
				PhotonNetwork.JoinRandomRoom(null, HOVER_SERVER_MAX_PLAYERS);
			}
			else
			{
				Debug.LogError((object)("Friends count " + friends.Length));
				string[] array = new string[friends.Length];
				for (int i = 0; i < friends.Length; i++)
				{
					array[i] = "steam[]" + friends[i].GetStringID();
				}
				_ = SteamCore.fastConnectString;
				if (InvitationManager.fastConnectInvitation != null)
				{
					lastConnectShouldTriggerPhotonStandardError = true;
					Debug.LogError((object)("Joining through invitation " + InvitationManager.fastConnectInvitation));
					PhotonNetwork.JoinRoom(InvitationManager.fastConnectInvitation);
					InvitationManager.ClearInvitation();
				}
				else
				{
					if (friends.Length != 0 && !m_mustIgnoreFriends)
					{
						Debug.LogError((object)"Checking friendlist");
						List<string> list = new List<string>();
						foreach (AccountUserDesc accountUserDesc in friends)
						{
							if (accountUserDesc.IsPlayingSameGame())
							{
								string[] richPresence = Core.GetRichPresence(accountUserDesc);
								if (richPresence[1] != null)
								{
									ParseRoomName(richPresence[1]);
									if (MatchmakingRoom(richPresence[1]))
									{
										Debug.LogError((object)("Found a friend playing the same game " + accountUserDesc.GetStringID() + " RP0 " + richPresence[0] + " RP1 " + richPresence[1]));
										if (richPresence != null && richPresence[1] != null && richPresence[1].Length > 5)
										{
											list.Add(richPresence[1]);
										}
									}
								}
							}
						}
						if (list.Count != 0)
						{
							int index = Random.Range(0, list.Count);
							m_lastJoinIsRichPresence = true;
							string text = list[index];
							Debug.LogError((object)("Connecting with friends - " + text));
							PhotonNetwork.JoinRoom(text);
							return;
						}
					}
					else
					{
						Debug.LogError((object)"FriendList is Empty");
					}
					m_mustIgnoreFriends = false;
					Debug.LogError((object)("Finishing joining procedure with privacy " + clientPrivacy));
					if (clientPrivacy == OnlinePrivacy.Online)
					{
						Debug.LogError((object)"Connecting Random Room...");
						TypedLobby typedLobby = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
						PhotonNetwork.JoinRandomRoom(null, HOVER_SERVER_MAX_PLAYERS, MatchmakingMode.FillRoom, typedLobby, null);
					}
					else if (clientPrivacy == OnlinePrivacy.FriendsOnly)
					{
						TypedLobby typedLobby2 = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
						PhotonNetwork.CreateRoom(GenerateRoomName(), new RoomOptions
						{
							MaxPlayers = HOVER_SERVER_MAX_PLAYERS
						}, typedLobby2);
						Debug.LogError((object)"Generated friends only room");
					}
				}
			}
		});
	}

	public Core.DetailedRichPresenceStatus GetAdvancedRichPresence()
	{
		Core.DetailedRichPresenceStatus detailedRichPresenceStatus = new Core.DetailedRichPresenceStatus();
		if (PhotonNetwork.inRoom)
		{
			detailedRichPresenceStatus.PartyMax = PhotonNetwork.room.MaxPlayers;
			detailedRichPresenceStatus.PartySize = PhotonNetwork.room.PlayerCount;
		}
		else
		{
			detailedRichPresenceStatus.PartyMax = 0;
			detailedRichPresenceStatus.PartySize = 0;
		}
		detailedRichPresenceStatus.DiscordIsInstance = true;
		bool flag = Game.activeSceneNameSource != null && Game.activeSceneNameSource != "Hover City Menu" && Game.activeSceneNameSource != "Intro";
		detailedRichPresenceStatus.DiscordLargeImageKey = ((!flag) ? "default_bg" : Game.activeSceneNameSource.Replace(" ", "_").Replace(".", string.Empty).ToLower());
		if (PhotonNetwork.inRoom)
		{
			detailedRichPresenceStatus.MultiplayerInfos = GetMatchmakingConnectionString();
			detailedRichPresenceStatus.MatchID = "!" + m_currLobbyInfo.RoomUID;
			detailedRichPresenceStatus.OptJoinKey = m_currLobbyInfo.SceneIndex + ";" + m_currLobbyInfo.PlatformType + ";" + m_currLobbyInfo.RoomUID + ";" + m_currLobbyInfo.PrivacyMode;
		}
		else
		{
			detailedRichPresenceStatus.MultiplayerInfos = null;
		}
		if (PhotonNetwork.inRoom)
		{
			if (clientPrivacy == OnlinePrivacy.Online)
			{
				detailedRichPresenceStatus.Status = "Playing Online";
			}
			else if (clientPrivacy == OnlinePrivacy.FriendsOnly)
			{
				detailedRichPresenceStatus.Status = "Playing with friends";
			}
		}
		else
		{
			detailedRichPresenceStatus.Status = "Playing Offline";
		}
		string stringById = LanguageMgr.GetStringById("SceneName." + Game.activeSceneNameSource, LanguageMgr.NullStringType.EntryID);
		detailedRichPresenceStatus.Details = stringById;
		if (PhotonNetwork.inRoom)
		{
			detailedRichPresenceStatus.PartyID = m_currLobbyInfo.RoomUID;
		}
		else
		{
			detailedRichPresenceStatus.PartyID = null;
		}
		return detailedRichPresenceStatus;
	}

	public string GetRichPresenceConnectionString()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Scene activeScene = SceneManager.GetActiveScene();
		int num;
		if (((Scene)(ref activeScene)).buildIndex == 8)
		{
			num = 1;
		}
		else
		{
			activeScene = SceneManager.GetActiveScene();
			if (((Scene)(ref activeScene)).buildIndex == 9)
			{
				num = 5;
			}
			else
			{
				activeScene = SceneManager.GetActiveScene();
				num = ((Scene)(ref activeScene)).buildIndex;
			}
		}
		int num2 = num;
		string stringID = Core.GetOnlineUser().GetStringID();
		return num2 + ":" + stringID;
	}

	public string GetMatchmakingConnectionString()
	{
		return PhotonNetwork.room.Name;
	}

	public virtual void OnPhotonJoinRoomFailed(object[] codeAndMsg)
	{
		switch ((short)codeAndMsg[0])
		{
		case 32765:
			Hover_OnGameFull();
			break;
		default:
			Hover_OnConnectionFail();
			break;
		case 32758:
		{
			OnlinePrivacy prevPrivacy = clientPrivacy;
			SetOnlinePrivacy(OnlinePrivacy.Offline);
			Debug.LogError((object)"Hover_InvalidGame");
			if (m_lastJoinIsRichPresence)
			{
				m_mustIgnoreFriends = true;
				SetOnlinePrivacy(prevPrivacy);
			}
			else if (clientPrivacy == OnlinePrivacy.FriendsOnly)
			{
				GuiGameInterface.CreateAdvancedPopup(999, "GuiGeneral.gameIsNotExistingPrivate", LanguageMgr.GetStringById("GuiGeneral.gameIsNotExistingPrivate"), LanguageMgr.GetStringById("GuiGeneral.continue"), null, delegate
				{
					SetOnlinePrivacy(prevPrivacy);
				});
			}
			else
			{
				GuiGameInterface.CreateAdvancedPopup(999, "GuiGeneral.gameIsNotExistingPublic", LanguageMgr.GetStringById("GuiGeneral.gameIsNotExistingPublic"), LanguageMgr.GetStringById("GuiGeneral.continue"), null, delegate
				{
					SetOnlinePrivacy(prevPrivacy);
				});
			}
			break;
		}
		}
	}

	public virtual void OnPhotonRandomJoinFailed(object[] codeAndMsg)
	{
		switch ((short)codeAndMsg[0])
		{
		default:
			Hover_OnConnectionFail();
			break;
		case 32765:
			Hover_OnGameFull();
			break;
		case 32760:
			if (clientPrivacy == OnlinePrivacy.Online)
			{
				Debug.LogError((object)"Creating Random Room...");
				PhotonNetwork.CreateRoom(GenerateRoomName(), typedLobby: new TypedLobby(hoverLobbyName, LobbyType.SqlLobby), roomOptions: new RoomOptions
				{
					MaxPlayers = HOVER_SERVER_MAX_PLAYERS
				});
			}
			else if (clientPrivacy == OnlinePrivacy.FriendsOnly)
			{
				TypedLobby typedLobby3 = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
				PhotonNetwork.CreateRoom(GenerateRoomName(), new RoomOptions
				{
					MaxPlayers = HOVER_SERVER_MAX_PLAYERS
				}, typedLobby3);
				Debug.LogError((object)"Generated friends only room");
			}
			break;
		case 32758:
			if (lastConnectShouldTriggerPhotonStandardError)
			{
				Hover_OnConnectionFail();
				break;
			}
			lastConnectShouldTriggerPhotonStandardError = true;
			if (clientPrivacy == OnlinePrivacy.Online)
			{
				Debug.LogError((object)"OnPhotonRandomJoinFailed - Connecting Random Room...");
				TypedLobby typedLobby = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
				PhotonNetwork.JoinRandomRoom(null, HOVER_SERVER_MAX_PLAYERS, MatchmakingMode.FillRoom, typedLobby, null);
			}
			else if (clientPrivacy == OnlinePrivacy.FriendsOnly)
			{
				TypedLobby typedLobby2 = new TypedLobby(hoverLobbyName, LobbyType.SqlLobby);
				PhotonNetwork.CreateRoom(GenerateRoomName(), new RoomOptions
				{
					MaxPlayers = HOVER_SERVER_MAX_PLAYERS
				}, typedLobby2);
				Debug.LogError((object)"OnPhotonRandomJoinFailed - Generated friends only room");
			}
			break;
		}
	}

	public Dictionary<string, int> CreateRoomAttributes()
	{
		HoverLobbyInfo hoverLobbyInfo = GenerateLobbyInfo(save: false);
		return new Dictionary<string, int>
		{
			{
				"ROOM_PRIVACY_MODE",
				(int)GetOnlinePrivacy()
			},
			{ "GAMEPLAY_MAP", hoverLobbyInfo.SceneIndex }
		};
	}

	public void OnJoinedRoom()
	{
		m_currLobbyInfo = ParseRoomName(PhotonNetwork.room.Name);
		Debug.Log((object)"Joined Room() called by PUN. Now this client is in a room. From here on, your game would be running. For reference, all callbacks are listed in enum: PhotonNetworkingMessage");
		Core.CreateMatchmakingStatus("Hover - Online Session", crossPlatformState != CrossPlatformState.OnlyMine, m_currLobbyInfo.RoomUID, Encoding.UTF8.GetBytes(GetMatchmakingConnectionString()), CreateRoomAttributes());
		Debug.LogError((object)("Joined Room " + PhotonNetwork.room.Name));
		GuiConnectionState.SetConnectionInfoText("Online");
		last_joined_room = DateTime.Now;
		Core.SetDetailedRichPresence(GetAdvancedRichPresence());
		if ((Object)(object)Game.current_player_manager != (Object)null && (Object)(object)Game.instance != (Object)null)
		{
			Game.instance.UpdatePlayerNickName();
		}
		announcedPlayerList.Clear();
		roomConnectionTime = Time.unscaledTime;
		if (HoverPhotonCore.OnJoinedRoomEvent != null)
		{
			HoverPhotonCore.OnJoinedRoomEvent();
		}
	}

	public static void TryToAnnoucedAPlayer(Player_Manager pm)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)instance == (Object)null) && !((Object)(object)pm == (Object)null) && ClientSync.connectedAnInRoom && pm.displayNameReady && !InGameEditorManager.captureMode && pm.gamer_type != GamerType.npc && pm.gamer_type != GamerType.netNpc && !announcedPlayerList.Contains(pm.displayName))
		{
			announcedPlayerList.Add(pm.displayName);
			if (!((Object)(object)pm != (Object)(object)Game.current_player_manager) || !(roomConnectionTime + 3f > pm.spawnTime))
			{
				GuiMessageManager.PlayMessage(instance.ls_playerJoinRoom.GetString().Replace("#", pm.displayName), 0.8f, "Ready", string.Empty, string.Empty, pm.player_state_character.color_light, string.Empty);
			}
		}
	}

	public void OnLeftRoom()
	{
		if (PhotonNetwork.connected)
		{
			SetOnlinePrivacy(clientPrivacy);
		}
		if (HoverPhotonCore.OnLeftRoomEvent != null)
		{
			HoverPhotonCore.OnLeftRoomEvent();
		}
	}

	public void LeaveRoomAndLobby()
	{
		if (PhotonNetwork.inRoom)
		{
			PhotonNetwork.LeaveRoom();
		}
		if (PhotonNetwork.insideLobby)
		{
			PhotonNetwork.LeaveLobby();
		}
		PhotonNetwork.networkingPeer.NewSceneLoaded();
		PhotonNetwork.networkingPeer.SetLevelInPropsIfSynced(SceneManagerHelper.ActiveSceneName);
		m_currentLobby = null;
	}

	private void OnDestroy()
	{
		LeaveRoomAndLobby();
		if (PhotonNetwork.connected)
		{
			PhotonNetwork.Disconnect();
		}
		EventManager.OnReturnToMainMenu -= OnReturnToMainMenu;
	}

	private void OnReturnToMainMenu()
	{
		m_bLastDisconnectionIsNormal = true;
		Disconnect();
	}

	static HoverPhotonCore()
	{
		HOVER_SERVER_MAX_PLAYERS = 16;
		MustReconnectMaster = false;
		announcedPlayerList = new List<string>();
	}
}
