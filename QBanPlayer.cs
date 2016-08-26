using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;

namespace QBan
{
    public class QBanPlayer : UnturnedPlayerComponent
    {
        private bool kickPlayer;
        private bool runCheck;
        private DateTime startTime;
        private BanDataValues bData;
        private bool ipB;
        private int timeLeft;
        private float ping;

        protected override void Load()
        {
            kickPlayer = false;
            runCheck = false;
        }

        internal void Start()
        {
            startTime = DateTime.Now;
            runCheck = true;
        }

        private void SetKick(BanDataValues banData, bool ipBan)
        {
            kickPlayer = true;
            bData = banData;
            ipB = ipBan;
        }

        public void FixedUpdate()
        {
            if (runCheck)
            {
                ping = Player.Ping;
                if (ping == 0)
                    ping = 1;
                if ((DateTime.Now - startTime).TotalSeconds >= QBan.Instance.Configuration.Instance.KickGracePeriod + (ping * 10))
                {
                    runCheck = false;
                    uint uIP = QBan.GetIP(Player.CSteamID);
                    string sIP = Parser.getIPFromUInt32(uIP);
                    Logger.Log(string.Format("{0} [{1}] ({2}) IP: {3}", Player.CharacterName, Player.SteamName, Player.CSteamID, sIP));

                    if (!QBan.Players.ContainsKey(Player.CSteamID))
                    {
                        PlayersValues playerData = new PlayersValues();
                        playerData.playerSID = Player.CSteamID;
                        playerData.playerCharName = Player.CharacterName;
                        playerData.playerSteamName = Player.SteamName;
                        playerData.playerUIP = uIP;
                        QBan.Players.Add(Player.CSteamID, playerData);
                    }
                    else
                    {
                        // update the stored player name if it doesn't match what they are currently using.
                        PlayersValues playerData = QBan.Players[Player.CSteamID];
                        if (playerData.playerCharName != Player.CharacterName || playerData.playerSteamName != Player.SteamName || playerData.playerUIP != uIP)
                        {
                            playerData.playerCharName = Player.CharacterName;
                            playerData.playerSteamName = Player.SteamName;
                            playerData.playerUIP = uIP;
                        }
                    }

                    //check to see if the player needs to be banned.
                    BanDataValues checkBan = QBan.DataStore.GetQBanData(Player.CSteamID);
                    bool ipBanMatch = false;
                    if (checkBan == null)
                    {
                        // Don't query an ip ban if the players IP isn't set.
                        if (uIP != 0)
                            checkBan = QBan.DataStore.GetIPQBanData(uIP);
                        if (checkBan != null)
                            ipBanMatch = true;
                    }
                    if (checkBan != null)
                    {
                        // Don't try to ban if it has expired, or if they have the iban.overide permission.
                        timeLeft = (int)(checkBan.duration - (DateTime.Now - checkBan.setTime).TotalSeconds);
                        if ((timeLeft <= 0) || (ipBanMatch && Player.HasPermission("ibanoveride")))
                        {
                            enabled = false;
                            return;
                        }

                        if (ipBanMatch)
                        {
                            Logger.Log(string.Format("IP ban: IP match on this player {0} [{1}] ({2}), Matches player: {3} [{4}] ({5}) IP: {6}, kicking!", Player.CharacterName, Player.SteamName, Player.CSteamID.ToString(), checkBan.targetCharName, checkBan.targetSteamName, checkBan.targetSID.ToString(), sIP));
                        }

                        if ((checkBan.targetCharName == "" || checkBan.targetSteamName == "" || checkBan.uIP != uIP) && !ipBanMatch)
                        {
                            checkBan.targetCharName = Player.CharacterName.Sanitze();
                            checkBan.targetSteamName = Player.SteamName.Sanitze();
                            checkBan.uIP = uIP;
                            // Update player info on ban.
                            QBan.DataStore.SetQBanData(Player.CSteamID, checkBan);
                        }
                        else
                        {
                            BanDataValues temp = checkBan;
                            checkBan = new BanDataValues();
                            checkBan.targetSID = Player.CSteamID;
                            checkBan.targetCharName = Player.CharacterName.Sanitze();
                            checkBan.targetSteamName = Player.SteamName.Sanitze();
                            checkBan.adminSID = temp.adminSID;
                            checkBan.adminCharName = temp.adminCharName;
                            checkBan.adminSteamName = temp.adminSteamName;
                            checkBan.setTime = temp.setTime;
                            checkBan.duration = temp.duration;
                            checkBan.reason = temp.reason;
                            checkBan.isIPBan = false;
                            checkBan.isIPBMatch = true;
                            checkBan.uIP = temp.uIP;

                            // Either use the preset time that was in the matching ban, or adjust the time and duration to be at the current time, with the same expiration time. Will also determine whether or not the auto ip bans will appear in the bans list with the ip banned entry, or at the end of the list.
                            if (!QBan.Instance.Configuration.Instance.IPBanAutoAddUsePresetTime)
                            {
                                checkBan.setTime = DateTime.Now;
                                checkBan.duration = (uint)timeLeft;
                            }
                        }

                        // Send the player over to the player component to handle the kicking and syncing of ban data.
                        SetKick(checkBan, ipBanMatch);
                    }
                    else
                        enabled = false;
                }
            }

            if (kickPlayer)
            {
                try
                {
                    // Don't sync/kick if the time left is negative.
                    if ((QBan.Instance.Configuration.Instance.EnableInternalSync && !ipB) || (QBan.Instance.Configuration.Instance.EnableInternalSync && QBan.Instance.Configuration.Instance.IPBanAutoAdd && ipB))
                    {
                        bData.reason += ipB ? " (IP ban match.)" : "";
                        if (QBan.Instance.Configuration.Instance.IPBanAutoAdd && ipB)
                            QBan.DataStore.SetQBanData(Player.CSteamID, bData);
                        SteamBlacklist.ban(bData.targetSID, 0, bData.adminSID, bData.reason, (uint)timeLeft);
                        SteamBlacklist.save();
                        Logger.Log(String.Format("Player: {0} [{1}] ({2}), IP: {3}, has been synced to internal bans, From IP Ban: {4}.", bData.targetCharName, bData.targetSteamName, bData.targetSID.ToString(), Parser.getIPFromUInt32(bData.uIP), ipB.ToString()));
                    }
                    else
                    {
                        if (ipB)
                            Player.Kick(string.Format("IP ban blocked ip address detected. Original IP ban for: {0}, Time left: {1} seconds.", bData.reason, timeLeft));
                        else
                            Player.Kick(string.Format("Banned for: {0}, Time left: {1} seconds.", bData.reason, timeLeft));
                    }
                    kickPlayer = false;
                }
                catch
                {
                    // NRE on kick/ban, run on next interval.
                }

            }
        }
    }
}
