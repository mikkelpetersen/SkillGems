using System;
using System.Threading;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using GemLevelling.Input;
using GemLevelling.Settings;
using GemLevelling.UI;

namespace GemLevelling;

/// <summary>
/// Clicks the level up button of every gem that can level up, through InputHumanizer, while the level key is held or
/// automatic levelling is on.
/// </summary>
public sealed class GemLevelling : BaseSettingsPlugin<GemLevellingSettings>
{
    private const int IdleScanIntervalTicks = 10;
    private const int ConfirmTicks = 2;

    private static readonly TimeSpan LevelUpTimeout = TimeSpan.FromSeconds(2);

    private GemInput? _input;
    private SettingsPanel? _settingsPanel;
    private SyncTask<bool>? _levelling;
    private bool _isLevelling;
    private bool _hasLevelableGem;
    private int _ticksUntilScan;

    public GemLevelling()
    {
        Name = "Gem Levelling";
    }

    public override bool Initialise()
    {
        _input = new GemInput(GameController);
        _settingsPanel = new SettingsPanel(Settings, _input, Graphics);
        return true;
    }

    public override Job Tick()
    {
        if (_isLevelling || ShouldLevel())
        {
            TaskUtils.RunOrRestart(ref _levelling, LevelNextGemAsync);
        }

        return null!;
    }

    public override void DrawSettings()
    {
        _settingsPanel?.Draw();
    }

    public override void AreaChange(AreaInstance area)
    {
        ForgetLevelableGem();
    }

    private bool ShouldLevel()
    {
        if (!Settings.AutomaticLevelling.Value && !Settings.LevelKey.IsPressed())
        {
            ForgetLevelableGem();
            return false;
        }

        if (--_ticksUntilScan > 0)
        {
            return _hasLevelableGem;
        }

        _ticksUntilScan = IdleScanIntervalTicks;
        _hasLevelableGem = CanClickInGame() && FindLevelableGem() is not null;
        return _hasLevelableGem;
    }

    private void ForgetLevelableGem()
    {
        _hasLevelableGem = false;
        _ticksUntilScan = 0;
    }

    private bool CanClickInGame() =>
        GameController.InGame &&
        !GameController.IsLoading &&
        GameController.Window.IsForeground() &&
        GameController.Player is { IsValid: true, IsAlive: true } &&
        !IsBlockingPanelOpen();

    private bool IsBlockingPanelOpen()
    {
        var ingameUi = GameController.IngameState.IngameUi;

        return ingameUi.OpenRightPanel.IsVisible ||
               ingameUi.InventoryPanel.IsVisible ||
               ingameUi.Atlas.IsVisible ||
               ingameUi.TreePanel.IsVisible ||
               ingameUi.SyndicatePanel.IsVisible ||
               ingameUi.ChatTitlePanel.IsVisible ||
               ingameUi.DelveWindow.IsVisible;
    }

    private GemLevelUpElement? FindLevelableGem()
    {
        if (GameController.IngameState.IngameUi.GemLvlUpPanel is not { IsVisible: true } panel || panel.GemsToLvlUp is not { } gems)
        {
            return null;
        }

        foreach (var gem in gems)
        {
            if (panel.MeetRequirementForGem(gem) && gem.LevelUpButton is { IsVisible: true })
            {
                return gem;
            }
        }

        return null;
    }

    /// <summary>
    /// Clicks the first gem that can level up and waits until the game shows the result: the gem's level rose or its
    /// entry left the panel, for <see cref="ConfirmTicks"/> ticks in a row. The timeout is only a safety bound. It then
    /// waits InputHumanizer's humanized delay, so that delay lies between two clicks.
    /// </summary>
    private async SyncTask<bool> LevelNextGemAsync()
    {
        _isLevelling = true;

        try
        {
            var cancellationToken = ZoneCancellationToken;

            if (_input is not { } input || !CanClickInGame() || FindLevelableGem() is not { } gem ||
                gem.GemEntity is not { } gemEntity || gemEntity.GetComponent<SkillGem>() is not { } skillGem)
            {
                return false;
            }

            var gemAddress = gemEntity.Address;
            var levelBefore = skillGem.Level;

            if (await input.ClickAsync(gem.LevelUpButton.GetClientRectCache, cancellationToken) is not { } delayBeforeNextClick)
            {
                return false;
            }

            var confirmedTicks = 0;

            bool IsLevelled()
            {
                confirmedTicks = HasGemLevelled(gemAddress, levelBefore) ? confirmedTicks + 1 : 0;
                return confirmedTicks >= ConfirmTicks;
            }

            var isLevelled = await TaskUtils.CheckEveryFrame(() => cancellationToken.IsCancellationRequested || IsLevelled(), LevelUpTimeout);
            await Task.Delay(delayBeforeNextClick, cancellationToken);

            return isLevelled;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _isLevelling = false;
            ForgetLevelableGem();
        }
    }

    private bool HasGemLevelled(long gemAddress, int levelBefore)
    {
        if (GameController.IngameState.IngameUi.GemLvlUpPanel?.GemsToLvlUp is not { } gems)
        {
            return true;
        }

        foreach (var gem in gems)
        {
            if (gem.GemEntity is { } gemEntity && gemEntity.Address == gemAddress)
            {
                return gemEntity.GetComponent<SkillGem>() is not { } skillGem || skillGem.Level > levelBefore;
            }
        }

        return true;
    }
}
