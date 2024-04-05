using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TestYPhysics : MonoBehaviour
{
    public Transform Floor;
    public Transform Particle;
    public float Gravity = 9.8f; // Acceleration due to gravity
    public float Bounciness = 0.8f; // Coefficient of restitution (bounciness factor)
    
    // Initialize particle properties
    float _positionY = 0; // Initial position
    float _velocityY = 0; // Initial velocity
    float _floorLevel = 100.0f; // Initial floor level (adjust as needed)
    float _lastFloorLevel = 100.0f; // Initial floor level (adjust as needed)

    private void Start()
    {
        _positionY = Particle.position.y;
        _floorLevel = Input.mousePosition.y / 100f;
    }

    private void Update()
    {
        _lastFloorLevel = _floorLevel;
        _floorLevel = Input.mousePosition.y / 100f;
        
        // Update particle position and velocity
        _velocityY -= Gravity * Time.deltaTime;
        _positionY += _velocityY;

        // Check if the particle hits the floor
        if (_positionY <= _floorLevel)
        {
            // Bounce off the floor
            _positionY = _floorLevel;
            _velocityY *= -Bounciness;
            
            float floorVelocity = _floorLevel - _lastFloorLevel;
            _velocityY += floorVelocity;
            
        }
        Particle.position = new Vector3(0, _positionY, 0);
        Floor.position = new Vector3(0, _floorLevel, 0);
    }
}
