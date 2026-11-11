using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace GemLevelling.Settings;

public sealed class GemLevellingSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);

    public ToggleNode AutomaticLevelling { get; set; } = new(false);

    public HotkeyNodeV2 LevelKey { get; set; } = Keys.A;
}
