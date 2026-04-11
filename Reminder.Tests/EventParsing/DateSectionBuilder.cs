using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class DateSectionBuilder
{
    private readonly string _date;
    private readonly List<string> _blocks = new();

    public DateSectionBuilder(string date)
    {
        _date = date;
    }

    public DateSectionBuilder WithBlocks(params string[] blocks)
    {
        _blocks.AddRange(blocks);
        return this;
    }

    public void AssertMatches(FileParsingResult result)
    {
        var dateOnly = DateOnly.ParseExact(_date, "dd.MM.yyyy");
        var section = result.DateSections.FirstOrDefault(s => s.Date == dateOnly);
        Assert.NotNull(section);
        Assert.Equal(_blocks.Count, section.EventBlocks.Count);
        for (int i = 0; i < _blocks.Count; i++)
        {
            Assert.Equal(_blocks[i], section.EventBlocks[i]);
        }
    }

    public void AssertBlockCount(FileParsingResult result, int expectedCount)
    {
        var dateOnly = DateOnly.ParseExact(_date, "dd.MM.yyyy");
        var section = result.DateSections.FirstOrDefault(s => s.Date == dateOnly);
        Assert.NotNull(section);
        Assert.Equal(expectedCount, section.EventBlocks.Count);
    }
}
