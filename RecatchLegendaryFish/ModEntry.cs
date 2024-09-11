using System.Linq;

using StardewValley;
using StardewValley.GameData.Locations;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using cantorsdust.Common;

using RecatchLegendaryFish.Framework;

namespace RecatchLegendaryFish;
// ReSharper disable once UnusedType.Global
/// <summary>The entry class called by SMAPI.</summary>
internal class ModEntry : Mod
{
#region Properties
    /// <summary>The mod configuration.</summary>
    private ModConfig Config = null!;

    /// <summary>Whether the mod is currently enabled.</summary>
    private bool IsEnabled = true;
#endregion

#region Public methods
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        CommonHelper.RemoveObsoleteFiles(this, "RecatchLegendaryFish.pdb");

        this.Config = helper.ReadConfig<ModConfig>();

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
    }
#endregion

#region Private methods
#region Event handlers
    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (this.IsEnabled && e.Name.IsEquivalentTo("Data/Locations"))
        {
            e.Edit(
                asset =>
                {
                    // Known limitation: there's no good way to handle ItemId being an item query instead
                    // of an item ID, but all vanilla legendary fish (and likely most modded ones) use an
                    // item ID.
                    foreach (SpawnFishData fish in asset.AsDictionary<string, LocationData>().Data.Values
                                                        .Where(location => location.Fish is not null)
                                                        .SelectMany(location => location.Fish.Where(fish => fish.CatchLimit == 1 && ItemContextTagManager.HasBaseTag(fish.ItemId, "fish_legendary"))))
                    {
                        fish.CatchLimit = -1;
                    }
                },
                AssetEditPriority.Late // handle new legendary fish added by mods
            );
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GenericModConfigMenuIntegration.Register(
            this.ModManifest,
            this.Helper.ModRegistry,
            this.Monitor,
            getConfig: () => this.Config,
            reset: () => this.Config = new(),
            save: () => this.Helper.WriteConfig(this.Config));
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Context.IsPlayerFree && this.Config.ToggleKey.JustPressed())
            this.OnToggle();
    }

    /// <summary>Handle the toggle key.</summary>
    private void OnToggle()
    {
        this.IsEnabled = !this.IsEnabled;
        this.Helper.GameContent.InvalidateCache("Data/Locations");

        string? key = this.Config.ToggleKey.GetKeybindCurrentlyDown()?.ToString();

        string message = this.IsEnabled
                             ? I18n.Message_Enabled(key: key)
                             : I18n.Message_Disabled(key: key);

        Game1.addHUDMessage(
            new(message, HUDMessage.newQuest_type) {
                timeLeft = 2500,
            });
    }
#endregion
#endregion
}
