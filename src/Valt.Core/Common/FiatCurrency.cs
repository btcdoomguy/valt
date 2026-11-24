using Valt.Core.Common.Exceptions;

namespace Valt.Core.Common;

public record FiatCurrency(string Code, int Decimals, string Symbol, bool SymbolOnRight, string CultureName)
{
    public static readonly FiatCurrency Brl = new("BRL", 2, "R$", false, "pt-BR");
    public static readonly FiatCurrency Usd = new("USD", 2, "$", false, "en-US");
    public static readonly FiatCurrency Eur = new("EUR", 2, "€", false, "de-DE");
    public static readonly FiatCurrency Aud = new("AUD", 2, "$", false, "en-AU");
    public static readonly FiatCurrency Bgn = new("BGN", 2, "лв", false, "bg-BG");
    public static readonly FiatCurrency Cad = new("CAD", 2, "$", false, "en-CA");
    public static readonly FiatCurrency Chf = new("CHF", 2, "Fr", false, "de-CH");
    public static readonly FiatCurrency Cny = new("CNY", 2, "¥", false, "zh-CN");
    public static readonly FiatCurrency Czk = new("CZK", 2, "Kč", false, "cs-CZ");
    public static readonly FiatCurrency Dkk = new("DKK", 2, "kr", false, "da-DK");
    public static readonly FiatCurrency Gbp = new("GBP", 2, "£", false, "en-GB");
    public static readonly FiatCurrency Hkd = new("HKD", 2, "$", false, "zh-HK");
    public static readonly FiatCurrency Huf = new("HUF", 2, "Ft", false, "hu-HU");
    public static readonly FiatCurrency Idr = new("IDR", 2, "Rp", false, "id-ID");
    public static readonly FiatCurrency Ils = new("ILS", 2, "₪", false, "he-IL");
    public static readonly FiatCurrency Inr = new("INR", 2, "₹", false, "hi-IN");
    public static readonly FiatCurrency Isk = new("ISK", 2, "kr", false, "is-IS");
    public static readonly FiatCurrency Jpy = new("JPY", 0, "¥", false, "ja-JP");
    public static readonly FiatCurrency Krw = new("KRW", 0, "₩", false, "ko-KR");
    public static readonly FiatCurrency Mxn = new("MXN", 2, "$", false, "es-MX");
    public static readonly FiatCurrency Myr = new("MYR", 2, "RM", false, "ms-MY");
    public static readonly FiatCurrency Nok = new("NOK", 2, "kr", false, "nb-NO");
    public static readonly FiatCurrency Nzd = new("NZD", 2, "$", false, "en-NZ");
    public static readonly FiatCurrency Php = new("PHP", 2, "₱", false, "fil-PH");
    public static readonly FiatCurrency Pln = new("PLN", 2, "zł", false, "pl-PL");
    public static readonly FiatCurrency Ron = new("RON", 2, "lei", false, "ro-RO");
    public static readonly FiatCurrency Sek = new("SEK", 2, "kr", false, "sv-SE");
    public static readonly FiatCurrency Sgd = new("SGD", 2, "$", false, "en-SG");
    public static readonly FiatCurrency Thb = new("THB", 2, "฿", false, "th-TH");
    public static readonly FiatCurrency Try = new("TRY", 2, "₺", false, "tr-TR");
    public static readonly FiatCurrency Uyu = new("UYU", 2, "$U", false, "es-UY");
    public static readonly FiatCurrency Pyg = new("PYG", 0, "₲", false, "es-PY");
    public static readonly FiatCurrency Zar = new("ZAR", 2, "R", false, "en-ZA");

    private static readonly IReadOnlyList<FiatCurrency> AllCurrencies =
    [
        Brl, Usd, Eur, Aud, Bgn, Cad, Chf, Cny, Czk, Dkk, Gbp,
        Hkd, Huf, Idr, Ils, Inr, Isk, Jpy, Krw, Mxn, Myr, Nok,
        Nzd, Php, Pln, Ron, Sek, Sgd, Thb, Try, Uyu, Pyg, Zar
    ];

    private static readonly Dictionary<string, FiatCurrency> ByCode =
        AllCurrencies.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    public static FiatCurrency GetFromCode(string code)
    {
        return TryGetFromCode(code, out var currency)
            ? currency!
            : throw new InvalidCurrencyCodeException(code);
    }

    public static bool TryGetFromCode(string code, out FiatCurrency? currency)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            currency = null;
            return false;
        }

        return ByCode.TryGetValue(code, out currency);
    }

    public static IEnumerable<FiatCurrency> GetAll() => AllCurrencies;

    public override string ToString()
    {
        return Code;
    }
}