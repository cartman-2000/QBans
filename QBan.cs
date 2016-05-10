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
        public static DataManager DataStore;
        private DateTime lastCalledExpiredTimer = DateTime.Now;
        private DateTime lastCalledQueueTimer = DateTime.Now;

        internal static Dictionary<CSteamID, PlayersValues> Players = new Dictionary<CSteamID, PlayersValues>();

        protected override void Load()
        {
            Instance = this;
            DataStore = new DataManager();
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            // Save defaults for new config options.
            Instance.Configuration.Save();
        }

        protected override void Unload()
        {
            DataStore = null;
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
                    DataStore.CheckExpiredBanData();
                }
            }
        }

        public static uint GetIP(CSteamID cSteamID)
        {
            // Grab the players ip address, for use in ip banning.
            P2PSessionState_t sessionState;
            SteamGameServerNetworking.GetP2PSessionState(cSteamID, out sessionState);
            return sessionState.m_nRemoteIP;
        }

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            QBanPlayer qbp = player.GetComponent<QBanPlayer>();
            qbp.Start();
        }
    }
}
