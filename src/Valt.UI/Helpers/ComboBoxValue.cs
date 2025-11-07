namespace Valt.UI.Helpers;

public class ComboBoxValue(string text, string value)
{
    public string Text { get; set; } = text;
    public string Value { get; set; } = value;
    
    public override string ToString() => Value;
}