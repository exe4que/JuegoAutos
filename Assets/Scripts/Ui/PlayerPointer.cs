using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceGame.Ui
{
    public class PlayerPointer : MonoBehaviour
    {
        private readonly Color[] Colors =
        {
            new Color32(236,132,62, 255),
            new Color32(231,198,93,255),
            new Color32(102,129,165,255)
        };
        
        [SerializeField]
        private TMP_Text _idLabel;
        [SerializeField]
        private Image _pointerImage;
        
        private Transform _carTransform;
        private bool _isInitialized = false;
        private Camera _mainCamera;
        public void Initialize(int id, Transform carTransform, Camera mainCamera)
        {
            _idLabel.text = (id + 1).ToString();
            _carTransform = carTransform;
            _pointerImage.color = Colors[id];
            _mainCamera = mainCamera;
            _isInitialized = true;
        }
        
        private void Update()
        {
            if (_isInitialized)
            {
                //world to screen - overlay canvas
                var screenPos = _mainCamera.WorldToScreenPoint(_carTransform.position);
                transform.position = screenPos;
            }
        }
    }
}
