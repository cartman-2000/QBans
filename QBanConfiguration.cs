using Rocket.API;

namespace QBan
{
    public class QBanConfiguration : IRocketPluginConfiguration
    {
        public bool EnableInternalSync = true;
        public bool EnableExpiredExport = true;
        public bool ReasonManditory = true;
    }
}
