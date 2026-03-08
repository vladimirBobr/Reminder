using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class TextBlockSplitterTests
{
    [Fact]
    public void ShouldSplitSingleBlockWithoutEmptyLines()
    {
        var input = """
Блок 1 строка 1
Блок 1 строка 2
Блок 1 строка 3
""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Single(blocks);
        Assert.Equal("""
Блок 1 строка 1
Блок 1 строка 2
Блок 1 строка 3
""", blocks[0]);
    }

    [Fact]
    public void ShouldSplitMultipleBlocksSeparatedByEmptyLines()
    {
        var input = """
Блок 1 строка 1
Блок 1 строка 2

Блок 2 строка 1

Блок 3 строка 1
Блок 3 строка 2
Блок 3 строка 3
""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(3, blocks.Length);

        Assert.Equal("""
Блок 1 строка 1
Блок 1 строка 2
""", blocks[0]);

        Assert.Equal("Блок 2 строка 1", blocks[1]);

        Assert.Equal("""
Блок 3 строка 1
Блок 3 строка 2
Блок 3 строка 3
""", blocks[2]);
    }

    [Fact]
    public void ShouldHandleLeadingAndTrailingEmptyLines()
    {
        var input = """

Блок 1 строка 1

Блок 2 строка 1

Блок 3 строка 1

""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(3, blocks.Length);

        Assert.Equal("Блок 1 строка 1", blocks[0]);
        Assert.Equal("Блок 2 строка 1", blocks[1]);
        Assert.Equal("Блок 3 строка 1", blocks[2]);
    }

    [Fact]
    public void ShouldHandleMultipleConsecutiveEmptyLines()
    {
        var input = """
Блок 1 строка 1


Блок 2 строка 1



Блок 3 строка 1
""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(3, blocks.Length);

        Assert.Equal("Блок 1 строка 1", blocks[0]);
        Assert.Equal("Блок 2 строка 1", blocks[1]);
        Assert.Equal("Блок 3 строка 1", blocks[2]);
    }

    [Fact]
    public void ShouldHandleTabs()
    {
        var input = """
Блок 1 строка 1
	Блок 1 строка 2
        
Блок 2 строка 1
	Блок 2 строка 2
""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(2, blocks.Length);

        Assert.Equal("""
Блок 1 строка 1
	Блок 1 строка 2
""", blocks[0]);

        Assert.Equal("""
Блок 2 строка 1
	Блок 2 строка 2
""", blocks[1]);
    }

    [Fact]
    public void ShouldHandleCarriageReturnAndNewLine()
    {
        var input = """
                    Блок 1 строка 1

                    Блок 2 строка 1

                    Блок 3 строка 1
                    """;

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(3, blocks.Length);

        Assert.Equal("Блок 1 строка 1", blocks[0]);
        Assert.Equal("Блок 2 строка 1", blocks[1]);
        Assert.Equal("Блок 3 строка 1", blocks[2]);
    }

    [Fact]
    public void ShouldHandleMixedWhitespaceAndEmptyLines()
    {
        var input = """
Блок 1 строка 1
	Блок 1 строка 2

			

Блок 2 строка 1

Блок 3 строка 1
	Блок 3 строка 2
""";

        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(3, blocks.Length);

        Assert.Equal("""
Блок 1 строка 1
	Блок 1 строка 2
""", blocks[0]);

        Assert.Equal("Блок 2 строка 1", blocks[1]);

        Assert.Equal("""
Блок 3 строка 1
	Блок 3 строка 2
""", blocks[2]);
    }

    [Fact]
    public void ShouldHandleSingleCharacter()
    {
        var input = "a";
        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Single(blocks);
        Assert.Equal("a", blocks[0]);
    }

    [Theory]
    [InlineData("", "Empty string")]
    [InlineData(" ", "Single space")]
    [InlineData("\t", "Single tab")]
    [InlineData("\n", "Single newline")]
    [InlineData("\r", "Single carriage return")]
    [InlineData("\r\n", "CRLF")]
    [InlineData("   \n\t\r\n  ", "Mixed whitespace")]
    [InlineData("\u00A0", "Non-breaking space")]
    [InlineData("\u2000", "En quad")]
    [InlineData("\u200B", "Zero-width space")]
    [InlineData("\u00A0\u2000\u200B", "Mixed Unicode whitespace")]
    [InlineData("\u200C", "Zero-width non-joiner")]
    [InlineData("\u200D", "Zero-width joiner")]
    public void ShouldHandleWhitespaceOnly(string input, string description)
    {
        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.True(blocks.Length == 0, $"Expected empty blocks for: {description}");
    }

    [Fact]
    public void ShouldHandleLongStringWithManyBlocks()
    {
        var input = string.Join("\n\n", Enumerable.Repeat("Блок", 1000));
        var blocks = TextBlockSplitter.SplitIntoBlocks(input);
        Assert.Equal(1000, blocks.Length);
    }

    
}
