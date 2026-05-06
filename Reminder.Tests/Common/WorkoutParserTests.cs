using ReminderApp.Common;
using Xunit;

namespace Reminder.Tests.Common;

public class WorkoutParserTests
{
    private void AssertWorkoutsEquals(List<WorkoutParser.ParsedWorkout> expected, WorkoutParser.ParseResult actual, string? expectedIntroContains = null)
    {
        Assert.Equal(expected.Count, actual.Workouts.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Date, actual.Workouts[i].Date);
            Assert.Equal(expected[i].DayName, actual.Workouts[i].DayName);
            Assert.Equal(expected[i].DayNum, actual.Workouts[i].DayNum);
            // Normalize line endings: replace \r\n with \n for comparison
            var expectedDesc = expected[i].Description.Trim().Replace("\r\n", "\n");
            var actualDesc = actual.Workouts[i].Description.Trim().Replace("\r\n", "\n");
            Assert.Equal(expectedDesc, actualDesc);
        }
        
        if (expectedIntroContains != null)
        {
            Assert.Contains(expectedIntroContains, actual.Intro);
        }
    }

    [Fact]
    public void Parse_WithEmptyText_ReturnsEmptyList()
    {
        // Arrange
        var text = "";
        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        // Assert
        Assert.Empty(result.Workouts);
        Assert.Equal("", result.Intro);
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
        Assert.Empty(result.Workouts);
    }

    [Fact]
    public void Parse_WithSimpleMessage_ParsesCorrectly()
    {
        // Arrange
        var text = """
            Задание 

            ✅️Вт 2км разминка
            5×1500 ~3.40 темп

            ✅️Чт 8км ~5.00+

            ✅️Сб 2км разминка
            С собой взять гель

            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка
                5×1500 ~3.40 темп
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = """
                8км ~5.00+
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 9),
                DayName = "Суббота",
                DayNum = 5,
                Description = """
                2км разминка
                С собой взять гель
                """
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_SortsByDate()
    {
        // Arrange - text has days in random order: Сб, Вт, Чт
        var text = """
            Задание

            ✅️Сб 10км
            ✅️Вт 5км
            ✅️Чт 8км
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = "5км"
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = "8км"
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 9),
                DayName = "Суббота",
                DayNum = 5,
                Description = "10км"
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_WithIntro_ParsesCorrectly()
    {
        // Arrange - message with intro before first day
        var text = """
            Владимир, добрый день!
            Хорошая неделя. Все тренировки хорошие качественные!

            ✅️ Вт 2км разминка, гибкость.

            6 - 7км ~4.15

            2км заминка .

            ✅️ Чт 2км разминка, гибкость.
            2×4× 500 м ( темп из 3.25 ) / 3мин между отрезками/ 5мин между сериями.
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка, гибкость.
                6 - 7км ~4.15
                2км заминка .
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = """
                2км разминка, гибкость.
                2×4× 500 м ( темп из 3.25 ) / 3мин между отрезками/ 5мин между сериями.
                """
            },
        };

        AssertWorkoutsEquals(expected, result, "Владимир, добрый день!\nХорошая неделя.");
    }

    [Fact]
    public void Parse_RemovesEmojis_CleansDescription()
    {
        // Arrange
        var text = """
            Задание

            ✅️Вт 2км разминка

            ✅️

            1км заминка
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка
                1км заминка
                """
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_HandlesLongDescription_MultiLine()
    {
        // Arrange
        var text = """
            ✅️Вт 2км разминка, гибкость. 
            5×1500 ~3.40-3.45 темп на км/ 3мин трусца

            1км заминка
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка, гибкость.
                5×1500 ~3.40-3.45 темп на км/ 3мин трусца
                1км заминка
                """
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_WithRealCoachMessage_ParsesCorrectly()
    {
        // Arrange - real message from coach
        var text = """
            Задание 

            ✅️Вт 2км разминка, гибкость. 
            5×1500 ~3.40-3.45 темп на км/ 3мин трусца

            1км заминка 

            ✅️Чт 8км ~5.00+

            ✅️Сб 2км разминка, гибкость. 
            5×600м ~3.10 / 2,5-3 мин трусца

            1км заминка 

            ✅️Вс 10км ~5.00 +
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка, гибкость.
                5×1500 ~3.40-3.45 темп на км/ 3мин трусца
                1км заминка
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = "8км ~5.00+"
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 9),
                DayName = "Суббота",
                DayNum = 5,
                Description = """
                2км разминка, гибкость.
                5×600м ~3.10 / 2,5-3 мин трусца
                1км заминка
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 10),
                DayName = "Воскресенье",
                DayNum = 6,
                Description = "10км ~5.00 +"
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_WithZavtraPrefix_ParsesCorrectly()
    {
        // Arrange - message with "Завтра" before first day (should be ignored after emoji removal)
        var text = """
            ✅️ Завтра после сегодняшнего прогрессивного надо сбегать 10км 5.00+ темп

            ✅️ Чт 12км за тренировку
            1км разминочный легко + 11км ~4.40 + заминка 200м.

            ✅️ Сб 2км разминка, гибкость

            6×1км 3.45 и быстрее
            Отдых 3минуты шаг/трусца

            Заминка 1км

            ✅️ Вс кросс 2я 14-16 км
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = """
                12км за тренировку
                1км разминочный легко + 11км ~4.40 + заминка 200м.
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 9),
                DayName = "Суббота",
                DayNum = 5,
                Description = """
                2км разминка, гибкость
                6×1км 3.45 и быстрее
                Отдых 3минуты шаг/трусца
                Заминка 1км
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 10),
                DayName = "Воскресенье",
                DayNum = 6,
                Description = """
                кросс 2я 14-16 км
                """
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_WithMonday_ParsesCorrectly()
    {
        // Arrange - message with Monday
        var text = """
            ✅️ Пн силовая

            ✅️ Вт 2км разминка

            ✅️ Чт 8-10 км лёгкий бег
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 4),
                DayName = "Понедельник",
                DayNum = 0,
                Description = "силовая"
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = "2км разминка"
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = "8-10 км лёгкий бег"
            },
        };

        AssertWorkoutsEquals(expected, result);
    }

    [Fact]
    public void Parse_WithMultipleDays_ParsesAll()
    {
        // Arrange
        var text = """
            Задание 

            Вт 2км разминка
            5×1500 ~3.40 темп

            Чт 8км ~5.00+
            Взять гель

            Взять изотоник

            Сб 2км разминка



            потом отчитайся как всё прошло плиз
            """;

        var referenceDate = new DateTime(2026, 5, 4); // Monday

        // Act
        var result = WorkoutParser.Parse(text, referenceDate);

        var expected = new List<WorkoutParser.ParsedWorkout> {
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 5),
                DayName = "Вторник",
                DayNum = 1,
                Description = """
                2км разминка
                5×1500 ~3.40 темп
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 7),
                DayName = "Четверг",
                DayNum = 3,
                Description = """
                8км ~5.00+
                Взять гель
                Взять изотоник
                """
            },
            new WorkoutParser.ParsedWorkout
            {
                Date = new DateTime(2026, 5, 9),
                DayName = "Суббота",
                DayNum = 5,
                Description = """
                2км разминка
                потом отчитайся как всё прошло плиз
                """
            },
        };

        AssertWorkoutsEquals(expected, result);
    }
}