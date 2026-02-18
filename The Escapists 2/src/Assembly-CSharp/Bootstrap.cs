using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using GameAnalytics;
using UnityEngine;

// Token: 0x02000B83 RID: 2947
public class Bootstrap : MonoBehaviour
{
	// Token: 0x060049C3 RID: 18883 RVA: 0x0003B605 File Offset: 0x00039805
	public Bootstrap()
	{
	}

	// Token: 0x17000A4F RID: 2639
	// (get) Token: 0x060049C4 RID: 18884 RVA: 0x0006904C File Offset: 0x0006724C
	public static Bootstrap Instance
	{
		get
		{
			return Bootstrap.m_instance;
		}
	}

	// Token: 0x060049C5 RID: 18885 RVA: 0x00069053 File Offset: 0x00067253
	protected virtual void OnDestroy()
	{
		if (Bootstrap.m_instance == this)
		{
			Bootstrap.m_instance = null;
			if (Bootstrap.m_netGlobalRoomViewGO != null)
			{
				Object.Destroy(Bootstrap.m_netGlobalRoomViewGO);
				Bootstrap.m_netGlobalRoomViewGO = null;
			}
		}
	}

	// Token: 0x060049C6 RID: 18886 RVA: 0x001B15D8 File Offset: 0x001AF7D8
	private void ProcessCommandLine()
	{
		if (Bootstrap.m_sHasProcessedCommandLine)
		{
			return;
		}
		Bootstrap.m_sHasProcessedCommandLine = true;
		string[] commandLineArgs = this.GetCommandLineArgs();
		if (commandLineArgs != null)
		{
			string text = string.Join(" ", commandLineArgs);
			Debug.LogFormat("Bootstrap.ProcessCommandLine - Processing command line \"{0}\"", new object[] { text });
			Debug.Log(" *1* command line " + text);
			string[] array = null;
			if (text.Contains("-GoCommando:"))
			{
				string text2 = string.Empty;
				try
				{
					text2 = commandLineArgs.Where((string row) => row.Contains("-GoCommando:")).Single<string>();
					text2 = text2.Replace("-GoCommando:", string.Empty);
					Debug.LogFormat("Bootstrap.ProcessCommandLine - Found GoCommando directive \"{0}\"", new object[] { text2 });
					array = this.ReadCommandoArgsFromFile(string.Format("{0}/Resources/GoCommando/{1}", Application.dataPath, text2));
					goto IL_00E1;
				}
				catch (Exception)
				{
					goto IL_00E1;
				}
			}
			if (text.Contains("-Commando:"))
			{
				array = this.ReadCommandoArgsFromCommandLine(commandLineArgs);
			}
			IL_00E1:
			if (array != null)
			{
				string text3 = " *2* command line ";
				string[] array2 = array;
				Debug.Log(text3 + ((array2 != null) ? array2.ToString() : null));
				if (new Commando(Bootstrap.CmdLineOptions, "-Commando:", "-GoCommando:").ParseCommandLine(array))
				{
					this.ApplyCommandLineOptions();
					return;
				}
				Application.Quit();
			}
		}
	}

	// Token: 0x060049C7 RID: 18887 RVA: 0x00069085 File Offset: 0x00067285
	private string[] GetCommandLineArgs()
	{
		return Environment.GetCommandLineArgs();
	}

	// Token: 0x060049C8 RID: 18888 RVA: 0x001B1720 File Offset: 0x001AF920
	private string[] ReadCommandoArgsFromCommandLine(string[] args)
	{
		string[] array = null;
		string text = string.Empty;
		try
		{
			text = args.Where((string row) => row.Contains("-Commando:")).Single<string>();
		}
		catch (Exception)
		{
		}
		if (!string.IsNullOrEmpty(text))
		{
			text = text.Replace("-Commando:", string.Empty);
			Debug.LogFormat("Bootstrap.ReadCommandoArgsFromCommandLine - Found Commando args \"{0}\"", new object[] { text });
			array = text.Split(new char[] { ';' });
		}
		return array;
	}

	// Token: 0x060049C9 RID: 18889 RVA: 0x001B17B8 File Offset: 0x001AF9B8
	private string[] ReadCommandoArgsFromFile(string fullPath)
	{
		string[] array = null;
		if (File.Exists(fullPath))
		{
			List<string> list = new List<string>();
			using (StreamReader streamReader = File.OpenText(fullPath))
			{
				string text = streamReader.ReadLine();
				while (!string.IsNullOrEmpty(text))
				{
					if (!text.StartsWith("#"))
					{
						list.Add(text);
					}
					text = streamReader.ReadLine();
				}
			}
			array = list.ToArray();
		}
		else
		{
			Debug.LogErrorFormat("Bootstrap.ProcessCommandLine - Cannot find file \"{0}\"", new object[] { fullPath });
		}
		return array;
	}

	// Token: 0x060049CA RID: 18890 RVA: 0x001B1844 File Offset: 0x001AFA44
	private void ApplyCommandLineOptions()
	{
		Debug.Log("   *******  ApplyCommandLineOptions");
		if (Bootstrap.CmdLineOptions.ActiveLogGroups.Count > 0)
		{
			DebugHelpers.ClearActiveLogGroups();
			foreach (DebugHelpers.LogGroup logGroup in Bootstrap.CmdLineOptions.ActiveLogGroups)
			{
				DebugHelpers.LogGroupActive(logGroup, true);
			}
		}
		if (Bootstrap.CmdLineOptions.ActiveLogNetGroups.Count > 0)
		{
			DebugHelpers.ClearActiveLogNetGroups();
			foreach (DebugHelpers.LogNetGroup logNetGroup in Bootstrap.CmdLineOptions.ActiveLogNetGroups)
			{
				DebugHelpers.LogGroupActive(logNetGroup, true);
			}
		}
		if (Bootstrap.CmdLineOptions.AnalyticsTrackers.Count > 0)
		{
			GAHelpers.ActiveTrackerHelper.ClearActiveTrackers();
			foreach (NetAnalytics.Tracker tracker in Bootstrap.CmdLineOptions.AnalyticsTrackers)
			{
				GAHelpers.TrackerActive(tracker, true);
			}
		}
		if (Bootstrap.CmdLineOptions.AnalyticsEvents.Count > 0)
		{
			GAHelpers.ActiveCategoriesHelper.ClearActiveCategories();
			foreach (NetAnalytics.EventCategory eventCategory in Bootstrap.CmdLineOptions.AnalyticsEvents)
			{
				GAHelpers.CategoryActive(eventCategory, true);
			}
		}
		if (Bootstrap.CmdLineOptions.ActivePrefixes.Count > 0)
		{
			DebugHelpers.ClearActivePrefixes();
			foreach (DebugHelpers.Prefix prefix in Bootstrap.CmdLineOptions.ActivePrefixes)
			{
				DebugHelpers.PrefixActive(prefix, true);
			}
		}
		PhotonPeer networkingPeer = PhotonNetwork.networkingPeer;
		if (networkingPeer != null)
		{
			T17NetPhotonLagSimulationGui.Instance.NetLagSimulationGuiOn = Bootstrap.CmdLineOptions.NetLagSimulationGuiEnabled;
			networkingPeer.IsSimulationEnabled = Bootstrap.CmdLineOptions.NetLagSimulationEnabled;
			float num = Mathf.Clamp(Bootstrap.CmdLineOptions.NetLagSimulationLag, 0f, 500f);
			networkingPeer.NetworkSimulationSettings.IncomingLag = (int)num;
			networkingPeer.NetworkSimulationSettings.OutgoingLag = (int)num;
			float num2 = Mathf.Clamp(Bootstrap.CmdLineOptions.NetLagSimulationJitter, 0f, 100f);
			networkingPeer.NetworkSimulationSettings.IncomingJitter = (int)num2;
			networkingPeer.NetworkSimulationSettings.OutgoingJitter = (int)num2;
			float num3 = Mathf.Clamp(Bootstrap.CmdLineOptions.NetLagSimulationLoss, 0f, 10f);
			networkingPeer.NetworkSimulationSettings.IncomingLossPercentage = (int)num3;
			networkingPeer.NetworkSimulationSettings.OutgoingLossPercentage = (int)num3;
		}
		T17NetConfig.NetPhotonRpcTTY = Bootstrap.CmdLineOptions.NetConfigRpcTTY;
		T17NetConfig.NetDebugGuiPanel = Bootstrap.CmdLineOptions.NetDebugGuiPanel;
		if (Bootstrap.CmdLineOptions.NetDrawPlayerPos)
		{
			DebugHelpers.LogGroupActive(DebugHelpers.LogNetGroup.NetworkPlayerPos, true);
		}
		if (!string.IsNullOrEmpty(Bootstrap.CmdLineOptions.TestScript))
		{
			TestScript testScript = TestScript.Instance;
			if (testScript == null)
			{
				Bootstrap.TestScriptGO = new GameObject("TestScript");
				if (Bootstrap.TestScriptGO != null)
				{
					testScript = Bootstrap.TestScriptGO.AddComponent<TestScript>();
					Object.DontDestroyOnLoad(Bootstrap.TestScriptGO);
				}
			}
			if (testScript != null)
			{
				base.StartCoroutine(this.PerformTestScriptAfterDelay(5f));
			}
		}
		if (Bootstrap.CmdLineOptions.Quit > 0)
		{
			Bootstrap.m_QuitTimer = (float)Bootstrap.CmdLineOptions.Quit;
			Debug.Log("   *******  ApplyCommandLineOptions      QUIT  " + Bootstrap.m_QuitTimer.ToString());
		}
		if (Bootstrap.CmdLineOptions.QuitExitCode > 0)
		{
			Bootstrap.m_QuitTimerExitCode = Bootstrap.CmdLineOptions.QuitExitCode;
			Debug.Log("   *******  ApplyCommandLineOptions      QuitExitCode  " + Bootstrap.m_QuitTimerExitCode.ToString());
		}
		if (Bootstrap.CmdLineOptions.LoadLevel != string.Empty)
		{
			Bootstrap.m_LoadLevel = Bootstrap.CmdLineOptions.LoadLevel;
			Debug.Log("   *******  ApplyCommandLineOptions      LoadLevel  " + Bootstrap.m_LoadLevel);
		}
		if (Bootstrap.CmdLineOptions.LoadLevelConfigID >= 0)
		{
			Bootstrap.m_LoadLevelConfigID = Bootstrap.CmdLineOptions.LoadLevelConfigID;
			Debug.Log("   *******  ApplyCommandLineOptions      LoadLevelConfigID  " + Bootstrap.m_LoadLevel);
		}
		if (!string.IsNullOrEmpty(Bootstrap.CmdLineOptions.RequestConnectionState))
		{
			object obj = Enum.Parse(typeof(NetConnectionState), Bootstrap.CmdLineOptions.RequestConnectionState, true);
			if (obj != null)
			{
				Bootstrap.m_StartupConnectionState = (NetConnectionState)obj;
			}
		}
		if (!string.IsNullOrEmpty(Bootstrap.CmdLineOptions.VersionOverride))
		{
			GlobalStart.m_VersionString = Bootstrap.CmdLineOptions.VersionOverride;
		}
		if (!string.IsNullOrEmpty(Bootstrap.CmdLineOptions.PhotonServerAddress))
		{
			Debug.Log("   *******  ApplyCommandLineOptions      ServerAddress  " + Bootstrap.CmdLineOptions.PhotonServerAddress);
			Debug.Log("   *******  ApplyCommandLineOptions      ServerPort  " + Bootstrap.CmdLineOptions.PhotonServerPort.ToString());
			PhotonNetwork.PhotonServerSettings.UseMyServer(Bootstrap.CmdLineOptions.PhotonServerAddress, Bootstrap.CmdLineOptions.PhotonServerPort, "master");
			return;
		}
		string text = Path.Combine(Application.dataPath, "LANSettings.ini");
		if (File.Exists(text))
		{
			IniParser iniParser = new IniParser(text);
			string @string = iniParser.GetString("Server", "ServerAddress", string.Empty);
			int @int = iniParser.GetInt("Server", "ServerPort", 5055);
			if (!string.IsNullOrEmpty(@string))
			{
				Debug.Log("   *******  ApplyCommandLineOptions      ServerAddress  " + @string);
				Debug.Log("   *******  ApplyCommandLineOptions      ServerPort  " + @int.ToString());
				PhotonNetwork.PhotonServerSettings.UseMyServer(@string, @int, "master");
			}
		}
	}

	// Token: 0x060049CB RID: 18891 RVA: 0x0006908C File Offset: 0x0006728C
	private IEnumerator PerformTestScriptAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		Bootstrap.PerformTestScript(Bootstrap.CmdLineOptions.TestScript);
		yield break;
	}

	// Token: 0x060049CC RID: 18892 RVA: 0x0006909B File Offset: 0x0006729B
	internal static void PerformTestScript(string scriptName)
	{
		TestScript.Instance.Execute(scriptName, new TestScript.OnTestScriptCompleteDelegate(Bootstrap.TestScriptCompleteCallback));
	}

	// Token: 0x060049CD RID: 18893 RVA: 0x000690B4 File Offset: 0x000672B4
	private static void TestScriptCompleteCallback(TestScript.ResultCode result)
	{
		if (result == TestScript.ResultCode.Success)
		{
			Debug.Log("Bootstrap.TestScriptCompleteCallback - SUCCESS.");
			return;
		}
		Debug.LogErrorFormat("Bootstrap.TestScriptCompleteCallback - FAILED, returnValue = {0}", new object[] { result.ToString() });
	}

	// Token: 0x17000A50 RID: 2640
	// (get) Token: 0x060049CE RID: 18894 RVA: 0x000690E4 File Offset: 0x000672E4
	// (set) Token: 0x060049CF RID: 18895 RVA: 0x000690EB File Offset: 0x000672EB
	public static bool ApplicationHasFocus
	{
		get
		{
			return Bootstrap.m_applicationHasFocus;
		}
		private set
		{
			Bootstrap.m_applicationHasFocus = value;
		}
	}

	// Token: 0x060049D0 RID: 18896 RVA: 0x000690F3 File Offset: 0x000672F3
	public void OnApplicationFocus(bool focus)
	{
		Bootstrap.ApplicationHasFocus = focus;
	}

	// Token: 0x060049D1 RID: 18897 RVA: 0x001B1DD0 File Offset: 0x001AFFD0
	private void Awake()
	{
		if (Bootstrap.m_instance != null)
		{
			Debug.LogError("More than one Bootstrap instance has been created, it expects to be a singleton.", this);
			return;
		}
		Bootstrap.m_instance = this;
		this.m_PersistentScriptsGO = GameObject.Find("NetPersistentScripts");
		if (this.m_PersistentScriptsGO == null)
		{
			GameObject gameObject = Resources.Load("Prefabs/Network/PersistentScripts") as GameObject;
			if (gameObject != null)
			{
				this.m_PersistentScriptsGO = Object.Instantiate<GameObject>(gameObject);
				if (this.m_PersistentScriptsGO != null)
				{
					this.m_PersistentScriptsGO.SetNetViewID(T17NetConfig.GetReservedNetID(T17NetConfig.ReservedNetID.PersistentScripts));
					this.m_PersistentScriptsGO.name = "NetPersistentScripts";
					Object.DontDestroyOnLoad(this.m_PersistentScriptsGO);
				}
				else
				{
					Debug.LogErrorFormat("Bootstrap.Awake - Failed to Instantiate Prefab {0}", new object[] { "Prefabs/Network/PersistentScripts" });
				}
			}
			else
			{
				Debug.LogErrorFormat("Bootstrap.Awake - Failed to load Prefab {0}", new object[] { "Prefabs/Network/PersistentScripts" });
			}
		}
		else
		{
			Debug.LogErrorFormat("Bootstrap.Awake - Already Exists -- NetPersistentScripts", new object[0]);
		}
		if (Bootstrap.m_netGlobalRoomViewGO == null)
		{
			GameObject gameObject2 = Resources.Load("Prefabs/Network/NetGlobalRoomGameView") as GameObject;
			if (gameObject2 != null)
			{
				Bootstrap.m_netGlobalRoomViewGO = Object.Instantiate<GameObject>(gameObject2);
				if (Bootstrap.m_netGlobalRoomViewGO != null)
				{
					Bootstrap.m_netGlobalRoomViewGO.SetNetViewID(T17NetConfig.GetReservedNetID(T17NetConfig.ReservedNetID.GlobalRoomView));
					Bootstrap.m_netGlobalRoomViewGO.name = "NetGlobalRoomView";
					Object.DontDestroyOnLoad(Bootstrap.m_netGlobalRoomViewGO);
				}
				else
				{
					Debug.LogErrorFormat("Bootstrap.Awake - failed to instantiate prefab ({0}) for m_netGlobalRoomViewGO", new object[] { "Prefabs/Network/NetGlobalRoomGameView" });
				}
			}
			else
			{
				Debug.LogErrorFormat("Bootstrap.Awake - failed to load prefab ({0}) for m_netGlobalRoomViewGO", new object[] { "Prefabs/Network/NetGlobalRoomGameView" });
			}
		}
		this.ProcessCommandLine();
	}

	// Token: 0x060049D2 RID: 18898 RVA: 0x0003A836 File Offset: 0x00038A36
	private void Start()
	{
	}

	// Token: 0x060049D3 RID: 18899 RVA: 0x001B1F68 File Offset: 0x001B0168
	public static void Quit(int exitCode)
	{
		Environment.ExitCode = exitCode;
		Debug.LogFormat("BootStrap.Update - UNITY_STANDALONE_WIN -- ExitCode={0}", new object[] { Environment.ExitCode });
		Debug.LogFormat("BootStrap.Update - UNITY_STANDALONE_WIN -- Calling Application.Quit", new object[0]);
		Application.Quit();
		Debug.LogFormat("BootStrap.Update - UNITY_STANDALONE_WIN -- After Application.Quit", new object[0]);
	}

	// Token: 0x060049D4 RID: 18900 RVA: 0x000690FB File Offset: 0x000672FB
	private void Update()
	{
		if (Bootstrap.m_QuitTimer > 0f)
		{
			Bootstrap.m_QuitTimer -= UpdateManager.deltaTime;
			if (Bootstrap.m_QuitTimer <= 0f)
			{
				Bootstrap.Quit(Bootstrap.m_QuitTimerExitCode);
			}
		}
	}

	// Token: 0x060049D5 RID: 18901 RVA: 0x0006912F File Offset: 0x0006732F
	public static void RequestStartupConnectionState()
	{
		NetConnectAndJoinRoom.RequestConnectionState(Bootstrap.m_StartupConnectionState, false);
	}

	// Token: 0x060049D6 RID: 18902 RVA: 0x001B1FC0 File Offset: 0x001B01C0
	public void CheckAndExecStartUpCommand()
	{
		Debug.Log("********  CheckAndExecStartUpCommand   ***** ");
		if (Bootstrap.m_LoadLevel != string.Empty && Bootstrap.m_LoadLevelConfigID >= 0)
		{
			GlobalStart.GetInstance().m_DebugForceLoadLevel = Bootstrap.m_LoadLevel;
			GlobalStart.GetInstance().StartLevelLoad(Bootstrap.m_LoadLevel, Bootstrap.m_LoadLevelConfigID);
			GlobalStart.GetInstance().DARTTestMoveON();
		}
	}

	// Token: 0x060049D7 RID: 18903 RVA: 0x0003A836 File Offset: 0x00038A36
	public void InToLevel()
	{
	}

	// Token: 0x060049D8 RID: 18904 RVA: 0x001B2020 File Offset: 0x001B0220
	static Bootstrap()
	{
	}

	// Token: 0x04003A84 RID: 14980
	private static Bootstrap m_instance = null;

	// Token: 0x04003A85 RID: 14981
	public static readonly PhotonTestEvents m_PhotonEvents = new PhotonTestEvents();

	// Token: 0x04003A86 RID: 14982
	public static readonly Bootstrap.CommandLineOptions CmdLineOptions = new Bootstrap.CommandLineOptions();

	// Token: 0x04003A87 RID: 14983
	private const string COMMANDO_FILE_PREFIX = "-GoCommando:";

	// Token: 0x04003A88 RID: 14984
	private const string COMMANDO_FILE_COMMENT_PREFIX = "#";

	// Token: 0x04003A89 RID: 14985
	private const string COMMANDO_ARGS_PREFIX = "-Commando:";

	// Token: 0x04003A8A RID: 14986
	private const char COMMANDO_ARGS_SEPARATOR = ';';

	// Token: 0x04003A8B RID: 14987
	private static bool m_sHasProcessedCommandLine = false;

	// Token: 0x04003A8C RID: 14988
	private static float m_QuitTimer = 0f;

	// Token: 0x04003A8D RID: 14989
	private static int m_QuitTimerExitCode = 0;

	// Token: 0x04003A8E RID: 14990
	private static string m_LoadLevel = string.Empty;

	// Token: 0x04003A8F RID: 14991
	private static int m_LoadLevelConfigID = 0;

	// Token: 0x04003A90 RID: 14992
	public static NetConnectionState m_StartupConnectionState = NetConnectionState.OfflineMode;

	// Token: 0x04003A91 RID: 14993
	private static GameObject TestScriptGO;

	// Token: 0x04003A92 RID: 14994
	private const string m_netPersistentScriptsPreFabName = "Prefabs/Network/PersistentScripts";

	// Token: 0x04003A93 RID: 14995
	private static GameObject m_netGlobalRoomViewGO = null;

	// Token: 0x04003A94 RID: 14996
	private const string m_netGlobalRoomViewPreFabName = "Prefabs/Network/NetGlobalRoomGameView";

	// Token: 0x04003A95 RID: 14997
	private static bool m_applicationHasFocus = true;

	// Token: 0x04003A96 RID: 14998
	private GameObject m_PersistentScriptsGO;

	// Token: 0x02000B84 RID: 2948
	public class CommandLineOptions
	{
		// Token: 0x060049D9 RID: 18905 RVA: 0x001B2080 File Offset: 0x001B0280
		public CommandLineOptions()
		{
		}

		// Token: 0x04003A97 RID: 14999
		[Commando.NameAttribute("ActiveLogGroup")]
		public readonly List<DebugHelpers.LogGroup> ActiveLogGroups = new List<DebugHelpers.LogGroup>();

		// Token: 0x04003A98 RID: 15000
		[Commando.NameAttribute("ActiveLogNetGroup")]
		public readonly List<DebugHelpers.LogNetGroup> ActiveLogNetGroups = new List<DebugHelpers.LogNetGroup>();

		// Token: 0x04003A99 RID: 15001
		[Commando.NameAttribute("ActivePrefix")]
		public readonly List<DebugHelpers.Prefix> ActivePrefixes = new List<DebugHelpers.Prefix>();

		// Token: 0x04003A9A RID: 15002
		[Commando.NameAttribute("AnalyticTracker")]
		public readonly List<NetAnalytics.Tracker> AnalyticsTrackers = new List<NetAnalytics.Tracker>();

		// Token: 0x04003A9B RID: 15003
		[Commando.NameAttribute("AnalyticEvent")]
		public readonly List<NetAnalytics.EventCategory> AnalyticsEvents = new List<NetAnalytics.EventCategory>();

		// Token: 0x04003A9C RID: 15004
		public readonly string NetLoginID;

		// Token: 0x04003A9D RID: 15005
		public readonly int NetDebugResendCount = 10;

		// Token: 0x04003A9E RID: 15006
		public readonly PhotonLogLevel NetLogLevel;

		// Token: 0x04003A9F RID: 15007
		public readonly bool TrafficStatsCaptureDisabled;

		// Token: 0x04003AA0 RID: 15008
		public readonly bool SkipBootFlow;

		// Token: 0x04003AA1 RID: 15009
		public readonly bool UsingPatchables = true;

		// Token: 0x04003AA2 RID: 15010
		public readonly string InviteRoomName;

		// Token: 0x04003AA3 RID: 15011
		public readonly bool NetLagSimulationEnabled;

		// Token: 0x04003AA4 RID: 15012
		public readonly bool NetLagSimulationGuiEnabled;

		// Token: 0x04003AA5 RID: 15013
		public readonly float NetLagSimulationLag = 100f;

		// Token: 0x04003AA6 RID: 15014
		public readonly float NetLagSimulationJitter;

		// Token: 0x04003AA7 RID: 15015
		public readonly float NetLagSimulationLoss = 1f;

		// Token: 0x04003AA8 RID: 15016
		public readonly bool NetTrafficEnabled;

		// Token: 0x04003AA9 RID: 15017
		public readonly bool NetTrafficGuiEnabled;

		// Token: 0x04003AAA RID: 15018
		public readonly bool NetTrafficShowTraffic;

		// Token: 0x04003AAB RID: 15019
		public readonly bool NetTrafficShowHealth;

		// Token: 0x04003AAC RID: 15020
		public readonly bool NetConfigRpcTTY;

		// Token: 0x04003AAD RID: 15021
		public readonly bool NetDebugGuiPanel;

		// Token: 0x04003AAE RID: 15022
		public readonly bool NetDrawPlayerPos;

		// Token: 0x04003AAF RID: 15023
		public readonly string TestScript = string.Empty;

		// Token: 0x04003AB0 RID: 15024
		public readonly int Quit;

		// Token: 0x04003AB1 RID: 15025
		public readonly int QuitExitCode;

		// Token: 0x04003AB2 RID: 15026
		public readonly string LoadLevel = string.Empty;

		// Token: 0x04003AB3 RID: 15027
		public readonly int LoadLevelConfigID;

		// Token: 0x04003AB4 RID: 15028
		public readonly string RequestConnectionState = string.Empty;

		// Token: 0x04003AB5 RID: 15029
		public readonly string VersionOverride = string.Empty;

		// Token: 0x04003AB6 RID: 15030
		public readonly bool UnlockAllLevels;

		// Token: 0x04003AB7 RID: 15031
		[Commando.NameAttribute("PhotonServerAddress")]
		public readonly string PhotonServerAddress = string.Empty;

		// Token: 0x04003AB8 RID: 15032
		[Commando.NameAttribute("PhotonServerPort")]
		public readonly int PhotonServerPort = 5055;
	}
}