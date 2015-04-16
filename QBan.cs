// Plugin written by cartman-2000. https://github.com/cartman-2000/QBans

using Rocket.RocketAPI;
using Rocket.RocketAPI.Events;
using SDG;
using Steamworks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QBan
{
    public class QBan : RocketPlugin
    {
        public static QBan Instance;
        public DataStore dataStore;
        private DateTime lastCalledExpire = DateTime.Now;
        private DateTime lastCalledQueue = DateTime.Now;

        private static Dictionary<CSteamID, PlayersValues> Players = new Dictionary<CSteamID, PlayersValues>();
        private static Dictionary<CSteamID, BanDataValues> BanSync = new Dictionary<CSteamID, BanDataValues>();

        protected override void Load()
        {
            Instance = this;
            dataStore = new DataStore();
            RocketServerEvents.OnPlayerConnected += Events_OnPlayerConnected;
        }

        // search by name of a previous player.
        public static PlayersValues GetPlayer(string search)
        {
            try
            {
                return Players.Values.First(contents => contents.playerCharName.ToLower().Contains(search.ToLower()) || contents.playerSteamName.ToLower().Contains(search.ToLower()));
            }
            catch
            {
                return new PlayersValues();
            }
        }

        // Exact match to a previous player, by SteamID64 number.
        public static PlayersValues GetPlayer(CSteamID search)
        {
            PlayersValues result;
            if(Players.TryGetValue(search, out result))
            {
                return result;
            }
            else
            {
            return new PlayersValues();
            }
        }

        public void FixedUpdate()
        {
            if(this.Loaded)
            {
                CheckExpiredBans();
                QueueBanSync();
            }
        }

        // Queue's banned players to the internal bans.
        private void QueueBanSync()
        {
            if ((DateTime.Now - lastCalledQueue).TotalSeconds > 10)
            {
                foreach (KeyValuePair<CSteamID, BanDataValues> pair in BanSync)
                {
                    SteamBlacklist.ban(pair.Key, pair.Value.adminSID, pair.Value.reason, pair.Value.duration);
                    SteamBlacklist.save();
                    RocketChatManager.print(String.Format("Player {0}[{1}]({2}), has been synced to internal bans.", pair.Value.targetCharName, pair.Value.targetSteamName, pair.Value.targetSID));
                }
                BanSync.Clear();
                lastCalledQueue = DateTime.Now;
            }
        }

        // runs through and removes all of the bans that have expired.
        private void CheckExpiredBans()
        {
            if ((DateTime.Now - lastCalledExpire).TotalSeconds > 600)
            {
                dataStore.CheckExpiredBanData();
                lastCalledExpire = DateTime.Now;
            }
        }


        public void Events_OnPlayerConnected(RocketPlayer player)
        {
            if(!Players.ContainsKey(player.CSteamID))
            {
                PlayersValues playerData = new PlayersValues();
                playerData.playerSID = player.CSteamID;
                playerData.playerCharName = player.CharacterName;
                playerData.playerSteamName = player.SteamName;
                Players.Add(player.CSteamID, playerData);
            }

            //check to see if the player needs to be banned.
            BanDataValues checkBan = dataStore.GetQBanData(player.CSteamID);
            if (checkBan.targetSID != (CSteamID)0)
            {
                DateTime curTime = DateTime.Now;
                uint timeLeft = (uint)(checkBan.duration - (curTime - checkBan.setTime).TotalSeconds);

                // Don't try to ban if it has expired.
                if ((int)timeLeft <= 0)
                {
                    return;
                }

                // NRE bug in inventory save on kick on connect, but the kick still goes through, so this error can be ignored.
                try
                {
                    Steam.kick(checkBan.targetSID, checkBan.reason);
                }
                catch
                {
                    //
                }

                if (checkBan.targetCharName == "" || checkBan.targetSteamName == "")
                {
                    checkBan.targetCharName = player.CharacterName;
                    checkBan.targetSteamName = player.SteamName;
                    // Update player info on ban.
                    dataStore.SetQBanData(player.CSteamID, checkBan);
                }

                // Have to send the adding to the blacklist to a timed update as it would NRE here, 
                // SteamBlacklist.ban also calls ban on the player which causes the same NRE as kicking them do here.
                if (!BanSync.ContainsKey(player.CSteamID))
                {
                    checkBan.duration = timeLeft;
                    BanSync.Add(player.CSteamID, checkBan);
                }
            }
        }
    }
}
