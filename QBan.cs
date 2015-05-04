// Plugin written by cartman-2000. https://github.com/cartman-2000/QBans

using Rocket.Logging;
using Rocket.RocketAPI;
using Rocket.RocketAPI.Events;
using SDG;
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
        private DateTime lastCalledTimer = DateTime.Now;

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
                if ((DateTime.Now - lastCalledTimer).TotalSeconds > 600)
                {
                    if (Configuration.EnableInternalSync)
                    {
                        QueueBanSync();
                    }
                    dataStore.CheckExpiredBanData();
                    lastCalledTimer = DateTime.Now;
                }
            }
        }

        // Queue's banned players to the internal bans.
        private void QueueBanSync()
        {
            foreach (KeyValuePair<CSteamID, BanDataValues> pair in BanSync)
            {
                BanDataValues check = dataStore.GetQBanData(pair.Key);
                // Don't sync if the time left is negative, and if they aren't still banned.
                if ((int)(pair.Value.duration - (DateTime.Now - pair.Value.setTime).TotalSeconds) > 0 && check.targetSID != (CSteamID)0)
                {
                    SteamBlacklist.ban(pair.Key, pair.Value.adminSID, pair.Value.reason, pair.Value.duration);
                    SteamBlacklist.save();
                    Logger.Log(String.Format("Player {0}[{1}]({2}), has been synced to internal bans.", pair.Value.targetCharName, pair.Value.targetSteamName, pair.Value.targetSID));
                }
            }
            BanSync.Clear();
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
                // Only sync to the internal blacklist if syncing has been enabled in the config file.
                if (!BanSync.ContainsKey(player.CSteamID) && Configuration.EnableInternalSync)
                {
                    checkBan.duration = timeLeft;
                    BanSync.Add(player.CSteamID, checkBan);
                }
            }
        }
    }
}
