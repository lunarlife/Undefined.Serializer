namespace Undefined.Serializer.Configuration;

public readonly struct SwitcherSettings
{
    public int Switcher { get; }
    public bool ExcludeNoSwitchers { get; }

    public SwitcherSettings(bool excludeNoSwitchers, int switcher)
    {
        ExcludeNoSwitchers = excludeNoSwitchers;
        Switcher = switcher;
    }
}