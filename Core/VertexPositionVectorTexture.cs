using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IdolOfMadderCrimson.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionVectorTexture : IVertexType
{
    readonly VertexDeclaration IVertexType.VertexDeclaration
    {
        get
        {
            return new VertexDeclaration(
                [
                    new VertexElement(
                            0,
                            VertexElementFormat.Vector3,
                            VertexElementUsage.Position,
                            0
                        ),
                        new VertexElement(
                            sizeof(float) * 3,
                            VertexElementFormat.Vector4,
                            VertexElementUsage.Color,
                            0
                        ),
                        new VertexElement(
                            sizeof(float) * 7,
                            VertexElementFormat.Vector2,
                            VertexElementUsage.TextureCoordinate,
                            0
                        )
                ]);
        }
    }

    /// <summary>
    ///     The position of this vertex.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    ///     The vectorial color of this vertex.
    /// </summary>
    public Vector4 Color;

    /// <summary>
    ///     The texture coordinates of this vertex.
    /// </summary>
    public Vector2 TextureCoordinate;

    public VertexPositionVectorTexture(Vector3 position, Vector4 color, Vector2 textureCoordinate)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
    }
}
