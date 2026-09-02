using UniFutsal.Data;
using Xunit;

namespace UniFutsal.Tests.Import
{
    public class ClubImporterTests
    {
        [Fact]
        public void CsvHelper_ParseLine_HandlesMultipleFields()
        {
            var result = CsvHelper.ParseLine("a,b,c,d,e,f,g");
            Assert.Equal(7, result.Length);
        }

        [Fact]
        public void CsvHelper_GetField_CaseInsensitive()
        {
            var header = CsvHelper.ParseLine("Name,SHORT_name,CITY");
            var fields = CsvHelper.ParseLine("Madrid FS,MAD,Madrid");

            Assert.Equal("Madrid FS", CsvHelper.GetField(header, fields, "name"));
            Assert.Equal("MAD", CsvHelper.GetField(header, fields, "short_name"));
            Assert.Equal("Madrid", CsvHelper.GetField(header, fields, "city"));
        }
    }
}