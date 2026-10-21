using System;
using Godot;

public partial class Settings : CenterContainer
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetNode<HSlider>("PanelContainer/VBoxContainer/MarginContainer/HSlider").ValueChanged +=
            GameSpeedChanged;
        GetNode<CheckButton>("PanelContainer/VBoxContainer/HBoxContainer/CheckButton").Toggled +=
            ChangeFullscreen;
        GetNode<Button>("PanelContainer/VBoxContainer/MarginContainer2/Button").Pressed += Close;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    private void GameSpeedChanged(double newVal) { }

    private void ChangeFullscreen(bool fullscreen) { }

    private void Close() { }
}
