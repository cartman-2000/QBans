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
            // Save defaults for new config options.
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
            string sIP = Parser.getIPFromUInt32(uIP);
            Logger.Log(player.CharacterName + " [" + player.SteamName + "] IP: " + sIP);

            if (!Players.ContainsKey(player.CSteamID))
            {
                PlayersValues playerData = new PlayersValues();
                playerData.playerSID = player.CSteamID;
                playerData.playerCharName = player.CharacterName;
                playerData.playerSteamName = player.SteamName;
                playerData.playerUIP = uIP;
                Players.Add(player.CSteamID, playerData);
            }
            else
            {
                // update the stored player name if it doesn't match what they are currently using.
                PlayersValues playerData;
                Players.TryGetValue(player.CSteamID, out playerData);
                if (playerData.playerCharName != player.CharacterName || playerData.playerSteamName != player.SteamName || playerData.playerUIP != uIP)
                {
                    playerData.playerCharName = player.CharacterName;
                    playerData.playerSteamName = player.SteamName;
                    playerData.playerUIP = uIP;
                }
            }

            //check to see if the player needs to be banned.
            BanDataValues checkBan = dataStore.GetQBanData(player.CSteamID);
            bool ipBanMatch = false;
            if (checkBan == null)
            {
                checkBan = dataStore.GetIPQBanData(uIP);
                if(checkBan != null)
                    ipBanMatch = true;
            }
            if (checkBan != null)
            {
                // Don't try to ban if it has expired.
                if (checkBan.duration - (DateTime.Now - checkBan.setTime).TotalSeconds <= 0)
                    return;

                if (ipBanMatch)
                {
                    Logger.Log(string.Format("IP ban: IP match on this player, Matches player: {0}[{1}]({2}) IP: {3}, kicking!", checkBan.targetCharName, checkBan.targetSteamName, checkBan.targetSID, sIP));
                }

                if ((checkBan.targetCharName == "" || checkBan.targetSteamName == "" || checkBan.uIP != uIP) && !ipBanMatch)
                {
                    checkBan.targetCharName = player.CharacterName.Sanitze();
                    checkBan.targetSteamName = player.SteamName.Sanitze();
                    checkBan.uIP = uIP;
                    // Update player info on ban.
                    dataStore.SetQBanData(player.CSteamID, checkBan);
                }
                else
                {
                    BanDataValues temp = checkBan;
                    checkBan = new BanDataValues();
                    checkBan.targetSID = player.CSteamID;
                    checkBan.targetCharName = player.CharacterName;
                    checkBan.targetSteamName = player.SteamName;
                    checkBan.adminSID = temp.adminSID;
                    checkBan.adminCharName = temp.adminCharName;
                    checkBan.adminSteamName = temp.adminSteamName;
                    checkBan.reason = temp.reason;
                    checkBan.duration = temp.duration;
                    checkBan.isIPBan = false;
                    checkBan.uIP = temp.uIP;
                    checkBan.setTime = temp.setTime;
                    if (Instance.Configuration.Instance.IPBanAutoAdd)
                        dataStore.SetQBanData(player.CSteamID, checkBan);
                }

                // Send the player over to the player component to handle the kicking and syncing of ban data.
                QBanPlayer qbanplayer = player.GetComponent<QBanPlayer>();
                qbanplayer.SetKick(player, checkBan, ipBanMatch);
            }
        }
    }
}
