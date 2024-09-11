using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;

namespace TimeSpeed.Framework.Integrations.UiInfoSuite2;
internal class TimePausedIcon : IconBase {
#region Properties
    protected override bool ShouldShow => ModEntry.IsTimeFrozen;
#endregion

#region Lifecycle
    public TimePausedIcon(IModHelper helper) : base(helper) {}
#endregion

#region Logic
    protected override void UpdateStatusData()
    {
        this.HoverText = this.ShouldShow ? I18n.Integrations_UiInfoSuite2_TimePausedMessage_Paused() : null;
    }

    protected override (Texture2D?, Rectangle) GenerateIcon()
    {
        if (this.SpriteSheet is null)
        {
            ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | {nameof(this.SpriteSheet)} is null", LogLevel.Alert);
        }

        ModEntry.IMonitor.LogOnce($"{this.GetType().Name} | {nameof(this.GenerateIcon)} | end", LogLevel.Alert);

        return (this.SpriteSheet, this.TimePausedIconSpriteLocation);
    }
#endregion
}
