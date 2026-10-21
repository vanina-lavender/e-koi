using System;
using Godot;

public partial class MainMenuButton : RichTextLabel
{
    public string OriginalText { get; set; }
    private bool hovered = false;

    [Signal]
    public delegate void ClickedEventHandler();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        OriginalText = Text;
        MouseEntered += () =>
        {
            Text = $"[u]{OriginalText}";
            hovered = true;
        };
        MouseExited += () =>
        {
            Text = OriginalText;
            hovered = false;
        };
    }

    // Emit Clicked signal when clicked. Weird workaround so that we can use RichTextLabels instead of buttons.
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (hovered && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            EmitSignal(SignalName.Clicked);
        }
    }
}
