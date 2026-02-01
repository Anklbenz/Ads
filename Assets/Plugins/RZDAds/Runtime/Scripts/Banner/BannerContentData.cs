namespace Plugins.RZDAds.Runtime.Scripts.Banner
{
    public class VideoBanner : BannerContent
    {
    }
    
    public class ImageBanner : BannerContent
    {
    }

    public abstract class BannerContent
    {
        public string LocalPath;
        public uint Id;
        public string Title;
        public string Description;
        public string Url;
        public int Duration;
    }
}