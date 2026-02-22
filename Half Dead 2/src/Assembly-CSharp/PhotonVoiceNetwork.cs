using System;
using System.Linq;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using ExitGames.Client.Photon.Voice;
using UnityEngine;

[DisallowMultipleComponent]
public class PhotonVoiceNetwork : MonoBehaviour
{
	private static PhotonVoiceNetwork _instance;

	private static object instanceLock = new object();

	private static bool destroyed = false;

	public static float BackgroundTimeout = 60f;

	internal UnityVoiceFrontend client;

	private string unityMicrophoneDevice;

	private int photonMicrophoneDeviceID = -1;

	private AudioInEnumerator photonMicEnumerator = new AudioInEnumerator();

	internal static PhotonVoiceNetwork instance
	{
		get
		{
			lock (instanceLock)
			{
				if (destroyed)
				{
					return null;
				}
				if (_instance == null)
				{
					PhotonVoiceNetwork photonVoiceNetwork = UnityEngine.Object.FindObjectOfType<PhotonVoiceNetwork>();
					if (photonVoiceNetwork != null)
					{
						_instance = photonVoiceNetwork;
					}
					else
					{
						GameObject gameObject = new GameObject();
						_instance = gameObject.AddComponent<PhotonVoiceNetwork>();
						gameObject.name = "PhotonVoiceNetworkSingleton";
						UnityEngine.Object.DontDestroyOnLoad(gameObject);
					}
				}
				return _instance;
			}
		}
		set
		{
			lock (instanceLock)
			{
				if (_instance != null && value != null)
				{
					if (_instance.GetInstanceID() != value.GetInstanceID())
					{
						Debug.LogErrorFormat("PUNVoice: Destroying a duplicate instance of PhotonVoiceNetwork as only one is allowed.");
						UnityEngine.Object.Destroy(value);
					}
				}
				else
				{
					_instance = value;
				}
			}
		}
	}

	public static Func<PhotonVoiceRecorder, IAudioSource> AudioSourceFactory { get; set; }

	public static UnityVoiceFrontend Client => instance.client;

	public static VoiceClient VoiceClient => instance.client.VoiceClient;

	public static ExitGames.Client.Photon.LoadBalancing.ClientState ClientState => instance.client.State;

	public static string CurrentRoomName => (instance.client.CurrentRoom != null) ? instance.client.CurrentRoom.Name : string.Empty;

	public static AudioInEnumerator PhotonMicrophoneEnumerator => instance.photonMicEnumerator;

	public static string MicrophoneDevice
	{
		get
		{
			return instance.unityMicrophoneDevice;
		}
		set
		{
			if (value != null && !Microphone.devices.Contains(value))
			{
				Debug.LogError("PUNVoice: " + value + " is not a valid microphone device");
				return;
			}
			instance.unityMicrophoneDevice = value;
			if (PhotonVoiceSettings.Instance.DebugInfo)
			{
				Debug.LogFormat("PUNVoice: Setting global Unity microphone device to {0}", instance.unityMicrophoneDevice);
			}
			PhotonVoiceRecorder[] array = UnityEngine.Object.FindObjectsOfType<PhotonVoiceRecorder>();
			foreach (PhotonVoiceRecorder photonVoiceRecorder in array)
			{
				if (photonVoiceRecorder.photonView.isMine && photonVoiceRecorder.MicrophoneDevice == null)
				{
					photonVoiceRecorder.MicrophoneDevice = null;
				}
			}
		}
	}

	public static int PhotonMicrophoneDeviceID
	{
		get
		{
			return instance.photonMicrophoneDeviceID;
		}
		set
		{
			if (!PhotonMicrophoneEnumerator.IDIsValid(value))
			{
				Debug.LogError("PUNVoice: " + value + " is not a valid Photon microphone device");
				return;
			}
			instance.photonMicrophoneDeviceID = value;
			if (PhotonVoiceSettings.Instance.DebugInfo)
			{
				Debug.LogFormat("PUNVoice: Setting global Photon microphone device to {0}", instance.photonMicrophoneDeviceID);
			}
			PhotonVoiceRecorder[] array = UnityEngine.Object.FindObjectsOfType<PhotonVoiceRecorder>();
			foreach (PhotonVoiceRecorder photonVoiceRecorder in array)
			{
				if (photonVoiceRecorder.photonView.isMine && photonVoiceRecorder.PhotonMicrophoneDeviceID == -1)
				{
					photonVoiceRecorder.PhotonMicrophoneDeviceID = -1;
				}
			}
		}
	}

	private PhotonVoiceNetwork()
	{
		client = new UnityVoiceFrontend(ConnectionProtocol.Udp);
	}

	private void OnDestroy()
	{
		if (!(this != _instance))
		{
			destroyed = true;
			photonMicEnumerator.Dispose();
			client.Dispose();
		}
	}

	[RuntimeInitializeOnLoadMethod]
	public static void RuntimeInitializeOnLoad()
	{
		if (Microphone.devices.Length < 1)
		{
			Debug.LogError("PUNVoice: No microphone device found");
		}
	}

	private void Awake()
	{
		instance = this;
	}

	public static bool Connect()
	{
		// Check if custom server settings are provided in config.ini
		string customServerAddress = GetCustomServerAddress();
		int customVoiceServerPort = GetCustomVoiceServerPort();

		if (!string.IsNullOrEmpty(customServerAddress))
		{
			UnityEngine.Debug.Log("Using custom voice server settings from config.ini: " + customServerAddress + ":" + customVoiceServerPort);
			string masterServerAddress = $"{customServerAddress}:{customVoiceServerPort}";
			return instance.client.Connect(masterServerAddress, null, null, PhotonNetwork.player.NickName, new ExitGames.Client.Photon.LoadBalancing.AuthenticationValues(PhotonNetwork.player.UserId));
		}

		if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.SelfHosted)
		{
			string masterServerAddress = $"{PhotonNetwork.PhotonServerSettings.ServerAddress}:{PhotonNetwork.PhotonServerSettings.VoiceServerPort}";
			return instance.client.Connect(masterServerAddress, null, null, PhotonNetwork.player.NickName, new ExitGames.Client.Photon.LoadBalancing.AuthenticationValues(PhotonNetwork.player.UserId));
		}
		instance.client.AppId = PhotonNetwork.PhotonServerSettings.VoiceAppID;
		instance.client.AppVersion = PhotonNetwork.gameVersion;
		return instance.client.ConnectToRegionMaster(PhotonNetwork.networkingPeer.CloudRegion.ToString());
	}

	private static string GetCustomServerAddress()
	{
		try
		{
			string gameDirectory = System.IO.Path.GetDirectoryName(Application.dataPath);
			string configPath = System.IO.Path.Combine(gameDirectory, "LANSettings.ini");
			if (System.IO.File.Exists(configPath))
			{
				IniParser parser = new IniParser();
				parser.Load(configPath);
				return parser.GetValue("Server", "ServerAddress", string.Empty);
			}
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogWarning("Failed to read custom server address from LANSettings.ini: " + e.Message);
		}
		return string.Empty;
	}

	private static int GetCustomVoiceServerPort()
	{
		try
		{
			string gameDirectory = System.IO.Path.GetDirectoryName(Application.dataPath);
			string configPath = System.IO.Path.Combine(gameDirectory, "LANSettings.ini");
			if (System.IO.File.Exists(configPath))
			{
				IniParser parser = new IniParser();
				parser.Load(configPath);
				int port = parser.GetIntValue("Server", "VoiceServerPort", 0);
				// If voice server port not specified, use default or main server port
				if (port == 0)
				{
					port = parser.GetIntValue("Server", "ServerPort", PhotonNetwork.PhotonServerSettings.VoiceServerPort);
				}
				return port;
			}
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogWarning("Failed to read custom voice server port from LANSettings.ini: " + e.Message);
		}
		return PhotonNetwork.PhotonServerSettings.VoiceServerPort;
	}

	public static void Disconnect()
	{
		instance.client.Disconnect();
	}

	protected void OnEnable()
	{
		if (!(this != _instance))
		{
		}
	}

	protected void OnApplicationQuit()
	{
		if (!(this != _instance))
		{
			client.Disconnect();
			client.Dispose();
		}
	}

	protected void Update()
	{
		if (!(this != _instance))
		{
			client.VoiceClient.DebugLostPercent = PhotonVoiceSettings.Instance.DebugLostPercent;
			client.Service();
		}
	}

	private void OnJoinedRoom()
	{
		if (!(this != _instance) && (PhotonVoiceSettings.Instance.WorkInOfflineMode || !PhotonNetwork.offlineMode) && PhotonVoiceSettings.Instance.AutoConnect)
		{
			ExitGames.Client.Photon.LoadBalancing.ClientState state = client.State;
			if (state == ExitGames.Client.Photon.LoadBalancing.ClientState.Joined)
			{
				client.OpLeaveRoom();
			}
			else
			{
				client.Reconnect();
			}
		}
	}

	private void OnLeftRoom()
	{
		if (!(this != _instance) && (PhotonVoiceSettings.Instance.WorkInOfflineMode || !PhotonNetwork.offlineMode) && PhotonVoiceSettings.Instance.AutoDisconnect)
		{
			client.Disconnect();
		}
	}

	private void OnDisconnectedFromPhoton()
	{
		if (!(this != _instance) && (PhotonVoiceSettings.Instance.WorkInOfflineMode || !PhotonNetwork.offlineMode) && PhotonVoiceSettings.Instance.AutoDisconnect)
		{
			client.Disconnect();
		}
	}

	internal static void LinkSpeakerToRemoteVoice(PhotonVoiceSpeaker speaker)
	{
		instance.client.LinkSpeakerToRemoteVoice(speaker);
	}

	internal static void UnlinkSpeakerFromRemoteVoice(PhotonVoiceSpeaker speaker)
	{
		if (!destroyed)
		{
			instance.client.UnlinkSpeakerFromRemoteVoice(speaker);
		}
	}
}
