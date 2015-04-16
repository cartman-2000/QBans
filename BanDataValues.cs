using System;
using Steamworks;

namespace QBan
{
    public class BanDataValues
    {
        public BanDataValues()
        {

        }

        public CSteamID targetSID { get; set; }
        public string targetCharName { get; set; }
        public string targetSteamName { get; set; }

        public CSteamID adminSID { get; set; }
        public string adminCharName { get; set; }
        public string adminSteamName { get; set; }

        public string reason { get; set; }
        public uint duration { get; set; }
        public DateTime setTime { get; set; }
    }
}
