using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Generates sample CSV templates demonstrating all 6 transaction types.
/// The template serves as user documentation for the expected CSV format.
/// </summary>
internal class CsvTemplateGenerator : ICsvTemplateGenerator
{
    public string GenerateTemplate()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var stringWriter = new StringWriter();
        using var csv = new CsvWriter(stringWriter, config);

        // Write header
        csv.WriteField("date");
        csv.WriteField("description");
        csv.WriteField("amount");
        csv.WriteField("account");
        csv.WriteField("to_account");
        csv.WriteField("to_amount");
        csv.WriteField("category");
        csv.NextRecord();

        // 1. FiatDetails - Expense (negative fiat amount, no to_account)
        WriteRow(csv, "2024-01-15", "Grocery shopping", FormatFiat(-150.00m), "Checking [USD]", "", "", "Food");

        // 2. FiatDetails - Income (positive fiat amount, no to_account)
        WriteRow(csv, "2024-01-16", "Salary", FormatFiat(5000.00m), "Savings [BRL]", "", "", "Income");

        // 3. FiatToBitcoinDetails - Buy bitcoin (fiat to btc exchange)
        WriteRow(csv, "2024-01-17", "Stack sats", FormatFiat(-500.00m), "Checking [USD]", "Hardware Wallet [btc]", FormatBtc(0.00850000m), "Investment");

        // 4. BitcoinToFiatDetails - Sell bitcoin (btc to fiat exchange)
        WriteRow(csv, "2024-01-18", "Sold bitcoin", FormatBtc(-0.01000000m), "Hardware Wallet [btc]", "Checking [EUR]", FormatFiat(450.00m), "Trading");

        // 5. FiatToFiatDetails - Transfer between fiat accounts
        WriteRow(csv, "2024-01-19", "Transfer to savings", FormatFiat(-1000.00m), "Checking [USD]", "Savings [USD]", FormatFiat(1000.00m), "Transfer");

        // 6. BitcoinToBitcoinDetails - Transfer between bitcoin accounts
        WriteRow(csv, "2024-01-20", "Consolidate bitcoin", FormatBtc(-0.05000000m), "Exchange Wallet [btc]", "Hardware Wallet [btc]", FormatBtc(0.05000000m), "Transfer");

        // 7. BitcoinDetails - Bitcoin income (positive btc amount, no to_account)
        WriteRow(csv, "2024-01-21", "Bitcoin income", FormatBtc(0.00100000m), "Hardware Wallet [btc]", "", "", "Income");

        return stringWriter.ToString();
    }

    public void SaveTemplate(string filePath)
    {
        var content = GenerateTemplate();
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    private static void WriteRow(CsvWriter csv, string date, string description, string amount,
        string account, string toAccount, string toAmount, string category)
    {
        csv.WriteField(date);
        csv.WriteField(description);
        csv.WriteField(amount);
        csv.WriteField(account);
        csv.WriteField(toAccount);
        csv.WriteField(toAmount);
        csv.WriteField(category);
        csv.NextRecord();
    }

    private static string FormatFiat(decimal value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string FormatBtc(decimal value)
    {
        return value.ToString("F8", CultureInfo.InvariantCulture);
    }
}
