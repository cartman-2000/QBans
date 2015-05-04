using Rocket.RocketAPI;

namespace QBan
{
    public class QBanConfiguration : RocketConfiguration
    {
        public bool EnableInternalSync;
        public bool EnableExpiredExport;

        public RocketConfiguration DefaultConfiguration
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
