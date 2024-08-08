using RaceGame.Gameplay;
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
        [SerializeField]
        private Image _backgroundImage;
        
        private Transform _carTransform;
        private bool _isInitialized = false;
        private Camera _mainCamera;
        private int _id;
        private float _maxPointerSize = 0f;
        private float _normalPointerSize = 0f;
        
        public void Initialize(int id, Transform carTransform, Camera mainCamera)
        {
            _id = id;
            _idLabel.text = (id + 1).ToString();
            _carTransform = carTransform;
            _pointerImage.color = Colors[id];
            _mainCamera = mainCamera;
            _isInitialized = true;
            
            _normalPointerSize = _pointerImage.rectTransform.sizeDelta.x;
            _maxPointerSize = _backgroundImage.rectTransform.sizeDelta.x;
        }
        
        private void Update()
        {
            if (_isInitialized)
            {
                //world to screen - overlay canvas
                var screenPos = _mainCamera.WorldToScreenPoint(_carTransform.position);
                transform.position = screenPos;

                HandleTurboAnimations();
            }
        }

        private void HandleTurboAnimations()
        {
            if (TrackManager.Instance.IsTurboReady(_id))
            {
                _pointerImage.rectTransform.sizeDelta = new Vector2(_maxPointerSize, _maxPointerSize);
            }
            else
            {
                if (TrackManager.Instance.IsTurboActive(_id))
                {
                    //TODO: animate the pointer
                    _pointerImage.rectTransform.sizeDelta = new Vector2(_normalPointerSize, _normalPointerSize);
                }
                else
                {
                    float normalizedCooldown = TrackManager.Instance.GetCarTurboCooldownNormalized(_id);
                    float currentSize = Utils.Remap(0f, 1f, _normalPointerSize, _maxPointerSize, 1f - normalizedCooldown);
                    _pointerImage.rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
                }
            }
        }
    }
}
