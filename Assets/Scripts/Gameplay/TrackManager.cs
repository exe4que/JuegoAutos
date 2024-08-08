using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace RaceGame.Gameplay
{
    public class TrackManager : Singleton<TrackManager>
    {
        [BoxGroup("General")]
        [SerializeField] 
        [Range(0f, 100f)] 
        private float _carsSpeed = 5f;

        [BoxGroup("General")]
        [SerializeField] 
        [Range(0f, 5f)] 
        private float _carsReachMaxSpeedDuration = 1f;

        [BoxGroup("General")]
        [SerializeField] 
        [Range(0f, 50f)] 
        private float _carsBounciness = 0.5f;

        [BoxGroup("General")]
        [SerializeField] 
        private float _gravity = 9.8f; // Acceleration due to gravity
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboSpeedMultiplier = 2f;
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboDuration = 2f;
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboReachMaxSpeedDuration = 0.3f;
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboGetBackToNormalSpeedDuration = 1f;
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboCooldown = 10f;
        
        [BoxGroup("Turbo")]
        [SerializeField]
        private float _carsTurboDoubleTapMaxDuration = 0.5f;
        

        [BoxGroup("Track")]
        [Header("Load Track list from start module")] 
        [SerializeField]
        private TrackModule _startModule;

        [BoxGroup("Track")]
        [SerializeField] 
        private List<TrackModule> _trackModules = new();
        
        [BoxGroup("Track")]
        [EnumToggleButtons]
        public DebugOptions DebugOption;
        
        private Dictionary<int, CarTrackData> _carTrackData = new();

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
                ProcessCarMovement(carData);
            }
        }

        private void ProcessCarMovement(CarTrackData carData)
        {
            float targetSpeed = ProcessTopSpeed(carData);
            float normalSpeed = _carsSpeed * carData.SpeedMultiplier;
            float acceleration = ProcessAcceleration(carData, targetSpeed, normalSpeed);

            carData.CurrentSpeed =
                Mathf.MoveTowards(carData.CurrentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
            //log top speed and acceleration
            //Debug.Log($"Car {carData.CarId} top speed: {targetSpeed} acceleration: {acceleration}");

            if (carData.CurrentSpeed > 0)
            {
                MoveCarAlongTrack(carData);
            }
        }

        private void MoveCarAlongTrack(CarTrackData carData)
        {
            carData.TrackPosition += carData.CurrentSpeed * Time.fixedDeltaTime;
            //carData.TrackPosition += _carsSpeed * Time.fixedDeltaTime * carData.SpeedMultiplier;

            if (carData.TrackPosition > carData.CurrentModulePositionRange.y)
            {
                TrackModule nextModule = carData.CurrentModule.GetNextModule();
                carData.CurrentModule = nextModule;

                // Prevents errors due to floating point precision
                if (carData.CurrentModulePositionRange.y > carData.TotalLength)
                {
                    carData.CurrentModulePositionRange.y %= carData.TotalLength;
                    carData.TrackPosition %= carData.TotalLength;
                }

                carData.CurrentModulePositionRange = new Vector2(carData.CurrentModulePositionRange.y,
                    carData.CurrentModulePositionRange.y + nextModule.GetLength(carData.XOffset));
            }

            float normalizedPosition = (carData.TrackPosition - carData.CurrentModulePositionRange.x) /
                                       (carData.CurrentModulePositionRange.y -
                                        carData.CurrentModulePositionRange.x);

            Vector3 position =
                carData.CurrentModule.GetTrackPoint(normalizedPosition, carData.XOffset, out Vector3 tangent);
            position.y = CalculateYPosition(carData, position.y);
            carData.CarTransform.position = position;
            carData.CarTransform.rotation = Quaternion.LookRotation(tangent);
        }

        private float ProcessTopSpeed(CarTrackData carData)
        {
            float targetSpeed = 0f;
            
            if (!carData.IsAccelerating)
            {
                targetSpeed = 0f;
            }
            else
            {
                targetSpeed = _carsSpeed * carData.SpeedMultiplier;
                if(IsTurboActive(carData.CarId))
                {
                    targetSpeed *= _carsTurboSpeedMultiplier;
                }
            }

            return targetSpeed;
        }

        private float ProcessAcceleration(CarTrackData carData, float targetSpeed, float normalSpeed)
        {
            float acceleration = 0f;

            if (!carData.IsAccelerating)
            {
                //deceleration
                acceleration = normalSpeed / _carsReachMaxSpeedDuration;
            }
            else if (IsTurboActive(carData.CarId))
            {
                //turbo acceleration
                acceleration = targetSpeed / _carsTurboReachMaxSpeedDuration;
            }
            else
            {
                //normal acceleration
                acceleration = targetSpeed / _carsReachMaxSpeedDuration;
            }

            return acceleration;
        }

        private float CalculateYPosition(CarTrackData carData, float floorLevel)
        {
            // Update particle position and velocity
            carData.YVelocity -= _gravity * Time.fixedDeltaTime;
            carData.PositionY += carData.YVelocity;

            // Check if the particle hits the floor
            if (carData.PositionY <= floorLevel)
            {
                // Bounce off the floor
                carData.PositionY = floorLevel;
                carData.YVelocity *= -_carsBounciness;

                float floorVelocity = floorLevel - carData.LastFloorLevel;
                carData.YVelocity += floorVelocity;
            }

            carData.LastFloorLevel = floorLevel;
            return carData.PositionY;
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
                TrackPosition = _trackModules[0].GetLength(0) * 0.5f,
                CurrentSpeed = 0f,
                LastAccelerationTime = 0f,
                LastTurboTime = _carsTurboDuration
            };
            float normalTrackLength = GetTrackTotalLength(0);
            //I don't know why I need to multiply by 1.1f, but it works
            float carTrackLength = GetTrackTotalLength(xOffset * 1.1f);

            carTrackData.SpeedMultiplier = carTrackLength / normalTrackLength;
            carTrackData.TotalLength = carTrackLength;
            _carTrackData.Add(carId, carTrackData);
            
            Events.GameplayEvents.OnCarRegistered?.Invoke(carId, xOffset, carTransform);
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
            bool isTurboReady = IsTurboReady(carId);
            bool isTurboActive = IsTurboActive(carId);
            float tapDuration = Time.time - _carTrackData[carId].LastAccelerationTime;
            bool fastDoubleTap = tapDuration < _carsTurboDoubleTapMaxDuration;
            _carTrackData[carId].LastAccelerationTime = Time.time;
            //log turbo ready, turbo active, tap duration and fast double tap
            //Debug.Log($"Car {carId}, turbo ready: {isTurboReady}, turbo active: {isTurboActive}, tap duration: {tapDuration}, fast double tap: {fastDoubleTap}");
            if(isTurboReady && !isTurboActive && fastDoubleTap)
            {
                _carTrackData[carId].LastTurboTime = Time.time;
            }
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

        public float GetCarTrackLength(int carId)
        {
            return _carTrackData[carId].TotalLength;
        }
        
        public bool IsTurboActive(int carId)
        {
            return Time.time - _carTrackData[carId].LastTurboTime < _carsTurboDuration;
        }
        
        public bool IsTurboReady(int carId)
        {
            return !IsTurboActive(carId) && GetCarTurboCooldown(carId) <= 0;
        }
        
        public float GetCarTurboCooldown(int carId)
        {
            return Mathf.Max(0, _carsTurboCooldown - (Time.time - (_carTrackData[carId].LastTurboTime + _carsTurboDuration)));
        }
        
        public float GetCarTurboCooldownNormalized(int carId)
        {
            return GetCarTurboCooldown(carId) / _carsTurboCooldown;
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
            public float YVelocity;
            public float PositionY;
            public float LastFloorLevel;
            public float CurrentSpeed;
            public float LastTurboTime;
            public float LastAccelerationTime;
        }

        [Flags]
        public enum DebugOptions
        {
            None = 0,
            ModuleSize = 1 << 0,
            ModuleConnections = 1 << 1,
            ModuleBezier = 1 << 2,
            ModuleCurveRadius = 1 << 3,
            All = ModuleSize | ModuleConnections | ModuleBezier | ModuleCurveRadius
        }
    }
}
