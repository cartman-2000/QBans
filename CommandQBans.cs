using Rocket.Unturned;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;

namespace QBan
{
    class CommandQBans : IRocketCommand
    {
        public bool RunFromConsole
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
            get { return "[\"playername\"] [page]"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public void Execute(RocketPlayer caller, params string[] command)
        {
            if (command.Length > 2)
            {
                RocketChat.Say(caller, "Error: Too many areguments in command.");
                return;
            }

            // Parse playername in bans command.
            string target = "";
            if (command.Length > 0)
            {
                if (command[0].Trim() == "help")
                {
                    RocketChat.Say(caller, this.Syntax + " - " + this.Help);
                    return;
                }

                // Don't support SteamID64 number in this command.
                try
                {
                    // Check for id number in the command.
                    command[0].StringToCSteamID();
                    RocketChat.Say(caller, "Error: SteamID64 Number's aren't supported in this command.");
                    return;
                }
                catch
                {
                    //
                }

                target = command[0].Trim();
            }

            int recordcount;
            int pagination = 1;
            if (caller == null)
            {
                recordcount = 10;
            }
            else
            {
                recordcount = 4;
            }

            if (command.Length == 2)
            {
                int Out;
                if (int.TryParse(command[1], out Out))
                {
                    pagination = Out;
                    if (pagination < 0)
                    {
                        RocketChat.Say(caller, "Error: page number is negative.");
                        return;
                    }
                }
                else
                {
                    RocketChat.Say(caller, "Error: page number is not a number.");
                    return;
                }

            }

            KeyValuePair<int, List<BanDataValues>> list = QBan.Instance.dataStore.GetQBanDataList(target, recordcount, pagination);
            if (list.Value.Count == 0)
            {
                RocketChat.Say(caller, String.Format("Error: Can't find any players by the name of: {0}", target));
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
                    RocketChat.Say(caller, String.Format("{0}. {1} [{2}] ({3}), by {4} [{5}]", lineNumbers, (value.targetCharName.Length == 0 ? "Not set" : value.targetCharName.Truncate(14)), (value.targetSteamName.Length == 0 ? "Not set" : value.targetSteamName.Truncate(14)), value.targetSID.ToString(), value.adminCharName.Truncate(12), value.adminSteamName.Truncate(12)));
                    RocketChat.Say(caller, String.Format("Set: {0:M/d/yy HH:mm}, Till: {1:M/d/yy}, Left: {2}, Reason: {3}", value.setTime, value.setTime.AddSeconds(value.duration), timeLeftFormat, value.reason));
                    lineNumbers++;
                }
            }
        }
    }
}
