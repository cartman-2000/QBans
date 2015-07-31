using Rocket.API;

namespace QBan
{
    public class QBanConfiguration : IRocketPluginConfiguration
    {
        public bool EnableInternalSync;
        public bool EnableExpiredExport;
        public bool ReasonManditory;
        public void LoadDefaults()
        {
        EnableInternalSync = true;
        EnableExpiredExport = true;
        ReasonManditory = true;
        }
    }
}
