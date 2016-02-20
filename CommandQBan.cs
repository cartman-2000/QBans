using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;

namespace QBan
{
    public class CommandQBan : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Name
        {
            get { return "ban"; }
        }

        public string Help
        {
            get { return QBanCommandCommon.BanHelp; }
        }

        public string Syntax
        {
            get { return QBanCommandCommon.Syntax; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public List<string> Permissions
        {
            get { return new List<string>() { "qban.ban" }; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                return;
            }
            QBanCommandCommon.Ban(caller, command, BanType.BAN);
        }
    }
}
