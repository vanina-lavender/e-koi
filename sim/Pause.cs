using System;
using Godot;

public partial class Pause : ColorRect
{
    public override void _Ready()
    {
        GetNode<Button>(
            "CenterContainer/PanelContainer/MarginContainer/VBoxContainer/Resume"
        ).Pressed += () =>
        {
            GetNode<PauseHandler>("/root/Simulation/PauseHandler").TogglePause();
        };
        GetNode<Button>(
            "CenterContainer/PanelContainer/MarginContainer/VBoxContainer/Settings"
        ).Pressed += () => { };
        GetNode<Button>(
            "CenterContainer/PanelContainer/MarginContainer/VBoxContainer/Quit"
        ).Pressed += () =>
        {
            GetTree().Quit();
        };
    }
}
