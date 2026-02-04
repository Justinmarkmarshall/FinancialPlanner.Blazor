using FinancialPlanner.Blazor.Components.Models;

namespace FinancialPlanner.Blazor.Services
{
    public class HistoryService : IHistoryService
    {
        public List<MonthDto> GetHistoricalMonths(DateTime startDate, DateTime endDate)
        {            
            var months = new List<MonthDto>();

            while (startDate <= endDate)
            {
                var monthStart = new DateTime(startDate.Year, startDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                months.Add(new MonthDto
                {
                    Name = monthStart.ToString("MMMM yyyy"),
                    StartDate = monthStart,
                    EndDate = monthEnd
                });
                startDate = startDate.AddMonths(1);
            }

            return months;
        }
    }


    public interface IHistoryService
    {
        /// <summary>
        /// Given end date and start date, return all of the months between startDate and end with their start and end date
        /// It enables us to to have a year drop down list which could contain a year whose months are not already in the DB
        /// It will be used before DbService GetOrCreateMonths to populate the Months in the DB
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public List<MonthDto> GetHistoricalMonths(DateTime startDate, DateTime endDate);
    }
}
