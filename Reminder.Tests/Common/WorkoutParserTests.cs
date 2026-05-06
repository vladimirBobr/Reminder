using ReminderApp.Common;

namespace Reminder.Tests.Common;

public class WorkoutParserTests
{
    [Fact]
    public void Parse_WithEmptyText_ReturnsEmptyList()
    {
        // Arrange
        var text = "";
        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithNullText_ReturnsEmptyList()
    {
        // Arrange
        string? text = null;
        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text!, referenceDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithSingleDay_ParsesCorrectly()
    {
        // Arrange
        var text = "✅️Вт 2км разминка";
        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Single(result);
        Assert.Equal("Вт", result[0].DayName);
        Assert.Equal(1, result[0].DayNum);
        Assert.Equal(new DateTime(2026, 5, 5), result[0].Date);
        Assert.Equal("2км разминка", result[0].Description);
    }

    [Fact]
    public void Parse_WithMultipleDays_ParsesAll()
    {
        // Arrange
        var text = @"Задание 

✅️Вт 2км разминка
5×1500 ~3.40 темп

✅️Чт 8км ~5.00+

✅️Сб 2км разминка";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Equal(3, result.Count);
        
        // Check first workout (Вт) - offset 1 from Monday
        Assert.Equal("Вт", result[0].DayName);
        Assert.Equal(1, result[0].DayNum);
        Assert.Equal(new DateTime(2026, 5, 5), result[0].Date);
        Assert.Contains("2км разминка", result[0].Description);
        Assert.Contains("5×1500", result[0].Description);
        
        // Check second workout (Чт) - offset 3 from Monday
        Assert.Equal("Чт", result[1].DayName);
        Assert.Equal(3, result[1].DayNum);
        Assert.Equal(new DateTime(2026, 5, 7), result[1].Date);
        
        // Check third workout (Сб) - offset 5 from Monday
        Assert.Equal("Сб", result[2].DayName);
        Assert.Equal(5, result[2].DayNum);
        Assert.Equal(new DateTime(2026, 5, 9), result[2].Date);
    }

    [Fact]
    public void Parse_RemovesEmojis_CleansDescription()
    {
        // Arrange
        var text = @"✅️Вт 2км разминка

✅️

1км заминка";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Single(result);
        // Emoji-only lines and empty lines should be removed
        Assert.DoesNotContain("✅", result[0].Description);
        Assert.DoesNotContain("✅️", result[0].Description);
    }

    [Fact]
    public void Parse_SortsByDate()
    {
        // Arrange - text has days in random order: Сб, Вт, Чт
        var text = @"✅️Сб 10км
✅️Вт 5км
✅️Чт 8км";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert - should be sorted by date: Вт (5.05), Чт (7.05), Сб (9.05)
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].DayNum);  // Вт = offset 1
        Assert.Equal(3, result[1].DayNum);  // Чт = offset 3
        Assert.Equal(5, result[2].DayNum);  // Сб = offset 5
        Assert.Equal("Вт", result[0].DayName);
        Assert.Equal("Чт", result[1].DayName);
        Assert.Equal("Сб", result[2].DayName);
    }

    [Fact]
    public void Parse_HandlesLongDescription_MultiLine()
    {
        // Arrange
        var text = @"✅️Вт 2км разминка, гибкость. 
5×1500 ~3.40-3.45 темп на км/ 3мин трусца

1км заминка";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Single(result);
        Assert.Contains("2км разминка", result[0].Description);
        Assert.Contains("5×1500", result[0].Description);
        Assert.Contains("1км заминка", result[0].Description);
    }

    [Fact]
    public void Parse_WithRealCoachMessage_ParsesCorrectly()
    {
        // Arrange - real message from coach
        var text = @"Задание 

✅️Вт 2км разминка, гибкость. 
5×1500 ~3.40-3.45 темп на км/ 3мин трусца

1км заминка 

✅️Чт 8км ~5.00+

✅️Сб 2км разминка, гибкость. 
5×600м ~3.10 / 2,5-3 мин трусца

1км заминка 

✅️Вс 10км ~5.00 +";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Equal(4, result.Count);
        
        // Tuesday (Вт)
        Assert.Equal("Вт", result[0].DayName);
        Assert.Contains("2км разминка", result[0].Description);
        Assert.Contains("5×1500", result[0].Description);
        
        // Thursday (Чт)
        Assert.Equal("Чт", result[1].DayName);
        Assert.Contains("8км", result[1].Description);
        
        // Saturday (Сб)
        Assert.Equal("Сб", result[2].DayName);
        Assert.Contains("2км разминка", result[2].Description);
        
        // Sunday (Вс)
        Assert.Equal("Вс", result[3].DayName);
        Assert.Contains("10км", result[3].Description);
    }

    [Fact]
    public void Parse_WithMonday_ParsesCorrectly()
    {
        // Arrange - message with Monday
        var text = @"✅️ Пн силовая

✅️ Вт 2км разминка

✅️ Чт 8-10 км лёгкий бег";

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Equal(3, result.Count);
        
        // Monday should be first
        Assert.Equal("Пн", result[0].DayName);
        Assert.Equal(new DateTime(2026, 5, 4), result[0].Date);
    }
}