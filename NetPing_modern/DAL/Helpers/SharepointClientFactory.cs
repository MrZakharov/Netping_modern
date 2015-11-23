using NetPing.Global.Config;

namespace NetPing.DAL
{
    internal class SharepointClientFactory : ISharepointClientFactory
    {
        private readonly IConfig _config;

        public SharepointClientFactory(IConfig config)
        {
            _config = config;
        }

        public SharepointClient Create()
        {
            return new SharepointClient(new SharepointClientParameters()
            {
                Password = _config.SPSettings.Password,
                User = _config.SPSettings.Login,
                Url = _config.SPSettings.SiteUrl,
                RequestTimeout = _config.SPSettings.RequestTimeout
            });
        }
    }
}