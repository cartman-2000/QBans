﻿using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QBan
{
    public class QBanConfiguration : IRocketPluginConfiguration
    {
        public bool EnableInternalSync;
        public bool EnableExpiredExport;
        public bool ReasonManditory;
        public uint KickGracePeriod;
        [XmlArray("Bans"), XmlArrayItem(ElementName = "Ban")]
        public List<BanDataValues> Bans;
        [XmlArray("ExpiredBans"), XmlArrayItem(ElementName = "ExpiredBan")]
        public List<BanDataValues> ExpiredBans;
        public void LoadDefaults()
        {
            EnableInternalSync = true;
            EnableExpiredExport = true;
            ReasonManditory = true;
            KickGracePeriod = 6;
            Bans = new List<BanDataValues>();
            ExpiredBans = new List<BanDataValues>();
        }
    }
}
