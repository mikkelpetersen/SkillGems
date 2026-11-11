using System;
using ExileCore;
using ExileUI.Kit.Settings;
using GemLevelling.Input;
using GemLevelling.Settings;

namespace GemLevelling.UI;

/// <summary>The Gem Levelling settings page, built with ExileUI.Kit in the tabs layout.</summary>
internal sealed class SettingsPanel
{
    private readonly GemLevellingSettings _settings;
    private readonly GemInput _input;
    private readonly Graphics _graphics;
    private readonly SettingsPage _page = new(SettingsLayout.Tabs);
    private readonly SettingsSection[] _sections;
    private readonly Action<SettingsPage> _drawStatusBar;

    public SettingsPanel(GemLevellingSettings settings, GemInput input, Graphics graphics)
    {
        _settings = settings;
        _input = input;
        _graphics = graphics;
        _drawStatusBar = DrawStatusBar;
        _sections =
        [
            new SettingsSection("Levelling", "When gems that can level up are clicked.", DrawLevelling),
        ];
    }

    public void Draw() => _page.Draw(_settings, _sections, statusBar: _drawStatusBar);

    private void DrawStatusBar(SettingsPage page)
    {
        var isInputLoaded = _input.IsInputHumanizerLoaded;
        page.Indicator(_graphics, "InputHumanizer", isInputLoaded,
            isInputLoaded ? "InputHumanizer is loaded." : "InputHumanizer is not loaded, so Gem Levelling cannot click.");
    }

    private void DrawLevelling(SettingsPage page)
    {
        page.Toggle("Automatic Levelling", _settings.AutomaticLevelling,
            "Levels gems as soon as they can level up, without holding the key. Background input in InputHumanizer is recommended.");
        page.Hotkey("Level Key", _settings.LevelKey, "Levels gems while the key is held.");
    }
}
