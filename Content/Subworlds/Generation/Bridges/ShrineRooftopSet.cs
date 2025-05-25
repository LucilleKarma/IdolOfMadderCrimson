using System.Collections.Generic;

namespace IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;

/// <summary>
/// Represents a set of rooftops atop each other.
/// </summary>
public record ShrineRooftopSet(List<ShrineRooftopInfo> Rooftops)
{
    public ShrineRooftopSet() : this([]) { }

    /// <summary>
    /// Adds a new rooftop to this set.
    /// </summary>
    public ShrineRooftopSet Add(ShrineRooftopInfo rooftop)
    {
        Rooftops.Add(rooftop);
        return this;
    }
}
