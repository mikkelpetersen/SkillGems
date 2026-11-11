using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using InputHumanizer.Input;
using ExileInput = ExileCore.Input;
using RectangleF = ExileCore.Shared.Drawing.RectangleF;

namespace GemLevelling.Input;

/// <summary>
/// Clicks a gem's level up button through InputHumanizer, which is the only way this plugin sends input. The click is
/// instant, and the delay between two clicks is InputHumanizer's own.
/// </summary>
internal sealed class GemInput(GameController gameController)
{
    private const string RequesterName = "Gem Levelling";
    private const string TryGetInputControllerMethod = "InputHumanizer.TryGetInputController";
    private const float ClickAreaInset = 0.25f;
    private const float CursorMovedTolerance = 4f;
    private const int ImmediateMoveDistance = 1;
    private const int ImmediateMoveDelay = 0;

    public bool IsInputHumanizerLoaded => TryGetControllerMethod() is not null;

    /// <summary>
    /// Takes InputHumanizer's input lock, clicks a random point inside <paramref name="clientRect"/> at once, puts the
    /// cursor back when the click moved it, and gives the lock back. Returns the humanized delay to wait before the
    /// next click, or <see langword="null"/> when InputHumanizer is not loaded, is busy with another plugin or could
    /// not click.
    /// </summary>
    public async SyncTask<TimeSpan?> ClickAsync(RectangleF clientRect, CancellationToken cancellationToken)
    {
        if (TryGetControllerMethod()?.Invoke(RequesterName) is not { } controller)
        {
            return null;
        }

        using (controller)
        {
            var cursorBefore = ExileInput.MousePositionNum;

            if (!await controller.InstantClick(MouseButtons.Left, ToScreen(RandomPointWithin(clientRect)), cancellationToken))
            {
                return null;
            }

            if (Vector2.Distance(ExileInput.MousePositionNum, cursorBefore) > CursorMovedTolerance)
            {
                await controller.MoveMouse(ToScreen(cursorBefore), ImmediateMoveDistance, ImmediateMoveDelay, ImmediateMoveDelay,
                    cancellationToken);
            }

            return TimeSpan.FromMilliseconds(controller.GenerateDelay());
        }
    }

    private static Vector2 RandomPointWithin(RectangleF clientRect)
    {
        var horizontal = ClickAreaInset + Random.Shared.NextSingle() * (1f - 2f * ClickAreaInset);
        var vertical = ClickAreaInset + Random.Shared.NextSingle() * (1f - 2f * ClickAreaInset);

        return new Vector2(clientRect.X + clientRect.Width * horizontal, clientRect.Y + clientRect.Height * vertical);
    }

    private Vector2 ToScreen(Vector2 clientPoint)
    {
        var window = gameController.Window.GetWindowRectangleTimeCache;
        return clientPoint + new Vector2(window.X, window.Y);
    }

    private Func<string, IInputController?>? TryGetControllerMethod() =>
        gameController.PluginBridge.GetMethod<Func<string, IInputController?>>(TryGetInputControllerMethod);
}
