
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;


public class MouseDragComposite : InputBindingComposite<Vector2>
{
    static MouseDragComposite()
    {
        InputSystem.RegisterBindingComposite<MouseDragComposite>();
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
    }

    public override Vector2 ReadValue(ref InputBindingCompositeContext context)
    {
        var b = context.ReadValueAsButton(Button);
        var x = context.ReadValue<float>(Axis1);
        var y = context.ReadValue<float>(Axis2);
        var v = new Vector2(x, y);

        return  b && v.magnitude > 0.0f ? v : default;
    }

    public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
    {
        return ReadValue(ref context).magnitude;
    }

    #region Fields

    [InputControl(layout = "Button")]
    public int Button;

    [InputControl(layout = "Axis")] 
    public int Axis1;

    [InputControl(layout = "Axis")]
    public int Axis2;

    #endregion
}