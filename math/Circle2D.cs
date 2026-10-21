using System;
using Godot;

/// <summary>
/// A circle with a defined position and radius.
/// </summary>
public class Circle2D
{
    /// <summary>
    /// The position of the center of this circle.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The radius of this circle.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// The calculated diameter of this circle.
    /// </summary>
    public float Diameter
    {
        get => Radius * 2;
    }

    /// <summary>
    /// The calculated area of this circle.
    /// </summary>
    public float Area
    {
        get => (float)Math.PI * Radius * Radius;
    }

    /// <summary>
    /// The calculated circumference of this circle.
    /// </summary>
    public float Circumference
    {
        get => 2f * (float)Math.PI * Radius;
    }

    /// <summary>
    /// Constructs a Circle2D with the given parameters.
    /// </summary>
    /// <param name="position">The position of the center of the new circle.</param>
    /// <param name="radius">The radius of the new circle.</param>
    public Circle2D(Vector2 position, float radius)
    {
        this.Position = position;
        this.Radius = radius;
    }
}
