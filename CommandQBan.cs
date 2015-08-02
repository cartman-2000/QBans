using System;
using System.Collections.Generic;
using SDG.Unturned;
using Steamworks;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;

namespace QBan
{
    class CommandQBan : IRocketCommand
    {
        public bool AllowFromConsole
        {
            get { return true; }
        }

        public string Name
        {
            get { return "ban"; }
        }

        public string Help
        {
            get { return "Bans player, no duration for permban, 4th peram is days, hours, or minutes."; }
        }

        public string Syntax
        {
            get { return "<\"Playername\"|SteamID64> <\"reason\"> [duration] [d|h|m]"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "qban.ban" }; }
        }

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                return;
            }

            UnturnedPlayer target = null;
            long banDuration = 1000000000;
            long maxDuration = banDuration;
            string banReason = "Banned";
            string playerName = command[0].Replace("><", "");

            if (command.Length > 4)
            {
                UnturnedChat.Say(caller, "Too many arguments in command.");
                return;
            }

            // Fail on invalid steam id or missing playername. And set the reason for the ban, if one is set.
            if (command.Length >= 1)
            {
                if (playerName.Trim() == String.Empty || playerName.Trim() == "0")
                {
                    UnturnedChat.Say(caller, "Error: Invalid player name in ban command.");
                    return;
                }
                if ((command.GetStringParameter(1) != null ? command.GetStringParameter(1) : "").Replace("><", "").Trim() == String.Empty && QBan.Instance.Configuration.Instance.ReasonManditory)
                {
                    UnturnedChat.Say(caller, "Error: Reason is manditory on ban command.");
                    return;
                }
                else if (command.Length >= 2)
                {
                    banReason = command.GetStringParameter(1).Replace("><", "").Trim();
                }
            }

            // Get uint out of the duration, set the default duration if duration either empty or set to 0.
            if (command.Length >= 3)
            {
                if (!long.TryParse(command[2], out banDuration))
                {
                    UnturnedChat.Say(caller, "Error: Invalid number entered for duration.");
                    return;
                }
                else if (banDuration < 0)
                {
                    UnturnedChat.Say(caller, "Error: Duration is a negative number.");
                    return;
                }
                else if (banDuration == 0 || banDuration >= maxDuration)
                {
                    banDuration = maxDuration;
                }
            }

            // Parse and handle time modifier.
            if (command.Length == 4)
            {
                switch (command[3].ToLower())
                {
                    case "d":
                        banDuration = banDuration * (60 * 60 * 24);
                        break;
                    case "h":
                        banDuration = banDuration * (60 * 60);
                        break;
                    case "m":
                        banDuration = banDuration * 60;
                        break;
                    default:
                        UnturnedChat.Say(caller, "Error: Improper time modifier entered into command.");
                        return;
                }
                if (banDuration > maxDuration)
                {
                    banDuration = maxDuration;
                }
            }

            // Set caller info as it is null if it is sent from the console.
            CSteamID callerCSteamID;
            string callerCharName;
            string callerSteamName;
            if (caller is ConsolePlayer)
            {
                callerCSteamID = (CSteamID)0;
                callerCharName = "Console";
                callerSteamName = "Console";
            }
            else
            {
                UnturnedPlayer unturnedCaller = (UnturnedPlayer)caller;
                callerCSteamID = unturnedCaller.CSteamID;
                callerCharName = unturnedCaller.CharacterName.Replace("><", "");
                callerSteamName = unturnedCaller.SteamName.Replace("><", "");
            }

            // Is what is entered in the command a SteamID64 number. Also set the variable to check to see if the player has played on the server since it's start.
            bool isCSteamID;
            PlayersValues getPlayer;
            try
            {
                getPlayer = QBan.GetPlayer(playerName.StringToCSteamID());
                isCSteamID = true;
            }
            catch
            {
                getPlayer = QBan.GetPlayer(playerName);
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
            target = UnturnedPlayer.FromName(playerName);
            if (target != null)
            {
                // player is online, handle them normally.
                data.targetSID = target.CSteamID;
                data.targetCharName = target.CharacterName.Replace("><", "");
                data.targetSteamName = target.SteamName.Replace("><", "");
                SetBan(caller, data);
            }
            else
            {
                // Player is either entered incorrectly or is offline, run checks to see if the player can be found out of the previous players dictionary, or directly ban for an entered SteamID64 number.
                // Got a hit on the GetPlayer info, they have previously played on the server since last start.
                if (getPlayer != null)
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
                        UnturnedChat.Say(caller, String.Format("Error: Can't find a player by the name of {0}, that has played on the server before.", playerName));
                        return;
                    }
                    else
                    {
                        data.targetSID = playerName.StringToCSteamID();
                        data.targetCharName = "";
                        data.targetSteamName = "";
                        if (QBan.Instance.dataStore.SetQBanData(data.targetSID, data))
                        {
                            //Unsync a previous set ban so the player info can be set when they next connect to the server.
                            SteamBlacklist.unban(data.targetSID);
                            SteamBlacklist.save();

                            UnturnedChat.Say(caller, String.Format("Player SteamID64:{0}, has been banned for {1} seconds.", data.targetSID.ToString(), data.duration.ToString()));
                            UnturnedChat.Say(caller, String.Format("Reason: {0}", data.reason));
                            Logger.Log(String.Format("Admin {0}[{1}]({2}), has banned SteamID64:{3} for {4}, for {5} seconds.", callerCharName, callerSteamName, callerCSteamID.ToString(), data.targetSID.ToString(), data.reason, data.duration.ToString()));
                        }
                        else
                        {
                            UnturnedChat.Say(caller, "Error: Was unable to set the ban record for the player.");
                            return;
                        }
                    }
                }
            }
        }

        // Seperated the duplicate lines of code for the messages and the ban saving/syncing.
        private static void SetBan(IRocketPlayer caller, BanDataValues data)
        {
            if (QBan.Instance.dataStore.SetQBanData(data.targetSID, data))
            {
                if (QBan.Instance.Configuration.Instance.EnableInternalSync)
                {
                    SteamBlacklist.ban(data.targetSID, data.adminSID, data.reason, data.duration);
                    SteamBlacklist.save();
                }

                UnturnedChat.Say(caller, String.Format("Player {0}[{1}], has been banned for {2} seconds.", data.targetCharName.Truncate(12), data.targetSteamName.Truncate(12), data.duration.ToString()));
                UnturnedChat.Say(caller, String.Format("Reason: {0}", data.reason));
                Logger.Log(String.Format("Admin {0}[{1}]({2}), has banned player {3}[{4}]({5}) for {6}, for {7} seconds.", data.adminCharName, data.adminSteamName, data.adminSID.ToString(), data.targetCharName, data.targetSteamName, data.targetSID.ToString(), data.reason, data.duration.ToString()));
            }
            else
            {
                UnturnedChat.Say(caller, "Error: Was unable to set the ban record for the player.");
            }
        }
    }
}
