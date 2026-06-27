using UnityEngine;
using UnityEngine.UI;

namespace Map.Noel.Prepare
{
    /// <summary>
    /// スライドパズルの1ピース。
    /// </summary>
    public class SlidePazzlePiece : InteractableObject
    {
        [SerializeField] private int _pieceIndex;
        [SerializeField] private Image _sideImage;

        private GimmickSlidePazzle _gimmick;
        private Image[] _pieceImages;
        private bool _isBlank;

        public int PieceIndex => _pieceIndex;

        protected override void Awake()
        {
            base.Awake();
            CachePieceImages();
        }

        public void SetManager(GimmickSlidePazzle gimmick)
        {
            _gimmick = gimmick;
        }

        public void SetPieceIndex(int pieceIndex)
        {
            _pieceIndex = pieceIndex;
        }

        public void SetBlank(bool isBlank)
        {
            _isBlank = isBlank;
            CachePieceImages();

            bool visible = !_isBlank;
            for (int i = 0; i < _pieceImages.Length; i++)
            {
                _pieceImages[i].enabled = visible;
            }
        }

        public void SetSideVisible(bool visible)
        {
            _sideImage.enabled = visible;
        }

        protected override void Interact()
        {
            if (_isBlank) return;
            _gimmick?.OnPieceClicked(this);
        }

        private void CachePieceImages()
        {
            if (_pieceImages == null || _pieceImages.Length == 0)
            {
                _pieceImages = GetComponentsInChildren<Image>(true);
            }
        }
    }
}