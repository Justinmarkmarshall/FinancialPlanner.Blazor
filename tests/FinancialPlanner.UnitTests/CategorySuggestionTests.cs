using FinancialPlanner.Blazor.Services;
using Microsoft.Extensions.Logging;
using Moq;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.UnitTests
{

    public class CategorySuggestionTests
    {
        private readonly Mock<ILogger<BankStatementImportService>> _loggerMock = new();
        private readonly BankStatementImportService _importService;

        public CategorySuggestionTests()
        {
            _importService = new BankStatementImportService(_loggerMock.Object);
        }
        

        #region SuggestExpenditureCategory Tests

        //[Theory]
        //[InlineData("ONE STOP STORES NEWPORT GB GOOGLE 3986", CategoryType.Miscellaneous)] // No matching rule
        //[InlineData("SPAR BASSALEG ROAD NEWPORT GB GOOGLE 3986", CategoryType.Groceries)]
        //[InlineData("MGP*Vinted London GB", CategoryType.PersonalCare)]
        //[InlineData("TESCO STORES 2931 NEWPORT GB GOOGLE 3986", CategoryType.Groceries)]
        //[InlineData("Google One London GB", CategoryType.Utilities)]
        //[InlineData("ARGOS NEWPORT NEWPORT GB GOOGLE 3986", CategoryType.Miscellaneous)]
        //[InlineData("DVLA-EF22ZXJ", CategoryType.Car)]
        //[InlineData("NCC COLLECTIONS AC", CategoryType.Utilities)]
        //[InlineData("Google YouTube London GB", CategoryType.Utilities)]
        //[InlineData("NYX*Tesco Newport GB GOOGLE 3986", CategoryType.Groceries)]
        //[InlineData("Withdrawal 05 October 2025", CategoryType.Miscellaneous)] // No matching rule
        //[InlineData("ST WOOLOS BARBERS NEWPORT GB GOOGLE 3986", CategoryType.PersonalCare)]
        //[InlineData("SLC RECEIPTS", CategoryType.Miscellaneous)] // No matching rule
        //[InlineData("NEW LIFE TRUST", CategoryType.Tithing)]
        //[InlineData("OPENAI *CHATGPT SUBSCR OPENAI.COM US 8157", CategoryType.Education)]
        //[InlineData("NATIONAL ASSURANCE", Category.Utilities)]
        //[InlineData("SQ *MIKE'S WINDOW CLEANIN Cwmbran GB GOOGLE 3986", CategoryType.Home)]
        //[InlineData("WHICH LIMITED", Category.Miscellaneous)] // No matching rule
        //[InlineData("ASPIEGEL LTD. IRELAND IE", Category.Miscellaneous)] // No matching rule
        //[InlineData("AMAZON UK* OH87Z6QN5 LONDON GB 8774", Category.Miscellaneous)]
        //[InlineData("VIRGIN MEDIA PYMTS", CategoryType.Utilities)]
        //[InlineData("VODAFONE LTD", Category.Utilities)]
        //[InlineData("CREATION.CO.UK", Category.DebtRepayment)]
        //[InlineData("KAERCHER ECOMUK BANBURY OXON GB", Category.Miscellaneous)] // No matching rule
        //[InlineData("OVO ENERGY", Category.Utilities)]
        //[InlineData("CARDIFF WEST BK MID GLAMORGAN GB GOOGLE 3986", Category.Miscellaneous)] // No matching rule
        //[InlineData("RONTEC CARDIFF WEST MID GLAMORGAN GB GOOGLE 3986", Category.Miscellaneous)] // No matching rule
        //[InlineData("DWR CYMRU WELSH WA", Category.Utilities)]
        //[InlineData("NEWPORT MARKET LONDON GB 2748", Category.DiningOut)]
        //[InlineData("AMAZON* JI4HY5TI5 LONDON GB 8774", Category.Miscellaneous)]
        //[InlineData("B & Q 1300 NEWPORT GB GOOGLE 3986", Category.Home)]
        //[InlineData("MICROSOFT*MICROSOFT 365 P MSBILL.INFO GB", Category.Education)] // No matching rule
        //[InlineData("VWFS UK LIMITED", Category.Car)]
        //[InlineData("ADMIRAL INSURANCE", Category.Car)]
        //[InlineData("Amazon.co.uk*YA2MI9YQ5 AMAZON.CO.UK GB 8774", Category.Miscellaneous)]
        //[InlineData("Amazon Prime*YG84G9DI5 amzn.co.uk/pm GB 8774", Category.Utilities)] // Matches PRIME
        //[InlineData("Withdrawal 23 October 2025", Category.Miscellaneous)] // No matching rule
        //[InlineData("COTSWOLD GLIDING CLUB STROUD, GLOUC GB GOOGLE 3986", Category.Miscellaneous)] // No matching rule
        //[InlineData("Zettle_*Brinko?s Event Ca GLOUCESTER GB GOOGLE 3986", Category.Miscellaneous)] // No matching rule
        //[InlineData("TESCO PFS 3952 NEWPORT GWENT GB GOOGLE 3986", CategoryType.Groceries)]
        //[InlineData("ICELAND NEWPORT NEWPORT GB GOOGLE 3986", CategoryType.Groceries)]
        //[InlineData("B&M 607 - NEWPORT NEWPORT GB GOOGLE 3986", Category.Groceries)]
        //[InlineData("DUNELM F0460 Newport GB GOOGLE 3986", Category.Home)]
        //[InlineData("MCDONALDS CARDIFF ROAD GB GOOGLE 3986", Category.DiningOut)]
        //[InlineData("SPOTIFY", Category.Utilities)]
        //[InlineData("MR J M MARSHAL", Category.Housing)]
        //public void SuggestExpenditureCategory_Returns_Correct_Category(string description, CategoryType expectedCategory)
        //{
        //    var result = _importService.SuggestExpenditureCategory(description);
        //    Assert.Equal(expectedCategory, result);
        //}

        //[Fact]
        //public void SuggestExpenditureCategory_Is_Case_Insensitive()
        //{
        //    var upperResult = _importService.SuggestExpenditureCategory("TESCO STORES");
        //    var lowerResult = _importService.SuggestExpenditureCategory("tesco stores");
        //    var mixedResult = _importService.SuggestExpenditureCategory("Tesco Stores");

        //    Assert.Equal(Category.Groceries, upperResult);
        //    Assert.Equal(Category.Groceries, lowerResult);
        //    Assert.Equal(Category.Groceries, mixedResult);
        //}

        //[Fact]
        //public void SuggestExpenditureCategory_Returns_Miscellaneous_For_Unknown()
        //{
        //    var result = _importService.SuggestExpenditureCategory("UNKNOWN MERCHANT XYZ123");
        //    Assert.Equal(Category.Miscellaneous, result);
        //}

        //#endregion

        //#region SuggestIncomeCategory Tests

        //[Theory]
        //[InlineData("Hansen Technologies", IncomeCategory.Salary)]
        //[InlineData("HANSEN TECHNOLOGIE", IncomeCategory.Salary)]
        //[InlineData("Hansen Bonus", IncomeCategory.Salary)]
        //[InlineData("S Saha Roy", IncomeCategory.Lodger)]
        //[InlineData("Roy Sankalan", IncomeCategory.Other)] // ROY alone doesn't match "S SAHA ROY"
        //[InlineData("MR J M MARSHAL", IncomeCategory.Other)] // This is an expenditure rule, not income
        //public void SuggestIncomeCategory_Returns_Correct_Category(string description, IncomeCategory expectedCategory)
        //{
        //    var result = _importService.SuggestIncomeCategory(description);
        //    Assert.Equal(expectedCategory, result);
        //}

        //[Fact]
        //public void SuggestIncomeCategory_Is_Case_Insensitive()
        //{
        //    var upperResult = _importService.SuggestIncomeCategory("HANSEN TECHNOLOGIES");
        //    var lowerResult = _importService.SuggestIncomeCategory("hansen technologies");
        //    var mixedResult = _importService.SuggestIncomeCategory("Hansen Technologies");

        //    Assert.Equal(IncomeCategory.Salary, upperResult);
        //    Assert.Equal(IncomeCategory.Salary, lowerResult);
        //    Assert.Equal(IncomeCategory.Salary, mixedResult);
        //}

        //[Fact]
        //public void SuggestIncomeCategory_Returns_Other_For_Unknown()
        //{
        //    var result = _importService.SuggestIncomeCategory("UNKNOWN PAYER XYZ123");
        //    Assert.Equal(IncomeCategory.Other, result);
        //}

        //[Fact]
        //public void SuggestIncomeCategory_Matches_Partial_Pattern()
        //{
        //    // "S SAHA ROY" should match descriptions containing that pattern
        //    var result = _importService.SuggestIncomeCategory("Bank credit from S Saha Roy on 01/01/2025");
        //    Assert.Equal(IncomeCategory.Lodger, result);
        //}

        #endregion
    }
}
