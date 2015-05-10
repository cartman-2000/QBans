using Rocket.Logging;
using Rocket.RocketAPI;
using SDG;
using Steamworks;
using System;

namespace QBan
{
    class CommandQBan : IRocketCommand
    {
        public bool RunFromConsole
        {
            get { return true; }
        }

        public string Name
        {
            get { return "ban"; }
        }

        public string Help
        {
            get { return "[\"Playername\"|SteamID64] [\"reason\"] [duration] [d|h|m] - bans player, no duration for permban, 4th peram is days, hours, or minutes."; }
        }

        public void Execute(RocketPlayer caller, params string[] command)
        {
            SteamPlayer target = null;
            long banDuration = 1000000000;
            long maxDuration = banDuration;
            string banReason = "Banned";

            if (command.Length == 0)
            {
                RocketChatManager.Say(caller, this.Help);
                return;
            }

            if (command.Length == 1)
            {
                if (command[0].Contains("/"))
                {
                    command = Parser.getComponentsFromSerial(command[0], '/');
                }
            }

            if (command.Length > 4 || command[0] == "0")
            {
                RocketChatManager.Say(caller, "Invalid arguments in command.");
                return;
            }

            // Set reason for ban, if one has been set.
            if (command.Length >= 2)
            {
                if (command[1] != "")
                {
                    banReason = command[1].Replace("><", "");
                }
            }

            // Get uint out of the duration, set the default duration if duration either empty or set to 0.
            if (command.Length >= 3)
            {
                if (!long.TryParse(command[2], out banDuration) && command[2] != "")
                {
                    RocketChatManager.Say(caller, "Error: Invalid number entered for duration.");
                    return;
                }
                else if (banDuration < 0)
                {
                    RocketChatManager.Say(caller, "Error: Duration is a negative number.");
                    return;
                }
                else if (command[2] == "" || banDuration == 0 || banDuration >= maxDuration)
                {
                    banDuration = maxDuration;
                }
            }

            // Parse and handle time modifier.
            if (command.Length == 4)
            {
                if (command[3] != "d" && command[3] != "h" && command[3] != "m")
                {
                    RocketChatManager.Say(caller, "Improper time modifier entered into command.");
                    return;
                }
                if (command[3] == "d" && banDuration * (60 * 60 * 24) < maxDuration)
                {
                    banDuration = banDuration * (60 * 60 * 24);
                }
                else if (command[3] == "h" && banDuration * (60 * 60) < maxDuration)
                {
                    banDuration = banDuration * (60 * 60);
                }
                else if (command[3] == "m" && banDuration * 60 < maxDuration)
                {
                    banDuration = banDuration * 60;
                }
                else
                {
                    banDuration = maxDuration;
                }
            }

            // Set caller info as it is null if it is sent from the console.
            CSteamID callerCSteamID;
            string callerCharName;
            string callerSteamName;
            if (caller == null)
            {
                callerCSteamID = (CSteamID)0;
                callerCharName = "Console";
                callerSteamName = "Console";
            }
            else
            {
                callerCSteamID = caller.CSteamID;
                callerCharName = caller.CharacterName.Replace("><", "");
                callerSteamName = caller.SteamName.Replace("><", "");
            }

            // Is what is entered in the command a SteamID64 number. Also set the variable to check to see if the player has played on the server since it's start.
            bool isCSteamID;
            PlayersValues getPlayer;
            try
            {
                getPlayer = QBan.GetPlayer(command[0].StringToCSteamID());
                isCSteamID = true;
            }
            catch
            {
                getPlayer = QBan.GetPlayer(command[0]);
                isCSteamID = false;
            }

            BanDataValues data = new BanDataValues();
            data.adminSID = callerCSteamID;
            data.adminCharName = callerCharName;
            data.adminSteamName = callerSteamName;
            data.reason = banReason;
            data.duration = (uint)banDuration;
            data.setTime = DateTime.Now;

            // Running player checks.
            if (PlayerTool.tryGetSteamPlayer(command[0], out target))
            {
                // player is online, handle them normally.
                data.targetSID = target.SteamPlayerID.CSteamID;
                data.targetCharName = target.SteamPlayerID.CharacterName.Replace("><", "");
                data.targetSteamName = target.SteamPlayerID.SteamName.Replace("><", "");
                SetBan(caller, data);
            }
            else
            {
                // Player is either entered incorrectly or is offline, run checks to see if the player can be found out of the previous players dictionary, or directly ban for an entered SteamID64 number.
                // Got a hit on the GetPlayer info, they have previously played on the server since last start.
                if (getPlayer.playerSID != (CSteamID)0)
                {
                    data.targetSID = getPlayer.playerSID;
                    data.targetCharName = getPlayer.playerCharName.Replace("><", "");
                    data.targetSteamName = getPlayer.playerSteamName.Replace("><", "");
                    SetBan(caller, data);
                }
                // Didn't get a hit on the player info, They haven't played on the server since last start. Continue if a SteamID64 number was entered in the command.
                else
                {
                    // Can't ban a player if the SteamID64 number can't be found. Explicitly add the ban if what was entered was a SteamID64 number.
                    if (!isCSteamID)
                    {
                        RocketChatManager.Say(caller, String.Format("Can't find a player by the name of {0}, that has played on the server before.", command[0]));
                        return;
                    }
                    else
                    {
                        data.targetSID = command[0].StringToCSteamID();
                        data.targetCharName = "";
                        data.targetSteamName = "";
                        if (QBan.Instance.dataStore.SetQBanData(data.targetSID, data))
                        {
                            //Unsync a previous set ban so the player info can be set when they next connect to the server.
                            SteamBlacklist.unban(data.targetSID);
                            SteamBlacklist.save();

                            RocketChatManager.Say(caller, String.Format("Player SteamID64:{0}, has been banned for {1} seconds.", data.targetSID.ToString(), data.duration.ToString()));
                            RocketChatManager.Say(caller, String.Format("Reason: {0}", data.reason));
                            Logger.Log(String.Format("Admin {0}[{1}]({2}), has banned SteamID64:{3} for {4}, for {5} seconds.", callerCharName, callerSteamName, callerCSteamID.ToString(), data.targetSID.ToString(), data.reason, data.duration.ToString()));
                        }
                        else
                        {
                            RocketChatManager.Say(caller, "Error: Was unable to set the ban record for the player.");
                            return;
                        }
                    }
                }
            }
        }

        // Seperated the duplicate lines of code for the messages and the ban saving/syncing.
        private static void SetBan(RocketPlayer caller, BanDataValues data)
        {
            if (QBan.Instance.dataStore.SetQBanData(data.targetSID, data))
            {
                if (QBan.Instance.Configuration.EnableInternalSync)
                {
                    SteamBlacklist.ban(data.targetSID, data.adminSID, data.reason, data.duration);
                    SteamBlacklist.save();
                }

                RocketChatManager.Say(caller, String.Format("Player {0}[{1}], has been banned for {2} seconds.", data.targetCharName.Truncate(12), data.targetSteamName.Truncate(12), data.duration.ToString()));
                RocketChatManager.Say(caller, String.Format("Reason: {0}", data.reason));
                Logger.Log(String.Format("Admin {0}[{1}]({2}), has banned player {3}[{4}]({5}) for {6}, for {7} seconds.", data.adminCharName, data.adminSteamName, data.adminSID.ToString(), data.targetCharName, data.targetSteamName, data.targetSID.ToString(), data.reason, data.duration.ToString()));
            }
            else
            {
                RocketChatManager.Say(caller, "Error: Was unable to set the ban record for the player.");
            }
        }
    }
}
