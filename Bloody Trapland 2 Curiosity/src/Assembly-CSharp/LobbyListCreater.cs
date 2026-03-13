using System;
using System.Collections.Generic;
using Afterengine.Game;
using AfterShockUnity;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListCreater : MonoBehaviour
{
	private static LobbyListCreater Instance;

	public Text CurrentRegionText;

	public ControllerButton JoinButton;

	public ServerInfoHolder infoHolder;

	public float LobbyGameDownOffset = 25f;

	public int counter;

	public ControllerHandler controller;

	public ControllerHandler controllerMenu;

	public List<GameObject> allHosts;

	public KTimer RTimer = new KTimer(1f);

	public Text playerOnlineName;

	public Text PlayersOnline;

	public string RoomName;

	public Text Region;

	public List<TraplandRegions> regions;

	public int CurrentRegion;

	public string RegionString;

	public static string GlobalCurrentRegion;

	public static string CurrentRegionJoinRoomText;

	private bool First;

	private Delegator connectToRegionTimer;

	private int RandomNumber;

	public CloudRegionCode region;

	public string NameToJoin;

	private int m_currentRegion = -1;

	private static string ConnectOnDone;

	private void Awake()
	{
		Instance = this;
		((Behaviour)this).enabled = false;
	}

	private void Start()
	{
		RandomNumber = 3;
		NetworkManager.ConnectedToMaster = Connected;
		GameNetworkManager.JoinStandardLobby();
		((Component)infoHolder).gameObject.SetActive(false);
		GameNetworkManager.Instance.GetRoomList = DrawRoomList;
		RTimer.StartTimer();
		playerOnlineName.text = PlayerPrefs.GetString("Player Nick", "Player");
		SetRegion(PhotonNetwork.networkingPeer.CloudRegion);
		if (playerOnlineName.text == "Player")
		{
			ChangeName();
		}
		Refresh();
	}

	public void SetRegion(CloudRegionCode code)
	{
		for (int i = 0; i < regions.Count; i++)
		{
			if (regions[i].code == code)
			{
				region = regions[i].code;
				string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + regions[i].Name.ToUpper());
				CurrentRegion = i;
				break;
			}
		}
	}

	private void Connected()
	{
		Refresh();
	}

	public static void SetRegion(string code)
	{
		if ((Object)(object)Instance != (Object)null)
		{
			if (!string.IsNullOrEmpty(code))
			{
				Instance.SetRegion((CloudRegionCode)Enum.Parse(typeof(CloudRegionCode), code, ignoreCase: true));
			}
			Instance.ConnectToRegion();
		}
	}

	public static void ForceUpdateRegionlabel(CloudRegionCode regioncode)
	{
		if (!((Object)(object)Instance != (Object)null))
		{
			return;
		}
		TraplandRegions traplandRegions = null;
		for (int i = 0; i < Instance.regions.Count; i++)
		{
			if (Instance.regions[i].code == regioncode)
			{
				traplandRegions = Instance.regions[i];
				break;
			}
		}
		if (traplandRegions != null && Instance.regions.Contains(traplandRegions))
		{
			int currentRegion = Instance.regions.IndexOf(traplandRegions);
			Instance.CurrentRegion = currentRegion;
			Instance.m_currentRegion = Instance.CurrentRegion;
			string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + Instance.regions[Instance.m_currentRegion].Name.ToUpper());
			Instance.CurrentRegionText.text = ((!string.IsNullOrEmpty(localStringIFExits)) ? localStringIFExits : Instance.regions[Instance.m_currentRegion].Name);
			string text = Instance.RegionString + ": " + Instance.regions[Instance.m_currentRegion].code;
			Instance.Region.text = text;
			CurrentRegionJoinRoomText = text;
		}
		Instance.Refresh(RunCleen: true);
	}

	private void ConnectToRegion()
	{
		Debug.Log((object)"connect");
		InGameNetworkManager.SafeLeave = true;
		PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.PhotonCloud;
		PhotonNetwork.PhotonServerSettings.PreferredRegion = regions[CurrentRegion].code;
		string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + regions[CurrentRegion].Name.ToUpper());
		PhotonNetwork.Disconnect();
	}

	private void OnEnable()
	{
		m_currentRegion = -1;
		if (!PhotonNetwork.connectedAndReady)
		{
			NetworkManager.Connect();
			GameNetworkManager.JoinStandardLobby();
		}
		else
		{
			GameNetworkManager.JoinStandardLobby();
		}
	}

	private void SetText()
	{
		string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + regions[CurrentRegion].Name.ToUpper());
		CurrentRegionText.text = ((!string.IsNullOrEmpty(localStringIFExits)) ? localStringIFExits : regions[CurrentRegion].Name);
		string text = RegionString + ": " + regions[CurrentRegion].code;
		Region.text = text;
		CurrentRegionJoinRoomText = text;
	}

	private void ReConnectNow()
	{
		GameNetworkManager.Instance.PlayerDissconetedFromPhoton = null;
	}

	public void ChangeRegion()
	{
		Cleen();
		CurrentRegion++;
		if (CurrentRegion >= regions.Count)
		{
			CurrentRegion = 0;
		}
		string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + regions[CurrentRegion].Name.ToUpper());
		if (connectToRegionTimer != null)
		{
			connectToRegionTimer.Remove();
		}
		NetworkManager.IsConnectedToMasterState = 0;
		connectToRegionTimer = _Timer.AddDelegate(delegate
		{
			ConnectToRegion();
		}, 1.5f);
	}

	private void DrawRoomList(RoomInfo[] RoomInfoList)
	{
		if (ConnectOnDone != string.Empty)
		{
			if ((Object)(object)controller != (Object)null)
			{
				((Behaviour)controller).enabled = false;
			}
			if ((Object)(object)controllerMenu != (Object)null)
			{
				((Behaviour)controllerMenu).enabled = false;
			}
			StartSceneHandler.JoinRoom(ulong.Parse(ConnectOnDone).ToString());
			ConnectOnDone = string.Empty;
		}
		else
		{
			if ((Object)(object)PlayersOnline == (Object)null)
			{
				return;
			}
			if (GameDebugHandler.DebugMode)
			{
				RandomNumber = 0;
			}
			else
			{
				RandomNumber = Random.RandomRange(3, 5);
			}
			int num = PhotonNetwork.countOfPlayers + RandomNumber;
			PlayersOnline.text = num.ToString();
			Cleen();
			if (RoomInfoList.Length <= 0)
			{
				JoinButton.CantPress = true;
			}
			else
			{
				JoinButton.CantPress = false;
			}
			foreach (RoomInfo roomInfo in RoomInfoList)
			{
				bool locked = false;
				if (roomInfo.CustomProperties[(object)"private"] != null)
				{
					locked = roomInfo.CustomProperties[(object)"private"].ToString() == "1";
				}
				int result = PhotonNetwork.GetPing();
				if (roomInfo.CustomProperties[(object)"ping"] != null)
				{
					string text = roomInfo.CustomProperties[(object)"ping"].ToString();
					if (!string.IsNullOrEmpty(text))
					{
						int.TryParse(text, out result);
					}
				}
				if (roomInfo.CustomProperties[(object)"count"] != null && roomInfo.CustomProperties[(object)"gameStatus"] != null && roomInfo.CustomProperties[(object)"gameName"] != null)
				{
					Create(result, roomInfo.Name, roomInfo.CustomProperties[(object)"gameName"].ToString(), roomInfo.CustomProperties[(object)"count"].ToString(), roomInfo.CustomProperties[(object)"gameStatus"].ToString(), locked);
				}
				else
				{
					Create(result, roomInfo.Name, "Game", "0", "Lobby", locked);
				}
			}
		}
	}

	public void ChangeName()
	{
	}

	public void OnChangeDone(string name)
	{
		InputHandler instance = InputHandler.Instance;
		instance.ChangeDone = (Action<string>)Delegate.Remove(instance.ChangeDone, new Action<string>(OnChangeDone));
		playerOnlineName.text = name;
		PlayerPrefs.SetString("Player Nick", name);
		((Behaviour)controllerMenu).enabled = true;
	}

	public static void ChangeTempRegionJoinGame()
	{
		ChangeRegionJoinGame(Instance.region.ToString(), Instance.NameToJoin);
	}

	public static void ChangeRegionJoinGame(string regionName, string serverName)
	{
		if (string.IsNullOrEmpty(regionName) || regionName.ToLower() == "none" || regionName.ToLower() == "selfhosted")
		{
			Debug.Log((object)"[LobbyListCreater] Self-hosted server detected, joining without region change");
			ConnectOnDone = serverName;
			NetworkManager.ApplyNetworkConfigurationStatic();
			if ((Object)(object)Instance != (Object)null)
			{
				((Behaviour)Instance).enabled = true;
			}
			return;
		}
		if (regionName != PhotonNetwork.networkingPeer.CloudRegion.ToString())
		{
			SetRegion(regionName);
		}
		ConnectOnDone = serverName;
		if ((Object)(object)Instance != (Object)null)
		{
			((Behaviour)Instance).enabled = true;
			return;
		}
		_Timer.AddDelegate(delegate
		{
			if ((Object)(object)Instance != (Object)null)
			{
				((Behaviour)Instance).enabled = true;
			}
		});
	}

	private void Update()
	{
		if (m_currentRegion != CurrentRegion)
		{
			m_currentRegion = CurrentRegion;
			string localStringIFExits = GameLocalization.GetLocalStringIFExits("REGION_" + regions[m_currentRegion].Name.ToUpper());
			CurrentRegionText.text = ((!string.IsNullOrEmpty(localStringIFExits)) ? localStringIFExits : regions[m_currentRegion].Name);
			string text = RegionString + ": " + regions[m_currentRegion].code;
			Region.text = text;
			CurrentRegionJoinRoomText = text;
		}
		if (RTimer.Update())
		{
			GameNetworkManager.Instance.GetRoomList = DrawRoomList;
			GameNetworkManager.JoinStandardLobby();
			RTimer.StartTimer();
		}
	}

	public void Refresh(bool RunCleen = false)
	{
		if (RunCleen)
		{
			Cleen();
		}
		GameNetworkManager.Instance.GetRoomList = DrawRoomList;
		GameNetworkManager.JoinStandardLobby();
	}

	private void Cleen()
	{
		counter = 0;
		controller.buttons.Clear();
		while (allHosts.Count > 0)
		{
			if ((Object)(object)allHosts[0] != (Object)null)
			{
				Object.Destroy((Object)(object)allHosts[0].gameObject);
			}
			allHosts.RemoveAt(0);
		}
	}

	private void Create(int ping, string gameName, string name, string count, string levelName, bool locked)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(name))
		{
			name = string.Empty;
		}
		if (string.IsNullOrEmpty(count))
		{
			count = "0";
		}
		if (string.IsNullOrEmpty(levelName))
		{
			levelName = "Lobby";
		}
		GameObject val = Object.Instantiate<GameObject>(((Component)infoHolder).gameObject);
		val.SetActive(true);
		val.transform.parent = ((Component)this).transform;
		val.transform.localPosition = LobbyGameDownOffset * Vector3.down * (float)counter;
		val.GetComponent<ServerInfoHolder>().SetInfo(ping, gameName, name, count, levelName, locked);
		if (counter == 0)
		{
			controller.OnEnableStarterButton = val.GetComponent<ControllerButton>();
		}
		controller.buttons.Add(val.GetComponent<ControllerButton>());
		counter++;
		allHosts.Add(val);
	}

	static LobbyListCreater()
	{
		ConnectOnDone = string.Empty;
	}
}
