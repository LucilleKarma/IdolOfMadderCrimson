namespace IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;

/// <summary>
/// Represents data for a rooftop generation set for a bridge.
/// </summary>
/// <param name="Width">The width of the rooftop.</param>
/// <param name="Height">The height of the rooftop.</param>
/// <param name="VerticalOffset">The vertical placement offset of this rooftop relative to the base roof level.</param>
public record struct ShrineRooftopInfo(int Width, int Height, int VerticalOffset);
