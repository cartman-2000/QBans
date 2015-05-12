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
            get { return "[\"playername\"|SteamID64] [\"Reason\"] - Kicks a player."; }
        }

        public string Name
        {
            get { return "kick"; }
        }

        public bool RunFromConsole
        {
            get { return true; }
        }

        public void Execute(RocketPlayer caller, params string[] command)
        {
            RocketPlayer target = null;
            if(command.Length == 0)
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

            if(command.Length > 2)
            {
                RocketChatManager.Say(caller, "Error: Too many arguments in command.");
                return;
            }

            // Fail on invalid steam id or missing playername.
            if (command[0] == "" || command[0] == " " || command[0] == "0")
            {
                RocketChatManager.Say(caller, "Error: Invalid player name in kick command.");
                return;
            }

            // Fail if there is no reason if it has been set as mandetory.
            if (command.Length == 1 && QBan.Instance.Configuration.ReasonManditory)
            {
                RocketChatManager.Say(caller, "Error: Reason is manditory on kick command.");
                return;
            }
            else if ((command[1] == "" || command [1] == " ") && QBan.Instance.Configuration.ReasonManditory)
            {
                RocketChatManager.Say(caller, "Error: Reason is manditory on kick command.");
                return;
            }

            // Get the player info on the server.
            try
            {
                target = RocketPlayer.FromCSteamID(command[0].StringToCSteamID());
            }
            catch
            {
                target = RocketPlayer.FromName(command[0]);
            }

            // see if we can kick them.
            try
            {
                string reason = "You've been kicked.";
                if (command.Length == 2)
                {
                    reason = command[1];
                }
                target.Kick(reason);
                if (caller != null)
                {
                    RocketChatManager.Say(caller, String.Format("You've kicked player {0}[{1}]({2}).", target.CharacterName, target.SteamName, target.CSteamID));
                    Logger.Log(String.Format("Player {0}[{1}]({2}) has been kicked by admin {3}[{4}]({5}). Reason: {6}", target.CharacterName, target.SteamName, target.CSteamID, caller.CharacterName, caller.SteamName, caller.CSteamID, reason));
                }
                else
                {
                    Logger.Log(String.Format("Player {0}[{1}]({2}), has been kicked by Console. Reason: {3}", target.CharacterName, target.SteamName, target.CSteamID, reason));
                }
            }
            catch
            {
                RocketChatManager.Say(caller, String.Format("Player {0} not found.", command[0]));
            }
        }
    }
}
