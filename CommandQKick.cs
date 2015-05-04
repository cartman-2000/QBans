using System;
using Rocket.Logging;
using Rocket.RocketAPI;
using SDG;

namespace QBan
{
    class CommandQKick : IRocketCommand
    {
        public string Help
        {
            get { return "[playername|SteamID64]/[Reason] - Kicks a player."; }
        }

        public string Name
        {
            get { return "kick"; }
        }

        public bool RunFromConsole
        {
            get { return true; }
        }

        public void Execute(RocketPlayer caller, string command)
        {
            RocketPlayer target = null;
            String[] componentsFromString = Parser.getComponentsFromSerial(command, '/');

            if(componentsFromString.Length == 0)
            {
                RocketChatManager.Say(caller, this.Help);
                return;
            }
            if(componentsFromString.Length > 2)
            {
                RocketChatManager.Say(caller, "Invalid arguments.");
                return;
            }

            // Get the player info on the server.
            try
            {
                target = RocketPlayer.FromCSteamID(componentsFromString[0].StringToCSteamID());
            }
            catch
            {
                target = RocketPlayer.FromName(componentsFromString[0]);
            }

            // see if we can kick them.
            try
            {
                if (componentsFromString.Length == 2)
                {
                    target.Kick(componentsFromString[1]);
                }
                else
                {
                    target.Kick("You've been kicked.");
                }
                if (caller != null)
                {
                    RocketChatManager.Say(caller, String.Format("You've kicked player {0}[{1}]({2}).", target.CharacterName, target.SteamName, target.CSteamID));
                    Logger.Log(String.Format("Player {0}[{1}]({2}) has been kicked by admin {3}[{4}]({5})", target.CharacterName, target.SteamName, target.CSteamID, caller.CharacterName, caller.SteamName, caller.CSteamID));
                }
                else
                {
                    Logger.Log(String.Format("Player {0}[{1}]({2}), has been kicked by Console.", target.CharacterName, target.SteamName, target.CSteamID));
                }
            }
            catch
            {
                RocketChatManager.Say(caller, String.Format("Player {0} not found.", componentsFromString[0]));
            }
        }
    }
}
