using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace QBan
{
    public class CommandQBans : IRocketCommand
    {
        public bool AllowFromConsole
        {
            get { return true; }
        }

        public string Name
        {
            get { return "bans"; }
        }

        public string Help
        {
            get { return "Shows a list of bans on the server, use playername to narrow the list."; }
        }

        public string Syntax
        {
            get { return "<page> [\"playername\"]"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "qban.bans" }; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length > 2)
            {
                UnturnedChat.Say(caller, "Error: Too many areguments in command.");
                return;
            }

            // Parse playername in bans command.
            string target = command.GetStringParameter(1) != null ? command.GetStringParameter(1) : "";
            if (command.Length == 1)
            {
                if (command[0].Trim() == "help")
                {
                    UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                    return;
                }
            }

            int recordcount;
            int pagination = 1;
            if (caller is ConsolePlayer)
            {
                recordcount = 10;
            }
            else
            {
                recordcount = 4;
            }

            if (command.GetInt32Parameter(0) != null)
            {
                pagination = (int)command.GetInt32Parameter(0);
                if (pagination < 1)
                {
                    UnturnedChat.Say(caller, "Error: Page number is zero, or negative.");
                    return;
                }
            }
            else if (command.Length > 0)
            {
                UnturnedChat.Say(caller, "Error: Page number is not a number.");
                return;
            }

            KeyValuePair<int, List<BanDataValues>> list = QBan.Instance.dataStore.GetQBanDataList(target, recordcount, pagination);
            if (list.Value.Count == 0)
            {
                UnturnedChat.Say(caller, String.Format("Error: Can't find any players by the name of: {0}", target));
                return;
            }
            else
            {
                int lineNumbers = list.Key;
                foreach (BanDataValues value in list.Value)
                {
                    int timeLeft = (int)(value.duration - (DateTime.Now - value.setTime).TotalSeconds);
                    string timeLeftFormat = "";

                    // Format a days, hours minutes and seconds left string for time left
                    if (timeLeft >= (60 * 60 * 24))
                    {
                        timeLeftFormat = ((int)(timeLeft / (60 * 60 * 24))).ToString() + "d ";
                    }
                    if (timeLeft >= (60 * 60))
                    {
                        timeLeftFormat +=  ((int)((timeLeft / (60 * 60)) %  24)).ToString() + "h ";
                    }
                    if (timeLeft >= 60)
                    {
                        timeLeftFormat += ((int)((timeLeft / 60) % 60)).ToString() + "m";
                    }
                    else if (timeLeft <= 0)
                    {
                        timeLeftFormat += "Expired";
                    }
                    else
                    {
                        timeLeftFormat += timeLeft.ToString() + "s";
                    }

                    // build the strings for the ban info.
                    if (caller is ConsolePlayer)
                    {
                        Logger.Log(String.Format("{0}. {1} [{2}] ({3}), by {4} [{5}]", lineNumbers, (value.targetCharName.Length == 0 ? "Not set" : value.targetCharName), (value.targetSteamName.Length == 0 ? "Not set" : value.targetSteamName), value.targetSID.ToString(), value.adminCharName, value.adminSteamName));
                        Logger.Log(String.Format("{0:M/d/yy HH:mm}|{1:M/d/yy}|{2}, {3}|{4}, Reason: {5}", value.setTime, value.setTime.AddSeconds(value.duration), timeLeftFormat, value.isIPBan.ToString(), (value.uIP == 0 ? "Not set" : Parser.getIPFromUInt32(value.uIP)), value.reason));
                    }
                    else
                    {
                        UnturnedChat.Say(caller, String.Format("{0}. {1} [{2}] ({3}), by {4} [{5}]", lineNumbers, (value.targetCharName.Length == 0 ? "Not set" : value.targetCharName.Truncate(14)), (value.targetSteamName.Length == 0 ? "Not set" : value.targetSteamName.Truncate(14)), value.targetSID.ToString(), value.adminCharName.Truncate(12), value.adminSteamName.Truncate(12)));
                        UnturnedChat.Say(caller, String.Format("{0:M/d/yy HH:mm}|{1:M/d/yy}|{2}, {3}|{4}, Reason: {5}", value.setTime, value.setTime.AddSeconds(value.duration), timeLeftFormat, value.isIPBan.ToString(), (value.uIP == 0 ? "Not set" : Parser.getIPFromUInt32(value.uIP)), value.reason));
                    }
                    lineNumbers++;
                }
            }
        }
    }
}
