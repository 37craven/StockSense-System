using StockSense.Web.Helpers;

namespace StockSense.Tests;

public sealed class PurchaseOrderEmailTemplateTests
{
    [Fact]
    public void Build_creates_branded_attachment_email_and_encodes_reference()
    {
        var html = PurchaseOrderEmailTemplate.Build("OS-123<script>");

        Assert.Contains("SAP SHOP", html);
        Assert.Contains("New purchase order", html);
        Assert.Contains("PDF order slip attached", html);
        Assert.Contains("OS-123&lt;script&gt;", html);
        Assert.DoesNotContain("OS-123<script>", html);
    }
}
