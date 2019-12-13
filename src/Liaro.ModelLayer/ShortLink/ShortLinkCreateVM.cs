using FluentValidation;
using Liaro.Common;

namespace Liaro.ModelLayer.ShortLink
{
    public class ShortLinkCreateVM
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }


    public class ShortLinkValidator : AbstractValidator<ShortLinkCreateVM>
    {
        public ShortLinkValidator()
        {
            RuleFor(sl => sl.Target).NotNull()
                                    .NotEmpty()
                                    .WithMessage("لطفا لینک مقصد را وارد نمایید")
                                    .Must(StringUtils.LinkMustBeAUri)
                                    .WithMessage("لطفا لینک مقصد را به صورت صحیح وارد نمایید");
        }

    }
}