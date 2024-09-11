namespace InstantGrowTrees.Framework;
/// <summary>The mod configuration model.</summary>
internal class ModConfig
{
#region Accessors
    /// <summary>The configuration for fruit trees.</summary>
    public FruitTreeConfig FruitTrees { get; set; } = new();

    /// <summary>The configuration for non-fruit trees.</summary>
    public RegularTreeConfig NonFruitTrees { get; set; } = new();
#endregion
}
