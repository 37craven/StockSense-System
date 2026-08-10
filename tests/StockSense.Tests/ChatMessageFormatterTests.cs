using StockSense.Client.Components;

namespace StockSense.Tests;

public sealed class ChatMessageFormatterTests
{
    [Fact]
    public void Parse_preserves_table_structure_and_cell_text()
    {
        const string content = "| Part | Stock |\n| --- | ---: |\n| Air Filter | 3 |\n| Brake Pad | 0 |";

        var table = Assert.IsType<ChatTableBlock>(Assert.Single(ChatMessageFormatter.Parse(content)));

        Assert.Equal(["Part", "Stock"], table.Headers);
        Assert.Equal(["Air Filter", "3"], table.Rows[0]);
        Assert.Equal(["Brake Pad", "0"], table.Rows[1]);
    }

    [Theory]
    [InlineData("Here are the products currently available:\n\n| Product | Brand | Price | Availability |\n| --- | --- | ---: | :---: |\n| Honda Oil 800 mL | Honda | ₱334.29 | Available |", "Product", "Honda Oil 800 mL")]
    [InlineData("Inventory summary: 2 items need attention.\n\n| Product | On hand | Reorder point | Status |\n| --- | ---: | ---: | --- |\n| Brake Pad | 0 | 5 | Critical |", "Product", "Brake Pad")]
    [InlineData("Sales summary for August 6, 2026.\n\n| Product | Units | Sales | Transactions |\n|---|---:|---:|---:|\n| Engine Oil | 4 | ₱1,400.00 | 3 |", "Product", "Engine Oil")]
    public void Parse_recognizes_customer_employee_and_admin_report_tables(
        string content,
        string expectedHeader,
        string expectedFirstCell)
    {
        var table = Assert.IsType<ChatTableBlock>(ChatMessageFormatter.Parse(content)[1]);

        Assert.Equal(expectedHeader, table.Headers[0]);
        Assert.Equal(expectedFirstCell, table.Rows[0][0]);
    }

    [Fact]
    public void Table_cells_use_an_accessible_placeholder_for_missing_mobile_card_values()
    {
        var table = new ChatTableBlock(
            ["Product", "Availability"],
            [["Engine Oil"]]);

        Assert.Equal("Engine Oil", table.CellAt(table.Rows[0], 0));
        Assert.Equal("—", table.CellAt(table.Rows[0], 1));
        Assert.Equal(string.Empty, table.CellAt(table.Rows[0], 1, string.Empty));
    }

    [Theory]
    [InlineData("Completed", ChatStatusTone.Positive)]
    [InlineData("Pending", ChatStatusTone.Attention)]
    [InlineData("Partially received", ChatStatusTone.Attention)]
    [InlineData("Overdue", ChatStatusTone.Critical)]
    [InlineData("Custom state", ChatStatusTone.Neutral)]
    public void Status_semantics_are_bounded_to_status_columns(string value, ChatStatusTone expected)
    {
        var table = new ChatTableBlock(["Order", "Status"], [["OS-1", value]]);

        Assert.Equal(expected, table.StatusToneAt(table.Rows[0], 1));
        Assert.Equal(ChatStatusTone.None, table.StatusToneAt(table.Rows[0], 0));
    }

    [Fact]
    public void Arbitrary_status_text_remains_plain_data()
    {
        const string untrusted = "<img src=x onerror=alert(1)>";
        var table = new ChatTableBlock(["Status"], [[untrusted]]);

        Assert.Equal(untrusted, table.CellAt(table.Rows[0], 0));
        Assert.Equal(ChatStatusTone.Neutral, table.StatusToneAt(table.Rows[0], 0));
    }

    [Fact]
    public void Parse_recognizes_headings_lists_code_and_paragraphs_in_order()
    {
        const string content = "## Recommended\n- Engine oil\n- Air filter\n\nDetails here.\n\n```text\n10W-40\n```";

        var blocks = ChatMessageFormatter.Parse(content);

        Assert.Collection(
            blocks,
            block => Assert.Equal("Recommended", Assert.IsType<ChatHeadingBlock>(block).Text),
            block => Assert.Equal(["Engine oil", "Air filter"], Assert.IsType<ChatListBlock>(block).Items),
            block => Assert.Equal("Details here.", Assert.IsType<ChatParagraphBlock>(block).Text),
            block => Assert.Equal("10W-40", Assert.IsType<ChatCodeBlock>(block).Code));
    }

    [Fact]
    public void Parse_keeps_html_as_text_instead_of_creating_markup()
    {
        const string content = "<script>alert('x')</script>";

        var paragraph = Assert.IsType<ChatParagraphBlock>(Assert.Single(ChatMessageFormatter.Parse(content)));

        Assert.Equal(content, paragraph.Text);
    }

    [Fact]
    public void Parse_recognizes_unicode_bullet_lists()
    {
        const string content = "• Engine oil: 10W-40\n• Spark plug: CPR8EA-9";

        var list = Assert.IsType<ChatListBlock>(Assert.Single(ChatMessageFormatter.Parse(content)));

        Assert.False(list.Ordered);
        Assert.Equal(["Engine oil: 10W-40", "Spark plug: CPR8EA-9"], list.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n  ")]
    public void Parse_empty_content_returns_no_blocks(string? content)
    {
        Assert.Empty(ChatMessageFormatter.Parse(content));
    }

    [Theory]
    [InlineData("1. First\n2. Second")]
    [InlineData("1) First\n2) Second")]
    public void Parse_recognizes_ordered_list_variants(string content)
    {
        var list = Assert.IsType<ChatListBlock>(Assert.Single(ChatMessageFormatter.Parse(content)));
        Assert.True(list.Ordered);
        Assert.Equal(["First", "Second"], list.Items);
    }

    [Fact]
    public void Parse_preserves_unclosed_code_fence_as_code_text()
    {
        var code = Assert.IsType<ChatCodeBlock>(Assert.Single(ChatMessageFormatter.Parse("```sql\nSELECT * FROM Products")));
        Assert.Equal("sql", code.Language);
        Assert.Equal("SELECT * FROM Products", code.Code);
    }

    [Fact]
    public void Parse_does_not_promote_malformed_markdown_table()
    {
        var blocks = ChatMessageFormatter.Parse("| Product | Stock |\n| -- | nope |\n| Oil | 2 |");
        Assert.DoesNotContain(blocks, block => block is ChatTableBlock);
    }

    [Fact]
    public void Formatter_preserves_links_and_script_payloads_as_plain_text_data()
    {
        const string payload = "[click](javascript:alert(1)) <script>steal()</script>";
        var paragraph = Assert.IsType<ChatParagraphBlock>(Assert.Single(ChatMessageFormatter.Parse(payload)));
        Assert.Equal(payload, paragraph.Text);
    }

    [Fact]
    public void Parse_preserves_unicode_and_emoji()
    {
        const string content = "Available: ✅ 油 — ₱334.29";
        var paragraph = Assert.IsType<ChatParagraphBlock>(Assert.Single(ChatMessageFormatter.Parse(content)));
        Assert.Equal(content, paragraph.Text);
    }
}
