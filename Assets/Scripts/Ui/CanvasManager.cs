using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RaceGame.Ui
{
    public class CanvasManager : Singleton<CanvasManager>
    {
        [SerializeField]
        private Camera _mainCamera;
        
        [SerializeField]
        private AssetReference _carUiPrefab;
        
        private void Awake()
        {
            Events.GameplayEvents.OnCarRegistered += OnCarRegistered;
        }

        private void OnDestroy()
        {
            Events.GameplayEvents.OnCarRegistered -= OnCarRegistered;
        }

        private async void OnCarRegistered(int id, float xOffset, Transform carTransform)
        {
            var carUi = await _carUiPrefab.InstantiateAsync().Task;
            carUi.transform.SetParent(transform);
            carUi.transform.position = carTransform.position;
            carUi.transform.rotation = carTransform.rotation;
            carUi.transform.localScale = Vector3.one;
            carUi.GetComponent<PlayerPointer>().Initialize(id, carTransform, _mainCamera);
        }
    }
}

