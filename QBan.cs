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
        private static Dictionary<CSteamID, BanDataValues> BanQueue = new Dictionary<CSteamID, BanDataValues>();

        protected override void Load()
        {
            Instance = this;
            dataStore = new DataStore();
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
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
                if ((DateTime.Now - lastCalledQueueTimer).TotalSeconds > 5)
                    HandleBanQueue();
            }
        }

        // Queue's banned players to the internal bans.
        private void HandleBanQueue()
        {
            try
            {
                foreach (KeyValuePair<CSteamID, BanDataValues> pair in BanQueue)
                {
                    BanDataValues check = dataStore.GetQBanData(pair.Key);
                    int timeLeft = (int)(pair.Value.duration - (DateTime.Now - pair.Value.setTime).TotalSeconds);
                    // Don't sync/kick if the time left is negative, and if they aren't still banned.
                    if (timeLeft > 0 && check != null)
                    {
                        if (Instance.Configuration.Instance.EnableInternalSync)
                        {
                            SteamBlacklist.ban(pair.Key, pair.Value.adminSID, pair.Value.reason, (uint)timeLeft);
                            SteamBlacklist.save();
                            Logger.Log(String.Format("Player {0}[{1}]({2}), has been synced to internal bans.", pair.Value.targetCharName, pair.Value.targetSteamName, pair.Value.targetSID));
                        }
                        else
                        {
                            Steam.kick(pair.Key, pair.Value.reason);
                        }
                    }
                }
                BanQueue.Clear();
            }
            catch
            {
                // NRE on kick/ban, run on next interval.
            }
        }

        public void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            if (!Players.ContainsKey(player.CSteamID))
            {
                PlayersValues playerData = new PlayersValues();
                playerData.playerSID = player.CSteamID;
                playerData.playerCharName = player.CharacterName;
                playerData.playerSteamName = player.SteamName;
                Players.Add(player.CSteamID, playerData);
            }
            else
            {
                // update the stored player name if it doesn't match what they are currently using.
                PlayersValues playerData;
                Players.TryGetValue(player.CSteamID, out playerData);
                if (playerData.playerCharName != player.CharacterName || playerData.playerSteamName != player.SteamName)
                {
                    playerData.playerCharName = player.CharacterName;
                    playerData.playerSteamName = player.SteamName;
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
                    checkBan.targetCharName = player.CharacterName;
                    checkBan.targetSteamName = player.SteamName;
                    // Update player info on ban.
                    dataStore.SetQBanData(player.CSteamID, checkBan);
                }

                // Handle the kicking/syncing of the player in the Ban Queue, Kicking a player in OnConnect will NRE.
                if (!BanQueue.ContainsKey(player.CSteamID))
                    BanQueue.Add(player.CSteamID, checkBan);
            }
        }
    }
}
