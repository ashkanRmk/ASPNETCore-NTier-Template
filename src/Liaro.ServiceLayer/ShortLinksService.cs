using System.Linq;
using System.Threading.Tasks;
using Liaro.Common;
using Liaro.DataLayer.Abstract;
using Liaro.Entities;
using Microsoft.EntityFrameworkCore;

namespace Liaro.ServiceLayer
{
    public interface IShortLinksService
    {
        Task<string> AddShortLink(ShortLink shortlink);
        Task UpdateShortLink(ShortLink shortlink);
        Task RemoveShortLink(ShortLink shortlink);
        Task<ShortLink> GetShortLinkBySource(string source);
    }


    public class ShortLinksService : IShortLinksService
    {
        private readonly IEntityBaseRepository<ShortLink> _shortLinks;

        public ShortLinksService(
            IEntityBaseRepository<ShortLink> shortLinks
        )
        {
            _shortLinks = shortLinks;
        }

        public async Task<string> AddShortLink(ShortLink shortlink)
        {
            // if source of a shortlink was empty, it will generate 4chars unique key for it.
            if (string.IsNullOrEmpty(shortlink.Source))
            {
                bool exist = true;
                string code = null;

                while (exist)
                {
                    code = StringUtils.GetUniqueKey(4);
                    exist = await _shortLinks.GetAllQueryable()
                                            .IgnoreQueryFilters()
                                            .AnyAsync(x => x.Source == code);
                }
                shortlink.Source = code;
            }
            _shortLinks.Add(shortlink);
            await _shortLinks.CommitAsync();
            return shortlink.Source;
        }

        public async Task UpdateShortLink(ShortLink shortlink)
        {
            _shortLinks.Update(shortlink);
            await _shortLinks.CommitAsync();
        }

        public async Task RemoveShortLink(ShortLink shortlink)
        {
            _shortLinks.Delete(shortlink);
            await _shortLinks.CommitAsync();
        }

        public async Task<ShortLink> GetShortLinkBySource(string source)
        {
            return await _shortLinks.GetAllQueryable()
                                        .Where(x => x.Source == source)
                                        .SingleOrDefaultAsync();
        }

    }
}