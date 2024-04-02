using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrackManager : Singleton<TrackManager>
{
    [SerializeField]
    [Range(0f, 30f)]
    private float _carsSpeed = 5f;
    
    [Header("Load Track list from start module")]
    [SerializeField]
    private TrackModule _startModule;
    
    [SerializeField]
    private List<TrackModule> _trackModules = new ();
    
    private Dictionary<int, CarTrackData> _carTrackData = new ();

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (_startModule != null)
        {
            _trackModules.Clear();
            _trackModules.Add(_startModule);
            
            TrackModule currentModule = _startModule;
            TrackModule nextModule = currentModule.GetNextModule();
            while (nextModule != null && nextModule != _startModule)
            {
                _trackModules.Add(nextModule);
                currentModule = nextModule;
                nextModule = currentModule.GetNextModule();
            }
            _startModule = null;
            EditorUtility.SetDirty(this);
        }
    }
    #endif

    private void FixedUpdate()
    {
        foreach (var pair in _carTrackData)
        {
            CarTrackData carData = pair.Value;
            if (carData.IsAccelerating)
            {
                carData.TrackPosition += _carsSpeed * Time.fixedDeltaTime * carData.SpeedMultiplier;
                
                if (carData.TrackPosition > carData.CurrentModulePositionRange.y)
                {
                    TrackModule nextModule = carData.CurrentModule.GetNextModule();
                    carData.CurrentModule = nextModule;
                    
                    // Prevents errors due to floating point precision
                    if(carData.CurrentModulePositionRange.y > carData.TotalLength)
                    {
                        carData.CurrentModulePositionRange.y %= carData.TotalLength;
                        carData.TrackPosition %= carData.TotalLength;
                    }
                    carData.CurrentModulePositionRange = new Vector2(carData.CurrentModulePositionRange.y, carData.CurrentModulePositionRange.y + nextModule.GetLength(carData.XOffset));
                }
                
                float normalizedPosition = (carData.TrackPosition - carData.CurrentModulePositionRange.x) / (carData.CurrentModulePositionRange.y - carData.CurrentModulePositionRange.x);
                
                carData.CarTransform.position = carData.CurrentModule.GetTrackPoint(normalizedPosition, carData.XOffset, out Vector3 tangent);
                carData.CarTransform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }

    public void RegisterCar(int carId, float xOffset, Transform carTransform)
    {
        CarTrackData carTrackData = new CarTrackData
        {
            CarId = carId,
            IsAccelerating = false,
            CarTransform = carTransform,
            XOffset = xOffset,
            CurrentModule = _trackModules[0],
            CurrentModulePositionRange = new Vector2(0, _trackModules[0].GetLength(xOffset)),
            TrackPosition = _trackModules[0].GetLength(0) * 0.5f
        };
        float normalTrackLength = GetTrackTotalLength(0);
        float carTrackLength = GetTrackTotalLength(xOffset);
        
        carTrackData.SpeedMultiplier =  carTrackLength / normalTrackLength;
        carTrackData.TotalLength = carTrackLength;
        _carTrackData.Add(carId, carTrackData);
    }

    private float GetTrackTotalLength(float xOffset)
    {
        float totalLength = 0f;
        foreach (TrackModule trackModule in _trackModules)
        {
            totalLength += trackModule.GetLength(xOffset);
        }
        return totalLength;
    }
    
    public void AccelerateCar(int carId)
    {
        _carTrackData[carId].IsAccelerating = true;
    }
    
    public void DecelerateCar(int carId)
    {
        _carTrackData[carId].IsAccelerating = false;
    }
    
    public float GetCarSpeed(int carId)
    {
        return _carTrackData[carId].SpeedMultiplier;
    }
    
    public float GetCarTrackPosition(int carId)
    {
        return _carTrackData[carId].TrackPosition;
    }
    
    private class CarTrackData
    {
        public int CarId;
        public bool IsAccelerating;
        public Transform CarTransform;
        public float XOffset;
        public TrackModule CurrentModule;
        public Vector2 CurrentModulePositionRange;
        public float TrackPosition;
        public float SpeedMultiplier;
        public float TotalLength;
    }
}
