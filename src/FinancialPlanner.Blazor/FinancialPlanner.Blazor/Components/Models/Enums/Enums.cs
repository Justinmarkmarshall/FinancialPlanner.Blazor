namespace FinancialPlanner.Blazor.Components.Models.Enums
{
    public class Enums
    {
        public enum CategoryType
        {
            Housing = 0,
            Utilities = 1,
            Groceries = 2,
            Tithing = 3,
            Car = 4,
            Healthcare = 5,
            Insurance = 6,
            DebtRepayment = 7,
            Savings = 8,
            Entertainment = 9,
            DiningOut = 10,
            PersonalCare = 11,
            Education = 12,
            Miscellaneous = 13, 
            Home = 14
        }

        // these will end up being rows in a DB per user
        public enum IncomeCategoryType
        {
            Salary = 0,
            Lodger = 1,
            Investment = 2,
            Bonus = 3,
            Gift = 4,
            Other = 5
        }

        public enum CashflowType
        {
            Projected = 0,
            Actual = 1
        }
    }
}
