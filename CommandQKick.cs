using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;

namespace QBan
{
    class CommandQKick : IRocketCommand
    {
        public bool AllowFromConsole
        {
            get { return true; }
        }

        public string Help
        {
            get { return "Kicks a player."; }
        }

        public string Syntax
        {
            get { return "<\"playername\"|SteamID64> <\"Reason\">"; }
        }

        public string Name
        {
            get { return "kick"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "qban.kick" }; }
        }

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if(command.Length == 0)
            {
                UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                return;
            }

            UnturnedPlayer target = command.GetUnturnedPlayerParameter(0);
            if(command.Length > 2)
            {
                UnturnedChat.Say(caller, "Error: Too many arguments in command.");
                return;
            }

            // Fail on invalid steam id or missing playername.
            if (command[0].Trim() == String.Empty || command[0].Trim() == "0")
            {
                UnturnedChat.Say(caller, "Error: Invalid player name in kick command.");
                return;
            }

            string commandReason = command.GetStringParameter(1) != null ? command.GetStringParameter(1) : "";
            // Fail if there is no reason when it has been set as mandetory.
            if (commandReason.Trim() == String.Empty && QBan.Instance.Configuration.Instance.ReasonManditory)
            {
                UnturnedChat.Say(caller, "Error: Reason is manditory on kick command.");
                return;
            }

            // see if we can kick them.
            try
            {
                string reason = "You've been kicked.";
                if (command.Length == 2)
                {
                    reason = commandReason.Trim();
                }
                target.Kick(reason);
                if (!(caller is ConsolePlayer))
                {
                    if (caller != target)
                    {
                        UnturnedChat.Say(caller, String.Format("You've kicked player {0}[{1}]({2}).", target.CharacterName, target.SteamName, target.CSteamID));
                    }
                    UnturnedPlayer unturnedCaller = (UnturnedPlayer)caller;
                    Logger.Log(String.Format("Player {0}[{1}]({2}) has been kicked by admin {3}[{4}]({5}). Reason: {6}", target.CharacterName, target.SteamName, target.CSteamID, unturnedCaller.CharacterName, unturnedCaller.SteamName, unturnedCaller.CSteamID, reason));
                }
                else
                {
                    Logger.Log(String.Format("Player {0}[{1}]({2}), has been kicked by Console. Reason: {3}", target.CharacterName, target.SteamName, target.CSteamID, reason));
                }
            }
            catch
            {
                UnturnedChat.Say(caller, String.Format("Player {0} not found.", command[0]));
            }
        }
    }
}
