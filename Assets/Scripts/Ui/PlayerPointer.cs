using DG.Tweening;
using RaceGame.Gameplay;
using Sirenix.OdinInspector;
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
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private float _turboReadyScaleUpDuration = 0.5f;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private float _turboReadyScaleUpSize = 2f;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private Ease _turboReadyScaleUpEase = Ease.OutBack;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private float _turboReadyFadeDuration = 0.5f;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private Ease _turboReadyFadeEase = Ease.InOutSine;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private float _turboReadyPunchDuration = 0.2f;
        [BoxGroup("Turbo Ready Animation")]
        [SerializeField]
        private float _turboReadyPunchSize = 0.1f;
        
        private Transform _carTransform;
        private bool _isInitialized = false;
        private Camera _mainCamera;
        private int _id;
        private float _maxPointerSize = 0f;
        private float _normalPointerSize = 0f;
        private PointerState _pointerState = PointerState.Normal;
        private Color _backgroundOriginalColor;
        
        public void Initialize(int id, Transform carTransform, Camera mainCamera)
        {
            _id = id;
            _idLabel.text = (id + 1).ToString();
            _carTransform = carTransform;
            _pointerImage.color = Colors[id];
            _mainCamera = mainCamera;
            _backgroundOriginalColor = _backgroundImage.color;
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

        Sequence _turboReadySequence;
        private void HandleTurboAnimations()
        {
            if (TrackManager.Instance.IsTurboReady(_id))
            {
                if(_pointerState != PointerState.TurboReady)
                {
                    PlayTurboReadyAnimation();
                    _pointerState = PointerState.TurboReady;
                }
                _pointerImage.rectTransform.sizeDelta = new Vector2(_maxPointerSize, _maxPointerSize);
            }
            else
            {
                if (TrackManager.Instance.IsTurboActive(_id))
                {
                    if(_pointerState != PointerState.TurboActive)
                    {
                        ClearTurboReadyAnimation(true);
                        PlayTurboActiveAnimation();
                        _pointerState = PointerState.TurboActive;
                    }
                    _pointerImage.rectTransform.sizeDelta = new Vector2(_normalPointerSize, _normalPointerSize);
                }
                else
                {
                    _pointerState = PointerState.Normal;
                    float normalizedCooldown = TrackManager.Instance.GetCarTurboCooldownNormalized(_id);
                    float currentSize = Utils.Remap(0f, 1f, _normalPointerSize, _maxPointerSize, 1f - normalizedCooldown);
                    _pointerImage.rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
                }
            }
        }

        private void PlayTurboActiveAnimation()
        {
            //todo
        }

        [Button]
        private void PlayTurboReadyAnimation()
        {
            if (_turboReadySequence != null)
            {
                _turboReadySequence.Kill();
            }
            _turboReadySequence = DOTween.Sequence();
            _turboReadySequence.Append(_backgroundImage.rectTransform
                .DOScale(_turboReadyScaleUpSize, _turboReadyScaleUpDuration).SetEase(_turboReadyScaleUpEase));
            _turboReadySequence.Join(_backgroundImage.DOFade(0f, _turboReadyFadeDuration)
                .SetEase(_turboReadyFadeEase));
            _turboReadySequence.Join(_pointerImage.rectTransform.DOPunchScale(Vector3.one * _turboReadyPunchSize, _turboReadyPunchDuration));
            _turboReadySequence.AppendInterval(1f);
            _turboReadySequence.AppendCallback(() => ClearTurboReadyAnimation(false));
            _turboReadySequence.SetLoops(-1);
        }

        private void ClearTurboReadyAnimation(bool killSequence)
        {
            if (killSequence)
            {
                _turboReadySequence?.Kill();
            }
            _backgroundImage.rectTransform.localScale = Vector3.one;
            _backgroundImage.color = _backgroundOriginalColor;
            _pointerImage.rectTransform.localScale = Vector3.one;
        }

        private enum PointerState
        {
            Normal,
            TurboReady,
            TurboActive
        }
    }
}
