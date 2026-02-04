using FinancialPlanner.Blazor.Components.Models;
using FinancialPlanner.Blazor.Services;

namespace FinancialPlanner.UnitTests
{
    public class GetExpendituresForMonthTests
    {
        private CashflowService expenditureService = new CashflowService();

        private DateTime protestantReformationDate = new DateTime(1517, 10, 31);
        private DateTime year3000 = new DateTime(3000, 1, 1);
        private Tuple<DateTime, DateTime> january26 = new Tuple<DateTime, DateTime>(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        private DateTime middleOfMonthPaymentDate = new DateTime(2026, 1, 15);

        [Fact]
        public void Returns_Empty_When_Expenditures_Is_Null()
        {
            List<CashflowDto> expenditures = null;
            var month = new MonthDto { StartDate = DateTime.Today, EndDate = DateTime.Today };
            var result = expenditureService.GetCashflowForMonth(month, expenditures);
            Assert.Empty(result);
        }

        [Fact]
        public void Returns_Empty_When_No_Expenditures_Match()
        {
            var expenditures = new List<CashflowDto>
            {
                new ExpenditureDto { StartDate = DateTime.Today.AddMonths(-2), EndDate = DateTime.Today.AddMonths(-1) }
            };
            var month = new MonthDto { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(30) };
            var result = expenditureService.GetCashflowForMonth(month, expenditures);
            Assert.Empty(result);
        }

        [Fact]
        public void Returns_Expenditure_When_Within_Month_Range()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 1, 31);
            var expenditures = new List<CashflowDto>
            {
                new ExpenditureDto { StartDate = start, EndDate = end, Recurring = true }
            };
            var month = new MonthDto { StartDate = start, EndDate = end };
            var result = expenditureService.GetCashflowForMonth(month, expenditures);
            Assert.Single(result);
        }

        [Fact]
        public void NonRecurring_Expenditures_Should_Have_NullStartDate_and_NullEndDate_and_only_consider_date()
        {
            // Arrange
            var month = new MonthDto { StartDate = january26.Item1, EndDate = january26.Item2 };
            var expenditures = new List<CashflowDto>
            {
                new ExpenditureDto { PaymentDate = protestantReformationDate, Recurring = false, StartDate = null, EndDate = null }, // Non-recurring date outside month, not returned
                new ExpenditureDto { PaymentDate = middleOfMonthPaymentDate, Recurring = false, StartDate = null, EndDate = null }, // Non-recurring date within month, should be returned
                new ExpenditureDto { PaymentDate = middleOfMonthPaymentDate, Recurring = true, StartDate = protestantReformationDate, EndDate = year3000 }, // Recurring date within outside month
                new ExpenditureDto { PaymentDate = middleOfMonthPaymentDate, Recurring = true, StartDate = null, EndDate = null } // Recurring with bad data, should not be considered
            };

            // Act
            var result = expenditureService.GetCashflowForMonth(month, expenditures).ToList();
            
            // Assert
            Assert.Equal(result.Count, 2);
            Assert.Equal(middleOfMonthPaymentDate, result[0].PaymentDate);
        }
    }
}