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
    class CommandQUnban : IRocketCommand
    {
        public bool AllowFromConsole
        {
            get { return true; }
        }

        public string Name
        {
            get { return "unban"; }
        }

        public string Help
        {
            get { return "Unbans a player on the server."; }
        }

        public string Syntax
        {
            get { return "<\"playername\"|SteamID64>"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "qban.unban" }; }
        }

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                return;
            }

            if (command.Length > 1)
            {
                UnturnedChat.Say(caller, "Error: Too many arguments in command.");
                return;
            }

            // Fail on invalid steam id or missing playername.
            if (command[0].Trim() == String.Empty || command[0].Trim() == "0")
            {
                UnturnedChat.Say(caller, "Error: Invalid player name in unban command.");
                return;
            }

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
                callerCharName = unturnedCaller.CharacterName;
                callerSteamName = unturnedCaller.SteamName;
            }

            bool isCSteamID;
            BanDataValues banData;
            try
            {
                banData = QBan.Instance.dataStore.GetQBanData(command[0].StringToCSteamID());
                isCSteamID = true;
            }
            catch
            {
                banData = QBan.Instance.dataStore.GetQBanData(command[0]);
                isCSteamID = false;
            }

            if (isCSteamID)
            {
                // Player has been found.
                if (banData.targetSID != (CSteamID)0)
                {
                    RemoveBan(caller, callerCSteamID, callerCharName, callerSteamName, banData);
                }
                // Player hasen't been found.
                else
                {
                    // Check to see if the ban exists in the in-built bans, and remove if it is, otherwise fail.
                    SteamBlacklistID Out;
                    if (SteamBlacklist.checkBanned(command[0].StringToCSteamID(), out Out))
                    {
                        banData.targetSID = command[0].StringToCSteamID();
                        RemoveBan(caller, callerCSteamID, callerCharName, callerSteamName, banData);
                    }
                    else
                    {
                        UnturnedChat.Say(caller, String.Format("Error: Could not find player by ID {0} to unban.", command));
                        return;
                    }
                }
            }
            else
            {
                // Player has been found.
                if (banData.targetSID != (CSteamID)0)
                {
                    RemoveBan(caller, callerCSteamID, callerCharName, callerSteamName, banData);
                }
                // Player hasen't been found.
                else
                {
                    UnturnedChat.Say(caller, String.Format("Error: Could not find player {0} to unban.", command));
                    return;
                }
            }
        }

        // Put the duplicate messages and ban data handling lines in a separate function.
        private static void RemoveBan(IRocketPlayer caller, CSteamID callerCSteamID, string callerCharName, string callerSteamName, BanDataValues banData)
        {
            QBan.Instance.dataStore.RemoveQBanData(banData.targetSID);
            SteamBlacklist.unban(banData.targetSID);
            SteamBlacklist.save();

            UnturnedChat.Say(caller, String.Format("You have unbanned player {0}[{1}]({2}).", banData.targetCharName, banData.targetSteamName, banData.targetSID.ToString()));
            Logger.Log(String.Format("Admin {0}[{1}]({2}), has unbanned player {3}[{4}]({5}).", callerCharName, callerSteamName, callerCSteamID, banData.targetCharName, banData.targetSteamName, banData.targetSID));
        }
    }
}
