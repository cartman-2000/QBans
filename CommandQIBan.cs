using Rocket.API;
using Rocket.Unturned.Chat;
using System.Collections.Generic;

namespace QBan
{
    public class CommandQIBan : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public bool AllowFromConsole
        {
            get { return true; }
        }

        public string Help
        {
            get { return QBanCommandCommon.IBanHelp; }
        }

        public string Name
        {
            get { return "iban"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "qban.iban" }; }
        }

        public string Syntax
        {
            get { return QBanCommandCommon.Syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, this.Syntax + " - " + this.Help);
                return;
            }
            QBanCommandCommon.Ban(caller, command, BanType.IPBAN);
        }
    }
}
