namespace CardWallet.Application.DTOs.CardExchange;

public class SubmitCardRequest
{
    public string Provider { get; set; } = string.Empty;
    public int FaceValue { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}
