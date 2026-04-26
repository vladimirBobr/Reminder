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
                if (dateSection.Events.Count == 0)
                {
                    // Секция пуста - вставляем сразу после заголовка
                    var insertIndex = dateSection.ContentStartLineIndex + 1;
                    lines.Insert(insertIndex, "");
                    insertIndex++;
                    lines.Insert(insertIndex, note);
                    lines.Insert(insertIndex + 1, "");
                }
                else
                {
                    // Есть существующие события - вставляем перед первым
                    var firstEventStart = dateSection.Events[0].StartLineIndex;
                    
                    // Вставляем перед первым событием
                    lines.Insert(firstEventStart, note);
                    
                    // Вставляем пустую строку после новой заметки (перед старой)
                    lines.Insert(firstEventStart + 1, "");
                }
                
                resultMessage = "Добавили в существующую секцию";
            }
            else
            {
                // Ищем different_dates_section
                var diffDates = parseResult.DifferentDates;
                
                if (diffDates != null)
                {
                    var events = diffDates.Events;
                    var insertPos = events.FindIndex(e => e.Event.Date > date.Value);
                    
                    int insertIndex;
                    if (insertPos >= 0)
                    {
                        // Вставляем перед событием с большей датой
                        insertIndex = events[insertPos].StartLineIndex;
                        
                        // Вставляем новую запись
                        lines.Insert(insertIndex, $"{date.Value:dd.MM.yyyy} {note}");
                        insertIndex++;
                        
                        // Вставляем blank line ПОСЛЕ новой записи (между new и old)
                        lines.Insert(insertIndex, "");
                        
                        resultMessage = "Добавили в #different_dates_section#";
                        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage);
                    }
                    else if (events.Count > 0)
                    {
                        // Все события имеют меньшую дату - вставляем ПОСЛЕ последнего (с blank line)
                        var lastEvent = events[^1];
                        insertIndex = lastEvent.EndLineIndex + 1;
                        
                        // Вставляем blank line перед новой записью (между существующим и новым)
                        lines.Insert(insertIndex, "");
                        insertIndex++;
                        lines.Insert(insertIndex, $"{date.Value:dd.MM.yyyy} {note}");
                        insertIndex++;
                        
                        // Вставляем blank line после новой записи
                        lines.Insert(insertIndex, "");
                        
                        resultMessage = "Добавили в #different_dates_section#";
                        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage);
                    }
                    else
                    {
                        // Секция пуста - вставляем после заголовка (используем ContentEndLineIndex)
                        // Для пустой секции НЕ добавляем blank line - сразу запись после заголовка
                        insertIndex = diffDates.ContentEndLineIndex + 1;
                        lines.Insert(insertIndex, $"{date.Value:dd.MM.yyyy} {note}");
                        
                        resultMessage = "Добавили в #different_dates_section#";
                        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage);
                    }
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
                    // Секция пуста - вставляем после заголовка (используем ContentEndLineIndex)
                    var insertIdx = notesSection.ContentEndLineIndex + 1;
                    lines.Insert(insertIdx, "");
                    insertIdx++;
                    lines.Insert(insertIdx, note);
                    insertIdx++;
                    lines.Insert(insertIdx, "");
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