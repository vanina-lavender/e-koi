using System;
using Godot;

public partial class Globals : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    public static bool debugView = false;
    public static PackedScene simScene = ResourceLoader.Load<PackedScene>(
        "res://sim/simulation.tscn"
    );

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Manual Quit Key (F1)
        if (Input.IsActionJustPressed("Quit"))
        {
            GetTree().Quit();
        }
        if (Input.IsActionJustPressed("Debug"))
        {
            debugView = !debugView;
        }
        if (Input.IsActionJustPressed("Reset"))
        {
            GetTree().ChangeSceneToPacked(simScene);
        }
    }
}
