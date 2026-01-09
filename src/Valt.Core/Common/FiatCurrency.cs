using Valt.Core.Common.Exceptions;

namespace Valt.Core.Common;

public record FiatCurrency(string Code, int Decimals, string Symbol, bool SymbolOnRight, string CultureName)
{
    public static FiatCurrency Brl => new FiatCurrency("BRL", 2, "R$", false, "pt-BR");
    public static FiatCurrency Usd => new FiatCurrency("USD", 2, "$", false, "en-US");
    public static FiatCurrency Eur => new FiatCurrency("EUR", 2, "€", false, "de-DE");
    public static FiatCurrency Aud => new FiatCurrency("AUD", 2, "$", false, "en-AU");
    public static FiatCurrency Bgn => new FiatCurrency("BGN", 2, "лв", false, "bg-BG");
    public static FiatCurrency Cad => new FiatCurrency("CAD", 2, "$", false, "en-CA");
    public static FiatCurrency Chf => new FiatCurrency("CHF", 2, "Fr", false, "de-CH");
    public static FiatCurrency Cny => new FiatCurrency("CNY", 2, "¥", false, "zh-CN");
    public static FiatCurrency Czk => new FiatCurrency("CZK", 2, "Kč", false, "cs-CZ");
    public static FiatCurrency Dkk => new FiatCurrency("DKK", 2, "kr", false, "da-DK");
    public static FiatCurrency Gbp => new FiatCurrency("GBP", 2, "£", false, "en-GB");
    public static FiatCurrency Hkd => new FiatCurrency("HKD", 2, "$", false, "zh-HK");
    public static FiatCurrency Huf => new FiatCurrency("HUF", 2, "Ft", false, "hu-HU");
    public static FiatCurrency Idr => new FiatCurrency("IDR", 2, "Rp", false, "id-ID");
    public static FiatCurrency Ils => new FiatCurrency("ILS", 2, "₪", false, "he-IL");
    public static FiatCurrency Inr => new FiatCurrency("INR", 2, "₹", false, "hi-IN");
    public static FiatCurrency Isk => new FiatCurrency("ISK", 2, "kr", false, "is-IS");
    public static FiatCurrency Jpy => new FiatCurrency("JPY", 0, "¥", false, "ja-JP");
    public static FiatCurrency Krw => new FiatCurrency("KRW", 0, "₩", false, "ko-KR");
    public static FiatCurrency Mxn => new FiatCurrency("MXN", 2, "$", false, "es-MX");
    public static FiatCurrency Myr => new FiatCurrency("MYR", 2, "RM", false, "ms-MY");
    public static FiatCurrency Nok => new FiatCurrency("NOK", 2, "kr", false, "nb-NO");
    public static FiatCurrency Nzd => new FiatCurrency("NZD", 2, "$", false, "en-NZ");
    public static FiatCurrency Php => new FiatCurrency("PHP", 2, "₱", false, "fil-PH");
    public static FiatCurrency Pln => new FiatCurrency("PLN", 2, "zł", false, "pl-PL");
    public static FiatCurrency Ron => new FiatCurrency("RON", 2, "lei", false, "ro-RO");
    public static FiatCurrency Sek => new FiatCurrency("SEK", 2, "kr", false, "sv-SE");
    public static FiatCurrency Sgd => new FiatCurrency("SGD", 2, "$", false, "en-SG");
    public static FiatCurrency Thb => new FiatCurrency("THB", 2, "฿", false, "th-TH");
    public static FiatCurrency Try => new FiatCurrency("TRY", 2, "₺", false, "tr-TR");
    public static FiatCurrency Uyu => new FiatCurrency("UYU", 2, "$U", false, "es-UY");
    public static FiatCurrency Pyg => new FiatCurrency("PYG", 0, "₲", false, "es-PY");
    public static FiatCurrency Zar => new FiatCurrency("ZAR", 2, "R", false, "en-ZA");

    public static FiatCurrency GetFromCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "brl" => Brl,
            "usd" => Usd,
            "eur" => Eur,
            "aud" => Aud,
            "bgn" => Bgn,
            "cad" => Cad,
            "chf" => Chf,
            "cny" => Cny,
            "czk" => Czk,
            "dkk" => Dkk,
            "gbp" => Gbp,
            "hkd" => Hkd,
            "huf" => Huf,
            "idr" => Idr,
            "ils" => Ils,
            "inr" => Inr,
            "isk" => Isk,
            "jpy" => Jpy,
            "krw" => Krw,
            "mxn" => Mxn,
            "myr" => Myr,
            "nok" => Nok,
            "nzd" => Nzd,
            "php" => Php,
            "pln" => Pln,
            "ron" => Ron,
            "sek" => Sek,
            "sgd" => Sgd,
            "thb" => Thb,
            "try" => Try,
            "uyu" => Uyu,
            "pyg" => Pyg,
            "zar" => Zar,
            _ => throw new InvalidCurrencyCodeException(code)
        };
    }

    public static IEnumerable<FiatCurrency> GetAll()
    {
        yield return Brl;
        yield return Usd;
        yield return Eur;
        yield return Aud;
        yield return Bgn;
        yield return Cad;
        yield return Chf;
        yield return Cny;
        yield return Czk;
        yield return Dkk;
        yield return Gbp;
        yield return Hkd;
        yield return Huf;
        yield return Idr;
        yield return Ils;
        yield return Inr;
        yield return Isk;
        yield return Jpy;
        yield return Krw;
        yield return Mxn;
        yield return Myr;
        yield return Nok;
        yield return Nzd;
        yield return Php;
        yield return Pln;
        yield return Ron;
        yield return Sek;
        yield return Sgd;
        yield return Thb;
        yield return Try;
        yield return Uyu;
        yield return Pyg;
        yield return Zar;
    }

    public override string ToString()
    {
        return Code;
    }
}