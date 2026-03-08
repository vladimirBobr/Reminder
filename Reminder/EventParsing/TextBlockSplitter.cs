using System.Text;

namespace ReminderApp.EventParsing;

public static class TextBlockSplitter
{
    public static string[] SplitIntoBlocks(string input)
    {
        if (string.IsNullOrEmpty(input)) return [];

        var lines = input.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

        var blocks = new List<string>();
        var currentBlock = new StringBuilder();

        foreach (var line in lines)
        {
            if (IsAllWhitespace(line))
            {
                // Если текущий блок не пуст — завершаем его и добавляем в блоки
                if (currentBlock.Length > 0)
                {
                    blocks.Add(currentBlock.ToString());
                    currentBlock.Clear();
                }
            }
            else
            {
                if (currentBlock.Length > 0)
                    currentBlock.AppendLine();
                currentBlock.Append(line);
            }
        }

        // Не забываем последний блок
        if (currentBlock.Length > 0)
        {
            blocks.Add(currentBlock.ToString());
        }

        return blocks.ToArray();
    }

    private static bool IsAllWhitespace(string s)
    {
        if (string.IsNullOrEmpty(s)) return true;
        return s.All(c => char.IsWhiteSpace(c) || c == '\u200B' || c == '\u200C' || c == '\u200D');
    }
}
