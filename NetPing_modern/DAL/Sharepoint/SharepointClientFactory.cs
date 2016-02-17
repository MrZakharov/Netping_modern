using NetPing.Global.Config;

namespace NetPing.DAL
{
    public class SharepointClientFactory : ISharepointClientFactory
    {
        private readonly IConfig _config;

        public SharepointClientFactory(IConfig config)
        {
            _config = config;
        }

        public SharepointClient Create(bool isfirmware = false)
        {
            return new SharepointClient(new SharepointClientParameters()
            {
                Password = _config.SPSettings.Password,
                User = _config.SPSettings.Login,
                Url = isfirmware ? _config.SPSettings.SiteUrlFirmware : _config.SPSettings.SiteUrl,
                RequestTimeout = _config.SPSettings.RequestTimeout
            });
        }
    }
}