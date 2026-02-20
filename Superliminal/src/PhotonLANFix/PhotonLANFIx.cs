using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace SuperliminalLANFix
{
    [BepInPlugin("photonLAN.superliminal.lanfix", "Superliminal-LANFix", "1.0.0.0")]
    public class SuperliminalLANFix : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> PluginEnabled;
        internal static ConfigEntry<string> PhotonServerAddress;
        internal static ConfigEntry<int> PhotonServerPort;

        private void Awake()
        {
            Logger.LogInfo("[Superliminal-LANFix] Superliminal-LANFix initialized!");
            Logger.LogInfo("[Superliminal-LANFix] Based on ContentWarningOffline and REPO-PhotonServerSettings");

            // Initialize config entries
            PluginEnabled = Config.Bind("General", "Enable Plugin", true, new ConfigDescription("Enable or disable the plugin. If disabled, official Photon servers will be used."));
            
            PhotonServerAddress = Config.Bind("Photon", "Server", "127.0.0.1", new ConfigDescription("Photon Server Address"));
            PhotonServerPort = Config.Bind("Photon", "Server Port", 5055, new ConfigDescription("Photon Server Port", new AcceptableValueRange<int>(0, 65535)));

            var harmony = new Harmony("photonLAN.superliminal.lanfix");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Com.PillowCastle.SuperliminalRoyale.Launcher), "Connect")]
        public class ConnectPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Com.PillowCastle.SuperliminalRoyale.Launcher __instance)
            {
                // Check if plugin is disabled
                if (!PluginEnabled.Value)
                {
                    return true; // Continue with original method
                }
                
                // Check if server address is provided
                bool hasValidServerSettings = !string.IsNullOrEmpty(PhotonServerAddress.Value);
                
                if (hasValidServerSettings)
                {
                    // Get the customAppSettings field from the Launcher instance
                    var customAppSettingsField = typeof(Com.PillowCastle.SuperliminalRoyale.Launcher).GetField("customAppSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (customAppSettingsField != null)
                    {
                        var customAppSettings = (AppSettings)customAppSettingsField.GetValue(__instance);
                        
                        // Apply server settings
                        customAppSettings.Server = PhotonServerAddress.Value;
                        PhotonNetwork.PhotonServerSettings.AppSettings.Server = PhotonServerAddress.Value;
                        customAppSettings.FixedRegion = null; // Clear region to use custom server
                        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = null;
                        
                        // Force server version to 4
                        customAppSettings.UseNameServer = false;
                        PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = false;
                        PhotonNetwork.NetworkingClient.SerializationProtocol = SerializationProtocol.GpBinaryV16;
                        
                        if (PhotonServerPort.Value > 0)
                        {
                            customAppSettings.Port = PhotonServerPort.Value;
                            PhotonNetwork.PhotonServerSettings.AppSettings.Port = PhotonServerPort.Value;
                        }
                        
                        // Set fixed protocol and ports
                        customAppSettings.Protocol = ConnectionProtocol.Udp;
                        PhotonNetwork.PhotonServerSettings.AppSettings.Protocol = ConnectionProtocol.Udp;
                        PhotonNetwork.ServerPortOverrides = new PhotonPortDefinition(); // Disable alternative ports
                        
                        // Save the modified settings back to the Launcher
                        customAppSettingsField.SetValue(__instance, customAppSettings);
                    }
                }
                
                // Continue with original method
                return true;
            }
        }
    }
}
