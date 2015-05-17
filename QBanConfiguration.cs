using Rocket.API;

namespace QBan
{
    public class QBanConfiguration : IRocketPluginConfiguration
    {
        public bool EnableInternalSync;
        public bool EnableExpiredExport;
        public bool ReasonManditory;

        public IRocketPluginConfiguration DefaultConfiguration
        {
            get
            {
                return new QBanConfiguration()
                {
                    EnableInternalSync = true,
                    EnableExpiredExport = true,
                    ReasonManditory = true
                };
            }
        }
    }
}
