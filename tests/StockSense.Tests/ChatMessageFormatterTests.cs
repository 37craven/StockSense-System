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
}
