using Rocket.RocketAPI;
using SDG;
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
            get { return "[playername]/[page] - Shows a list of bans on the server, use playername to narrow the list."; }
        }

        public void Execute(RocketPlayer caller, string command)
        {
            String[] componentsFromString = Parser.getComponentsFromSerial(command, '/');
            if (componentsFromString.Length > 2)
            {
                RocketChatManager.Say(caller, "Too many areguments.");
            }

            // Don't support SteamID64 number in this command.
            try
            {
                if (componentsFromString.Length > 0)
                {
                    // Check for id number in the command.
                    componentsFromString[0].StringToCSteamID();
                    RocketChatManager.Say(caller, "SteamID64 Number's aren't supported in this command.");
                    return;
                }
            }
            catch
            {
                //
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

            if (componentsFromString.Length == 2)
            {
                int Out;
                if (int.TryParse(componentsFromString[1], out Out))
                {
                    pagination = Out;
                    if (pagination < 0)
                    {
                        RocketChatManager.Say("Error: page number is negative.");
                        return;
                    }
                }
                else
                {
                    RocketChatManager.Say("Error: page number is not a number.");
                    return;
                }

            }

            string target = "";
            if(componentsFromString.Length > 0)
            {
                if (componentsFromString[0] == "help")
                {
                    RocketChatManager.Say(caller, this.Help);
                    return;
                }
                target = componentsFromString[0];
            }

            KeyValuePair<int, List<BanDataValues>> list = QBan.Instance.dataStore.GetQBanDataList(target, recordcount, pagination);
            if (list.Value.Count == 0)
            {
                RocketChatManager.Say(caller, String.Format("Can't find any players by the name of: {0}", target));
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
                    RocketChatManager.Say(caller, String.Format("{0}. {1} [{2}] ({3}), by {4} [{5}]", lineNumbers, (value.targetCharName.Length == 0 ? "Not set" : value.targetCharName.Truncate(14)), (value.targetSteamName.Length == 0 ? "Not set" : value.targetSteamName.Truncate(14)), value.targetSID.ToString(), value.adminCharName.Truncate(12), value.adminSteamName.Truncate(12)));
                    RocketChatManager.Say(caller, String.Format("Set: {0}, Till: {1}, Left: {2}, Reason: {3}", value.setTime.ToString("M/d/yy HH:mm"), value.setTime.AddSeconds(value.duration).ToString("M/d/yy"),timeLeftFormat, value.reason));
                    lineNumbers++;
                }
            }
        }
    }
}
