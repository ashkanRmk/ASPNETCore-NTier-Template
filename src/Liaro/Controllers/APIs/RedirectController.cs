using System.Threading.Tasks;
using AutoMapper;
using Liaro.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Liaro.Common;
using Liaro.ServiceLayer.Security;
using Liaro.ServiceLayer;
using Liaro.ModelLayer;
using Liaro.ModelLayer.ShortLink;

namespace Liaro.Controllers.APIs
{
    public class RedirectController : ControllerBase
    {
        private readonly IShortLinksService _shortLinksService;
        private readonly IMapper _mapper;

        public RedirectController(
            IShortLinksService shortLinksService,
            IMapper mapper

        )
        {
            _shortLinksService = shortLinksService;
            _mapper = mapper;

        }

        [Authorize(Policy = CustomRoles.Admin)]
        [HttpPost("api/[controller]")]
        public async Task<IActionResult> Create([FromBody]ShortLinkCreateVM model)
        {
            var shortLink = _mapper.Map<ShortLinkCreateVM, ShortLink>(model);
            shortLink.Type = ShortLinkType.Other;
            shortLink.CretorUserId = User.UserId();
            var result = await _shortLinksService.AddShortLink(shortLink);
            return Ok(result);
        }

        [Authorize(Policy = CustomRoles.Admin)]
        [HttpPut("api/[controller]")]
        public async Task<IActionResult> Update([FromBody]ShortLinkUpdateVM model)
        {
            var shortLink = _mapper.Map<ShortLinkUpdateVM, ShortLink>(model);
            await _shortLinksService.UpdateShortLink(shortLink);
            return Ok();
        }

        [Authorize(Policy = CustomRoles.Admin)]
        [HttpDelete("api/[controller]/{source}")]
        public async Task<IActionResult> Remove(string source)
        {
            var res = await _shortLinksService.GetShortLinkBySource(source);
            if (res is null)
            {
                ModelState.AddModelError("Error", "لینک کوتاهی با این نام یافت نشد!");
                return NotFound(ModelState.DictionaryErrors());
            }
            await _shortLinksService.RemoveShortLink(res);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("/t/{source}")]
        public async Task<IActionResult> RedirectOtherId(string source)
        {
            var res = await _shortLinksService.GetShortLinkBySource(source);
            if (res is null)
                return Redirect("/notfound");

            res.VisitedCount++;
            await _shortLinksService.UpdateShortLink(res);
            return Redirect(res.Target);
        }
    }
}