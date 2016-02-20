using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QBan
{
    public class QBanConfiguration : IRocketPluginConfiguration
    {
        public bool EnableInternalSync = true;
        public bool EnableExpiredExport = true;
        public bool ReasonManditory = true;
        public uint KickGracePeriod = 4;
        public bool IPBanAutoAdd = false;
        public bool IPBanAutoAddUsePresetTime = true;
        [XmlArray("Bans"), XmlArrayItem(ElementName = "Ban")]
        public List<BanDataValues> Bans = new List<BanDataValues>();
        [XmlArray("ExpiredBans"), XmlArrayItem(ElementName = "ExpiredBan")]
        public List<BanDataValues> ExpiredBans = new List<BanDataValues>();

        public void LoadDefaults() { }
    }
}
