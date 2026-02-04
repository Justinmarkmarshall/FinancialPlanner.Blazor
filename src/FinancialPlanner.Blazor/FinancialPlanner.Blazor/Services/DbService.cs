using FinancialPlanner.Blazor.Components.Models;
using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.Services
{
    public class DbService(FinanceDbContext financeDbContext) : IDbService
    {
        public IEnumerable<ExpenditureDto> GetExpenditureDtos(CashflowType expenditureType = CashflowType.Projected)
        {
            var expenditures = financeDbContext.Expenditures
                .Include(e => e.Category)
                .Where(r => (int)r.ExpenseType == (int)expenditureType)
                .ToList();

            var expenditureDtos = expenditures.Select(e => new ExpenditureDto
            {
                Id = e.Id,
                Name = e.Name,
                Amount = e.Amount,
                PaymentDate = e.PaymentDate,
                Recurring = e.Recurring,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Category = e.Category?.Name ?? "Uncategorized",
                ExpenseCategoryId = e.ExpenseCategoryId,
                ExpenseType = (CashflowType)e.ExpenseType
            });
            return expenditureDtos;
        }

        public ExpenditureDto? GetExpenditureDto(string name, decimal amount, DateTime paymentDate, CashflowType cashflowType)
        {
            var expenditure = financeDbContext.Expenditures.FirstOrDefault(r => r.Name.ToLower() == name.ToLower() &&
            r.Amount == amount && r.PaymentDate == paymentDate && r.ExpenseType == cashflowType);

            if (expenditure == null)
            {
                return null;
            }
            return new ExpenditureDto()
            {
                Id = expenditure.Id,
                Name = expenditure.Name,
                Amount = expenditure.Amount,
                PaymentDate = expenditure.PaymentDate,
                Recurring = expenditure.Recurring,
                StartDate = expenditure.StartDate,
                EndDate = expenditure.EndDate,
                Category = expenditure.Category?.Name ?? "Uncategorized",
                ExpenseCategoryId = expenditure.ExpenseCategoryId,
                ExpenseType = (CashflowType)expenditure.ExpenseType
            };
        }

        public bool UpsertExpenditureDtos(IEnumerable<ExpenditureDto> expenditureDtos)
        {
            try
            {
                foreach (var expenditureDto in expenditureDtos)
                {
                    var existingExpenditureDto = GetExpenditureDto(expenditureDto.Name, expenditureDto.Amount, expenditureDto.PaymentDate, expenditureDto.ExpenseType);

                    if (existingExpenditureDto != null)
                    {
                        continue;
                    }
                    else
                    {
                        var expenditure = new DataAccess.Models.Expenditure
                        {
                            Name = expenditureDto.Name,
                            Amount = expenditureDto.Amount,
                            PaymentDate = expenditureDto.PaymentDate,
                            Recurring = expenditureDto.Recurring,
                            StartDate = expenditureDto.StartDate,
                            EndDate = expenditureDto.EndDate,
                            // needs to be retrieved from the DB
                            //Category = expenditureDto.CategoryType,
                            ExpenseType = expenditureDto.ExpenseType
                        };
                        financeDbContext.Expenditures.Add(expenditure);
                    }
                }

                financeDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public IEnumerable<IncomeDto> GetIncomeDtos(CashflowType incomeType = CashflowType.Projected)
        {
            var incomes = financeDbContext.Incomes.Where(r => r.IncomeType == incomeType).ToList();
            var incomeDtos = incomes.Select(i => new IncomeDto
            {
                Id = i.Id,
                Name = i.Name,
                Amount = i.Amount,
                PaymentDate = i.PaymentDate,
                Recurring = i.Recurring,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                // Category = i.Category.Name
            });
            return incomeDtos;
        }

        public IncomeDto? GetIncomeDto(string name, decimal amount, DateTime paymentDate, CashflowType cashflowType)
        {
            var income = financeDbContext.Incomes.FirstOrDefault(r => r.Name.ToLower() == name.ToLower() &&
            r.Amount == amount && r.PaymentDate == paymentDate && r.IncomeType == cashflowType);

            if (income == null)
            {
                return null;
            }
            return new IncomeDto()
            {
                Id = income.Id,
                Name = income.Name,
                Amount = income.Amount,
                PaymentDate = income.PaymentDate,
                Recurring = income.Recurring,
                StartDate = income.StartDate,
                EndDate = income.EndDate,
                // Category = income.Category.Name,
                IncomeType = income.IncomeType
            };
        }

        public bool UpsertIncomeDtos(IEnumerable<IncomeDto> incomeDtos)
        {
            try
            {
                foreach (var incomeDto in incomeDtos)
                {
                    var existingIncomeDto = GetIncomeDto(incomeDto.Name, incomeDto.Amount, incomeDto.PaymentDate, incomeDto.IncomeType);

                    if (existingIncomeDto != null)
                    {
                        continue;
                    }
                    else
                    {
                        var income = new DataAccess.Models.Income
                        {
                            Name = incomeDto.Name,
                            Amount = incomeDto.Amount,
                            PaymentDate = incomeDto.PaymentDate,
                            Recurring = incomeDto.Recurring,
                            StartDate = incomeDto.StartDate,
                            EndDate = incomeDto.EndDate,
                            // needs thought once the User account and customisable categories are plumbed in
                            //Category = incomeDto.Category,
                            IncomeType = incomeDto.IncomeType
                        };
                        financeDbContext.Incomes.Add(income);
                    }
                }

                financeDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public IEnumerable<MonthDto> GetOrCreateMonths(IEnumerable<MonthDto> monthDtos)
        {
            var results = new List<MonthDto>();

            foreach (var monthDto in monthDtos)
            {
                var existing = financeDbContext.Months
                    .FirstOrDefault(m => m.StartDate == monthDto.StartDate && m.EndDate == monthDto.EndDate);

                if (existing != null)
                {
                    results.Add(new MonthDto
                    {
                        Id = existing.Id,
                        Name = existing.Name,
                        StartDate = existing.StartDate,
                        EndDate = existing.EndDate,
                        Year = existing.StartDate.Year,
                        ProjectedTotalIncome = existing.ProjectedTotalIncome,
                        ProjectedTotalExpenditure = existing.ProjectedTotalExpenditure,
                        ProjectedSavings = existing.ProjectedSavings,
                        ActualTotalIncome = existing.ActualTotalIncome,
                        ActualTotalExpenditure = existing.ActualTotalExpenditure,
                        ActualSavings = existing.ActualSavings,
                        Notes = existing.Notes
                    });
                }
                else
                {
                    var newMonth = new DataAccess.Models.Month
                    {
                        Name = monthDto.Name,
                        StartDate = monthDto.StartDate,
                        EndDate = monthDto.EndDate,
                    };

                    financeDbContext.Months.Add(newMonth);
                    financeDbContext.SaveChanges();

                    results.Add(new MonthDto
                    {
                        Id = newMonth.Id,
                        Name = newMonth.Name,
                        StartDate = newMonth.StartDate,
                        EndDate = newMonth.EndDate,
                        Year = newMonth.StartDate.Year
                    });
                }
            }

            return results;
        }

        public bool UpdateMonthNotes(int monthId, string notes)
        {
            var month = financeDbContext.Months.Find(monthId);
            if (month == null)
            {
                return false;
            }

            month.Notes = notes;
            financeDbContext.SaveChanges();
            return true;
        }

        public BankStatementDto GetBankStatementForMonth(int monthId)
        {
            var bankStatements = financeDbContext.BankStatements
                .FirstOrDefault(bs => bs.MonthId == monthId);
            if (bankStatements == null)

            {
                return null;
            }

            return new BankStatementDto
            {
                Id = bankStatements.Id,
                Month = new MonthDto
                {
                    Id = bankStatements.Month.Id,
                    Name = bankStatements.Month.Name,
                    StartDate = bankStatements.Month.StartDate,
                    EndDate = bankStatements.Month.EndDate
                },
                Description = bankStatements.Description,
                TotalIncome = bankStatements.TotalIncome,
                TotalOutgoing = bankStatements.TotalOutgoing
            };
        }

    }

    public interface IDbService
    {
        public IEnumerable<ExpenditureDto> GetExpenditureDtos(CashflowType expenditureType = CashflowType.Projected);

        public ExpenditureDto? GetExpenditureDto(string name, decimal amount, DateTime paymentDate, CashflowType cashflowType);

        public bool UpsertExpenditureDtos(IEnumerable<ExpenditureDto> expenditureDtos);

        public IEnumerable<IncomeDto> GetIncomeDtos(CashflowType incomeType = CashflowType.Projected);

        public bool UpsertIncomeDtos(IEnumerable<IncomeDto> incomeDtos);

        public IncomeDto? GetIncomeDto(string name, decimal amount, DateTime paymentDate, CashflowType cashflowType);

        public IEnumerable<MonthDto> GetOrCreateMonths(IEnumerable<MonthDto> monthDtos);

        public bool UpdateMonthNotes(int monthId, string notes);

        public BankStatementDto GetBankStatementForMonth(int monthId);
    }
}