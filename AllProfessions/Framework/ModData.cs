namespace AllProfessions.Framework;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>The mod configuration model.</summary>
internal class ModData
{
    /// <summary>The professions to gain for each level.</summary>
    public ModDataProfessions[]? ProfessionsToGain { get; set; }
}
