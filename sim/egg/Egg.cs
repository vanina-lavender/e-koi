using System.Collections.Generic;
using Godot;

public partial class Egg : Node2D, IEdible
{
    public float CalorieValue
    {
        get => 100f;
    }
    public Fish laidBy;
    public Fish fertilizedBy = null;
    public Color lightColor;
    public Color outlineColor;
    public Color centerColor;
    public FoodType FoodType { get; set; } = FoodType.CARNIVORE;
    public Simulation sim;
    public static List<Egg> eggs = new();

    public Egg(Fish laidBy, Vector2 position, Simulation sim)
    {
        this.laidBy = laidBy;
        this.sim = sim;
        outlineColor = new Color(
            (laidBy.color.R + 1) / 2,
            (laidBy.color.G + 1) / 2,
            (laidBy.color.B + 1) / 2
        );
        lightColor = new Color(
            (outlineColor.R + 1) / 2,
            (outlineColor.G + 1) / 2,
            (outlineColor.B + 1) / 2
        );
        centerColor = new Color(1, 1, 1, 0);

        sim.AddChild(this);
        this.Position = position;
        sim.edibleList.Add(this);
        QueueRedraw();

        eggs.Add(this);
    }

    public float Eat()
    {
        QueueFree();
        eggs.Remove(this);
        return CalorieValue;
    }

    public void Fertilize(Fish fish)
    {
        if (fertilizedBy is not null)
        {
            return;
        }
        fertilizedBy = fish;
        centerColor = fish.color;
        QueueRedraw();
        Tween hatcher = GetTree().CreateTween();
        hatcher.TweenInterval(60);
        hatcher.TweenCallback(Callable.From(Hatch));
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 8f * laidBy.randSize, outlineColor, filled: false, width: 2);
        DrawCircle(Vector2.Zero, 8f * laidBy.randSize, lightColor);
        DrawCircle(Vector2.Zero, 4f * laidBy.randSize, centerColor);
    }

    private void Hatch()
    {
        Fish newFish = new Fish(sim, laidBy, fertilizedBy, Position);

        //failure to hatch
        if (newFish.geneVolitility < 7)
        {
            sim.AddChild(newFish);
            sim.fishlist.Add(newFish);
        }

        eggs.Remove(this);
        sim.edibleList.Remove(this);
        QueueFree();
    }
}
