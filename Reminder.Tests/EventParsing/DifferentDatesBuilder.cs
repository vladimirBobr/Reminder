using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class DifferentDatesBuilder
{
    private readonly List<string> _blocks = new();

    public DifferentDatesBuilder WithBlocks(params string[] blocks)
    {
        _blocks.AddRange(blocks);
        return this;
    }

    public void AssertMatches(FileParsingResult result)
    {
        Assert.NotNull(result.DifferentDates);
        Assert.Equal(_blocks.Count, result.DifferentDates.EventBlocks.Count);
        for (int i = 0; i < _blocks.Count; i++)
        {
            Assert.Equal(_blocks[i], result.DifferentDates.EventBlocks[i]);
        }
    }
}
