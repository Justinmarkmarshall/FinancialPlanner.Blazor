using FinancialPlanner.Blazor.Components.Models;

namespace FinancialPlanner.Blazor.Services
{
    public class CashflowService : ICashflowService
    {
        /// <summary>
        /// Using polymorphism we can use this method for calculating both income and expenditure per month
        /// </summary>
        /// <param name="month"></param>
        /// <param name="cashFlow"></param>
        /// <returns></returns>
        public IEnumerable<CashflowDto> GetCashflowForMonth(MonthDto month, List<CashflowDto>? cashFlow)
        {
            if (cashFlow == null)
                return Enumerable.Empty<CashflowDto>();

            var monthCashflow = new List<CashflowDto>();            

            foreach (var item in cashFlow)
            {

                if (item.Name.ToLower().Contains("pixel"))
                {

                }

                if (item.Recurring)
                {
                    // here we are essentially comparing 2 ranges, the start and end date of the month, 
                    // and the start and end date of the expenditure. For that reason, there must be a start and end date on the expenditure
                    if (!item.StartDate.HasValue || !item.EndDate.HasValue)
                        continue;

                    // if recurring, then the month start and end date must be within the expenditure start and end date
                    if (item.StartDate <= month.StartDate && item.EndDate >= month.EndDate)
                    {
                        monthCashflow.Add(item);
                    }
                }

                if (!item.Recurring)
                {
                    // if not recurring, then the payment date must be within the month
                    if (item.PaymentDate >= month.StartDate && item.PaymentDate <= month.EndDate)
                    {
                        monthCashflow.Add(item);
                    }
                }
            }

            return monthCashflow;
        }
    }

    public interface ICashflowService
    {
        public IEnumerable<CashflowDto> GetCashflowForMonth(MonthDto month, List<CashflowDto>? expenditures);
    }
}
