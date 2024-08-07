using UnityEngine;

namespace RaceGame.Gameplay
{
    public class Car : MonoBehaviour
    {
        [SerializeField] private int Id = 0;

        [SerializeField] private float XOffset = 0f;

        [SerializeField] private KeyCode AccelerateKey = KeyCode.Space;

        private void Start()
        {
            TrackManager.Instance.RegisterCar(Id, XOffset, this.transform);
        }

        private void Update()
        {
            if (Input.GetKeyDown(AccelerateKey))
            {
                TrackManager.Instance.AccelerateCar(Id);
            }
            else if (Input.GetKeyUp(AccelerateKey))
            {
                TrackManager.Instance.DecelerateCar(Id);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(this.transform.position,
                    $"Distance run: {TrackManager.Instance.GetCarTrackPosition(Id).ToString("F2")}/{TrackManager.Instance.GetCarTrackLength(Id).ToString("F2")}m"
                    + $"\nSpeed: {TrackManager.Instance.GetCarSpeed(Id).ToString("F2")}m/s");
            }
        }
    }
#endif
}
