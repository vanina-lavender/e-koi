using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// An independent simulated fish. Controls all basic fish AI, and stores all fish behavior and design parameters.
/// </summary>
public partial class Fish : Node2D, IEdible
{
    public Simulation sim;
    public List<Circle2D> Spine { get; set; }

    public Color color { get; set; }

    public float CalorieValue
    {
        get
        {
            float averageWidth = 0;
            foreach (Circle2D segment in Spine)
            {
                averageWidth += segment.Radius;
            }
            averageWidth /= Spine.Count;
            return 0.1f * averageWidth * spineSegmentLength * Spine.Count;
        }
    }
    public new Vector2 Position
    {
        get => Spine[0].Position;
        set => Spine[0].Position = value;
    }
    public float spineSegmentLength;
    public float randSize = 1;

    //goal finding
    public Vector2 goal = Vector2.Zero;
    private Egg eggGoal = null;
    private int timeSinceLastGoal = 0;

    public FoodType hungerType;
    public FoodType FoodType { get; set; } = FoodType.CARNIVORE;

    //hunger
    private float hunger = 50f; //exists within the range 0-100
    private float metabolism = 0.1f; //hunger is decreased by metabolism every second
    public float Speed { get; set; }

    //eyes & details
    public int pairsOfEyes = 1;
    private Color eyeColor = new Color(0, 0, 0);
    public bool blind = false;
    public int dorsalFins = 1; //supports [1,2] dorsal fins
    private bool turnDirection = true; //true = right, false = left

    //lifetime
    public long lifetime = 0; //lifetime in seconds
    private int maxGrowthTime = 300; //time fish takes to grow to max size in seconds
    private float spineLengthGrowth;
    private List<float> spineRadiiGrowths = new();
    private float growthProgressMultiplier = 0.5f;
    private bool dead = false;

    //other Data
    public string name;
    public string species;
    private static string[] nameOption =
    [
        "Jerry",
        "Petunia",
        "Bartholomew",
        "Robert",
        "[rainbow]Jason",
        "Stella",
        "Rose",
    ];
    private static string[] speciesOptions = ["Testu Magoi", "Magoi", "Doitsu Koi", "Ki Utsuri"];

    //generations
    public List<Fish> Parents { get; set; } = new();
    public float geneStrength;
    public float geneVolitility = 1;

    //static constants
    private static Color WHITE = new Color(1, 1, 1);
    private static Color LIGHTENER = new Color(.1f, .1f, .1f);
    private static Color GRAY = new Color(.6f, .6f, .6f);
    private static float PI = (float)Math.PI;

    //starvation
    private int starvationTime = 0;

    /// <summary>
    /// Constructs a new fish with the given parameters.
    /// </summary>
    /// <param name="sim">A reference to the Simulation this Fish will be added to.</param>
    public Fish(Simulation sim, Color color)
    {
        this.sim = sim;
        hungerType = (FoodType)sim.RNG.RandiRange(0, 2);
        randSize = sim.RNG.RandfRange(0.75f, 1.5f);

        spineSegmentLength = (randSize * 0.6f + 0.3f) * 20;
        Speed = 5;

        this.color = color;
        Spine = GenerateSpine(sim.RNG.RandiRange(10, 12), randSize);
        Position = new Vector2(sim.RNG.RandfRange(0, 1000), sim.RNG.RandfRange(0, 1000));

        geneStrength = sim.RNG.RandfRange(0, 1);
        //eyes
        int randomEye = sim.RNG.RandiRange(0, 103);
        int randomFin = sim.RNG.RandiRange(0, 100);
        if (randomEye == 0)
        {
            pairsOfEyes = 0;
            blind = true;
            Speed *= 0.5f;
        }
        else if (randomEye > 0 && randomEye < 3)
        {
            blind = true;
            eyeColor = GRAY;
            Speed *= 0.5f;
        }
        else if (randomEye > 100)
        {
            pairsOfEyes = randomEye - 100;
        }

        //calculate metabolism
        metabolism = 0.5f * (Speed / 10f) * randSize;

        if (randomFin < 3)
        {
            dorsalFins = 0;
            Speed *= 0.75f;
            metabolism *= 1.25f;
        }
        else if (randomFin > 97)
        {
            dorsalFins = 2;
            Speed *= 1.1f;
        }

        //run OnSecond check every second
        sim.Second += () => OnSecond();

        //tinyify the fish
        spineLengthGrowth = (spineSegmentLength / 2) / maxGrowthTime;
        spineSegmentLength /= 2;
        foreach (Circle2D segment in Spine)
        {
            spineRadiiGrowths.Add((segment.Radius / 2) / maxGrowthTime);
            segment.Radius /= 2;
        }
        name = nameOption[sim.RNG.RandiRange(0, nameOption.Count() - 1)];
        species = speciesOptions[sim.RNG.RandiRange(0, speciesOptions.Count() - 1)];
    }

    //create a fish from two others. geneStrength is used to determine a dominant fish, then traits are randomly inherited
    public Fish(Simulation sim, Fish parent1, Fish parent2, Vector2 position)
    {
        this.sim = sim;

        //higher volitility is always taken
        geneVolitility =
            parent1.geneVolitility > parent2.geneVolitility
                ? parent1.geneVolitility
                : parent2.geneVolitility;
        //inbreeding penalty
        foreach (Fish grandparent in parent1.Parents)
        {
            if (parent2.Parents.Contains<Fish>(grandparent))
            {
                geneVolitility += 2;
            }
        }

        Fish strongParent = parent1.geneStrength > parent2.geneStrength ? parent1 : parent2;
        Fish weakParent = parent1.geneStrength > parent2.geneStrength ? parent2 : parent1;

        hungerType = Chance(25) ? weakParent.hungerType : strongParent.hungerType;
        randSize = Chance(25)
            ? strongParent.randSize
            : (
                Chance(25) ? weakParent.randSize : (strongParent.randSize + weakParent.randSize) / 2
            );

        spineSegmentLength = (randSize * 0.6f + 0.3f) * 20;
        Speed = 5;

        color = Chance(25)
            ? strongParent.color
            : new Color(
                (strongParent.color.R * 2 + weakParent.color.R) / 3,
                (strongParent.color.G * 2 + weakParent.color.G) / 3,
                (strongParent.color.B * 2 + weakParent.color.B) / 3
            );

        Spine = GenerateSpine(
            Chance(25) ? weakParent.Spine.Count : strongParent.Spine.Count,
            randSize
        );
        Position = position;

        geneStrength = sim.RNG.RandfRange(0, 1);
        //eyes
        if (strongParent.pairsOfEyes == 0 || weakParent.pairsOfEyes == 0 && Chance(75))
        {
            pairsOfEyes = 0;
            blind = true;
        }
        else if (strongParent.pairsOfEyes > 1 && Chance(75))
        {
            pairsOfEyes = strongParent.pairsOfEyes;
        }
        else if (weakParent.pairsOfEyes > 1 && Chance(50))
        {
            pairsOfEyes = weakParent.pairsOfEyes;
        }
        else
        {
            pairsOfEyes = 1;
        }
        if (strongParent.blind || weakParent.blind && Chance(90))
        {
            blind = true;
        }
        //new eye mutations
        if (Chance(geneVolitility))
        {
            pairsOfEyes = 0;
            blind = true;
        }
        else if (Chance(2 * geneVolitility))
        {
            blind = true;
        }
        if (Chance(geneVolitility * 2))
        {
            pairsOfEyes = 1 + sim.RNG.RandiRange(1, 2);
        }
        if (blind)
        {
            eyeColor = GRAY;
            Speed *= 0.5f;
        }
        //calculate metabolism
        metabolism =
            0.5f
            * (Speed / 10f)
            * randSize
            * sim.RNG.RandfRange(1, (float)Math.Sqrt(geneVolitility)); // volitility penalty

        int randomFin = sim.RNG.RandiRange(0, 100);
        if (Chance(geneVolitility))
        {
            dorsalFins = 0;
            Speed *= 0.75f;
            metabolism *= 1.25f;
        }
        else if (Chance(geneVolitility))
        {
            dorsalFins = 2;
            Speed *= 1.1f;
        }

        //volitility penalties
        metabolism *= (float)Math.Pow(geneVolitility, 1 / 3f);
        if (Chance(50))
        {
            Speed /= (float)Math.Pow(geneVolitility, 1 / 3f);
        }
        lifetime = sim.RNG.RandiRange(0, (int)Math.Pow(geneVolitility, 2));

        //run OnSecond check every second
        sim.Second += () => OnSecond();

        //tinyify the fish
        spineLengthGrowth = (spineSegmentLength / 2) / maxGrowthTime;
        spineSegmentLength /= 2;
        foreach (Circle2D segment in Spine)
        {
            spineRadiiGrowths.Add((segment.Radius / 2) / maxGrowthTime);
            segment.Radius /= 2;
        }
        name = nameOption[sim.RNG.RandiRange(0, nameOption.Count() - 1)];
        species = Chance(25)
            ? weakParent.species
            : (
                Chance(25)
                    ? strongParent.species.Split(" ")[0]
                        + " "
                        + weakParent.species.Split(" ")[
                            sim.RNG.RandiRange(0, weakParent.species.Split(" ").Count() - 1)
                        ]
                    : strongParent.species
            );
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        goal.X = sim.RNG.RandfRange(0, 1920);
        goal.Y = sim.RNG.RandfRange(0, 1080);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        if (dead)
        {
            return;
        }
        //angle constraints
        Vector2 direction = (goal - Position).Normalized();
        Vector2 currentlyFacing = (Spine[0].Position - Spine[1].Position).Normalized();
        float deltaAngle = Mathf.Clamp(currentlyFacing.AngleTo(direction), -0.15f, 0.15f);
        direction = currentlyFacing.Rotated(deltaAngle);

        //hunger speed multiplier
        float hunger_speed = (float)(Math.Sin(0.05 * hunger - 25.5) / 4) + 1f;
        Position += direction * Speed * hunger_speed;

        //Constrain spine segments
        for (int i = 1; i < Spine.Count; i++)
        {
            Spine[i].Position =
                Spine[i - 1].Position
                + (Spine[i].Position - Spine[i - 1].Position).Normalized() * spineSegmentLength;
        }
        if (Math.Abs((goal - Position).Length()) < 30f)
        {
            if (eggGoal is not null)
            {
                foreach (Egg egg in Egg.eggs)
                {
                    if (Position.DistanceTo(egg.Position) < 30f)
                    {
                        eggGoal.Fertilize(this);
                    }
                }
            }
            else
            {
                sim.FindNearestEdible(
                    Position,
                    hungerType,
                    out Vector2 foodPosition,
                    out IEdible _
                );
                if (foodPosition == goal)
                {
                    for (int i = 0; i < sim.edibleList.Count; i++)
                    {
                        if (sim.edibleList[i].Position == foodPosition)
                        {
                            hunger = Mathf.Clamp(hunger + sim.edibleList[i].Eat(), 0, 100);
                            sim.edibleList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            FindNewGoal();
        }
        if (
            Input.IsActionJustPressed("Click")
            && (
                GetGlobalMousePosition().DistanceTo(Position) <= Spine[0].Radius * 2
                || GetGlobalMousePosition().DistanceTo(Spine[^1].Position) <= Spine[^1].Radius * 2
                || GetGlobalMousePosition().DistanceTo(Spine[Spine.Count() / 2].Position)
                    <= Spine[Spine.Count / 2].Radius * 2
            )
        )
        {
            sim.UpdateInspector(this);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        //fish body shape
        List<Vector2> pectoralFinPoints = new();
        List<float> pectoralFinAngles = new();
        List<Vector2> sides = [];
        for (int i = 0; i < Spine.Count; i++)
        {
            if (i == 0)
            {
                Vector2 distance =
                    (Spine[i].Position - Spine[i + 1].Position).Normalized() * Spine[i].Radius;
                for (int j = 0; j < 10; j++) //add 9 points in a semicircle
                {
                    sides.Add(
                        Spine[i].Position + distance.Rotated(FMath.DegreesToRadians(90 - j * 20))
                    );
                }
            }
            else if (i == Spine.Count - 1)
            {
                Vector2 distance =
                    (Spine[i - 1].Position - Spine[i].Position).Normalized() * Spine[i].Radius;
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(PI / 2));
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(3 * PI / 4));
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(PI));
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(-3 * PI / 4));
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(-PI / 2));
            }
            else
            {
                Vector2 distance =
                    (Spine[i].Position - Spine[i + 1].Position).Normalized() * Spine[i].Radius;
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(PI / 2));
                sides.Insert(i + 9, Spine[i].Position + distance.Rotated(-PI / 2));
                if (i == Spine.Count / 3 || i == Spine.Count * 2 / 3)
                {
                    pectoralFinPoints.Add(Spine[i].Position + distance.Rotated(PI / 2));
                    pectoralFinPoints.Add(Spine[i].Position + distance.Rotated(-PI / 2));
                    pectoralFinAngles.Add(
                        (pectoralFinPoints[pectoralFinPoints.Count - 2] - Spine[i].Position).Angle()
                    );
                    pectoralFinAngles.Add(
                        (pectoralFinPoints[pectoralFinPoints.Count - 1] - Spine[i].Position).Angle()
                    );
                }
            }
        }
        //draw pectoral fins
        if (dead)
        {
            Vector2[] spinePosition = new Vector2[Spine.Count];

            for (int i = 0; i < Spine.Count; i++)
            {
                spinePosition[i] = Spine[i].Position;
            }
            DrawPolyline(spinePosition, WHITE, 3);
            for (int i = 2; i < spinePosition.Length; i++)
            {
                DrawLine(spinePosition[i], sides[i + 9], WHITE, 3);
                DrawLine(sides[sides.Count - i], spinePosition[i], WHITE, 3);
            }
            // GD.Print(spinePosition[0].AngleTo(spinePosition[1]));

            DrawColoredPolygon(
                FMath.FastArc(
                    spinePosition[0],
                    -20,
                    200,
                    Spine[0].Radius / 6,
                    40,
                    8,
                    8,
                    (spinePosition[0] - spinePosition[1]).Angle() - FMath.DegreesToRadians(90)
                ),
                WHITE
            );
            DrawEyes();
            return;
        }
        for (int i = 0; i < pectoralFinPoints.Count; i++)
        {
            DrawColoredPolygon(
                FMath.FastEllipse(
                    pectoralFinPoints[i],
                    (i < 2) ? 15 : 8 * randSize * growthProgressMultiplier,
                    0.75f,
                    pectoralFinAngles[i] + ((i % 2 == 0) ? -1 : 1) * FMath.DegreesToRadians(20)
                ),
                color + LIGHTENER
            );
        }
        DrawColoredPolygon(sides.ToArray(), color); //shape
        //DrawPolyline(sides.ToArray(), color * LIGHTENER, 10); //outline
        //draw eyes
        DrawEyes();

        //draw dorsal fin(s)
        float totalRotationFactor = 0;
        for (int i = 1; i < Spine.Count - 1; i++)
        {
            totalRotationFactor += (Spine[i].Position - Spine[i - 1].Position)
                .Normalized()
                .AngleTo((Spine[i + 1].Position - Spine[i].Position).Normalized());
        }
        totalRotationFactor = (float)Math.Sqrt(Math.Abs(totalRotationFactor * 1000)) / 100;

        for (int i = 0; i < dorsalFins; i++)
        {
            int dorsalFinIndex = Spine.Count * (i + 1) / 3;
            Vector2 dorsalFinPosition = Spine[dorsalFinIndex].Position;
            float dorsalFinRotation = (
                Spine[dorsalFinIndex - 1].Position - Spine[dorsalFinIndex + 1].Position
            ).Angle();

            //determine if left or right rotating
            if (
                totalRotationFactor > 0.2f
                && turnDirection
                && FMath.JustifyAngleRadians((Spine[2].Position - Spine[1].Position).Angle())
                    < FMath.JustifyAngleRadians((Spine[1].Position - Spine[0].Position).Angle())
            )
            {
                turnDirection = !turnDirection;
            }
            else if (
                totalRotationFactor > 0.2f
                && !turnDirection
                && FMath.JustifyAngleRadians((Spine[2].Position - Spine[1].Position).Angle())
                    > FMath.JustifyAngleRadians((Spine[1].Position - Spine[0].Position).Angle())
            )
            {
                turnDirection = !turnDirection;
            }

            if (!turnDirection)
            {
                dorsalFinRotation += FMath.DegreesToRadians(180);
            }

            List<Vector2> points = new();
            points.AddRange(
                FMath.FastArc(
                    dorsalFinPosition,
                    0,
                    180,
                    spineSegmentLength * 2,
                    18,
                    1,
                    0.05f + (totalRotationFactor / 3),
                    dorsalFinRotation
                )
            );
            points.AddRange(
                FMath.FastArc(
                    dorsalFinPosition,
                    170,
                    10,
                    spineSegmentLength * 2,
                    16,
                    1,
                    0.1f + totalRotationFactor,
                    dorsalFinRotation
                )
            );
            DrawColoredPolygon(points.ToArray(), color + LIGHTENER);
        }

        if (Globals.debugView)
        {
            DrawCircle(
                Position + new Vector2(0, -40),
                totalRotationFactor * 10,
                new Color(1, 0, 0)
            );
            DrawCircle(goal, 20f, color);
            DrawCircle(
                Position,
                9f,
                hungerType switch
                {
                    FoodType.CARNIVORE => new Color(1, 0, 0),
                    FoodType.OMNIVORE => WHITE,
                    FoodType.HERBIVORE => new Color(0, 1, 0),
                    _ => throw new NotImplementedException(),
                }
            );
            foreach (Vector2 point in sides)
            {
                DrawCircle(point, 3, WHITE);
            }
            foreach (Circle2D point in Spine)
            {
                DrawCircle(point.Position, 3, WHITE);
            }
        }
    }

    private void DrawEyes()
    {
        for (int i = 0; i < pairsOfEyes; i++)
        {
            Vector2 distance =
                (Spine[i].Position - Spine[i + 1].Position).Normalized() * Spine[i].Radius * 0.75f;
            DrawCircle(
                Spine[i].Position + distance.Rotated(PI / 2),
                7 * randSize * growthProgressMultiplier,
                eyeColor
            );
            DrawCircle(
                Spine[i].Position + distance.Rotated(-PI / 2),
                7 * randSize * growthProgressMultiplier,
                eyeColor
            );
        }
    }

    /// <summary>
    /// Determines the Fish's new goal, and changes its goal vector.
    /// </summary>
    public void FindNewGoal()
    {
        eggGoal = null;
        timeSinceLastGoal = 0;
        if (
            hunger <= 50
            && sim.FindNearestEdible(Position, hungerType, out Vector2 foodPosition, out IEdible _)
        )
        {
            goal = foodPosition;
            return;
        }
        if (hunger >= 70 && growthProgressMultiplier >= 0.9 && sim.RNG.RandiRange(1, 3) == 1)
        {
            //path to fertilize egg
            foreach (Egg egg in Egg.eggs)
            {
                if (egg.fertilizedBy is null && egg.laidBy != this)
                {
                    goal = egg.Position;
                    eggGoal = egg;
                    break;
                }
            }
            return;
        }

        //generate a random goal at least 300u away from the fish
        Vector2 randomGoal = Position;
        while (Position.DistanceTo(randomGoal) < 300)
        {
            randomGoal.X = sim.RNG.RandfRange(0, 1920);
            randomGoal.Y = sim.RNG.RandfRange(0, 1080);
        }

        goal = randomGoal;
    }

    public float Eat()
    {
        QueueFree();
        return CalorieValue;
    }

    /// <summary>
    /// Generates a Fish spine with the specified parameters.
    /// Uses the following quartic function: -0.00517191x^4+0.220717x^3-3.10235x^2+13.88355x+12 where x is the number of segments.
    /// </summary>
    /// <param name="segments">The number of segments in the new spine.</param>
    /// <param name="sizeMultiplier">A float which all radii are multiplied by.</param>
    /// <returns>The new spine, as a List&lt;Circle2D&gt;</returns>
    public static List<Circle2D> GenerateSpine(int segments, float sizeMultiplier)
    {
        List<Circle2D> newSpine = new();
        for (int i = 1; i <= segments; i++)
        {
            float radius = (float)(
                -0.00517191 * Math.Pow(i, 4)
                + 0.220717 * Math.Pow(i, 3)
                - 3.10235 * Math.Pow(i, 2)
                + 13.88355 * i
                + 12
            );
            newSpine.Add(new Circle2D(Vector2.Zero, radius * sizeMultiplier));
        }
        return newSpine;
    }

    /// <summary>
    /// Runs every second to update state and logic.
    /// Lavender Vanina
    /// </summary>
    private void OnSecond()
    {
        hunger = Mathf.Clamp(hunger - metabolism, 0, 100);

        if (lifetime < maxGrowthTime && hunger > 25)
        {
            spineSegmentLength += spineLengthGrowth;
            growthProgressMultiplier += (float)(0.5 / maxGrowthTime);
            for (int i = 0; i < Spine.Count; i++)
            {
                Spine[i].Radius += spineRadiiGrowths[i];
            }
        }

        lifetime++;
        timeSinceLastGoal++;
        //boredom
        if (timeSinceLastGoal > 10)
        {
            FindNewGoal();
        }

        if (sim.RNG.RandiRange(1, 100) == 100)
        {
            LayEgg();
        }
        AgeDeathCheck();
        StarvationCheck();
    }

    public void LayEgg()
    {
        if (growthProgressMultiplier >= 0.9f && hunger >= 70)
        {
            int eggs = sim.RNG.RandiRange(-1, 5);
            for (int i = 0; i < eggs; i++)
            {
                Egg _ = new(
                    this,
                    Position
                        + new Vector2(sim.RNG.RandiRange(-20, 20), sim.RNG.RandiRange(-20, 20)),
                    sim
                );
            }
        }
    }

    private bool Chance(float odds)
    {
        return sim.RNG.RandfRange(0, 100) < odds;
    }

    public void Die()
    {
        eyeColor = new Color(0, 0, 0);
        dead = true;
        QueueRedraw();
    }

    public void AgeDeathCheck()
    {
        if (lifetime <= 300)
        {
            if (Chance(.000000001f + (geneVolitility * .005f)))
            {
                Die();
            }
        }
        else if (lifetime <= 420)
        {
            if (Chance(.01f + (geneVolitility * .05f)))
            {
                Die();
            }
        }
        else if (lifetime <= 540)
        {
            if (Chance(5f + (geneVolitility * .1f)))
            {
                Die();
            }
        }
        else if (lifetime >= 600)
        {
            if (Chance(50f + (geneVolitility * .5f)))
            {
                Die();
            }
        }
    }

    public void StarvationCheck()
    {
        if (hunger > 1)
        {
            starvationTime = 0;
            return;
        }
        starvationTime++;
        if (starvationTime > 15 && Chance(starvationTime / 10))
        {
            Die();
        }
    }
}
