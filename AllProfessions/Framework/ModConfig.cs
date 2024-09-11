using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using StardewModdingAPI;

namespace AllProfessions.Framework;
/// <summary>The mod configuration.</summary>
internal class ModConfig
{
#region Accessors
    /// <summary>The profession names or IDs which shouldn't be added.</summary>
    public HashSet<string>? IgnoreProfessions { get; set; } = [];
#endregion

#region Public methods
    /// <summary>Whether a given profession shouldn't be added automatically.</summary>
    /// <param name="profession">The profession to check.</param>
    public bool ShouldIgnore(Profession profession)
    {
        return this.IgnoreProfessions?.Contains(profession.ToString()) == true
               || this.IgnoreProfessions?.Contains(((int)profession).ToString()) == true;
    }

    /// <summary>Normalize the configured profession values.</summary>
    /// <returns>Returns whether any changes were made.</returns>
    public bool Normalize(IMonitor monitor)
    {
        bool changed = false;

        foreach (string raw in this.IgnoreProfessions?.ToArray() ?? [])
        {
            if (!Enum.TryParse(raw, ignoreCase: true, out Profession profession))
            {
                monitor.Log($"Ignored unknown profession name '{raw}' in the mod configuration.");
                this.IgnoreProfessions?.Remove(raw);
                changed = true;

                continue;
            }

            if (profession.ToString() == raw)
                continue;

            this.IgnoreProfessions?.Remove(raw);
            this.IgnoreProfessions?.Add(profession.ToString());
            changed = true;
        }

        return changed;
    }
#endregion

#region Private methods
    /// <summary>The method called after the config file is deserialized.</summary>
    /// <param name="context">The deserialization context.</param>
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
    [SuppressMessage("ReSharper",   "UnusedMember.Local")]
    [SuppressMessage("ReSharper",   "UnusedParameter.Local")]
    [OnDeserialized]
    private void OnDeserializedMethod(StreamingContext context)
    {
        this.IgnoreProfessions = new(this.IgnoreProfessions ?? [], StringComparer.OrdinalIgnoreCase);
    }
#endregion
}
