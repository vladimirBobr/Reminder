using System.Text;
using OneOf;
using ReminderApp.EventParsing;

namespace ReminderApp.EventStorage;

public record NoteModifierError(string Message);
public record NoteModifierSuccess(string ModifiedContent, string ResultMessage);

public static class NoteModifier
{
    public static OneOf<NoteModifierError, NoteModifierSuccess> ModifyContent(
        string content,
        string note,
        DateOnly? date = null)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note cannot be empty", nameof(note));

        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var parser = new FileParser();
        var parseResult = parser.ParseFile(normalizedContent);
        
        var lines = normalizedContent.Split('\n').ToList();
        string? resultMessage = null;

        if (date.HasValue)
        {
            // Попытка найти секцию с конкретной датой
            var dateSection = parseResult.DateSections.FirstOrDefault(s => s.Date == date.Value);
            
            if (dateSection != null)
            {
                // Вставляем в начало контента секции (перед существующими событиями)
                var insertIndex = dateSection.ContentStartLineIndex + 1;
                
                // Вставляем пустую строку перед заметкой
                if (insertIndex <= dateSection.ContentEndLineIndex 
                    && !string.IsNullOrWhiteSpace(lines[insertIndex]))
                {
                    lines.Insert(insertIndex, "");
                    insertIndex++;
                }
                else if (insertIndex <= dateSection.ContentEndLineIndex)
                {
                    // Уже есть пустая строка, пропускаем её
                    while (insertIndex <= dateSection.ContentEndLineIndex 
                           && string.IsNullOrWhiteSpace(lines[insertIndex]))
                    {
                        insertIndex++;
                    }
                }
                
                lines.Insert(insertIndex, note);
                
                // Вставляем пустую строку после заметки
                if (insertIndex + 1 < lines.Count 
                    && !string.IsNullOrWhiteSpace(lines[insertIndex + 1]))
                {
                    lines.Insert(insertIndex + 1, "");
                }
                
                resultMessage = "Добавили в существующую секцию";
            }
            else
            {
                // Ищем different_dates_section
                var diffDates = parseResult.DifferentDates;
                
                if (diffDates != null)
                {
                    // Находим позицию для вставки по дате
                    var events = diffDates.Events;
                    var insertPos = events.FindIndex(e => e.Event.Date > date.Value);
                    
                    int insertIndex;
                    if (insertPos >= 0)
                    {
                        // Вставляем перед событием с большей датой
                        insertIndex = events[insertPos].StartLineIndex;
                    }
                    else if (events.Count > 0)
                    {
                        // Все события имеют меньшую дату - вставляем после последнего
                        insertIndex = events[^1].EndLineIndex + 1;
                    }
                    else
                    {
                        // Секция пуста - вставляем после заголовка
                        insertIndex = diffDates.ContentStartLineIndex + 1;
                    }
                    
                    // Проверяем, нужна ли пустая строка перед новой записью
                    var needEmptyBefore = insertIndex > diffDates.ContentStartLineIndex + 1
                        && insertIndex > 0
                        && !string.IsNullOrWhiteSpace(lines[insertIndex - 1]);

                    // Проверяем, нужна ли пустая строка после новой записи
                    var needEmptyAfter = insertIndex < lines.Count
                        && !string.IsNullOrWhiteSpace(lines[insertIndex]);

                    if (needEmptyBefore)
                    {
                        lines.Insert(insertIndex, "");
                        insertIndex++;
                    }

                    lines.Insert(insertIndex, $"{date.Value:dd.MM.yyyy} {note}");

                    if (needEmptyAfter)
                    {
                        lines.Insert(insertIndex + 1, "");
                    }
                    
                    resultMessage = "Добавили в #different_dates_section#";
                }
                else
                {
                    // Секция different_dates_section отсутствует — создаём в конце файла
                    lines.Add("");
                    lines.Add("#different_dates_section#");
                    lines.Add("");
                    lines.Add($"{date.Value:dd.MM.yyyy} {note}");
                    lines.Add("");
                    resultMessage = "Добавили в #different_dates_section#";
                }
            }
        }
        else
        {
            // Без даты – добавляем в notes_section
            var notesSection = parseResult.NotesSection;
            
            if (notesSection != null)
            {
                // Вставляем в начало контента секции (перед существующими заметками)
                var insertIndex = notesSection.ContentStartLineIndex + 1;
                
                // Если контент пустой (секция пуста), вставляем сразу после заголовка
                if (notesSection.Events.Count == 0)
                {
                    lines.Insert(insertIndex, "");
                    insertIndex++;
                    lines.Insert(insertIndex, note);
                    lines.Insert(insertIndex + 1, "");
                }
                else
                {
                    // Есть существующий контент - вставляем перед ним
                    var firstEventStart = notesSection.Events[0].StartLineIndex;
                    
                    // Вставляем перед первой заметкой
                    lines.Insert(firstEventStart, note);
                    
                    // Вставляем пустую строку после новой заметки (перед старой)
                    lines.Insert(firstEventStart + 1, "");
                }
                
                resultMessage = "Добавили в #notes_section#";
            }
            else
            {
                // Секция notes_section отсутствует - создаём в конце файла
                lines.Add("");
                lines.Add("#notes_section#");
                lines.Add("");
                lines.Add(note);
                lines.Add("");
                resultMessage = "Добавили в #notes_section#";
            }
        }

        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage!);
    }
}