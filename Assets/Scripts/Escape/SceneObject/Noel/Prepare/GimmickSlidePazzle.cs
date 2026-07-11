using System.Collections.Generic;
using DG.Tweening;
using RunconaLib.Audio;
using UnityEngine;

namespace Escape.SceneObject.Noel.Prepare
{
    /// <summary>
    /// WoodBoxのスライドパズルギミック管理。
    /// </summary>
    public class GimmickSlidePazzle : MonoBehaviour
    {
        [SerializeField] private GameObject _woodBoxClose;
        [SerializeField] private GameObject _woodBoxOpen;

        [Header("Solve Fade")] [SerializeField]
        private Component _woodBoxCloseFadeTarget;

        [SerializeField] private Component _woodBoxOpenFadeTarget;
        [SerializeField] private float _solveFadeOutDuration = 0.2f;
        [SerializeField] private float _solveFadeInDuration = 0.2f;
        [SerializeField] private Ease _solveFadeEase = Ease.Linear;

        [Header("Tween")] [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _sideHideTiming = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _sideShowTiming = 0.5f;

        [Header("Puzzle Settings")] [SerializeField]
        private int _columns = 3;

        [SerializeField] private int _rows = 2;
        [SerializeField] private int _blankPieceIndex = 6;
        [SerializeField] private int[] _correctOrder = { 3, 4, 5, 1, 2, 6 };

        [Header("Runtime")] [SerializeField] private List<SlidePazzlePiece> _pieces = new List<SlidePazzlePiece>();

        private readonly Dictionary<int, SlidePazzlePiece> _pieceByIndex = new Dictionary<int, SlidePazzlePiece>();
        private readonly List<Vector2> _slotPositions = new List<Vector2>();
        private readonly Dictionary<int, int> _piecePositionByIndex = new Dictionary<int, int>();

        private int[] _board;
        private bool _isSolved;
        private bool _hasAppliedOnce;

        private void Start()
        {
            InitializePuzzle();
        }

        public void OnPieceClicked(SlidePazzlePiece clickedPiece)
        {
            if (_isSolved || clickedPiece == null) return;

            int clickedPosition = FindPositionByPieceIndex(clickedPiece.PieceIndex);
            int blankPosition = FindPositionByPieceIndex(_blankPieceIndex);
            if (clickedPosition < 0 || blankPosition < 0) return;

            if (!CanMove(clickedPosition, blankPosition)) return;

            MovePieces(clickedPosition, blankPosition);
            ApplyBoardToView();

            if (IsSolved())
            {
                UnlockWoodBox();
            }
        }

        private void InitializePuzzle()
        {
            SetupPieces();
            BuildSlotPositions();
            ResetBoardToInitialOrder();
            _hasAppliedOnce = false;
            _piecePositionByIndex.Clear();
            ApplyBoardToView();
        }

        private void SetupPieces()
        {
            _pieceByIndex.Clear();

            int maxPieces = Mathf.Min(_pieces.Count, _rows * _columns);
            for (int i = 0; i < maxPieces; i++)
            {
                SlidePazzlePiece piece = _pieces[i];
                int pieceIndex = i + 1;
                piece.SetManager(this);
                piece.SetPieceIndex(pieceIndex);
                piece.SetBlank(pieceIndex == _blankPieceIndex);
                _pieceByIndex[pieceIndex] = piece;
            }
        }

        private void BuildSlotPositions()
        {
            _slotPositions.Clear();

            RectTransform baseRect = _pieces[0].transform as RectTransform;
            Vector2 topLeft = baseRect.anchoredPosition;
            float pieceWidth = baseRect.sizeDelta.x;
            float pieceHeight = baseRect.sizeDelta.y;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    _slotPositions.Add(new Vector2(
                        topLeft.x + (pieceWidth * col),
                        topLeft.y - (pieceHeight * row)
                    ));
                }
            }
        }

        private void ResetBoardToInitialOrder()
        {
            int count = _rows * _columns;
            _board = new int[count];
            for (int i = 0; i < count; i++)
            {
                _board[i] = i + 1;
            }
        }

        private void ApplyBoardToView()
        {
            for (int position = 0; position < _board.Length; position++)
            {
                int pieceIndex = _board[position];
                if (!_pieceByIndex.TryGetValue(pieceIndex, out SlidePazzlePiece piece)) continue;

                RectTransform rectTransform = piece.transform as RectTransform;
                Vector2 targetPosition = _slotPositions[position];

                if (!_hasAppliedOnce || !_piecePositionByIndex.TryGetValue(pieceIndex, out int fromPosition))
                {
                    rectTransform.anchoredPosition = targetPosition;
                    rectTransform.SetSiblingIndex(position);
                    _piecePositionByIndex[pieceIndex] = position;
                    piece.SetSideVisible(position / _columns != _rows - 1);
                    continue;
                }

                if (fromPosition == position)
                {
                    rectTransform.anchoredPosition = targetPosition;
                    rectTransform.SetSiblingIndex(position);
                    continue;
                }

                rectTransform.DOKill();
                rectTransform.SetSiblingIndex(position);

                int fromRow = fromPosition / _columns;
                int toRow = position / _columns;
                bool toBottom = toRow == _rows - 1;

                if (fromRow == toRow)
                {
                    piece.SetSideVisible(!toBottom);
                    rectTransform.DOAnchorPos(targetPosition, _moveDuration).SetEase(Ease.InOutQuad);
                }
                else
                {
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(rectTransform.DOAnchorPos(targetPosition, _moveDuration).SetEase(Ease.InOutQuad));
                    float timing = toBottom ? _sideHideTiming : _sideShowTiming;
                    sequence.InsertCallback(_moveDuration * timing, () => piece.SetSideVisible(!toBottom));
                    sequence.OnComplete(() => piece.SetSideVisible(!toBottom));
                }

                _piecePositionByIndex[pieceIndex] = position;
            }

            _hasAppliedOnce = true;
        }

        private int FindPositionByPieceIndex(int pieceIndex)
        {
            for (int i = 0; i < _board.Length; i++)
            {
                if (_board[i] == pieceIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool CanMove(int clickedPosition, int blankPosition)
        {
            int clickedRow = clickedPosition / _columns;
            int clickedCol = clickedPosition % _columns;
            int blankRow = blankPosition / _columns;
            int blankCol = blankPosition % _columns;

            return clickedRow == blankRow || clickedCol == blankCol;
        }

        private void MovePieces(int clickedPosition, int blankPosition)
        {
            int clickedRow = clickedPosition / _columns;
            int clickedCol = clickedPosition % _columns;
            int blankRow = blankPosition / _columns;
            int blankCol = blankPosition % _columns;

            if (clickedRow == blankRow)
            {
                if (clickedPosition < blankPosition)
                {
                    for (int pos = blankPosition; pos > clickedPosition; pos--)
                    {
                        _board[pos] = _board[pos - 1];
                    }
                }
                else
                {
                    for (int pos = blankPosition; pos < clickedPosition; pos++)
                    {
                        _board[pos] = _board[pos + 1];
                    }
                }
            }
            else if (clickedCol == blankCol)
            {
                if (clickedPosition < blankPosition)
                {
                    for (int pos = blankPosition; pos > clickedPosition; pos -= _columns)
                    {
                        _board[pos] = _board[pos - _columns];
                    }
                }
                else
                {
                    for (int pos = blankPosition; pos < clickedPosition; pos += _columns)
                    {
                        _board[pos] = _board[pos + _columns];
                    }
                }
            }

            _board[clickedPosition] = _blankPieceIndex;
        }

        private bool IsSolved()
        {
            if (_correctOrder == null || _correctOrder.Length != _board.Length)
            {
                return false;
            }

            for (int i = 0; i < _board.Length; i++)
            {
                if (_board[i] != _correctOrder[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void UnlockWoodBox()
        {
            _isSolved = true;
            AudioManager.Instance.PlaySE(SESoundType.Correct);
            FadeSwitchService.Switch(_woodBoxCloseFadeTarget, _woodBoxOpenFadeTarget, _solveFadeOutDuration,
                _solveFadeInDuration, _solveFadeEase);
        }
    }
}