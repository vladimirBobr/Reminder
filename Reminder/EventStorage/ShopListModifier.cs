using OneOf;
using ReminderApp.EventParsing;

namespace ReminderApp.EventStorage;

public static class ShopListModifier
{
    public static OneOf<NoteModifierError, NoteModifierSuccess> ModifyContent(
        string content,
        string newItem)
    {
        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var parser = new FileParser();
        var parseResult = parser.ParseFile(normalizedContent);
        
        var lines = normalizedContent.Split('\n').ToList();

        var shoppingSection = parseResult.ShoppingSection;
        string resultMessage;

        if (shoppingSection != null)
        {
            // Insert into existing shopping section
            var insertIndex = shoppingSection.ContentStartLineIndex + 1;
            
            // If section has items, insert before first one
            if (shoppingSection.Items.Count > 0)
            {
                insertIndex = shoppingSection.Items[0].StartLineIndex;
            }
            
            lines.Insert(insertIndex, $"- {newItem}");
            resultMessage = "Item added to existing shopping section";
        }
        else
        {
            // No shopping section - create at end of file
            lines.Add("");
            lines.Add("#Shopping#");
            lines.Add($"- {newItem}");
            resultMessage = "Item added to new shopping section";
        }

        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage);
    }
}