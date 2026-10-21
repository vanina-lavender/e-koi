using Godot;

public interface IEdible
{
    /// <summary>
    /// The amount of calories this IEdible is worth.
    /// </summary>
    float CalorieValue { get; }

    /// <summary>
    /// Frees the IEdible object.
    /// </summary>
    /// <returns>
    /// The calorie value of the IEdible which was freed.
    /// </returns>
    float Eat();
    public Vector2 Position { get; set; }
    public FoodType FoodType { get; set; }
}
