using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
/// The main scene which manages the simulation. Stores references to all fish and manages non-fish object AI.
/// </summary>
public partial class Simulation : Node2D
{
    private Camera2D camera;
    private bool dragging;
    private Vector2 dragStart;
    public RandomNumberGenerator RNG { get; set; } = new();
    public List<IEdible> edibleList = new();
    private FoodType? selectedFoodType = null;
    private Container inspectorSpacer;
    private bool inspectorCollapsed = true;
    private Tween inspectorCollapsor;
    private Tween inspectorUncollapsor;
    private Fish currentInspectorFish;
    public List<Fish> fishlist = [];
    private PanelContainer renameMenu;
    private LineEdit nameEditor;
    public ColorRect pauseMenu;

    //Signal emitted every second.
    [Signal]
    public delegate void SecondEventHandler();
    private double timeSinceSecond = 0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        camera = GetNode<Camera2D>("Camera");
        pauseMenu = GetNode<ColorRect>("CanvasLayer/Pause");

        for (int i = 0; i < 10; i++)
        {
            fishlist.Add(
                new Fish(
                    sim: this,
                    new Color(RNG.RandfRange(0, 1), RNG.RandfRange(0, 1), RNG.RandfRange(0, 1))
                )
            );
        }
        foreach (Fish i in fishlist)
        {
            AddChild(i);
        }

        Button herbivoreSelectionButton = GetNode<Button>(
            "CanvasLayer/GUI/VBoxContainer/MarginContainer/CenterContainer/CenterContainer/FoodTypes/Herbivore"
        );
        herbivoreSelectionButton.ButtonDown += () =>
        {
            selectedFoodType = FoodType.HERBIVORE;
        };
        Button omnivoreSelectionButton = GetNode<Button>(
            "CanvasLayer/GUI/VBoxContainer/MarginContainer/CenterContainer/CenterContainer/FoodTypes/Omnivore"
        );
        omnivoreSelectionButton.ButtonDown += () =>
        {
            selectedFoodType = FoodType.OMNIVORE;
        };
        Button carnivoreSelectionButton = GetNode<Button>(
            "CanvasLayer/GUI/VBoxContainer/MarginContainer/CenterContainer/CenterContainer/FoodTypes/Carnivore"
        );
        carnivoreSelectionButton.ButtonDown += () =>
        {
            selectedFoodType = FoodType.CARNIVORE;
        };
        Food herbivoreIcon = new Food(FoodType.HERBIVORE)
        {
            Position = new Vector2(856, 1024),
            ZIndex = 4,
        };
        Food omnivoreIcon = new Food(FoodType.OMNIVORE)
        {
            Position = new Vector2(960, 1024),
            ZIndex = 4,
        };
        Food carnivoreIcon = new Food(FoodType.CARNIVORE)
        {
            Position = new Vector2(1064, 1024),
            ZIndex = 4,
        };
        GetNode<CanvasLayer>("CanvasLayer").AddChild(herbivoreIcon);
        GetNode<CanvasLayer>("CanvasLayer").AddChild(omnivoreIcon);
        GetNode<CanvasLayer>("CanvasLayer").AddChild(carnivoreIcon);

        GetNode<VBoxContainer>("CanvasLayer/GUI/VBoxContainer").GuiInput += HandleInput;
        GetNode<RichTextLabel>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/MarginContainer/HBoxContainer/PanelContainer/MarginContainer/VBoxContainer/Name"
        ).GuiInput += Rename;
        inspectorSpacer = GetNode<Container>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/InspectorSpacer"
        );
        GetNode<Button>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/MarginContainer/HBoxContainer/CenterContainer/MarginContainer/Collapse"
        ).Pressed += Collapse;

        Collapse();

        renameMenu = GetNode<PanelContainer>("CanvasLayer/Rename");
        nameEditor = renameMenu.GetNode<LineEdit>(
            "CenterContainer/PanelContainer/MarginContainer/VBoxContainer/LineEdit"
        );
        nameEditor.TextSubmitted += (string newText) =>
        {
            if (Regex.Replace(newText, "\\[.*?\\]", String.Empty).Trim().Length == 0)
            {
                newText = currentInspectorFish.name;
            }
            nameEditor.Text = "";
            currentInspectorFish.name = newText.Trim();
            renameMenu.Visible = false;
            UpdateInspector(currentInspectorFish);
        };
        nameEditor.EditingToggled += (bool editing) =>
        {
            if (editing)
            {
                return;
            }
            nameEditor.Text = string.Empty;
            renameMenu.Visible = false;
        };
    }

    public void Collapse()
    {
        if (inspectorCollapsed)
        {
            inspectorCollapsed = !inspectorCollapsed;
            if (inspectorUncollapsor is null)
            {
                inspectorUncollapsor = GetTree().CreateTween();
            }
            inspectorUncollapsor.Kill();
            inspectorUncollapsor = GetTree().CreateTween();
            inspectorUncollapsor
                .TweenProperty(inspectorSpacer, "custom_minimum_size", new Vector2(410, 0), 0.5f)
                .SetTrans(Tween.TransitionType.Cubic);
        }
        else
        {
            inspectorCollapsed = !inspectorCollapsed;
            if (inspectorCollapsor is null)
            {
                inspectorCollapsor = GetTree().CreateTween();
            }
            inspectorCollapsor.Kill();
            inspectorCollapsor = GetTree().CreateTween();
            inspectorCollapsor
                .TweenProperty(inspectorSpacer, "custom_minimum_size", new Vector2(0, 0), 0.5f)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }

    public void HandleInput(InputEvent e)
    {
        if (e.IsActionPressed("Click") && selectedFoodType is not null)
        {
            Food newFood = new(selectedFoodType ?? FoodType.OMNIVORE);
            newFood.Position = GetGlobalMousePosition();
            AddChild(newFood);
            edibleList.Add(newFood);
        }
        if (e.IsActionPressed("Pan"))
        {
            dragStart = GetLocalMousePosition();
            dragging = true;
        }
        else if (e.IsActionReleased("Pan"))
        {
            dragging = false;
        }
        if (dragging)
        {
            camera.Position += dragStart - GetLocalMousePosition();
        }
        if (e.IsAction("ZoomIn"))
        {
            float zoom = (float)Mathf.Clamp(camera.Zoom.X + 0.1, 0.5, 2);
            camera.Zoom = new Vector2(zoom, zoom);
        }
        if (e.IsAction("ZoomOut"))
        {
            float zoom = (float)Mathf.Clamp(camera.Zoom.X - 0.1, 0.5, 2);
            camera.Zoom = new Vector2(zoom, zoom);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        //Second signal
        timeSinceSecond += delta;
        if (timeSinceSecond >= 1)
        {
            timeSinceSecond -= 1;
            EmitSignal(SignalName.Second);
            if (currentInspectorFish is not null)
            {
                UpdateInspector(currentInspectorFish);
            }
        }
    }

    public bool FindNearestEdible(
        Vector2 position,
        FoodType foodType,
        out Vector2 foodPosition,
        out IEdible thisEdible
    )
    {
        foodPosition = Vector2.Zero;
        thisEdible = null;
        if (edibleList.Count > 0)
        {
            foreach (IEdible food in edibleList)
            {
                if (foodType == FoodType.OMNIVORE)
                {
                    foodPosition = food.Position;
                    thisEdible = food;
                    break;
                }
                else if ((food.FoodType == foodType) || (food.FoodType == FoodType.OMNIVORE))
                {
                    foodPosition = food.Position;
                    thisEdible = food;
                    break;
                }
            }
            foreach (IEdible food in edibleList)
            {
                if (foodType == FoodType.OMNIVORE)
                {
                    if ((foodPosition - position).Length() > (food.Position - position).Length())
                    {
                        foodPosition = food.Position;
                        thisEdible = food;
                    }
                }
                else if ((food.FoodType == foodType) || (food.FoodType == FoodType.OMNIVORE))
                {
                    if ((foodPosition - position).Length() > (food.Position - position).Length())
                    {
                        foodPosition = food.Position;
                        thisEdible = food;
                    }
                }
            }
        }
        if (thisEdible is not null)
        {
            return true;
        }
        return false;
    }

    public void UpdateInspector(Fish fish)
    {
        currentInspectorFish = fish;
        RichTextLabel data = GetNode<RichTextLabel>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/MarginContainer/HBoxContainer/PanelContainer/MarginContainer/VBoxContainer/Data"
        );
        RichTextLabel name = GetNode<RichTextLabel>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/MarginContainer/HBoxContainer/PanelContainer/MarginContainer/VBoxContainer/Name"
        );
        RichTextLabel age = GetNode<RichTextLabel>(
            "CanvasLayer/GUI/VBoxContainer/HBoxContainer/MarginContainer/HBoxContainer/PanelContainer/MarginContainer/VBoxContainer/Age"
        );
        age.Text = "[center]Age: " + Math.Floor(fish.lifetime / 60f);

        Color fishColor = fish.color;
        name.Text = $"[center][color={fishColor.ToHtml()}]";
        if (fishColor.R + fishColor.G + fishColor.B < 0.5)
        {
            name.Text += "[outline_size=12][outline_color=white]";
        }
        name.Text += fish.name;

        data.Text = $"[center]Length: {fish.Spine.Count * fish.spineSegmentLength:F2}\n";
        data.Text += $"Diet: {fish.hungerType.ToString().Capitalize():F2}\n";
        data.Text += $"Species: {fish.species}\n";
        if (fish.pairsOfEyes != 1)
        {
            data.Text +=
                $"[{(fish.pairsOfEyes > 1 ? "?" : "!")}] Pairs of Eyes: {fish.pairsOfEyes}\n";
        }
        if (fish.blind)
        {
            data.Text += "[!] Blind\n";
        }
        if (fish.dorsalFins != 1)
        {
            data.Text += $"[{(fish.dorsalFins > 1 ? "?" : "!")}] Dorsal Fins: {fish.dorsalFins}\n";
        }
    }

    public void Rename(InputEvent e)
    {
        if (currentInspectorFish is null || !e.IsActionPressed("Click"))
        {
            return;
        }
        renameMenu.Visible = true;
        nameEditor.GrabFocus();
    }
}
