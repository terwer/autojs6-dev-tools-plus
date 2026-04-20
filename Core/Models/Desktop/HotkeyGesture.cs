namespace Core.Models.Desktop;

public sealed record HotkeyGesture(
    string Key,
    bool Ctrl = false,
    bool Shift = false,
    bool Alt = false,
    bool Meta = false)
{
    public override string ToString()
    {
        var modifiers = new List<string>(4);

        if (Ctrl)
        {
            modifiers.Add("Ctrl");
        }

        if (Shift)
        {
            modifiers.Add("Shift");
        }

        if (Alt)
        {
            modifiers.Add("Alt");
        }

        if (Meta)
        {
            modifiers.Add("Meta");
        }

        modifiers.Add(Key);
        return string.Join('+', modifiers);
    }
}
