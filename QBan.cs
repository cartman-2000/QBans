// Plugin written by cartman-2000. https://github.com/cartman-2000/QBans

using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QBan
{
    public class QBan : RocketPlugin<QBanConfiguration>
    {
        public static QBan Instance;
        public DataStore dataStore;
        private DateTime lastCalledExpiredTimer = DateTime.Now;
        private DateTime lastCalledQueueTimer = DateTime.Now;

        private static Dictionary<CSteamID, PlayersValues> Players = new Dictionary<CSteamID, PlayersValues>();

        protected override void Load()
        {
            Instance = this;
            dataStore = new DataStore();
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            // Set defaults on new config.
            if (Instance.Configuration.Instance.KickGracePeriod == 0)
                Instance.Configuration.Instance.KickGracePeriod = 6;
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            dataStore.Unload();
            U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
        }

        // search by name of a previous player.
        public static PlayersValues GetPlayer(string search)
        {
            return Players.Values.FirstOrDefault(contents => contents.playerCharName.ToLower().Contains(search.ToLower()) || contents.playerSteamName.ToLower().Contains(search.ToLower()));
        }

        // Exact match to a previous player, by SteamID64 number.
        public static PlayersValues GetPlayer(CSteamID search)
        {
            PlayersValues result;
            if(Players.TryGetValue(search, out result))
                return result;
            return null;
        }

        public void FixedUpdate()
        {
            if (this.State == PluginState.Loaded)
            {
                if ((DateTime.Now - lastCalledExpiredTimer).TotalSeconds > 600)
                {
                    lastCalledExpiredTimer = DateTime.Now;
                    dataStore.CheckExpiredBanData();
                }
            }
        }

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            // Grab the players ip address, for use in ip banning.
            P2PSessionState_t sessionState;
            SteamGameServerNetworking.GetP2PSessionState(player.CSteamID, out sessionState);
            uint uIP = sessionState.m_nRemoteIP;
            Logger.Log(player.CharacterName + " [" + player.SteamName + "] IP: " + Parser.getIPFromUInt32(uIP));

            if (!Players.ContainsKey(player.CSteamID))
            {
                PlayersValues playerData = new PlayersValues();
                playerData.playerSID = player.CSteamID;
                playerData.playerCharName = player.CharacterName;
                playerData.playerSteamName = player.SteamName;
                playerData.playerUintIP = uIP;
                Players.Add(player.CSteamID, playerData);
            }
            else
            {
                // update the stored player name if it doesn't match what they are currently using.
                PlayersValues playerData;
                Players.TryGetValue(player.CSteamID, out playerData);
                if (playerData.playerCharName != player.CharacterName || playerData.playerSteamName != player.SteamName || playerData.playerUintIP != uIP)
                {
                    playerData.playerCharName = player.CharacterName;
                    playerData.playerSteamName = player.SteamName;
                    playerData.playerUintIP = uIP;
                }
            }

            //check to see if the player needs to be banned.
            BanDataValues checkBan = dataStore.GetQBanData(player.CSteamID);
            if (checkBan != null)
            {
                // Don't try to ban if it has expired.
                if (checkBan.duration - (DateTime.Now - checkBan.setTime).TotalSeconds <= 0)
                    return;

                if (checkBan.targetCharName == "" || checkBan.targetSteamName == "")
                {
                    checkBan.targetCharName = player.CharacterName.Sanitze();
                    checkBan.targetSteamName = player.SteamName.Sanitze();
                    // Update player info on ban.
                    dataStore.SetQBanData(player.CSteamID, checkBan);
                }

                // Handle the kicking/syncing of the player in the Ban Queue, Kicking a player in OnConnect will NRE.


                QBanPlayer qbanplayer = player.GetComponent<QBanPlayer>();
                qbanplayer.SetKick(player, checkBan);
//                if (!BanQueue.ContainsKey(player.CSteamID))
//                    BanQueue.Add(player.CSteamID, checkBan);
            }
        }
    }
}
