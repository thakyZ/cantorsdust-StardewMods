using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;

using StardewModdingAPI;
using System.Linq;

namespace TimeSpeed.Framework.Integrations.UiInfoSuite2;

[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
internal class TimeSpeedIcon : IconBase
{
#region Properties
    /// <summary>Wrapper to get the <see cref="ModEntry.TickIntervalSeconds" /> more easily.</summary>
    private int TimeSpeed => ModEntry.TickIntervalSeconds;

    /// <inheritdoc cref="IconBase.ShouldShow" />
    protected override bool ShouldShow => this.TimeSpeed != ModEntry.BaseGameTickIntervalSeconds;

    /// <summary>Cached set of time speeds.</summary>
    private Dictionary<int, Texture2D> CacheTimeSpeedIcons => [];

    /// <summary>Default icon box for use with <see cref="CacheTimeSpeedIcons"/></summary>
    private static readonly Rectangle DefaultIconBox = new Rectangle(0, 0, 15, 15);

    /// <summary>Empty icon box for use of no icon.</summary>
    private static readonly Rectangle EmptyIconBox = new Rectangle(0, 0, 0, 0);
#endregion

#region Lifecycle
    public TimeSpeedIcon(IModHelper helper) : base(helper) {}

    /// <inheritdoc cref="IconBase.Dispose" />
    protected override void Dispose(bool disposing)
    {
        this.ToggleOption(false);

        foreach (Texture2D texture in this.CacheTimeSpeedIcons.Values)
        {
            texture.Dispose();
        }

        this.CacheTimeSpeedIcons.Clear();
    }
#endregion

#region Logic
    protected override void UpdateStatusData()
    {
        this.HoverText = this.ShouldShow ? I18n.Integrations_UiInfoSuite2_TimeSpeedMessage_Changed(seconds: this.TimeSpeed) : null;
    }

    [SuppressMessage("Major Code Smell", "S2589:Boolean expressions should not be gratuitous")]
    protected override (Texture2D?, Rectangle) GenerateIcon()
    {
        int timeSpeed = this.TimeSpeed;

        if (this.CacheTimeSpeedIcons.ContainsKey(timeSpeed))
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | {nameof(timeSpeed)}: {timeSpeed} | {nameof(this.CacheTimeSpeedIcons)}.ContainsKey({nameof(timeSpeed)}) = true", LogLevel.Alert);

            return (this.CacheTimeSpeedIcons[timeSpeed], TimeSpeedIcon.DefaultIconBox);
        }

        Texture2D?      texture          = null;
        Texture?        oldTarget        = null;
        RenderTarget2D? target           = null;
        SpriteBatch?    spriteBatch      = null;
        bool            spriteBatchBegun = false;
        bool            spriteBatchDrew  = false;

        try
        {
            oldTarget   = Game1.graphics.GraphicsDevice.GetRenderTargets().FirstOrDefault().RenderTarget;
            target      = new(Game1.graphics.GraphicsDevice, 15, 15);
            spriteBatch = new(Game1.graphics.GraphicsDevice, 1);
            Game1.graphics.GraphicsDevice.SetRenderTarget(target);
            spriteBatch.Begin();
            spriteBatchBegun = true;
            Vector2 size = Game1.tinyFont.MeasureString(timeSpeed.ToString());

            if (size is { X: <= 15, Y: <= 15, })
            {
                spriteBatch.DrawString(Game1.tinyFont, timeSpeed.ToString(), Vector2.Zero, Color.DarkViolet);
                spriteBatchDrew = true;
            }
            else
            {
                ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | Size of spite is larger than 15x15 got {size.X}x{size.Y}");
            }
        }
        catch (Exception exception)
        {
            ModEntry.LogExceptionOnce(exception, $"{this.GetType().Name} | {nameof(this.GenerateIcon)} | Failed to create texture of speed of time.", LogLevel.Alert);
        }
        finally
        {
            if (spriteBatchBegun)
                spriteBatch?.End(); // NOSONAR

            if (spriteBatchDrew)
            {
                // ReSharper disable once RedundantCast
                texture = target as Texture2D;
            }

            if (oldTarget is not null)
                Game1.graphics.GraphicsDevice.SetRenderTarget((RenderTarget2D)oldTarget);
        }

        if (texture is not null)
        {
            ModEntry.IMonitor.LogOnce($"{nameof(TimeSpeedIcon)} | {nameof(this.GenerateIcon)} | {nameof(timeSpeed)}: {timeSpeed} | {nameof(texture)} is not null", LogLevel.Alert);

            return (texture, TimeSpeedIcon.DefaultIconBox);
        }

        Rectangle timeIncreasing = ModEntry.IsIncrease is null ? TimeSpeedIcon.EmptyIconBox :
                                     ModEntry.IsIncrease.Value ? this.TimeSpeedForwardIconSpriteLocation : this.TimeSpeedBackwardIconSpriteLocation;

        if (this.SpriteSheet is null)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | {nameof(timeSpeed)}: {timeSpeed} | {nameof(this.SpriteSheet)} is null", LogLevel.Alert);
        }

        ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | {nameof(timeSpeed)}: {timeSpeed} | end", LogLevel.Alert);

        return (this.SpriteSheet, timeIncreasing);
    }
#endregion
}
