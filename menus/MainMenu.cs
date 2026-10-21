using System;
using System.Numerics;
using Godot;

public partial class MainMenu : Node2D
{
    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
    {
        ((MainMenuButton)GetNode("Menu/Start")).Clicked += Start;
        ((MainMenuButton)GetNode("Menu/Options")).Clicked += Options;
        ((MainMenuButton)GetNode("Menu/Exit")).Clicked += Exit;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public void Start()
    {
        GetTree().ChangeSceneToPacked(Globals.simScene);
    }

    public void Options()
    {
        //TODO
    }

    public void Exit()
    {
        GetTree().Quit();
    }
}
