using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using MGS.Cluedo;
using MGS.Utils;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PhotonLANFix
{
    [BepInPlugin("photonLAN.clue.photonlanfix", "Clue-PhotonLANFix", "1.0.0.0")]
    public class PhotonLANFix : BaseUnityPlugin
    {
         internal static ConfigEntry<string> PhotonServerAddress;
        internal static ConfigEntry<int> PhotonServerPort;
        internal static ConfigEntry<bool> RandomAIAvatars;
        
        // Counter for AI players added this session
        private static int s_AICounter = 0;
        
        // Store loaded AI avatar textures: avatarN -> texture
        private static Dictionary<string, Texture2D> s_AIAvatarCache = new Dictionary<string, Texture2D>();
        
        // Store AI SteamID assignments synchronized from host: charId -> steamId
        private static Dictionary<int, string> s_AISteamIDAssignments = new Dictionary<int, string>();

        private void Awake()
        {
            Logger.LogInfo("[Clue-PhotonLANFix] Clue-PhotonLANFix initialized!");
            Logger.LogInfo("[Clue-PhotonLANFix] Based on REPO-PhotonServerSettings by 1A3Dev");
            Logger.LogInfo("[Clue-PhotonLANFix] Also based on ContentWarningOffline by Kirigiri, made with <3 \nhttps://discord.gg/TBs8Te5nwn");
            
            // Keep this object alive across scene loads
            DontDestroyOnLoad(gameObject);

             // Initialize config entries
            PhotonServerAddress = Config.Bind("Photon", "Server", "", new ConfigDescription("Photon Server Address"));
            PhotonServerPort = Config.Bind("Photon", "Server Port", 5055, new ConfigDescription("Photon Server Port", new AcceptableValueRange<int>(0, 65535)));
            RandomAIAvatars = Config.Bind("General", "Random AI Avatars", true, new ConfigDescription("Enable random AI avatars"));

            var harmony = new Harmony("photonLAN.clue.photonlanfix");
            harmony.PatchAll();
            
            // Patch EnterOfflineRoom manually using reflection
            try
            {
                var enterOfflineRoomMethod = typeof(PhotonNetwork).GetMethod("EnterOfflineRoom", BindingFlags.Static | BindingFlags.NonPublic);
                var postfixMethod = typeof(EnterOfflineRoomPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                if (enterOfflineRoomMethod != null && postfixMethod != null)
                {
                    harmony.Patch(enterOfflineRoomMethod, null, new HarmonyMethod(postfixMethod));
                    Logger.LogInfo("[Clue-PhotonLANFix] Successfully patched EnterOfflineRoom");
                }
                else
                {
                    Logger.LogError("[Clue-PhotonLANFix] Failed to find EnterOfflineRoom method");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Clue-PhotonLANFix] Failed to patch EnterOfflineRoom: {ex}");
            }
        }
        
        // Get received AI SteamID assignment for a character (from host)
        private static string GetReceivedAISteamID(int charId)
        {
            if (s_AISteamIDAssignments.TryGetValue(charId, out var steamId))
                return steamId;
                
            return null;
        }
        
        // Store used avatar numbers to ensure uniqueness
        private static List<int> s_UsedAvatarNumbers = new List<int>();
        
        // Get AI avatar identifier based on character ID (deterministic - same charId always gets same avatar)
        private static string GetAIAvatarIdByCharId(int charId)
        {
            // Use charId as seed for deterministic selection
            // This ensures both host and client select the same avatar for the same character
            int avatarNumber = (Mathf.Abs(charId) % 10) + 1; // 1-10 avatars
            
            // Ensure we get a unique avatar number that hasn't been used yet
            while (s_UsedAvatarNumbers.Contains(avatarNumber))
            {
                avatarNumber++;
                if (avatarNumber > 10)
                    avatarNumber = 1; // Wrap around if all avatars are used
            }
            
            s_UsedAvatarNumbers.Add(avatarNumber);
            return $"avatar{avatarNumber}";
        }
        
        // Get next AI avatar identifier (increments counter) - kept for backward compatibility
        private static string GetNextAIAvatarId()
        {
            int avatarNumber = (s_AICounter % 10) + 1; // 1-10 avatars
            
            // Ensure we get a unique avatar number that hasn't been used yet
            while (s_UsedAvatarNumbers.Contains(avatarNumber))
            {
                avatarNumber++;
                if (avatarNumber > 10)
                    avatarNumber = 1; // Wrap around if all avatars are used
            }
            
            s_UsedAvatarNumbers.Add(avatarNumber);
            s_AICounter++;
            return $"avatar{avatarNumber}";
        }
        
        // Resize texture to fit within max dimensions
        private static Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
        {
            if (source == null)
                return null;
            
            float ratio = Mathf.Min((float)maxWidth / source.width, (float)maxHeight / source.height);
            int newWidth = Mathf.RoundToInt(source.width * ratio);
            int newHeight = Mathf.RoundToInt(source.height * ratio);
            
            // Create new texture with target size
            Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            
            // Use RenderTexture for high-quality resizing
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            RenderTexture.active = rt;
            
            // Blit with scaling
            Graphics.Blit(source, rt);
            
            // Read back to texture
            resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resized.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return resized;
        }
        
        // Load AI avatar from file based on avatar identifier (avatar1, avatar2, etc.)
        private static Texture2D LoadAIAvatarByAvatarId(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
                return null;
                
            // Check cache first
            if (s_AIAvatarCache.TryGetValue(avatarId, out Texture2D cachedTexture))
                return cachedTexture;
                
            try
            {
                string imagesPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "BepInEx", "plugins", "images");
                
                if (!System.IO.Directory.Exists(imagesPath))
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Images directory not found: {imagesPath}");
                    return null;
                }
                
                // Try .png first, then .jpg
                string[] extensions = { ".png", ".jpg", ".jpeg" };
                
                foreach (string ext in extensions)
                {
                    string filePath = System.IO.Path.Combine(imagesPath, avatarId + ext);
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
                        // Create texture with RGBA32 format for consistency
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (ImageConversion.LoadImage(texture, fileData))
                        {
                            s_AIAvatarCache[avatarId] = texture;
                            Debug.Log($"[Clue-PhotonLANFix] Loaded AI avatar for {avatarId} from {filePath} (size: {texture.width}x{texture.height}, format: {texture.format})");
                            return texture;
                        }
                    }
                }
                
                Debug.Log($"[Clue-PhotonLANFix] No avatar image found for {avatarId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Clue-PhotonLANFix] Failed to load AI avatar for {avatarId}: {ex.Message}");
            }
            
            return null;
        }
        


        [HarmonyPatch(typeof(Web), "CheckConnection")]
        public class WebCheckConnectionPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref Action<bool> callback)
            {
                // Bypass internet connectivity check and always return true
                Debug.Log("[Clue-PhotonLANFix] Bypassing internet connectivity check");
                callback(true);
                return false; // Skip original method
            }
        }

        [HarmonyPatch(typeof(PhotonManager), "CreatePrivateRoom")]
        public class CreatePrivateRoomPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(PhotonManager __instance, int maxPlayers, int boardType, string roomCode)
            {
                // Check if Photon is still connecting
                if (PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer || 
                    PhotonNetwork.NetworkClientState == ClientState.ConnectingToNameServer)
                {
                    Debug.Log("[Clue-PhotonLANFix] Photon is still connecting, skipping immediate room creation");
                    return false; // Skip original method to prevent error
                }
                
                // If we're connected, continue with room creation
                if (PhotonNetwork.IsConnected)
                {
                    Debug.Log("[Clue-PhotonLANFix] Photon is connected, allowing room creation");
                    return true; // Continue with original method
                }
                
                // If not connected and not connecting, let the original method handle it
                Debug.Log("[Clue-PhotonLANFix] Photon is not connected, let original method handle");
                return true;
            }
        }

        [HarmonyPatch(typeof(PhotonManager), "Connect")]
        public class PhotonManagerConnectPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(PhotonManager __instance)
            {
                 // Check if server address is blank, then enter offline mode
                bool shouldEnterOfflineMode = string.IsNullOrEmpty(PhotonServerAddress.Value);
                
                // Set player name to Steam/Goldberg name instead of random (both online and offline)
                string steamName = string.Empty;
                string steamId = string.Empty;
                try
                {
                    // Try to get Steam name and Steam ID using Steamworks
                    steamName = Steamworks.SteamFriends.GetPersonaName();
                    steamId = Steamworks.SteamUser.GetSteamID().ToString();
                    Debug.Log($"[Clue-PhotonLANFix] Got Steam name: {steamName}, Steam ID: {steamId}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to get Steam information: {ex.Message}");
                }
                
                if (!string.IsNullOrEmpty(steamName) && steamName != "Unknown User")
                {
                    MainController.Preferences.PlayerName = steamName;
                    Debug.Log($"[Clue-PhotonLANFix] Set PlayerName to Steam name: {steamName}");
                    
                    // Manually set PhotonNetwork.LocalPlayer.NickName to ensure it's updated
                    if (PhotonNetwork.LocalPlayer != null)
                    {
                        MainController.PhotonManager.SetPlayerPhotonName(steamName);
                        Debug.Log($"[Clue-PhotonLANFix] Set PhotonPlayer NickName to: {PhotonNetwork.LocalPlayer.NickName}");
                    }
                    else
                    {
                        Debug.LogWarning("[Clue-PhotonLANFix] PhotonNetwork.LocalPlayer is null, waiting for OnConnectedToMaster");
                    }
                }
                else
                {
                    Debug.Log("[Clue-PhotonLANFix] Using default generated name");
                }
                
                // Set Photon user ID to Steam ID for proper avatar synchronization
                if (!string.IsNullOrEmpty(steamId))
                {
                    MainController.PhotonManager.SetLocalPlayerUserId(steamId);
                    Debug.Log($"[Clue-PhotonLANFix] Set Photon user ID to Steam ID: {steamId}");
                }
                
                if (shouldEnterOfflineMode)
                {
                    PhotonNetwork.OfflineMode = true;
                    Debug.Log("[Clue-PhotonLANFix] Both server address and AppId Realtime are blank, entering offline mode");
                    return false;
                }
                
                // Apply server settings if we have a valid server address
                PhotonNetwork.PhotonServerSettings.AppSettings.Server = PhotonServerAddress.Value;
                Debug.Log($"[Clue-PhotonLANFix] Changed Server Address: {PhotonNetwork.PhotonServerSettings.AppSettings.Server}");
                
                // Always disable NameServer and use Photon 4.x settings
                PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = false;
                PhotonNetwork.NetworkingClient.SerializationProtocol = SerializationProtocol.GpBinaryV16;
                
                // Set required authentication region parameter
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "US"; // Default to US region
                
                // Set CloudRegion using reflection (it has a private setter)
                var cloudRegionProperty = typeof(Photon.Realtime.LoadBalancingClient).GetProperty("CloudRegion", BindingFlags.Public | BindingFlags.Instance);
                var setMethod = cloudRegionProperty?.GetSetMethod(true); // true to get private method
                if (setMethod != null)
                {
                    setMethod.Invoke(PhotonNetwork.NetworkingClient, new object[] { "US" });
                    Debug.Log($"[Clue-PhotonLANFix] Set CloudRegion to: US");
                }
                else
                {
                    Debug.LogError("[Clue-PhotonLANFix] Failed to set CloudRegion property");
                }
                
                if (PhotonServerPort.Value > 0)
                {
                    PhotonNetwork.PhotonServerSettings.AppSettings.Port = PhotonServerPort.Value;
                    Debug.Log($"[Clue-PhotonLANFix] Changed Server Port: {PhotonNetwork.PhotonServerSettings.AppSettings.Port}");
                }
                
                // Set Protocol to UDP and disable Alternative UDP Ports
                PhotonNetwork.PhotonServerSettings.AppSettings.Protocol = ConnectionProtocol.Udp;
                PhotonNetwork.ServerPortOverrides = new PhotonPortDefinition();
                
                // Continue with original method to connect to online mode
                return true;
            }
        }
        
        [HarmonyPatch(typeof(PhotonLobbyEntryBaseUI), "AddPlayerEntry")]
        public class EnterOfflineRoomPatch
        {
            public static void Postfix()
            {
                // Set PhotonNetwork.LocalPlayer.NickName when entering offline room
                try
                {
                    if (PhotonNetwork.LocalPlayer != null)
                    {
                        PhotonNetwork.LocalPlayer.NickName = MainController.Preferences.PlayerName;
                        Debug.Log($"[Clue-PhotonLANFix] Set PhotonPlayer NickName to: {PhotonNetwork.LocalPlayer.NickName}");
                    }
                    else
                    {
                        Debug.LogWarning("[Clue-PhotonLANFix] PhotonNetwork.LocalPlayer is null in EnterOfflineRoom");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to set PhotonPlayer NickName in EnterOfflineRoom: {ex.Message}");
                }
            }
        }
        
        [HarmonyPatch(typeof(PhotonMultiplayerLobbyUI), "AddLobbyEntry")]
        public class PhotonMultiplayerLobbyUIAddLobbyEntryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PhotonMultiplayerLobbyUI __instance, PhotonMultiplayerController.PlayerRoomProperties lobbyPlayer, string localUserId, bool isMasterClient, int index)
            {
                try
                {
                    // Skip AI players - they should use the default AI profile picture
                    // isAI: 0 = human player, 1 = AI player
                    if (lobbyPlayer.isAI != 0)
                    {
                        Debug.Log($"[Clue-PhotonLANFix] Skipping profile picture for AI player {lobbyPlayer.playerName} (isAI={lobbyPlayer.isAI})");
                        return;
                    }

                    // Get the lobby entry UI
                    var lobbyEntriesField = typeof(PhotonMultiplayerLobbyUI).GetField("m_LobbyEntries", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (lobbyEntriesField != null)
                    {
                        var m_LobbyEntries = (List<PhotonLobbyEntryBaseUI>)lobbyEntriesField.GetValue(__instance);
                        if (index < m_LobbyEntries.Count && index >= 0)
                        {
                            PhotonLobbyEntryBaseUI entry = m_LobbyEntries[index];
                            bool isLocalPlayer = lobbyPlayer.playerId == localUserId;
                            
                            // Get Steam profile picture
                            Texture steamTexture = null;
                            if (isLocalPlayer)
                            {
                                steamTexture = MGS.Steam.SteamworksController.GetPlayerProfilePicture();
                            }
                            else
                            {
                                // For remote players, try to get their Steam ID from playerId and load their avatar
                                ulong steamId;
                                if (ulong.TryParse(lobbyPlayer.playerId, out steamId))
                                {
                                    // Use Steamworks to get the remote player's avatar
                                    Steamworks.CSteamID steamIdUser = new Steamworks.CSteamID(steamId);
                                    int imageId = Steamworks.SteamFriends.GetLargeFriendAvatar(steamIdUser);
                                    if (imageId != -1)
                                    {
                                        steamTexture = MGS.Steam.SteamworksController.GetSteamImageAsTexture2D(imageId);
                                    }
                                }
                            }
                            
                            // Set profile picture if we got it
                            if (steamTexture != null)
                            {
                                Texture2D texture2D = steamTexture as Texture2D;
                                if (texture2D != null)
                                {
                                    // Flip the texture vertically to match the game's expected orientation
                                    Texture2D flippedTexture = CustomTools.FlipTexture(texture2D);
                                    Sprite steamSprite = Sprite.Create(flippedTexture, new Rect(0f, 0f, flippedTexture.width, flippedTexture.height), new Vector2(0.5f, 0.5f));
                                    var profileIconField = typeof(PhotonLobbyEntryBaseUI).GetField("m_ProfileIcon", BindingFlags.Instance | BindingFlags.NonPublic);
                                    if (profileIconField != null)
                                    {
                                        var m_ProfileIcon = (Image)profileIconField.GetValue(entry);
                                        m_ProfileIcon.sprite = steamSprite;
                                        // Reset rotation
                                        m_ProfileIcon.transform.rotation = Quaternion.identity;
                                        Debug.Log($"[Clue-PhotonLANFix] Set profile picture for player {lobbyPlayer.playerName} (local: {isLocalPlayer})");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to set player profile picture: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MultiplayerLobbyEntryController), "UpdateVisuals")]
        public class MultiplayerLobbyEntryControllerUpdateVisualsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MultiplayerLobbyEntryController __instance, MultiplayerLobbyEntryController.VisualsData visualsData)
            {
                try
                {
                    // Skip AI players - they should use the default AI profile picture
                    // m_AiDiff > 0 indicates an AI player with difficulty set
                    // m_StaticAI indicates a static AI player
                    // m_NetID starting with "AI_" indicates an AI player
                    if (visualsData.m_AiDiff > 0 || visualsData.m_StaticAI || 
                        (!string.IsNullOrEmpty(visualsData.m_NetID) && visualsData.m_NetID.StartsWith("AI_")))
                    {
                        Debug.Log($"[Clue-PhotonLANFix] Skipping profile picture replacement for AI player (AiDiff={visualsData.m_AiDiff}, StaticAI={visualsData.m_StaticAI}, NetID={visualsData.m_NetID})");
                        return;
                    }

                    // Additional check: Look up player info from GameSettings to verify this is not an AI player
                    // This handles cases where AI players might have a human player's NetID assigned incorrectly
                    if (MainController.GameSettings != null && !string.IsNullOrEmpty(visualsData.m_NetID))
                    {
                        var playerInfoList = MainController.GameSettings.GetPlayerInfoList();
                        foreach (var playerInfo in playerInfoList)
                        {
                            // Find the player by NetID or PlayerId
                            if (playerInfo.NetworkPlayerId == visualsData.m_NetID || 
                                playerInfo.PlayerId.ToString() == visualsData.m_NetID)
                            {
                                // Check if this player is actually an AI
                                if (playerInfo.Type == PlayerInfo.PlayerType.AI || 
                                    playerInfo.Type == PlayerInfo.PlayerType.NetworkAI)
                                {
                                    Debug.Log($"[Clue-PhotonLANFix] Found AI player type {playerInfo.Type} for NetID {visualsData.m_NetID}, skipping profile picture");
                                    return;
                                }
                                break;
                            }
                        }
                    }

                    // If visualsData.m_ProfPic is null, it means we're using the default profile picture (gray avatar with person icon)
                    if (visualsData.m_ProfPic == null && !string.IsNullOrEmpty(visualsData.m_NetID))
                    {
                        Texture steamTexture = null;
                        string localPlayerId = MainController.Preferences.PlayerID;
                        
                        // Check if this is the local player first
                        if (visualsData.m_NetID == localPlayerId)
                        {
                            // Use cached profile picture for local player
                            steamTexture = MGS.Steam.SteamworksController.GetPlayerProfilePicture();
                        }
                        
                        // If not local player or cached picture not available, try Steam friends API
                        if (steamTexture == null)
                        {
                            ulong steamId;
                            if (ulong.TryParse(visualsData.m_NetID, out steamId))
                            {
                                Steamworks.CSteamID steamIdUser = new Steamworks.CSteamID(steamId);
                                int imageId = Steamworks.SteamFriends.GetLargeFriendAvatar(steamIdUser);
                                if (imageId != -1)
                                {
                                    steamTexture = MGS.Steam.SteamworksController.GetSteamImageAsTexture2D(imageId);
                                }
                            }
                        }
                        
                        if (steamTexture != null)
                        {
                            Texture2D texture2D = steamTexture as Texture2D;
                            if (texture2D != null)
                            {
                                // Flip the texture vertically to match the game's expected orientation
                                Texture2D flippedTexture = CustomTools.FlipTexture(texture2D);
                                Sprite steamSprite = Sprite.Create(flippedTexture, new Rect(0f, 0f, flippedTexture.width, flippedTexture.height), new Vector2(0.5f, 0.5f));
                                // Set the profile picture
                                var profilePicField = typeof(MultiplayerLobbyEntryController).GetField("m_ProfilePic", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (profilePicField != null)
                                {
                                    var m_ProfilePic = (Image)profilePicField.GetValue(__instance);
                                    m_ProfilePic.sprite = steamSprite;
                                    Debug.Log($"[Clue-PhotonLANFix] Replaced default profile picture for player with NetID: {visualsData.m_NetID}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to replace default profile picture: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(PhotonMultiplayerController), "AddPlayer")]
        public class PhotonMultiplayerControllerAddPlayerPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(PhotonMultiplayerController __instance, GameSettings settings, string playerName, int characterId, PlayerInfo.PlayerType type, int subtype, int pawnIndex, bool isHost)
            {
                try
                {
                    var playerListField = typeof(PhotonMultiplayerController).GetField("m_PlayerList", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (playerListField != null)
                    {
                        var m_PlayerList = (List<PhotonMultiplayerController.PlayerRoomProperties>)playerListField.GetValue(__instance);

                        PhotonMultiplayerController.PlayerRoomProperties currentPlayerRoom = default;
                        bool found = false;

                        // Determine if we're looking for an AI player based on the type parameter
                        bool lookingForAI = type == PlayerInfo.PlayerType.AI || type == PlayerInfo.PlayerType.NetworkAI;

                        // Find the player in m_PlayerList
                        for (int i = 0; i < m_PlayerList.Count; i++)
                        {
                            var player = m_PlayerList[i];
                            
                            // Match based on whether we're looking for AI or human player
                            // Also match by character ID to ensure we get the right player slot
                            bool isMatch = (player.charId == characterId) && 
                                           (player.isAI == (lookingForAI ? 1 : 0));

                            if (isMatch)
                            {
                                currentPlayerRoom = player;
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            string playerId = currentPlayerRoom.playerId;
                            Debug.Log($"[Clue-PhotonLANFix] AddPlayer for playerId: '{playerId}', playerName: '{playerName}', charId: {characterId}, isAI: {currentPlayerRoom.isAI}, type: {type}");

                            CharacterMetaData characterById = MainController.Characters.GetCharacterById(characterId);
                            PawnInfo pawn = default;
                            pawn.id = characterById.m_Id;
                            pawn.spawnPointId = pawnIndex;
                            pawn.type = characterById.m_Id - 1;
                            PlayerInfo player = new PlayerInfo
                            {
                                PlayerId = characterById.m_Id,
                                UserName = playerName,
                                Type = type,
                                PawnId = characterById.m_Id,
                                DisplayName = characterById.m_FullName,
                                m_IsHost = isHost,
                                NetworkPlayerId = playerId
                            };

                             // Only set Steam avatar for non-AI players (check isAI flag, playerId prefix, and PlayerType)
                            // isAI: 0 = human player, 1 = AI player
                            // PlayerType: AI, Human, Network, NetworkAI, Invalid
                            bool isAIPlayer = currentPlayerRoom.isAI != 0 || 
                                              playerId.StartsWith("AI_") || 
                                              type == PlayerInfo.PlayerType.AI || 
                                              type == PlayerInfo.PlayerType.NetworkAI;
                            
                            if (!isAIPlayer)
                            {
                                Texture steamTexture = null;
                                string localUserId = MainController.PhotonManager.GetLocalPlayerUserId();
                                
                                // Check if this is the local player first
                                if (playerId == localUserId)
                                {
                                    // Use cached profile picture for local player
                                    steamTexture = MGS.Steam.SteamworksController.GetPlayerProfilePicture();
                                    Debug.Log($"[Clue-PhotonLANFix] Using cached profile picture for local player {playerId}");
                                }
                                
                                // If not local player or cached picture not available, try Steam friends API
                                if (steamTexture == null)
                                {
                                    ulong steamId;
                                    if (ulong.TryParse(playerId, out steamId))
                                    {
                                        Steamworks.CSteamID steamIdUser = new Steamworks.CSteamID(steamId);
                                        int imageId = Steamworks.SteamFriends.GetLargeFriendAvatar(steamIdUser);
                                        if (imageId != -1)
                                        {
                                            steamTexture = MGS.Steam.SteamworksController.GetSteamImageAsTexture2D(imageId);
                                        }
                                    }
                                }

                                if (steamTexture != null)
                                {
                                    Texture2D texture2D = steamTexture as Texture2D;
                                    if (texture2D != null)
                                    {
                                        Texture2D flippedTexture = CustomTools.FlipTexture(texture2D);
                                        player.ProfilePictureRawData = flippedTexture.GetRawTextureData();
                                        player.ProfilePictureFormat = flippedTexture.format;
                                        player.ProfilePicSize = new Vector2(flippedTexture.width, flippedTexture.height);
                                        Debug.Log($"[Clue-PhotonLANFix] Set ProfilePictureRawData for playerId: '{playerId}'");
                                    }
                                }
                            }
                             else
                            {
                                // AI player - use avatar1, avatar2, etc., from local files if enabled
                                if (RandomAIAvatars.Value)
                                {
                                    string aiAvatarId = null;
                                    
                                    // Non-host clients: Check for received avatar ID assignment from host
                                    if (!PhotonNetwork.IsMasterClient)
                                    {
                                        aiAvatarId = GetReceivedAISteamID(characterById.m_Id); // Reusing this method name since it now stores avatar IDs
                                        Debug.Log($"[Clue-PhotonLANFix] Received avatar ID assignment for charId {characterById.m_Id}: {aiAvatarId}");
                                    }
                                    
                                    // Host or no received avatar ID: Assign avatar ID using deterministic selection based on charId
                                    if (string.IsNullOrEmpty(aiAvatarId))
                                    {
                                        aiAvatarId = GetAIAvatarIdByCharId(characterById.m_Id);
                                        
                                        if (!string.IsNullOrEmpty(aiAvatarId))
                                        {
                                            // Host: Store avatar ID assignment for synchronization
                                            if (PhotonNetwork.IsMasterClient)
                                            {
                                                s_AISteamIDAssignments[characterById.m_Id] = aiAvatarId; // Reusing this dictionary since it stores charId -> assignment
                                                Debug.Log($"[Clue-PhotonLANFix] Host assigned avatar ID {aiAvatarId} to charId {characterById.m_Id}");
                                            }
                                        }
                                    }
                                    
                                    if (!string.IsNullOrEmpty(aiAvatarId))
                                    {
                                        // Set the NetworkPlayerId to the avatar ID
                                        player.NetworkPlayerId = aiAvatarId;
                                        
                                        // Load avatar from local file
                                        Texture2D avatarTexture = LoadAIAvatarByAvatarId(aiAvatarId);
                                        
                                        if (avatarTexture != null)
                                        {
                                            // Store the image data
                                            player.ProfilePictureRawData = avatarTexture.GetRawTextureData();
                                            player.ProfilePictureFormat = avatarTexture.format;
                                            player.ProfilePicSize = new Vector2(avatarTexture.width, avatarTexture.height);
                                            
                                            Debug.Log($"[Clue-PhotonLANFix] Set avatar for AI player {playerId} (charId={characterById.m_Id}, avatarId={aiAvatarId})");
                                        }
                                        else
                                        {
                                            Debug.Log($"[Clue-PhotonLANFix] No avatar found for AI player {playerId} (charId={characterById.m_Id}, avatarId={aiAvatarId})");
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log($"[Clue-PhotonLANFix] No avatar ID available for AI player {playerId} (charId={characterById.m_Id})");
                                    }
                                }
                                else
                                {
                                    Debug.Log($"[Clue-PhotonLANFix] Random AI avatars are disabled, using default AI profile");
                                }
                            }

                            Debug.Log($"[Clue-PhotonLANFix] GameSettings Add Player: {characterById.m_Id} {characterById.m_FullName} {player.PlayerId}");
                            settings.AddPawn(pawn);
                            settings.AddPlayer(player);
                            settings.AddCharacterCard(characterById.m_Id);

                            return false; // Skip original method
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to patch AddPlayer: {ex.Message}");
                }

                return true; // Continue with original method if patch failed
            }
        }

        [HarmonyPatch(typeof(MGS.Cluedo.UI.CurrentPlayerTurnUI), "ShowBanner")]
        public class CurrentPlayerTurnUIShowBannerPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MGS.Cluedo.UI.CurrentPlayerTurnUI __instance)
            {
                try
                {
                    int currentPlayerId = MainController.GameController.GameData.GetAsInt("CurrentPlayerId");
                    PlayerInfo playerInfo = MainController.GameSettings.GetPlayerInfoById(currentPlayerId);
                    
                    // Check if this is an AI player
                    bool isAI = playerInfo.Type == PlayerInfo.PlayerType.AI || 
                                playerInfo.Type == PlayerInfo.PlayerType.NetworkAI;
                    
                    // Get the m_PlayerProfilePic field
                    var profilePicField = typeof(MGS.Cluedo.UI.CurrentPlayerTurnUI).GetField("m_PlayerProfilePic", BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    if (profilePicField != null)
                    {
                        var m_PlayerProfilePic = (Image)profilePicField.GetValue(__instance);
                        
                        if (isAI)
                        {
                            // For AI players, use the profile picture from GameSettings (set via SteamID mapping)
                            byte[] imageData = playerInfo.ProfilePictureRawData;
                            Vector2 size = playerInfo.ProfilePicSize;
                            TextureFormat format = playerInfo.ProfilePictureFormat;
                            
                            // If no data in PlayerInfo, try to load from NetworkPlayerId (which stores the avatar ID)
                            if (imageData == null || imageData.Length == 0)
                            {
                                string aiAvatarId = playerInfo.NetworkPlayerId;
                                if (!string.IsNullOrEmpty(aiAvatarId) && !aiAvatarId.StartsWith("AI_"))
                                {
                                    Texture2D avatarTexture = LoadAIAvatarByAvatarId(aiAvatarId);
                                    if (avatarTexture != null)
                                    {
                                        imageData = avatarTexture.GetRawTextureData();
                                        size = new Vector2(avatarTexture.width, avatarTexture.height);
                                        format = avatarTexture.format;
                                    }
                                }
                            }
                            
                            if (imageData != null && imageData.Length > 0)
                            {
                                // Use the stored texture format when recreating the texture
                                Texture2D texture = new Texture2D((int)size.x, (int)size.y, format, false);
                                texture.LoadRawTextureData(imageData);
                                texture.Apply();
                                Sprite avatarSprite = Sprite.Create(texture, new Rect(0f, 0f, size.x, size.y), new Vector2(0.5f, 0.5f));
                                m_PlayerProfilePic.sprite = avatarSprite;
                                m_PlayerProfilePic.color = new Color(1f, 1f, 1f, 1f);
                                Debug.Log($"[Clue-PhotonLANFix] Using SteamID-based avatar for AI player {playerInfo.UserName}");
                            }
                            else
                            {
                                // No avatar set, clear the sprite
                                m_PlayerProfilePic.sprite = null;
                                m_PlayerProfilePic.color = new Color(1f, 1f, 1f, 0f);
                                Debug.Log($"[Clue-PhotonLANFix] No avatar for AI player {playerInfo.UserName}");
                            }
                        }
                        else if (playerInfo.ProfilePictureRawData == null || playerInfo.ProfilePictureRawData.Length == 0)
                        {
                            // For human players without profile pictures, clear the sprite
                            m_PlayerProfilePic.sprite = null;
                            m_PlayerProfilePic.color = new Color(1f, 1f, 1f, 0f);
                        }
                        else
                        {
                            // Ensure the image is visible for human players with profile pictures
                            m_PlayerProfilePic.color = new Color(1f, 1f, 1f, 1f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Clue-PhotonLANFix] Failed to patch CurrentPlayerTurnUI.ShowBanner: {ex.Message}");
                }
            }
        }
    }
}
