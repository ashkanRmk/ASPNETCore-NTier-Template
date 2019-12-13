namespace Liaro.ModelLayer.ShortLink
{
    public class ShortLinkDetailVM
    {
        public int Id { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public int VisitedCount { get; set; }
        public ShortLinkType Type { get; set; }
    }
}