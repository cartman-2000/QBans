using Rocket.Logging;
using Rocket.RocketAPI;
using SDG;
using Steamworks;
using System;

namespace QBan
{
    class CommandQUnban : IRocketCommand
    {
        public bool RunFromConsole
        {
            get { return true; }
        }

        public string Name
        {
            get { return "unban"; }
        }

        public string Help
        {
            get { return "[\"Playername\"|SteamID64] - Unbans a player on the server."; }
        }

        public void Execute(RocketPlayer caller, params string[] command)
        {
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

            if (command.Length > 1)
            {
                RocketChatManager.Say(caller, "Invalid arguments in command.");
                return;
            }

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
                callerCharName = caller.CharacterName;
                callerSteamName = caller.SteamName;
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
                        RocketChatManager.Say(caller, String.Format("Could not find player by ID {0} to unban.", command));
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
                    RocketChatManager.Say(caller, String.Format("Could not find player {0} to unban.", command));
                    return;
                }
            }
        }

        // Put the duplicate messages and ban data handling lines in a separate function.
        private static void RemoveBan(RocketPlayer caller, CSteamID callerCSteamID, string callerCharName, string callerSteamName, BanDataValues banData)
        {
            QBan.Instance.dataStore.RemoveQBanData(banData.targetSID);
            SteamBlacklist.unban(banData.targetSID);
            SteamBlacklist.save();

            RocketChatManager.Say(caller, String.Format("You have Unbanned player {0}[{1}]({2}).", banData.targetCharName, banData.targetSteamName, banData.targetSID.ToString()));
            Logger.Log(String.Format("Admin {0}[{1}]({2}), has banned player {3}[{4}]({5}).", callerCharName, callerSteamName, callerCSteamID, banData.targetCharName, banData.targetSteamName, banData.targetSID));
        }
    }
}
