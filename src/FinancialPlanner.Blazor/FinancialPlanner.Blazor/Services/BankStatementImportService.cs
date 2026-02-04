using FinancialPlanner.Blazor.Components.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Text.RegularExpressions;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;
using static FinancialPlanner.Blazor.Components.Pages.StatementUpload;

namespace FinancialPlanner.Blazor.Services
{
    public partial class BankStatementImportService(ILogger<BankStatementImportService> logger) : IBankStatementImportService
    {
        private string[] formats = new[]
        {
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy-MM-dd",
            "dd-MMM-yy",  // Add this format for "27-Sep-25"
            "dd-MMM-yyyy" // Also handle 4-digit years like "27-Sep-2025"
        };

        public async Task<List<CashflowDto>> ProcessBankStatementFile(IBrowserFile file, UploadModel uploadModel)
        {
            var cashFlowDtos = new List<CashflowDto>();
            var income = new List<IncomeDto>();

            using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5MB limit
            using var reader = new StreamReader(stream);

            string? line;
            int lineNumber = 0;

            DateTime lastRunningDate = new DateTime();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                // Skip header row if specified
                if (lineNumber == 1 && uploadModel.HasHeaderRow)
                    continue;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var cashFlowDto = ParseCsvLine(line, uploadModel);

                    if (cashFlowDto != null)
                    {
                        cashFlowDtos.Add(cashFlowDto);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error parsing line {lineNumber}: {ex.Message}");
                }
            }

            return cashFlowDtos;
        }

        private CashflowDto? ParseCsvLine(string csvLine, UploadModel uploadModel)
        {
            var columns = ParseCsvColumns(csvLine);

            if (columns.Count <= Math.Max(uploadModel.AmountColumnIndex, Math.Max(uploadModel.DateColumnIndex, uploadModel.DescriptionColumnIndex)))
            {
                return null; // Not enough columns
            }            

            bool isIncome = false;

            try
            {
                var dateString = columns[0].Trim();
                var description = columns[2].Trim();
                var paidOut = columns[3].Trim();
                var paidIn = columns[4].Trim();

                if (!DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return null;
                }

                // we are failing to parse the pound symbol so it comes as the question mark
                if (paidIn.Contains("�"))
                {
                    isIncome = true;
                }

                var incomeAmount = 0m;
                var expAmount = 0m;

                if (AccountNumberPattern().IsMatch(description))
                {
                    // moving between bank accounts so ignore
                    return null;
                }

                if (isIncome && (!decimal.TryParse(paidIn.Replace("�", ""), NumberStyles.Currency | NumberStyles.Float, CultureInfo.InvariantCulture, out incomeAmount)))
                {
                    return null;
                }
                else if (!isIncome && !decimal.TryParse(paidOut.Replace("�", ""), NumberStyles.Currency | NumberStyles.Float, CultureInfo.InvariantCulture, out expAmount))
                {
                    return null;
                }

                if (isIncome)
                {
                    return new IncomeDto()
                    {
                        Name = description,
                        Amount = incomeAmount,
                        PaymentDate = date,
                        // to do, use Category object
                        //Category = SuggestIncomeCategory(description).ToString(),
                        //IncomeType = CashflowType.Actual.ToString()
                    };
                }
                else
                {
                    return new ExpenditureDto()
                    {
                        Name = description,
                        Amount = expAmount,
                        PaymentDate = date,
                        // to do, use Category object after the user has configured their categories
                        //Category = SuggestExpenditureCategory(description).ToString(), 
                        //ExpenseType = CashflowType.Actual.ToString()

                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing CSV line: {CsvLine}", csvLine);
                return null;
            }
        }

        public List<string> ParseCsvColumns(string csvLine)
        {
            var columns = new List<string>();
            var inQuotes = false;
            var currentColumn = string.Empty;

            for (int i = 0; i < csvLine.Length; i++)
            {
                var character = csvLine[i];

                if (character == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (character == ',' && !inQuotes)
                {
                    columns.Add(currentColumn);
                    currentColumn = string.Empty;
                }
                else
                {
                    currentColumn += character;
                }
            }

            columns.Add(currentColumn);
            return columns;
        }

        // to do read Customer Rules from DB or introduce ML or configurable rules engine
        public IncomeCategoryType SuggestIncomeCategory(string description)
        {
            var desc = description.ToUpper();

            var customerRule = Rules.CustomerRules
                .FirstOrDefault(r => desc.Contains(r.Pattern.ToUpper()));

            return customerRule != null ? customerRule.Category : IncomeCategoryType.Other;
        }

        public CategoryType SuggestExpenditureCategory(string description)
        {
            var desc = description.ToUpper();

            var merchantRule = Rules.MerchantRules
                .FirstOrDefault(r => desc.Contains(r.Pattern.ToUpper()));

            return merchantRule != null ? merchantRule.Category : CategoryType.Miscellaneous;
        }

        [GeneratedRegex(@"\d{6}\s\d{8}")]
        private static partial Regex AccountNumberPattern();

    }

    public interface IBankStatementImportService
    {
        Task<List<CashflowDto>> ProcessBankStatementFile(IBrowserFile file, UploadModel uploadModel);
    }
}
