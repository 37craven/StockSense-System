using StockSense.Application.DTOs;

namespace StockSense.Tests;

public sealed class BuildPayloadParserTests
{
    [Fact]
    public void ParseProductIds_IgnoresMetadataAndReturnsInventoryIds()
    {
        const string json = """
            [
              { "id": 10, "name": "Block" },
              { "id": 20, "name": "ECU" },
              { "id": -999, "category": "SYSTEM_METADATA" }
            ]
            """;

        var ids = BuildPayloadParser.ParseProductIds(json);

        Assert.Equal([10, 20], ids);
    }

    [Fact]
    public void ParseProductIds_RejectsDuplicateProducts()
    {
        const string json = """[{"id":10},{"id":10}]""";

        var error = Assert.Throws<ArgumentException>(() =>
            BuildPayloadParser.ParseProductIds(json));

        Assert.Contains("more than once", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("""[{"id":-999}]""")]
    public void ParseProductIds_RejectsPayloadWithoutInventoryProducts(string json)
    {
        Assert.Throws<ArgumentException>(() => BuildPayloadParser.ParseProductIds(json));
    }

    [Fact]
    public void ParseProductIds_RejectsMalformedJson()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            BuildPayloadParser.ParseProductIds("["));

        Assert.Contains("not valid JSON", error.Message);
    }
}
