
using AutoMapper;
using Liaro.Entities;
using Liaro.ModelLayer.ShortLink;

namespace Liaro.Mapping
{
    public class AutoMapperConfiguration : Profile
    {
        public AutoMapperConfiguration()
        {
            CreateMap<ShortLinkCreateVM, ShortLink>();
            CreateMap<ShortLinkUpdateVM, ShortLink>();

        }
    }
}