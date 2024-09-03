using System;
using System.Linq;
using System.Text;
using cantorsdust.Common;
using cantorsdust.Common.Extensions;

using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TimeSpeed.Framework;
using TimeSpeed.Framework.Managers;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

namespace TimeSpeed
{
    internal partial class ModEntry
    {
		internal static void HandleInputBridge(FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None, GameLocation? location = null)
		{
			if (ModEntry._instance is null)
                return;
		    ModEntry._instance.HandleInput(freezeTimeMethod, tickIntervalState, location, false);
		}
    }
}