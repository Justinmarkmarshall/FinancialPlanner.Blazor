using FinancialPlanner.Blazor.Components.Models;
using FinancialPlanner.Blazor.Services;
using Xunit;

namespace FinancialPlanner.UnitTests
{
    public class GetExpendituresForMonthTests
    {
        private CashflowService expenditureService = new CashflowService();

        private DateTime protestantReformationDate = new DateTime(1517, 10, 31);
        private DateTime year3000 = new DateTime(3000, 1, 1);
        private Tuple<DateTime, DateTime> january26 = new Tuple<DateTime, DateTime>(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        private DateTime middleOfMonthPaymentDate = new DateTime(2026, 1, 15);

        private DateTime may1_2026 = new DateTime(2026, 5, 1);
        private DateTime june30_2026 = new DateTime(2026, 6, 30);
        private DateTime july1_2026 = new DateTime(2026, 7, 1);
        private DateTime july31_2026 = new DateTime(2026, 7, 31);

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

        [Fact]
        public void Recurring_Expenditure_EndDate_On_MonthStart_Is_Included_For_That_Month()
        {
            // Arrange: recurring expenditure from May 1 to July 1 (inclusive) should be included in July
            var expenditures = new List<CashflowDto>
            {
                new ExpenditureDto { StartDate = may1_2026, EndDate = july1_2026, Recurring = true, PaymentDate = may1_2026, Name = "Garmin" }
            };
            var julyMonth = new MonthDto { StartDate = july1_2026, EndDate = july31_2026 };

            // Act
            var result = expenditureService.GetCashflowForMonth(julyMonth, expenditures).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Garmin", result[0].Name);
            Assert.Equal(july1_2026, result[0].EndDate);
        }

        [Fact]
        public void Recurring_Expenditure_EndDate_Before_MonthStart_Is_Excluded_From_That_Month()
        {
            // Arrange: recurring expenditure from May 1 to June 30 should NOT appear in July
            var expenditures = new List<CashflowDto>
            {
                new ExpenditureDto { StartDate = may1_2026, EndDate = june30_2026, Recurring = true, PaymentDate = may1_2026, Name = "Subscription" }
            };
            var julyMonth = new MonthDto { StartDate = july1_2026, EndDate = july31_2026 };

            // Act
            var result = expenditureService.GetCashflowForMonth(julyMonth, expenditures).ToList();

            // Assert
            Assert.Empty(result);
        }
    }
}