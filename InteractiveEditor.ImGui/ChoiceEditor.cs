using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

// A combo box for choosing (P7.9). The list comes from GetChoices() when the combo opens (P6.3), so it can
// change with the objects, and is kept while it stays open; the choice is written at once (P2.12).
// Objects that hold different values show a dash and nothing chosen (P7.19); a choice goes to all of them.
internal sealed class ChoiceEditor(ImGuiRow row) : ImGuiEditor(row)
{
    private IReadOnlyList<object?>? Choices;

    public override void Draw()
    {
        var mixed = Row.Mixed;
        var value = Node.ViewValue;
        var shown = mixed ? MixedText : Format(value);

        if (!Gui.BeginCombo("##value", shown))
        {
            Choices = null;
            return;
        }

        var picked = -1;

        // GetChoices reports a function that throws through BindFailed, whose subscribers may throw too.
        try
        {
            if (Choices == null || Gui.IsWindowAppearing())
                Choices = Node.GetChoices();

            for (var i = 0; i < Choices.Count; i++)
            {
                var text = Format(Choices[i]);
                var selected = !mixed && IsShown(Choices[i], text, value, shown);

                // The index keeps the ID apart for two choices that read the same.
                if (Gui.Selectable($"{text}##{i}", selected))
                    picked = i;

                if (selected)
                    Gui.SetItemDefaultFocus();
            }
        }
        finally
        {
            Gui.EndCombo();
        }

        if (picked < 0)
            return;

        var choice = Choices[picked];
        Row.Write(() => Node.SetValue(choice));
    }

    // The value itself, or one that shows the same (the text "2" for the number 2). An Equals of the
    // objects that throws only leaves the choice unmarked, inside the combo's Begin and End.
    private bool IsShown(object? choice, string text, object? value, string shown)
    {
        try
        {
            return Equals(choice, value) || value != null && text == shown;
        }
        catch (Exception e)
        {
            Row.OnFault(e);
            return false;
        }
    }
}
