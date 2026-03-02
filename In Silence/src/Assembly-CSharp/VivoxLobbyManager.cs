using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using VivoxUnity;

public class VivoxLobbyManager : MonoBehaviour
{
	private VivoxVoiceManager vivoxManager;

	private List<RosterItem> rosterObjects;

	[SerializeField]
	private GameObject screen_VivoxError;

	[SerializeField]
	private OptionsManager optionsManager;

	[SerializeField]
	private GameObject prefab_rosterItem;

	[SerializeField]
	private LobbyManager lobbyManager;

	private bool isTesting;

	private bool didSetuped;

	private IParticipant Participant;

	private bool isSpeaking;

	private bool hasShownVivoxError;

	public bool IsSpeaking
	{
		get
		{
			return isSpeaking;
		}
		private set
		{
			isSpeaking = value;
			optionsManager.ChangeTestingState(isSpeaking);
		}
	}

	public void Setup(string _playerName)
	{
		vivoxManager = VivoxVoiceManager.Instance;
		if (!vivoxManager)
		{
			Debug.LogError("NULL VIVOX MANAGER");
		}
		rosterObjects = new List<RosterItem>();
		vivoxManager.OnUserLoggedInEvent += OnUserLoggedIn;
		vivoxManager.OnUserLoggedOutEvent += OnUserLoggedOut;
		if (vivoxManager.LoginState == LoginState.LoggedIn)
		{
			OnUserLoggedIn();
			return;
		}
		LoginVivox(_playerName);
		StartCoroutine("CheckVivoxSession");
	}

	private IEnumerator CheckVivoxSession()
	{
		yield return new WaitForSeconds(1f);
		if (vivoxManager.LoginState == LoginState.LoggedOut && !hasShownVivoxError)
		{
			optionsManager.ChangeVoiceChatStatus(isConnected: false, isTesting);
		}
		else if (vivoxManager.LoginState == LoginState.LoggedIn)
		{
			optionsManager.ChangeVoiceChatStatus(isConnected: true, isTesting);
			screen_VivoxError.SetActive(value: false);
		}
		StartCoroutine("CheckVivoxSession");
	}

	public void JoinChannel()
	{
		string channelName = (PhotonNetwork.CurrentRoom.Name + "lobby").ToLower().Replace(" ", "");
		vivoxManager.JoinChannel(channelName, ChannelType.NonPositional, VivoxVoiceManager.ChatCapability.AudioOnly);
		vivoxManager.OnParticipantAddedEvent += OnParticipantAdded;
		vivoxManager.OnParticipantRemovedEvent += OnParticipantRemoved;
		optionsManager.isOnChannel = true;
		UpdateChannel(channelName);
	}

	public void DirectJoinChannel()
	{
		rosterObjects = new List<RosterItem>();
		string channelName = (PhotonNetwork.CurrentRoom.Name + "lobby").ToLower().Replace(" ", "");
		if (!vivoxManager)
		{
			vivoxManager = VivoxVoiceManager.Instance;
		}
		vivoxManager.JoinChannel(channelName, ChannelType.NonPositional, VivoxVoiceManager.ChatCapability.AudioOnly);
		vivoxManager.OnParticipantAddedEvent += OnParticipantAdded;
		vivoxManager.OnParticipantRemovedEvent += OnParticipantRemoved;
		optionsManager.isOnChannel = true;
		UpdateChannel(channelName);
		optionsManager.SetupDevices(VivoxVoiceManager.Instance.AudioInputDevices, VivoxVoiceManager.Instance.AudioOutputDevices);
	}

	public void DisconnectChannel()
	{
		vivoxManager.OnParticipantAddedEvent -= OnParticipantAdded;
		vivoxManager.OnParticipantRemovedEvent -= OnParticipantRemoved;
		vivoxManager.DisconnectAllChannels();
		optionsManager.isOnChannel = false;
		ClearAllRosters();
	}

	private void UpdateChannel(string _channelName)
	{
		if (!vivoxManager || vivoxManager.ActiveChannels.Count <= 0)
		{
			return;
		}
		IChannelSession channelSession = vivoxManager.ActiveChannels.FirstOrDefault((IChannelSession ac) => ac.Channel.Name == _channelName);
		foreach (IParticipant participant in vivoxManager.LoginSession.GetChannelSession(channelSession.Channel).Participants)
		{
			AddParticipant(participant);
		}
	}

	private void OnDestroy()
	{
		if (!GameSettings.isOffline)
		{
			vivoxManager.OnUserLoggedInEvent -= OnUserLoggedIn;
			vivoxManager.OnUserLoggedOutEvent -= OnUserLoggedOut;
		}
	}

	public void LoginVivox(string _playerName)
	{
		vivoxManager.Login(_playerName);
	}

	public void LogoutVivox()
	{
		vivoxManager.DisconnectAllChannels();
		vivoxManager.Logout();
	}

	private void OnUserLoggedIn()
	{
		MonoBehaviour.print("User Logged In");
		lobbyManager.FirstOpenSetup();
		optionsManager.SetupDevices(vivoxManager.AudioInputDevices, vivoxManager.AudioOutputDevices);
		optionsManager.ChangeVoiceChatStatus(isConnected: true, isTestChannel: false);
		VivoxVoiceManager.Instance.LoginSession.SetTransmissionMode(TransmissionMode.All);
	}

	private void OnUserLoggedOut()
	{
		MonoBehaviour.print("User Logout");
		vivoxManager.DisconnectAllChannels();
		optionsManager.ChangeVoiceChatStatus(isConnected: false, isTestChannel: false);
	}

	private void AddParticipant(IParticipant participant)
	{
		RosterItem roster = lobbyManager.GetRoster(participant.Account.DisplayName);
		roster.SetupRosterItem(participant);
		rosterObjects.Add(roster);
	}

	public void RemoveRoster(RosterItem rosterItem)
	{
		if (rosterItem != null)
		{
			rosterObjects.Remove(rosterItem);
			return;
		}
		Debug.LogError("Trying to remove a participant that has no roster item.");
		Debug.LogError("Roster Object Count=" + rosterObjects.Count);
	}

	private void ClearAllRosters()
	{
		rosterObjects.Clear();
	}

	private void OnParticipantAdded(string userName, ChannelId channel, IParticipant participant)
	{
		if (SceneManager.GetActiveScene().name == "Menu")
		{
			if (!isTesting)
			{
				MonoBehaviour.print("notesting");
				Debug.Log("OnPartAdded: " + userName);
				AddParticipant(participant);
			}
			else
			{
				SetupTesting(participant);
			}
		}
	}

	private void OnParticipantRemoved(string userName, ChannelId channel, IParticipant participant)
	{
		Debug.Log("OnPartRemoved: " + participant.Account.DisplayName);
	}

	public void RemoveEvents()
	{
		if (!vivoxManager)
		{
			vivoxManager = VivoxVoiceManager.Instance;
		}
		vivoxManager.OnUserLoggedInEvent -= OnUserLoggedIn;
		vivoxManager.OnUserLoggedOutEvent -= OnUserLoggedOut;
		vivoxManager.OnParticipantAddedEvent -= OnParticipantAdded;
		vivoxManager.OnParticipantRemovedEvent -= OnParticipantRemoved;
	}

	public bool IsLoggedIn()
	{
		return vivoxManager.LoginState == LoginState.LoggedIn;
	}

	public void TestMicrophone()
	{
		isTesting = true;
		vivoxManager.JoinChannel("EchoTest" + Random.Range(0, 99999), ChannelType.Echo, VivoxVoiceManager.ChatCapability.AudioOnly);
		vivoxManager.OnParticipantAddedEvent += OnParticipantAdded;
	}

	public void StopTesting()
	{
		if (isTesting)
		{
			isTesting = false;
			DisconnectChannel();
		}
	}

	public void SetupTesting(IParticipant participant)
	{
		MonoBehaviour.print("setuptesting");
		Participant = participant;
		IsSpeaking = participant.SpeechDetected;
		participant.PropertyChanged += delegate(object obj, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "SpeechDetected")
			{
				IsSpeaking = participant.SpeechDetected;
			}
		};
		didSetuped = true;
	}

	private void Update()
	{
		if (!didSetuped || !isTesting || !Participant.IsSelf)
		{
			return;
		}
		if (OptionsManager.isPushToTalk)
		{
			if (KeyBindingManager.GetKeyDown(KeyAction.voiceChat))
			{
				vivoxManager.AudioInputDevices.Muted = false;
			}
			if (KeyBindingManager.GetKeyUp(KeyAction.voiceChat))
			{
				vivoxManager.AudioInputDevices.Muted = true;
			}
		}
		else if (vivoxManager.AudioInputDevices.Muted)
		{
			vivoxManager.AudioInputDevices.Muted = false;
		}
	}

	public void DisconnectFromDirectMenu()
	{
		vivoxManager.DisconnectAllChannels();
		vivoxManager.OnUserLoggedInEvent -= OnUserLoggedIn;
		vivoxManager.OnUserLoggedOutEvent -= OnUserLoggedOut;
		vivoxManager.OnParticipantAddedEvent -= OnParticipantAdded;
		vivoxManager.OnParticipantRemovedEvent -= OnParticipantRemoved;
	}
}
