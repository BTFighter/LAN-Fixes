using System;
using System.IO;
using UnityEngine;

// Token: 0x02000EA2 RID: 3746
public class T17NetConnectAgent : T17NetAgentBase
{
	// Token: 0x06005EC1 RID: 24257 RVA: 0x0003A845 File Offset: 0x00038A45
	public T17NetConnectAgent()
	{
	}

	// Token: 0x06005EC2 RID: 24258 RVA: 0x0007447E File Offset: 0x0007267E
	public virtual bool Start()
	{
		this.Connect();
		return true;
	}

	// Token: 0x06005EC3 RID: 24259 RVA: 0x00239B10 File Offset: 0x00237D10
	protected virtual void Connect()
	{
		bool flag = PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.SelfHosted || this.ShouldUseCustomServer();
		Debug.Log("T17PhotonNetworking: Should use custom server? " + flag.ToString());
		if (flag)
		{
			Debug.Log("T17PhotonNetworking: Using custom server configuration");
			if (Platform.GetInstance().IsReadyForPhoton())
			{
				T17NetManager.SetTimePingIntervalToConnectingRate();
				if (T17NetManager.NetOfflineMode)
				{
					PhotonNetwork.offlineMode = false;
					T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime = T17NetManager.RealTime;
					T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.DisconnectRetryDelay;
				}
				else if (!PhotonNetwork.connecting && !PhotonNetwork.connected)
				{
					this.ConnectToCustomServer();
					T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.Connecting;
				}
				T17NetConnectAgent.m_fStartConnectTime = T17NetManager.RealTime;
				return;
			}
		}
		else
		{
			CloudRegionCode cloudRegionCode = NetConnectAndJoinRoom.PhotonRegion;
			if (T17NetInvites.Region != CloudRegionCode.none)
			{
				cloudRegionCode = T17NetInvites.Region;
			}
			if (Platform.GetInstance().IsReadyForPhoton())
			{
				T17NetManager.SetTimePingIntervalToConnectingRate();
				if (T17NetManager.NetOfflineMode)
				{
					PhotonNetwork.offlineMode = false;
					T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime = T17NetManager.RealTime;
					T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.DisconnectRetryDelay;
				}
				else if (!PhotonNetwork.connecting && !PhotonNetwork.connected)
				{
					this.ConnectToRegion(cloudRegionCode);
					T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.Connecting;
				}
				else if (T17NetManager.IsConnectedOnline() && cloudRegionCode != CloudRegionCode.none && PhotonNetwork.networkingPeer.CloudRegion != cloudRegionCode)
				{
					PhotonNetwork.Disconnect();
					T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime = T17NetManager.RealTime;
					T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.DisconnectRetryDelay;
				}
				T17NetConnectAgent.m_fStartConnectTime = T17NetManager.RealTime;
			}
		}
	}

	// Token: 0x06005EC4 RID: 24260 RVA: 0x00239C48 File Offset: 0x00237E48
	private void ConnectToRegion(CloudRegionCode region)
	{
		Debug.Log("T17PhotonNetworking: Initialising photon connection TimePingInterval: " + PhotonNetwork.networkingPeer.TimePingInterval.ToString());
		this.SetAuthValues();
		if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.SelfHosted)
		{
			string serverAddress = PhotonNetwork.PhotonServerSettings.ServerAddress;
			int serverPort = PhotonNetwork.PhotonServerSettings.ServerPort;
			if (!string.IsNullOrEmpty(serverAddress))
			{
				Debug.Log("T17PhotonNetworking: Using custom Photon server: " + serverAddress + ":" + serverPort.ToString());
				PhotonNetwork.ConnectToMaster(serverAddress, serverPort, PhotonNetwork.PhotonServerSettings.AppID, T17NetConnectAndJoinRoom.Instance.AppVersion);
				return;
			}
		}
		string text = Path.Combine(Application.dataPath, "LANSettings.ini");
		string text2 = string.Empty;
		if (File.Exists(text))
		{
			if (T17NetConnectAgent.photonServerConfig == null)
			{
				T17NetConnectAgent.photonServerConfig = new IniParser(text);
			}
			text2 = T17NetConnectAgent.photonServerConfig.GetString("Server", "ServerAddress", string.Empty);
			int @int = T17NetConnectAgent.photonServerConfig.GetInt("Server", "ServerPort", 5055);
			if (!string.IsNullOrEmpty(text2))
			{
				Debug.Log("T17PhotonNetworking: Using custom Photon server: " + text2 + ":" + @int.ToString());
				PhotonNetwork.ConnectToMaster(text2, @int, PhotonNetwork.PhotonServerSettings.AppID, T17NetConnectAndJoinRoom.Instance.AppVersion);
				return;
			}
		}
		if (region == CloudRegionCode.none)
		{
			PhotonNetwork.ConnectUsingSettings(T17NetConnectAndJoinRoom.Instance.AppVersion);
			return;
		}
		PhotonNetwork.ConnectToRegion(region, T17NetConnectAndJoinRoom.Instance.AppVersion);
	}

	// Token: 0x06005EC5 RID: 24261 RVA: 0x0003A836 File Offset: 0x00038A36
	private void SetAuthValues()
	{
	}

	// Token: 0x06005EC6 RID: 24262 RVA: 0x00074487 File Offset: 0x00072687
	public virtual void OnDisconnected()
	{
		PhotonNetwork.Disconnect();
		T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.DisconnectRetryDelay;
		T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime = T17NetManager.RealTime;
	}

	// Token: 0x06005EC7 RID: 24263 RVA: 0x0007449E File Offset: 0x0007269E
	public virtual void OnConnectedToMaster()
	{
		T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.Idle;
	}

	// Token: 0x06005EC8 RID: 24264 RVA: 0x0003A836 File Offset: 0x00038A36
	public virtual void OnJoinedRoom()
	{
	}

	// Token: 0x06005EC9 RID: 24265 RVA: 0x0003A836 File Offset: 0x00038A36
	public virtual void OnLeftRoom()
	{
	}

	// Token: 0x06005ECA RID: 24266 RVA: 0x0003A836 File Offset: 0x00038A36
	public virtual void OnCreateRoomFailed()
	{
	}

	// Token: 0x06005ECB RID: 24267 RVA: 0x0003A836 File Offset: 0x00038A36
	public virtual void OnJoinedRoomFailed()
	{
	}

	// Token: 0x06005ECC RID: 24268 RVA: 0x000744A6 File Offset: 0x000726A6
	public virtual void OnPlatformReadyToConnect()
	{
		this.Connect();
	}

	// Token: 0x06005ECD RID: 24269 RVA: 0x00239DB0 File Offset: 0x00237FB0
	public virtual void Update()
	{
		T17NetConnectAgent.ConnectingState state = T17NetConnectAgent.m_State;
		if (state != T17NetConnectAgent.ConnectingState.Connecting)
		{
			if (state == T17NetConnectAgent.ConnectingState.DisconnectRetryDelay && T17NetManager.RealTime > T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime + 1f)
			{
				this.Connect();
				T17NetConnectAgent.m_fStartConnectTime = T17NetManager.RealTime;
				T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.Connecting;
				return;
			}
		}
		else if (PhotonNetwork.connecting && T17NetManager.RealTime > T17NetConnectAgent.m_fStartConnectTime + 10f)
		{
			PhotonNetwork.Disconnect();
			T17NetConnectAgent.m_State = T17NetConnectAgent.ConnectingState.DisconnectRetryDelay;
			T17NetConnectAgent.m_fDisconnectedRetryDelayStartTime = T17NetManager.RealTime;
		}
	}

	// Token: 0x06005ECE RID: 24270 RVA: 0x0003A836 File Offset: 0x00038A36
	static T17NetConnectAgent()
	{
	}

	// Token: 0x06005ECF RID: 24271 RVA: 0x00239E24 File Offset: 0x00238024
	private bool ShouldUseCustomServer()
	{
		string text = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
		Debug.Log("T17PhotonNetworking: Checking for config file at: " + text);
		if (File.Exists(text))
		{
			Debug.Log("T17PhotonNetworking: Config file found");
			if (T17NetConnectAgent.photonServerConfig == null)
			{
				T17NetConnectAgent.photonServerConfig = new IniParser(text);
			}
			if (!string.IsNullOrEmpty(T17NetConnectAgent.photonServerConfig.GetString("Server", "ServerAddress", string.Empty)))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06005ED0 RID: 24272 RVA: 0x00239EA0 File Offset: 0x002380A0
	private void ConnectToCustomServer()
	{
		Debug.Log("T17PhotonNetworking: Initialising photon connection TimePingInterval: " + PhotonNetwork.networkingPeer.TimePingInterval.ToString());
		this.SetAuthValues();
		string text = PhotonNetwork.PhotonServerSettings.ServerAddress;
		int num = PhotonNetwork.PhotonServerSettings.ServerPort;
		if (PhotonNetwork.PhotonServerSettings.HostType != ServerSettings.HostingOption.SelfHosted || string.IsNullOrEmpty(text))
		{
			string text2 = Path.Combine(Path.GetDirectoryName(Application.dataPath), "LANSettings.ini");
			if (File.Exists(text2))
			{
				if (T17NetConnectAgent.photonServerConfig == null)
				{
					T17NetConnectAgent.photonServerConfig = new IniParser(text2);
				}
				text = T17NetConnectAgent.photonServerConfig.GetString("Server", "ServerAddress", string.Empty);
				num = T17NetConnectAgent.photonServerConfig.GetInt("Server", "ServerPort", 5055);
				if (!string.IsNullOrEmpty(text))
				{
					PhotonNetwork.PhotonServerSettings.UseMyServer(text, num, "master");
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			Debug.Log("T17PhotonNetworking: Using custom Photon server: " + text + ":" + num.ToString());
			PhotonNetwork.ConnectToMaster(text, num, PhotonNetwork.PhotonServerSettings.AppID, T17NetConnectAndJoinRoom.Instance.AppVersion);
			return;
		}
		Debug.LogError("T17PhotonNetworking: No custom server address configured");
	}

	// Token: 0x04004F49 RID: 20297
	private static float m_fStartConnectTime;

	// Token: 0x04004F4A RID: 20298
	private static float m_fDisconnectedRetryDelayStartTime;

	// Token: 0x04004F4B RID: 20299
	private static T17NetConnectAgent.ConnectingState m_State;

	// Token: 0x04004F4C RID: 20300
	private static IniParser photonServerConfig;

	// Token: 0x02000EA3 RID: 3747
	private enum ConnectingState
	{
		// Token: 0x04004F4E RID: 20302
		Idle,
		// Token: 0x04004F4F RID: 20303
		Connecting,
		// Token: 0x04004F50 RID: 20304
		DisconnectRetryDelay
	}
}