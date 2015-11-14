using System;
using Steamworks;
using System.Xml.Serialization;

namespace QBan
{
    public class BanDataValues
    {
        public BanDataValues() { }

        [XmlIgnore]
        public CSteamID targetSID { get; set; }
        [XmlAttribute("TSID")]
        public ulong XmltargetSID
        {
            get
            {
                return (ulong)targetSID;
            }
            set
            {
                targetSID = (CSteamID)value;
            }
        }

        [XmlAttribute("TCN")]
        public string targetCharName { get; set; }
        [XmlAttribute("TSN")]
        public string targetSteamName { get; set; }

        [XmlIgnore]
        public CSteamID adminSID { get; set; }
        [XmlAttribute("ASID")]
        public ulong XmladminSID
        {
            get
            {
                return (ulong)adminSID;
            }
            set
            {
                adminSID = (CSteamID)value;
            }
        }

        [XmlAttribute("ACN")]
        public string adminCharName { get; set; }
        [XmlAttribute("ASN")]
        public string adminSteamName { get; set; }

        [XmlAttribute("For")]
        public string reason { get; set; }
        [XmlAttribute("Length")]
        public uint duration { get; set; }

        [XmlAttribute("IPB")]
        public bool isIPBan { get; set; }
        [XmlAttribute("IPM")]
        public bool isIPBMatch { get; set; }
        [XmlAttribute("UIIP")]
        public uint uIP { get; set; }

        [XmlAttribute("SetT")]
        public DateTime setTime { get; set; }
    }
}
