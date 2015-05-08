using Rocket.RocketAPI;

namespace QBan
{
    public class QBanConfiguration : IRocketConfiguration
    {
        public bool EnableInternalSync;
        public bool EnableExpiredExport;

        public IRocketConfiguration DefaultConfiguration
        {
            get
            {
                return new QBanConfiguration()
                {
                    EnableInternalSync = true,
                    EnableExpiredExport = true
                };
            }
        }
    }
}
