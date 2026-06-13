using CardWallet.Application.DTOs.Wallets;
using FluentValidation;

namespace CardWallet.Application.Validators.Wallets;

public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Số tiền rút phải lớn hơn 0.");

        RuleFor(x => x.ReferenceId)
            .MaximumLength(100)
            .WithMessage("ReferenceId không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(255)
            .WithMessage("Description không được vượt quá 255 ký tự.");
    }
}