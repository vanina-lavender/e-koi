using System;
using Godot;

public partial class PauseHandler : Node
{
    private ColorRect pauseMenu;
    private double delay = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        pauseMenu = GetNode<ColorRect>("/root/Simulation/CanvasLayer/Pause");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        delay += delta;
        if (Input.IsActionJustPressed("Pause"))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (delay < 0.06)
        {
            return;
        }
        GetTree().Paused = !GetTree().Paused;
        pauseMenu.Visible = !pauseMenu.Visible;
    }
}
