using Xunit.Abstractions;

public static class TestHelper
{
    /// <summary>
    /// Выводит две строки side-by-side.
    /// Перед первой строкой с различиями — ставит `→` в начало строки EXPECTED.
    /// Без цветов — только текст.
    /// </summary>
    public static void WriteSideBySide(ITestOutputHelper output, string expected, string? actual)
    {
        var expectedLines = SplitLines(expected);
        var actualLines = SplitLines(actual);

        int maxLines = Math.Max(expectedLines.Count, actualLines.Count);

        // Находим первую строку с различием
        int firstDiffIndex = -1;
        for (int i = 0; i < maxLines; i++)
        {
            string left = i < expectedLines.Count ? expectedLines[i] : "";
            string right = i < actualLines.Count ? actualLines[i] : "";

            if (left != right)
            {
                firstDiffIndex = i;
                break;
            }
        }

        // Заголовок
        output.WriteLine("EXPECTED".PadRight(40) + "MODIFIED");
        output.WriteLine(new string('-', 80));

        for (int i = 0; i < maxLines; i++)
        {
            string left = i < expectedLines.Count ? expectedLines[i] : "";
            string right = i < actualLines.Count ? actualLines[i] : "";

            // Если это первая строка с различием — добавляем → в начало EXPECTED
            string leftWithMarker = left;
            if (i == firstDiffIndex)
            {
                leftWithMarker = "→" + left;
            }

            // 40 символов под EXPECTED, остальное — MODIFIED
            string leftPad = leftWithMarker.PadRight(40);

            output.WriteLine($"{leftPad}{right}");
        }

        output.WriteLine(new string('-', 80));
    }

    /// <summary>
    /// Разбивает строку на строки, удаляя пустые строки в конце.
    /// </summary>
    private static List<string> SplitLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string> { "" };

        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).ToList();

        // Удаляем пустые строки в конце
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines.Last()))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }
}
