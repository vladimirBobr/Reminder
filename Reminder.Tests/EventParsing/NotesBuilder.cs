using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public partial class FileParserTests
{
    public class NotesBuilder
    {
        private readonly List<string> _blocks = new();

        public NotesBuilder WithBlocks(params string[] blocks)
        {
            _blocks.AddRange(blocks);
            return this;
        }

        public void AssertMatches(FileParsingResult result)
        {
            Assert.NotNull(result.NotesSection);
            Assert.Equal(_blocks.Count, result.NotesSection.EventBlocks.Count);
            for (int i = 0; i < _blocks.Count; i++)
            {
                Assert.Equal(_blocks[i], result.NotesSection.EventBlocks[i]);
            }
        }
    }
}
