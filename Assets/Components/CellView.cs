using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private Color _startColor;
    [SerializeField] private Color _endColor;

    private Cell _cell;

    public void Init(Cell cell)
    {
        _cell = cell;
        cell.OnValueChanged += UpdateColorAndText;
        UpdateColorAndText(cell.Value);
    }

    public void UpdateColorAndText(int value)
    {
        _valueText.text = Mathf.Pow(2, value).ToString();
        float t = (float)value / 11f;
        _background.color = Color.Lerp(_startColor, _endColor, t);
    }

    private void OnDestroy()
    {
        if (_cell != null)
        {
            _cell.OnValueChanged -= UpdateColorAndText;
        }
    }
}