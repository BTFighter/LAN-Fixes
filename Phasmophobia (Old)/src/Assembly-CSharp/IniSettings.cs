using System;
using ExitGames.Client.Photon;

[Serializable]
public class IniSettings
{
	public string AppId;

	public string VoiceAppID;

	public string ServerAddress;

	public int ServerPort;

	public ConnectionProtocol Protocol;

	public string ServerVersion;
}
