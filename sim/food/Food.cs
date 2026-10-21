using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

/// <summary>
/// A randomized Food object, which may be of any FoodType. Fish will pursue these whilst under 50% hunger.
/// </summary>
public partial class Food : Node2D, IEdible
{
    private float radius;
    public FoodType FoodType { get; set; }
    public float CalorieValue
    {
        get => radius * radius;
    }
    private float rotationSpeed;
    private List<Circle2D> lumps; //used for Herbivore and Carnivore type food ONLY

    //Omnivore colors
    private static Color lightBrown = Color.FromString("#63371f", @default: new Color(0, 0, 0));
    private static Color mediumBrown = Color.FromString("#5c3018", @default: new Color(0, 0, 0));
    private static Color darkBrown = Color.FromString("#4a2510", @default: new Color(0, 0, 0));

    //Carnivore colors
    private static Color pink = Color.FromString("#e8355f", @default: new Color(0, 0, 0));
    private static Color red = Color.FromString("#d1214a", @default: new Color(0, 0, 0));

    //Pre-calculated geometry
    Vector2[] points;

    /// <summary>
    /// Constructs a new Food with random radius and type.
    /// </summary>
    public Food()
    {
        Random rng = new();
        this.FoodType = (FoodType)(int)rng.NextInt64(0, 3);
        Init();
    }

    /// <summary>
    /// Constructs a new Food with random radius and specified FoodType.
    /// </summary>
    /// <param name="foodType">The FoodType of this Food.</param>
    public Food(FoodType foodType)
    {
        this.FoodType = foodType;
        Init();
    }

    /// <summary>
    /// Initializes important Food properties and fields.
    /// </summary>
    private void Init()
    {
        Random rng = new();
        radius = (float)((rng.NextDouble() * 10) + 10);
        rotationSpeed = (float)((rng.NextDouble() * 0.1) - 0.05);
        switch (FoodType)
        {
            case FoodType.HERBIVORE:
            {
                lumps = [];
                int amountOfLumps = (int)rng.NextInt64(7, 12);
                for (int i = 0; i < amountOfLumps; i++)
                {
                    lumps.Add(
                        new Circle2D(
                            new Vector2(
                                (float)(rng.NextDouble() * 2 * radius - radius),
                                (float)(rng.NextDouble() * 2 * radius - radius)
                            ),
                            (radius / 3f) * ((float)(rng.NextDouble() * 0.5) + 0.75f)
                        )
                    );
                }
                break;
            }
            case FoodType.OMNIVORE:
            {
                List<Vector2> pointsList = [];
                pointsList.Add(new Vector2(-radius, radius / 3)); //top left
                pointsList.Add(new Vector2(radius, radius / 3)); //top right
                pointsList.Add(new Vector2(radius, -radius / 3)); //bottom right
                pointsList.Add(new Vector2(-radius, -radius / 3)); //bottom left
                points = pointsList.ToArray();
                break;
            }
            case FoodType.CARNIVORE:
            {
                lumps = [];
                int amountOfLumps = (int)rng.NextInt64(2, 5);
                //generate lumps
                for (int i = 0; i < amountOfLumps; i++)
                {
                    lumps.Add(
                        new Circle2D(
                            new Vector2(
                                (float)(rng.NextDouble() * radius - (radius * 0.5)),
                                (float)(rng.NextDouble() * radius - (radius * 0.5))
                            ),
                            (radius / 5f) * ((float)(rng.NextDouble() * 0.5) + 0.75f)
                        )
                    );
                }
                //pre-compute geometry
                List<Vector2> pointsList = [];
                for (int i = 0; i < 36; i++)
                {
                    pointsList.Add(
                        new Vector2(radius, 0).Rotated(FMath.DegreesToRadians(i * 10))
                            * (1 + ((float)Math.Sin(FMath.DegreesToRadians(i * 120)) / 12))
                    );
                }
                points = pointsList.ToArray();
                break;
            }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
    }

    public float Eat()
    {
        QueueFree();
        return CalorieValue;
    }

    public override void _Draw()
    {
        switch (FoodType)
        {
            case FoodType.HERBIVORE:
            {
                //algae clump
                for (int i = 0; i < lumps.Count; i++)
                {
                    DrawCircle(
                        lumps[i].Position,
                        lumps[i].Radius,
                        new Color(0.25f + 0.05f * i, 0.8f, 0.25f + 0.05f * i)
                    );
                }
                break;
            }
            case FoodType.OMNIVORE:
            {
                //pellet

                DrawCircle(new Vector2(-radius, 0), radius / 3, darkBrown);
                DrawColoredPolygon(points, mediumBrown);
                DrawCircle(new Vector2(radius, 0), radius / 3, lightBrown);
                break;
            }
            case FoodType.CARNIVORE:
            {
                //meat lump
                DrawColoredPolygon(points, red);
                for (int i = 0; i < lumps.Count; i++)
                {
                    DrawCircle(lumps[i].Position, lumps[i].Radius, pink);
                }
                break;
            }
        }
        Rotation += rotationSpeed;
    }
}
