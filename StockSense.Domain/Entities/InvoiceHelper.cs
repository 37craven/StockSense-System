namespace StockSense.Domain.Entities;

public static class InvoiceHelper
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string ShortCode()
    {
        Span<char> code = stackalloc char[5];
        for (int i = 0; i < 5; i++)
            code[i] = Chars[Random.Shared.Next(Chars.Length)];
        return new string(code);
    }
}
