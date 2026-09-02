using UniFutsal.Data;
using Xunit;

namespace UniFutsal.Tests.Import
{
    public class VenueImporterTests
    {
        [Fact]
        public void CsvHelper_ParseLine_SplitsCorrectly()
        {
            var result = CsvHelper.ParseLine("a,b,c");
            Assert.Equal(3, result.Length);
            Assert.Equal("a", result[0]);
            Assert.Equal("b", result[1]);
            Assert.Equal("c", result[2]);
        }

        [Fact]
        public void CsvHelper_GetField_FindsColumn()
        {
            var header = CsvHelper.ParseLine("name,capacity,city");
            var fields = CsvHelper.ParseLine("Test,5000,Madrid");

            Assert.Equal("Test", CsvHelper.GetField(header, fields, "name"));
            Assert.Equal("5000", CsvHelper.GetField(header, fields, "capacity"));
            Assert.Equal("Madrid", CsvHelper.GetField(header, fields, "city"));
        }

        [Fact]
        public void CsvHelper_GetField_ReturnsEmpty_WhenMissing()
        {
            var header = CsvHelper.ParseLine("name,capacity");
            var fields = CsvHelper.ParseLine("Test,5000");

            Assert.Equal("", CsvHelper.GetField(header, fields, "nonexistent"));
        }
    }
}