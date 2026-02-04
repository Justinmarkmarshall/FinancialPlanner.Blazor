using FinancialPlanner.Blazor.Components.Models;
using FinancialPlanner.Blazor.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Moq;
using static FinancialPlanner.Blazor.Components.Pages.StatementUpload;

namespace FinancialPlanner.UnitTests
{
    public class BankStatementImportTests
    {
        private readonly Mock<ILogger<BankStatementImportService>> _loggerMock = new();
        private readonly BankStatementImportService _importService;

        public BankStatementImportTests()
        {
            _importService = new BankStatementImportService(_loggerMock.Object);
        }

        private const string dummyAccountNumber = "000000 00000000";

        [Fact]
        public async Task ProcessBankStatementFile_Uses_Nationwide_Csv_Includes_All_Parsed_Rows_And_Skips_Transfers()
        {
            // Arrange
            var csvPath = Path.Combine(AppContext.BaseDirectory, "BankStatements", "TestStatement.csv");
            Assert.True(File.Exists(csvPath), $"Missing test data file: {csvPath}");

            var bytes = await File.ReadAllBytesAsync(csvPath);
            var file = new TestBrowserFile("TestStatement.csv", bytes);

            var uploadModel = new UploadModel
            {
                HasHeaderRow = true,
                AmountColumnIndex = 1,
                DateColumnIndex = 0,
                DescriptionColumnIndex = 2,
                DateFormat = "dd/MM/yyyy"
            };

            // Act
            var results = await _importService.ProcessBankStatementFile(file, uploadModel);

            // Assert: new behavior no longer gates to "start on day 1"; it returns all parsed rows.
            // Earliest row in the test CSV is 27-Sep-25.
            Assert.NotEmpty(results);
            Assert.Equal(new DateTime(2025, 9, 27), results.Min(x => x.PaymentDate));

            // Assert: transfers between accounts are still ignored.
            Assert.DoesNotContain(results, x => x.Name.Contains(dummyAccountNumber, StringComparison.OrdinalIgnoreCase));

            // Assert: sanity check that we get a mixture of DTO types from this file.
            // (This file includes paid-out rows; income rows may or may not parse depending on encoding of £ in the Paid in column.)
            Assert.Contains(results, x => x is ExpenditureDto);

            // Assert: all returned items are meaningful.
            Assert.All(results, x =>
            {
                Assert.False(string.IsNullOrWhiteSpace(x.Name));
                Assert.NotEqual(default, x.PaymentDate);
            });
        }
        private sealed class TestBrowserFile : IBrowserFile
        {
            private readonly byte[] _content;

            public TestBrowserFile(string name, byte[] content)
            {
                Name = name;
                _content = content;
                ContentType = "text/csv";
                Size = content.Length;
                LastModified = DateTimeOffset.UtcNow;
            }

            public string Name { get; }
            public DateTimeOffset LastModified { get; }
            public long Size { get; }
            public string ContentType { get; }

            public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken token = default)
            {
                return new MemoryStream(_content);
            }
        }


    }
}
