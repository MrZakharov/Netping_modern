namespace NetPing.DAL
{
    internal interface ISharepointClientFactory
    {
        SharepointClient Create(bool isfirmware = false);
    }
}