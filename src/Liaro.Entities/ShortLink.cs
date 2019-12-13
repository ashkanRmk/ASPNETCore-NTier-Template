using Liaro.Entities.Helpers;
using Liaro.ModelLayer;

namespace Liaro.Entities
{
    public class ShortLink : BaseClass
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public int VisitedCount { get; set; }
        public ShortLinkType Type { get; set; }
        public int CretorUserId { get; set; }
    }
}