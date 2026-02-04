using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.Components.Models
{
    public record MerchantRule(
        string Pattern,
        CategoryType Category
    );

    public record CustomerRule(
        string Pattern,
        IncomeCategoryType Category
    );

    public class Rules
    {
        // TO DO: read Merchant Rules from DB or introduce ML or configurable rules engine
        public static List<MerchantRule> MerchantRules = new()
        {
        };

        // TO DO: read Customer Rules from DB or introduce ML or configurable rules engine
        public static List<CustomerRule> CustomerRules { get; set; } = new List<CustomerRule>()
        {
        };
    }
}
