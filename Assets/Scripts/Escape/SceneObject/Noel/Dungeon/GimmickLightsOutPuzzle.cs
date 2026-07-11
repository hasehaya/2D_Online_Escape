using System;
using RunconaLib.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace Escape.SceneObject.Noel.Dungeon
{
    public class GimmickLightsOutPuzzle : MonoBehaviour
    {
        private const int CellCount = 9;
        private const int Columns = 3;

        [Header("Cells")] [SerializeField] private LightsOutPuzzleCell[] _cells = new LightsOutPuzzleCell[CellCount];

        [Header("Puzzle Settings")] [SerializeField]
        private bool[] _initialState = new bool[CellCount];

        [SerializeField] private bool[] _answerState =
        {
            true, true, true,
            true, true, true,
            true, true, true
        };

        [SerializeField] private bool _resetOnStart = true;
        [SerializeField] private bool _allowInputAfterSolved;

        [Header("Network")] [SerializeField]
        private string _boardBitsKey = PhotonRoomPropertyKeys.DungeonLightsOutPuzzleBoardBits;

        [SerializeField] private FlagType _completedFlag = FlagType.Dungeon_LightsOutPuzzleCompleted;
        [SerializeField] private bool _publishInitialStateOnStart = true;

        [Header("Events")] [SerializeField] private UnityEvent _onSolved;

        private bool[] _currentState = new bool[CellCount];
        private bool _isSolved;

        private void OnEnable()
        {
            SetupCells();

            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnFlagChanged += OnFlagChanged;
            }
        }

        private void OnDisable()
        {
            TeardownCells();

            if (GameStateService.Instance != null)
            {
                GameStateService.Instance.OnFlagChanged -= OnFlagChanged;
            }
        }

        private void Start()
        {
            if (IsCompletedFlagSet())
            {
                _isSolved = true;
                CopyAnswerToCurrentState();
                PublishBoardState();
                return;
            }

            if (_resetOnStart)
            {
                ResetPuzzle();
                return;
            }

            if (_publishInitialStateOnStart)
            {
                PublishBoardState();
            }
        }

        public void PressCell(int cellIndex)
        {
            if (_isSolved && !_allowInputAfterSolved) return;
            if (!IsValidIndex(cellIndex)) return;

            ToggleCross(cellIndex);
            PublishBoardState();

            if (IsSolved())
            {
                CompletePuzzle();
            }
        }

        public void ResetPuzzle()
        {
            NormalizeArraySizes();

            if (IsCompletedFlagSet())
            {
                _isSolved = true;
                CopyAnswerToCurrentState();
                PublishBoardState();
                return;
            }

            for (int i = 0; i < CellCount; i++)
            {
                _currentState[i] = _initialState[i];
            }

            _isSolved = false;
            PublishBoardState();
        }

        public void SetCellState(int cellIndex, bool isLit)
        {
            if (!IsValidIndex(cellIndex)) return;

            _currentState[cellIndex] = isLit;
            PublishBoardState();
        }

        public bool GetCellState(int cellIndex)
        {
            return IsValidIndex(cellIndex) && _currentState[cellIndex];
        }

        [ContextMenu("Solve Puzzle")]
        private void SolvePuzzleForDebug()
        {
            NormalizeArraySizes();

            for (int i = 0; i < CellCount; i++)
            {
                _currentState[i] = _answerState[i];
            }

            PublishBoardState();
            CompletePuzzle();
        }

        private void SetupCells()
        {
            NormalizeArraySizes();

            for (int i = 0; i < CellCount; i++)
            {
                if (_cells[i] == null) continue;
                _cells[i].SetCellIndex(i);
                _cells[i].OnPressed.RemoveListener(PressCell);
                _cells[i].OnPressed.AddListener(PressCell);
            }
        }

        private void OnFlagChanged(FlagType flag, bool value)
        {
            if (flag != _completedFlag || !value) return;

            _isSolved = true;
            CopyAnswerToCurrentState();
        }

        private void TeardownCells()
        {
            if (_cells == null) return;

            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == null) continue;
                _cells[i].OnPressed.RemoveListener(PressCell);
            }
        }

        private void ToggleCross(int cellIndex)
        {
            ToggleCell(cellIndex);

            int row = cellIndex / Columns;
            int col = cellIndex % Columns;

            if (row > 0) ToggleCell(cellIndex - Columns);
            if (row < Columns - 1) ToggleCell(cellIndex + Columns);
            if (col > 0) ToggleCell(cellIndex - 1);
            if (col < Columns - 1) ToggleCell(cellIndex + 1);
        }

        private void ToggleCell(int cellIndex)
        {
            if (!IsValidIndex(cellIndex)) return;
            _currentState[cellIndex] = !_currentState[cellIndex];
        }

        private void PublishBoardState()
        {
            if (string.IsNullOrEmpty(_boardBitsKey)) return;
            if (GameStateService.Instance == null) return;

            GameStateService.Instance.SetInt(_boardBitsKey, EncodeBoardBits());
        }

        private bool IsSolved()
        {
            NormalizeArraySizes();

            for (int i = 0; i < CellCount; i++)
            {
                if (_currentState[i] != _answerState[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void CompletePuzzle()
        {
            if (_isSolved) return;

            _isSolved = true;
            PublishCompletedFlag();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE(SESoundType.Correct);
            }

            _onSolved?.Invoke();
        }

        private void PublishCompletedFlag()
        {
            if (_completedFlag == FlagType.None) return;
            if (GameStateService.Instance == null) return;

            GameStateService.Instance.SetFlag(_completedFlag, true);
        }

        private bool IsCompletedFlagSet()
        {
            return _completedFlag != FlagType.None
                   && GameStateService.Instance != null
                   && GameStateService.Instance.GetFlag(_completedFlag);
        }

        private void CopyAnswerToCurrentState()
        {
            NormalizeArraySizes();

            for (int i = 0; i < CellCount; i++)
            {
                _currentState[i] = _answerState[i];
            }
        }

        private int EncodeBoardBits()
        {
            int bits = 0;

            for (int i = 0; i < CellCount; i++)
            {
                if (_currentState[i])
                {
                    bits |= 1 << i;
                }
            }

            return bits;
        }

        private static bool IsValidIndex(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < CellCount;
        }

        private void NormalizeArraySizes()
        {
            if (_cells == null || _cells.Length != CellCount) Array.Resize(ref _cells, CellCount);
            if (_initialState == null || _initialState.Length != CellCount) Array.Resize(ref _initialState, CellCount);
            if (_answerState == null || _answerState.Length != CellCount) Array.Resize(ref _answerState, CellCount);
            if (_currentState == null || _currentState.Length != CellCount) Array.Resize(ref _currentState, CellCount);
        }

        private void OnValidate()
        {
            NormalizeArraySizes();
        }
    }
}