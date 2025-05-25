using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;

namespace IdolOfMadderCrimson.Content.Tiles.Generic;

public abstract class WorldOrientedTileObject
{
    /// <summary>
    ///     The position of the object, in world coordinates.
    /// </summary>
    public Point Position;

    public WorldOrientedTileObject() { }

    public WorldOrientedTileObject(Point position) => Position = position;

    /// <summary>
    ///     Updates this object.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    ///     Renders this object.
    /// </summary>
    public abstract void Render();

    /// <summary>
    ///     Serializes this object data as a tag compound for world saving.
    /// </summary>
    public abstract TagCompound Serialize();

    /// <summary>
    ///     Deserializes a tag compound containing data for a object back into said object.
    /// </summary>
    public abstract WorldOrientedTileObject Deserialize(TagCompound tag);
}
