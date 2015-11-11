using Steamworks;

namespace QBan
{
    public class PlayersValues
    {
        public PlayersValues() { }

        public CSteamID playerSID  { get; set; }
        public string playerCharName  { get; set; }
        public string playerSteamName  { get; set; }
        public uint playerUIP { get; set; }
    }
}
