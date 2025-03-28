using System;
using Unity.VisualScripting;
using UnityEngine;


public class Cell
{
    public Vector2Int Position { get; set; }
    public int Value { get; set; }
    public bool IsMerged { get; set; } // Для предотвращения двойного слияния
    public CellView View { get; set; } // Ссылка на визуальный компонент

    public event Action<int> OnValueChanged;
    public event Action<Vector2Int> OnPositionChanged;

    public void SetView(CellView view)
    {
        View = view;
    }

    public void Cleanup()
    {
        OnValueChanged = null;
        OnPositionChanged = null;
    }

    public Cell(Vector2Int position, int value)
    {
        Position = position;
        Value = value;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        Position = newPosition;
        OnPositionChanged?.Invoke(Position);
    }

    public void SetValue(int newValue)
    {
        Value = newValue;
        View.UpdateColorAndText(Value);
        OnValueChanged?.Invoke(Value);
        
    }
}