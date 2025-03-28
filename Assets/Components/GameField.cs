using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameInput;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class GameField : MonoBehaviour
{
    [SerializeField] private int _fieldSize = 4;
    [SerializeField] private GameObject[] empty_cells;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private TMP_Text currentScoreField;
    [SerializeField] private TMP_Text highScoreField;

    [SerializeField] private GameObject _gameOverScreenPrefab;
    private GameInput _gameInput;
    private List<Cell> _cells = new List<Cell>();
    private bool[] _occupiedCells;
    private int _currentScore = 0;
    private int _highScore = 0;
    private Boolean instantiatedGameOverScreen = false;
    public event Action<int> OnScoreUpdated;
    private GameObject gameOverScreen;

    private void Awake()
    {
        _gameInput = new GameInput();
        _occupiedCells = new bool[empty_cells.Length];
    }

    private void Swipe(InputAction.CallbackContext ctx)
    {
        var dir = ctx.ReadValue<Vector2>();
        var swipeDir = Vector2Int.zero;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y) && Mathf.Abs(dir.x) > 10)
        {
            if (dir.x > 0)
            {
                swipeDir.x = 1;
            }
            else
            {
                swipeDir.x = -1;
            }
        }
        else if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x) && Mathf.Abs(dir.y) > 10)
        {
            if (dir.y > 0)
            {
                swipeDir.y = 1;
            }
            else
            {
                swipeDir.y = -1;
            }
        }

        if (!swipeDir.Equals(Vector2Int.zero))
        {
            MoveCells(swipeDir);
        }
    }

    void OnEnable()
    {
        _gameInput.Enable();
        _gameInput.KeyboardAction.MoveUp.performed += ctx => MoveCells(Vector2Int.up);
        _gameInput.KeyboardAction.MoveDown.performed += ctx => MoveCells(Vector2Int.down);
        _gameInput.KeyboardAction.MoveLeft.performed += ctx => MoveCells(Vector2Int.left);
        _gameInput.KeyboardAction.MoveRight.performed += ctx => MoveCells(Vector2Int.right);
        _gameInput.SwipeAction.MoveWithTouchScreen.performed += Swipe;
        _gameInput.SwipeAction.MoveWithMouse.performed += Swipe;
    }

    void OnDisable()
    {
        _gameInput.KeyboardAction.MoveUp.performed -= ctx => MoveCells(Vector2Int.up);
        _gameInput.KeyboardAction.MoveDown.performed -= ctx => MoveCells(Vector2Int.down);
        _gameInput.KeyboardAction.MoveLeft.performed -= ctx => MoveCells(Vector2Int.left);
        _gameInput.KeyboardAction.MoveRight.performed -= ctx => MoveCells(Vector2Int.right);
        _gameInput.SwipeAction.MoveWithTouchScreen.performed -= Swipe;
        _gameInput.SwipeAction.MoveWithMouse.performed -= Swipe;
        _gameInput.Disable();
    }

    void Start()
    {
        Boolean res = LoadGame();
        currentScoreField.text = _currentScore.ToString();
        highScoreField.text = _highScore.ToString();
        if (!res) CreateInitialCells();
        UpdateScore();
    }

    void Update()
    {
        currentScoreField.text = _currentScore.ToString();
        highScoreField.text = _highScore.ToString();
    }

    [System.Serializable]
    private class SaveData
    {
        public List<int> positions;
        public List<int> values;
        public int highScore;
    }

    private void CreateInitialCells()
    {
        CreateCell();
        CreateCell();
    }

    private void UpdateScore()
    {
        _currentScore = _cells.Sum(c => ((int)Math.Pow(2, c.Value)));
        if (_currentScore > _highScore) _highScore = _currentScore;
        OnScoreUpdated?.Invoke(_currentScore);
        currentScoreField.SetText(_currentScore.ToString());
        highScoreField.SetText(_highScore.ToString());
    }

    public int? GetEmptyCellIndex()
    {
        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < _occupiedCells.Length; i++)
        {
            if (!_occupiedCells[i])
            {
                emptyIndices.Add(i);
            }
        }

        if (emptyIndices.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, emptyIndices.Count);
        return emptyIndices[randomIndex];
    }

    public void CreateCell()
    {
        if (_cellPrefab == null)
        {
            Debug.LogError("Cell Prefab is not assigned!");
            return;
        }

        int? emptyIndex = GetEmptyCellIndex();
        if (emptyIndex == null) return;

        int value = Random.Range(0, 100) < 20 ? 2 : 1;
        Vector2Int position = GetPositionFromIndex(emptyIndex.Value);

        Cell newCell = new Cell(position, value);
        _cells.Add(newCell);
        _occupiedCells[emptyIndex.Value] = true;

        GameObject cellObj = Instantiate(_cellPrefab, empty_cells[emptyIndex.Value].transform);
        cellObj.transform.localPosition = Vector3.zero;
        CellView cellView = cellObj.GetComponent<CellView>();
        newCell.View = cellView;
        cellView.Init(newCell);
    }

    private Vector2Int GetPositionFromIndex(int index)
    {
        int x = index % _fieldSize;
        int y = index / _fieldSize;
        return new Vector2Int(x, y);
    }

    private int GetIndexFromPosition(Vector2Int position)
    {
        return position.y * _fieldSize + position.x;
    }

    public void MoveCells(Vector2Int direction)
    {
        bool cellsMoved = false;
        direction.y = -direction.y;
        var cellsToMove = _cells.OrderBy(c => direction.x == 1 ? -c.Position.x : c.Position.x)
            .ThenBy(c => direction.y == 1 ? -c.Position.y : c.Position.y)
            .ToList();
        foreach (var cell in cellsToMove)
        {
            if (cell == null) continue;

            int currentIndex = GetIndexFromPosition(cell.Position);
            _occupiedCells[currentIndex] = false;

            Vector2Int nextPos = cell.Position + direction;
            bool canMove = true;
            Cell targetCell = null;

            while (canMove && IsPositionValid(nextPos))
            {
                int nextIndex = GetIndexFromPosition(nextPos);
                targetCell = _cells.FirstOrDefault(c => c.Position == nextPos);

                if (targetCell == null)
                {
                    cell.SetPosition(nextPos);
                    nextPos += direction;
                    cellsMoved = true;
                }
                else if (targetCell.Value == cell.Value && !targetCell.IsMerged)
                {
                    targetCell.SetValue(targetCell.Value + 1);
                    _cells.Remove(cell);
                    if (cell.View != null)
                    {
                        Destroy(cell.View.gameObject);
                    }

                    targetCell.IsMerged = true;
                    cellsMoved = true;
                    break;
                }
                else
                {
                    canMove = false;
                }
            }

            if (cell.View != null && _cells.Contains(cell))
            {
                int newIndex = GetIndexFromPosition(cell.Position);
                _occupiedCells[newIndex] = true;
                cell.View.transform.SetParent(empty_cells[newIndex].transform);
                cell.View.transform.localPosition = Vector3.zero;
            }
        }

        foreach (var cell in _cells) cell.IsMerged = false;

        if (cellsMoved)
        {
            CreateCell();
            UpdateScore();
        }

        if (IsGameOver())
        {
            OnDisable();
            CreateGameOverScreen();
        }
    }

    private void CreateGameOverScreen()
    {
        if (_gameOverScreenPrefab == null)
        {
            Debug.LogError("Game over prefab is not assigned!");
            return;
        }

        if (!instantiatedGameOverScreen)
        {
            instantiatedGameOverScreen = true;
            gameOverScreen = Instantiate(_gameOverScreenPrefab);
            gameOverScreen.transform.SetParent(this.transform);
            gameOverScreen.transform.localPosition = Vector3.zero;
            Button resetButton = gameOverScreen.transform.Find("RestartButton").gameObject.GetComponent<Button>();
            resetButton.onClick.AddListener(() => { ResetGame(); });
            Button exitButton = gameOverScreen.transform.Find("ExitButton").gameObject.GetComponent<Button>();
            exitButton.onClick.AddListener(() => { ExitGame(); });
        }
    }

    private bool IsPositionValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < _fieldSize && pos.y >= 0 && pos.y < _fieldSize;
    }

    public void SaveGame()
    {
        SaveData data = new SaveData()
        {
            positions = _cells.Select(c => GetIndexFromPosition(c.Position)).ToList(),
            values = _cells.Select(c => c.Value).ToList(),
            highScore = _highScore
        };

        BinaryFormatter formatter = new BinaryFormatter();
        string path = Path.Combine(Application.persistentDataPath, "save.dat");
        using FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, data);
    }

    public Boolean LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.dat");
        if (!File.Exists(path)) return false;

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Open))
        {
            SaveData data = (SaveData)formatter.Deserialize(stream);
            _highScore = data.highScore;

            for (int i = 0; i < data.positions.Count; i++)
            {
                Vector2Int position = GetPositionFromIndex(data.positions[i]);
                Cell cell = new Cell(position, data.values[i]);
                _cells.Add(cell);
                _occupiedCells[data.positions[i]] = true;

                GameObject cellObj = Instantiate(_cellPrefab, empty_cells[data.positions[i]].transform);
                cellObj.transform.localPosition = Vector3.zero;
                CellView cellView = cellObj.GetComponent<CellView>();
                cell.View = cellView;
                cellView.Init(cell);
            }
        }

        return true;
    }

    public void ResetGame()
    {
        foreach (var cell in _cells)
        {
            if (cell.View != null)
            {
                Destroy(cell.View.gameObject);
            }
        }

        _cells.Clear();
        Array.Clear(_occupiedCells, 0, _occupiedCells.Length);
        _currentScore = 0;
        CreateInitialCells();
        UpdateScore();
        if (instantiatedGameOverScreen)
        {
            instantiatedGameOverScreen = false;
            Destroy(gameOverScreen);
        }

        OnEnable();
    }

    public bool IsGameOver()
    {
        if (GetEmptyCellIndex() != null) return false;

        foreach (var cell in _cells)
        {
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int checkPos = cell.Position + dir;
                if (IsPositionValid(checkPos) && _cells.Any(c => c.Position == checkPos && c.Value == cell.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void ExitGame()
    {
        ResetGame();
        SaveGame();
        Application.Quit(0);
    }
}