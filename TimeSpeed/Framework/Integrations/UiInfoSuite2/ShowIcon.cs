using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Menus;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using cantorsdust.Common.Integrations;

using UIInfoSuite2;

namespace TimeSpeed.Framework.Integrations.UiInfoSuite2;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Disabled for template for API.")]
[SuppressMessage("ReSharper",   "VirtualMemberNeverOverridden.Global",    Justification = "Disabled for template for API.")]
[SuppressMessage("ReSharper",   "UnusedMemberInSuper.Global",             Justification = "Disabled for template for API.")]
[SuppressMessage("ReSharper",   "UnusedMember.Global",                    Justification = "Disabled for template for API.")]
internal abstract class IconBase : IDisposable
{
#region Properties
    /// <summary>Nullable hover text for the status icon.</summary>
    protected string? HoverText { get; set; }

    /// <summary>Getter property to determine if the icon should be shown.</summary>
    protected abstract bool ShouldShow { get; }

    /// <summary>Time Speed Increased icon location in the <see cref="SpriteSheet"/>.</summary>
    protected Rectangle TimeSpeedForwardIconSpriteLocation => new(15, 15, 15, 15);

    /// <summary>Time Speed Decreased icon location in the <see cref="SpriteSheet"/>.</summary>
    protected Rectangle TimeSpeedBackwardIconSpriteLocation => new(0, 15, 15, 15);

    /// <summary>Time Paused icon location in the <see cref="SpriteSheet"/>.</summary>
    protected Rectangle TimePausedIconSpriteLocation => new(0, 0, 15, 15);

    /// <summary>Time Paused from Location icon location in the <see cref="SpriteSheet"/>.</summary>
    protected Rectangle TimePausedLocationIconSpriteLocation => new(15, 0, 15, 15);

    /// <summary>Time Paused from Event icon location in the <see cref="SpriteSheet"/>.</summary>
    protected Rectangle TimePausedEventIconSpriteLocation => new(30, 0, 15, 15);

    /// <summary>Cached instance of the current mod's helper class.</summary>
    protected IModHelper Helper { get; }

    /// <summary>The icon sheet that contains the icon we want to display.</summary>
    protected Texture2D? SpriteSheet { get; set; }

    /// <summary>The icon sheet that contains the icon we want to display.</summary>
    protected IUIInfoSuite2Api? UIInfoSuite2Api { get; }

    /// <summary>Determines if the instance of this class is already disposed.</summary>
    protected bool isDisposed;

    /// <summary>Instanced per screen icon of the Icon.</summary>
    protected PerScreen<ClickableTextureComponent> Icon => new();
#endregion

#region Life cycle
    /// <summary>Constructor for the <see cref="IconBase"/> class.</summary>
    /// <param name="helper">An instance of the current mod's helper class.</param>
    protected IconBase(IModHelper helper)
    {
        this.Helper                              =  helper;
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.UIInfoSuite2Api                     =  IntegrationHelper.GetUIInfoSuite2(helper.ModRegistry, ModEntry.IMonitor);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose() {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IconBase() {
        this.Dispose(false);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing) {
        if (this.isDisposed) return;

        this.ToggleOption(false);
        this.Helper.Events.GameLoop.GameLaunched -= this.OnGameLaunched;
        this.isDisposed                          =  true;
    }

    /// <summary>Toggles on or off the ability to show the icon.</summary>
    /// <param name="showStatus">Whether or not to show the status icon.</param>
    public virtual void ToggleOption(bool showStatus)
    {
#if DEBUG
        ModEntry.IMonitor.Log($"{this.GetType().Name} | {nameof(this.ToggleOption)} | {nameof(showStatus)}: {showStatus} | Toggled", LogLevel.Alert);
#endif

        this.Helper.Events.Display.RenderingHud           -= this.OnRenderingHudImpl;
        this.Helper.Events.Display.RenderedHud            -= this.OnRenderedHudImpl;
        this.Helper.Events.GameLoop.DayStarted            -= this.OnDayStartedImpl;
        this.Helper.Events.GameLoop.UpdateTicked          -= this.OnUpdateTickedImpl;
        this.Helper.Events.GameLoop.SaveLoaded            -= this.OnSaveLoadedImpl;
        this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnOneSecondUpdateTickedImpl;

        // ReSharper disable once InvertIf
        if (showStatus)
        {
            this.UpdateStatusDataImpl();

            this.Helper.Events.GameLoop.DayStarted            += this.OnDayStartedImpl;
            this.Helper.Events.Display.RenderingHud           += this.OnRenderingHudImpl;
            this.Helper.Events.Display.RenderedHud            += this.OnRenderedHudImpl;
            this.Helper.Events.GameLoop.UpdateTicked          += this.OnUpdateTickedImpl;
            this.Helper.Events.GameLoop.SaveLoaded            += this.OnSaveLoadedImpl;
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTickedImpl;
        }
    }
#endregion

#region Event subscriptions
    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.FindSpriteSheet();
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {}

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSaveLoadedImpl(object? sender, SaveLoadedEventArgs e) {
        this.OnSaveLoaded(sender, e);
    }

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {}

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnUpdateTickedImpl(object? sender, UpdateTickedEventArgs e) {
        this.OnUpdateTicked(sender, e);
    }

    /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e) {}

    /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnOneSecondUpdateTickedImpl(object? sender, OneSecondUpdateTickedEventArgs e) {
        this.OnOneSecondUpdateTicked(sender, e);
        this.UpdateStatusDataImpl();
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnDayStarted(object? sender, DayStartedEventArgs e) {}

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnDayStartedImpl(object? sender, DayStartedEventArgs e)
    {
        this.OnDayStarted(sender, e);
        this.UpdateStatusDataImpl();
    }

    /// <inheritdoc cref="IDisplayEvents.RenderingHud"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnRenderingHud(object? sender, RenderingHudEventArgs e) {}

    /// <inheritdoc cref="IDisplayEvents.RenderingHud"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnRenderingHudImpl(object? sender, RenderingHudEventArgs e)
    {
        // Draw icon
#if DEBUG
        if (this.UIInfoSuite2Api is null)
        {
            ModEntry.IMonitor.Log($"{this.GetType().Name} | {nameof(this.OnRenderingHud)} | {nameof(this.UIInfoSuite2Api)} = null", LogLevel.Alert);

            return;
        }

        if (!this.UIInfoSuite2Api.IsRenderingNormally())
        {
            ModEntry.IMonitor.Log($"{this.GetType().Name} | {nameof(this.OnRenderingHud)} | {nameof(this.UIInfoSuite2Api.IsRenderingNormally)}() = false", LogLevel.Alert);

            return;
        }

        (Texture2D? Texture, Rectangle IconRectangle) = this.GenerateIcon();

        if (Texture is null)
        {
            ModEntry.IMonitor.Log($"{this.GetType().Name} | {nameof(this.OnRenderingHud)} | (Texture2D? {nameof(Texture)}, {IconRectangle.GetType().Name} {nameof(IconRectangle)}) = (null, {IconRectangle})", LogLevel.Alert);

            return;
        }

        Point iconPosition = this.UIInfoSuite2Api.GetNewIconPosition();

        this.Icon.Value = new ClickableTextureComponent(
            new(iconPosition.X, iconPosition.Y, 40, 40),
            Texture,
            IconRectangle,
            1.3f);

        if (this.Icon.Value is null)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderingHud)} | {nameof(this.Icon)}.Value = null", LogLevel.Alert);

            return;
        }

        if (!this.ShouldShow)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderingHud)} | {nameof(this.ShouldShow)} = false", LogLevel.Alert);

            return;
        }

        ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | {nameof(this.Icon)}.Value.draw", LogLevel.Alert);
#else
        if (this.UIInfoSuite2Api is null || !this.UIInfoSuite2Api.IsRenderingNormally())
            return;

        (Texture2D? Texture, Rectangle IconRectangle) = this.GenerateIcon();

        if (Texture is null)
            return;

        Point iconPosition = this.UIInfoSuite2Api.GetNewIconPosition();

        this.Icon.Value = new(
            new(iconPosition.X, iconPosition.Y, 40, 40),
            Texture,
            IconRectangle,
            1.3f);

        if (this.Icon.Value is null || !this.ShouldShow)
            return;
#endif

        this.Icon.Value?.draw(Game1.spriteBatch);
    }

    /// <inheritdoc cref="IDisplayEvents.RenderedHud"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnRenderedHud(object? sender, RenderedHudEventArgs e) {}

    /// <inheritdoc cref="IDisplayEvents.RenderedHud"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnRenderedHudImpl(object? sender, RenderedHudEventArgs e)
    {
        this.OnRenderedHud(sender, e);

        // Show text on hover
#if DEBUG
        if (!this.ShouldShow)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | {nameof(this.ShouldShow)} = false", LogLevel.Alert);

            return;
        }

        if (this.Icon.Value is null)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | {nameof(this.Icon)}.Value = null", LogLevel.Alert);

            return;
        }

        if (!this.Icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | {nameof(this.Icon)}.Value.containsPoint = false", LogLevel.Alert);

            return;
        }

        if (string.IsNullOrEmpty(this.HoverText))
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | string.IsNullOrEmpty({nameof(this.HoverText)}) = true", LogLevel.Alert);

            return;
        }

        ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.OnRenderedHud)} | {nameof(IClickableMenu)}.{nameof(IClickableMenu.drawHoverText)}", LogLevel.Alert);
#else
        if (!this.ShouldShow || this.Icon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) != true || string.IsNullOrEmpty(this.HoverText))
            return;
#endif
        IClickableMenu.drawHoverText(Game1.spriteBatch, this.HoverText, Game1.dialogueFont);
    }
#endregion

#region Logic
    /// <summary>Update status data. Please call the base </summary>
    protected abstract void UpdateStatusData();

    /// <summary>Update status data. Please call the base </summary>
    private void UpdateStatusDataImpl() {
        this.FindSpriteSheet();
        this.UpdateStatusData();
    }

    private void FindSpriteSheet()
    {
        this.SpriteSheet ??= Texture2D.FromFile(Game1.graphics.GraphicsDevice, Path.Combine(this.Helper.DirectoryPath, "assets", "LooseSprites", "timeSpeed.png"));

        if (this.SpriteSheet is null)
        {
            ModEntry.IMonitor.Log($"{this.GetType().Name}: Could not find Robin sprite sheet.", LogLevel.Warn);
        }
    }

    protected abstract (Texture2D? Texture, Rectangle IconRectangle) GenerateIcon();
#endregion
}
