using FinancialPlanner.Blazor.Services;

namespace FinancialPlanner.UnitTests
{
    public class HistoryTests
    {
        private readonly HistoryService _historyService = new();

        [Fact]
        public void GetHistoricalMonths_Returns_Empty_When_StartDate_After_EndDate()
        {
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2025, 12, 1);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Empty(result);
        }

        [Fact]
        public void GetHistoricalMonths_Returns_OneMonth_When_SameMonth()
        {
            var startDate = new DateTime(2025, 12, 1);
            var endDate = new DateTime(2025, 12, 15);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Single(result);
            Assert.Equal("December 2025", result[0].Name);
            Assert.Equal(new DateTime(2025, 12, 1), result[0].StartDate);
            Assert.Equal(new DateTime(2025, 12, 31), result[0].EndDate);
        }

        [Fact]
        public void GetHistoricalMonths_Returns_CorrectMonths_For_MultipleMonths()
        {
            var startDate = new DateTime(2025, 10, 1);
            var endDate = new DateTime(2025, 12, 27);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Equal(3, result.Count);
            Assert.Equal("October 2025", result[0].Name);
            Assert.Equal("November 2025", result[1].Name);
            Assert.Equal("December 2025", result[2].Name);
        }

        [Fact]
        public void GetHistoricalMonths_Starts_At_FirstOfMonth()
        {
            var startDate = new DateTime(2025, 10, 15); // Not the first of the month
            var endDate = new DateTime(2025, 12, 27);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Equal(new DateTime(2025, 10, 1), result[0].StartDate);
        }

        [Fact]
        public void GetHistoricalMonths_Returns_FullYear_When_Range_Is_Entire_Year()
        {
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 12, 31);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Equal(12, result.Count);
            Assert.Equal("January 2025", result[0].Name);
            Assert.Equal("December 2025", result[11].Name);
        }

        [Fact]
        public void GetHistoricalMonths_Sets_Correct_EndDate_For_February_Leap_Year()
        {
            var startDate = new DateTime(2024, 2, 1);
            var endDate = new DateTime(2024, 2, 28);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Single(result);
            Assert.Equal(new DateTime(2024, 2, 29), result[0].EndDate); // 2024 is a leap year
        }

        [Fact]
        public void GetHistoricalMonths_Sets_Correct_EndDate_For_February_NonLeap_Year()
        {
            var startDate = new DateTime(2025, 2, 1);
            var endDate = new DateTime(2025, 2, 28);

            var result = _historyService.GetHistoricalMonths(startDate, endDate);

            Assert.Single(result);
            Assert.Equal(new DateTime(2025, 2, 28), result[0].EndDate); // 2025 is not a leap year
        }
    }
}
