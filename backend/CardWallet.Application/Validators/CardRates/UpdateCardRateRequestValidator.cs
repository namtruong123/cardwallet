using CardWallet.Application.DTOs.CardRates;
using FluentValidation;

namespace CardWallet.Application.Validators.CardRates;

public class UpdateCardRateRequestValidator : AbstractValidator<UpdateCardRateRequest>
{
    public UpdateCardRateRequestValidator()
    {
        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("Nhà mạng thẻ không hợp lệ.");

        RuleFor(x => x.FaceValue)
            .GreaterThan(0)
            .WithMessage("Mệnh giá phải lớn hơn 0.");

        RuleFor(x => x.DiscountPercent)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Chiết khấu phải nằm trong khoảng 0 đến 100.");
    }
}
