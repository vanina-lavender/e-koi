using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// <para>Provides useful helper math functions not included in the C# Math library. All values are returned in floats or ints.</para>
/// <para><b>Note:</b> FMath stands for Fish Math.</para>
/// </summary>
public static class FMath
{
    /// <summary>
    /// Converts an angle in radians to degrees.
    /// </summary>
    /// <param name="rads">The angle in radians to be converted.</param>
    /// <returns>The converted angle in degrees..</returns>
    public static float RadiansToDegrees(float rads)
    {
        return rads * (float)(180 / Math.PI);
    }

    /// <summary>
    /// Converts an angle in degrees to radians.
    /// </summary>
    /// <param name="degs">The angle in degrees to be converted.</param>
    /// <returns>The converted angle in radians.</returns>
    public static float DegreesToRadians(float degs)
    {
        return degs * (float)(Math.PI / 180);
    }

    /// <summary>
    /// Converts an angle in radians in the range [-PI,PI] to the equivalent angle in the range [0,2PI].
    /// </summary>
    /// <param name="rads">The angle in radians to be justified.</param>
    /// <returns>The justified angle in radians.</returns>
    public static float JustifyAngleRadians(float rads)
    {
        if (rads >= 0)
        {
            return rads;
        }
        else
        {
            return rads + (float)(2 * Math.PI);
        }
    }

    /// <summary>
    /// Converts an angle in degrees in the range [-180,180] to the equivalent angle in the range [0,360].
    /// </summary>
    /// <param name="degs">The angle in degrees to be justified.</param>
    /// <returns>The justified angle in degrees.</returns>
    public static float JustifyAngleDegrees(float degs)
    {
        if (degs >= 0)
        {
            return degs;
        }
        else
        {
            return degs + 360;
        }
    }

    /// <summary>
    /// Generates a fast ellipse by generating a circle with x values multiplied by stretch and y values divided by stretch.
    /// </summary>
    /// <param name="position">The center position of this ellipse.</param>
    /// <param name="radius">Radius of the circle this ellipse is based on.</param>
    /// <param name="stretch">Stretch factor the ellipse is generated using.</param>
    /// <param name="rotation">The rotation of this ellipse, in radians.</param>
    /// <returns>The new ellipse, as a Vector2[].</returns>
    public static Vector2[] FastEllipse(
        Vector2 position,
        float radius,
        float stretch,
        float rotation = 0
    )
    {
        List<Vector2> points = [];
        Vector2 distance = new(radius, 0);
        float deltaAngle = FMath.DegreesToRadians(10);
        for (int i = 0; i < 36; i++)
        {
            points.Add(distance.Rotated(deltaAngle * i));
            points[i] =
                position
                + new Vector2(points[i].X * stretch, points[i].Y / stretch).Rotated(rotation);
        }
        return points.ToArray();
    }

    /// <summary>
    /// Generates a fast arc by generating a portion of a circle, then modifying it.
    /// </summary>
    /// <param name="position">The position of the center of the arc.</param>
    /// <param name="startAngleDegrees">The angle of the first point on the arc.</param>
    /// <param name="endAngleDegrees">The angle of the last point on the arc.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="numberOfPoints">The number of points in the arc path.</param>
    /// <param name="stretchX">Stretch factor of the X components of the arc.</param>
    /// <param name="stretchY">Stretch factor of the Y components of the arc.</param>
    /// <param name="rotation">The rotation of the arc, in radians.</param>
    /// <returns>The new arc, as a Vector2[].</returns>
    public static Vector2[] FastArc(
        Vector2 position,
        float startAngleDegrees,
        float endAngleDegrees,
        float radius,
        int numberOfPoints,
        float stretchX = 1,
        float stretchY = 1,
        float rotation = 0
    )
    {
        List<Vector2> points = [];
        Vector2 distance = new(radius, 0);
        float deltaAngleRadians = FMath.DegreesToRadians(
            (endAngleDegrees - startAngleDegrees) / (numberOfPoints - 1)
        );

        if (deltaAngleRadians >= 0)
        {
            float startAngleRadians = FMath.DegreesToRadians(startAngleDegrees);
            for (int i = 0; i < numberOfPoints; i++)
            {
                points.Add(distance.Rotated(startAngleRadians + deltaAngleRadians * i));
                points[i] =
                    position
                    + new Vector2(points[i].X * stretchX, points[i].Y * stretchY).Rotated(rotation);
            }
        }
        else
        {
            float endAngleRadians = FMath.DegreesToRadians(endAngleDegrees);
            for (int i = 0; i < numberOfPoints; i++)
            {
                points.Add(distance.Rotated(endAngleRadians - deltaAngleRadians * i));
                points[i] =
                    position
                    + new Vector2(points[i].X * stretchX, points[i].Y * stretchY).Rotated(rotation);
            }
        }

        return points.ToArray();
    }
}
