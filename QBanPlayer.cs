using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;

namespace QBan
{
    public class QBanPlayer : UnturnedPlayerComponent
    {
        private bool kickPlayer;
        private DateTime startTime;
        private UnturnedPlayer pData;
        private BanDataValues bData;

        protected override void Load()
        {
            kickPlayer = false;
        }

        internal void SetKick(UnturnedPlayer player, BanDataValues banData)
        {
            startTime = DateTime.Now;
            kickPlayer = true;
            pData = player;
            bData = banData;
        }

        public void FixedUpdate()
        {
            if (kickPlayer)
            {
                try
                {
                    float ping = pData.Ping;
                    if (ping == 0)
                        ping = 1;
                    if ((DateTime.Now - startTime).TotalSeconds >= QBan.Instance.Configuration.Instance.KickGracePeriod + (pData.Ping * 10))
                    {
                        int timeLeft = (int)(bData.duration - (DateTime.Now - bData.setTime).TotalSeconds);
                        // Don't sync/kick if the time left is negative.
                        if (timeLeft > 0)
                        {
                            if (QBan.Instance.Configuration.Instance.EnableInternalSync)
                            {
                                SteamBlacklist.ban(bData.targetSID, bData.adminSID, bData.reason, (uint)timeLeft);
                                SteamBlacklist.save();
                                Logger.Log(String.Format("Player {0}[{1}]({2}), has been synced to internal bans.", bData.targetCharName, bData.targetSteamName, bData.targetSID));
                            }
                            else
                            {
                                Provider.kick(bData.targetSID, bData.reason);
                            }
                        }
                        kickPlayer = false;
                    }

                }
                catch
                {
                    // NRE on kick/ban, run on next interval.
                }

            }
        }

    }
}
