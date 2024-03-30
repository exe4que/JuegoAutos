using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class TrackModule : MonoBehaviour
{
    [SerializeField]
    private Vector2 _size = new Vector2(15, 15);

    [SerializeField]
    [Range(0, 3)]
    private int _rotation;
    
    [SerializeField]
    // 0 = Left, 1 = Up, 2 = Right, 3 = Down
    private Connection[] _connections = new Connection[4];
    
    private bool _isBezierGenerated = false;
    private int _bezierStartIndex = -1;
    private int _bezierEndIndex = -1;

    private Vector2 GetSize()
    {
        Vector2 size = _size;
        if(_rotation % 2 != 0)
        {
            size = new Vector2(_size.y, _size.x);
        }
        return size;
    }

    private Vector3 GetConnectionPosition(int index)
    {
        if (index < 0 || index >= _connections.Length)
        {
            Debug.LogError("Invalid index");
            return Vector3.zero;
        }
        Vector3 position = Vector3.zero;
        
        int realIndex = (index + _rotation) % 4; 
        Vector2 realSize = GetSize();
        switch (index)
        {
            case 0:
                position = transform.position + new Vector3(-realSize.x * 0.5f, 0f, -realSize.y * 0.5f + (realSize.y * _connections[realIndex].Position));
                break;
            case 1:
                position = transform.position + new Vector3(-realSize.x * 0.5f + realSize.x * _connections[realIndex].Position, 0f, realSize.y * 0.5f);
                break;
            case 2:
                position = transform.position + new Vector3(realSize.x * 0.5f, 0f, realSize.y * 0.5f - (realSize.y * _connections[realIndex].Position));
                break;
            case 3:
                position = transform.position + new Vector3(realSize.x * 0.5f - realSize.x * _connections[realIndex].Position, 0f, -realSize.y * 0.5f);
                break;
        }

        return position;
    }

    private Vector3 GetBezierPoint(float t)
    {
        Vector3 start = GetConnectionPosition(_bezierStartIndex);
        Vector3 end = GetConnectionPosition(_bezierEndIndex);
        Vector3 center = (start + end) * 0.5f;
            
        if(_bezierStartIndex % 2 != _bezierEndIndex % 2)
        {
            if(_bezierStartIndex % 2 == 0)
            {
                center = new Vector3(end.x, center.y, start.z);
            }
            else
            {
                center = new Vector3(start.x, center.y, end.z);
            }
        }
        //Vector3 bezierPoint = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * center + t * t * end;
        Vector3 a = Vector3.Lerp(start, center, t);
        Vector3 b = Vector3.Lerp(center, end, t);
        Vector3 bezierPoint = Vector3.Lerp(a, b, t);
        return bezierPoint;
    }

#if UNITY_EDITOR
    private int _lastRotation = 0;
    private Vector3 _lastPosition;
    private List<TrackModule> _trackModules = new();
#endif
    private void Update()
    {
#if UNITY_EDITOR
        HandleRotation();
        HandleAttachment();
        GenerateBezier();
#endif
    }

#if UNITY_EDITOR
    private void HandleRotation()
    {
        if (_lastRotation != _rotation)
        {
            _lastRotation = _rotation;
            transform.rotation = Quaternion.Euler(0f, _rotation * -90f, 0f);
            //Clear all connections
            for (int index = 0; index < _connections.Length; index++)
            {
                _connections[index].ConnectedModule = null;
                _connections[index].ConnectedConnectionIndex = 0;
            }
        }
    }

    private void HandleAttachment()
    {
        //find all track modules
        if (UnityEditor.Selection.Contains(gameObject))
        {
            if(_trackModules.Count == 0)
            {
                _trackModules.AddRange(FindObjectsOfType<TrackModule>().ToList());
                _trackModules.Remove(this);
            }
        }
        else
        {
            _trackModules.Clear();
            return;
        }

        //handle attachment
        if (this.transform.position != _lastPosition)
        {
            for (var index = 0; index < _connections.Length; index++)
            {
                int realIndex = (index + _rotation) % 4;
                if (!_connections[realIndex].Enabled)
                {
                    continue;
                }
                bool connected = false;
                foreach (var otherModule in _trackModules)
                {
                    for (var otherIndex = 0; otherIndex < otherModule._connections.Length; otherIndex++)
                    {
                        Vector3 connectionPosition = GetConnectionPosition(index);
                        Vector3 otherConnectionPosition = otherModule.GetConnectionPosition(otherIndex);
                        if (Vector3.Distance(connectionPosition, otherConnectionPosition) < 2f)
                        {
                            //Align this gameobject with the other gameobject
                            Vector3 direction = otherConnectionPosition - connectionPosition;
                            transform.position += direction;
                            _connections[realIndex].ConnectedModule = otherModule;
                            _connections[realIndex].ConnectedConnectionIndex = otherIndex;
                            connected = true;
                            break;
                        }
                    }
                    if (connected)
                    {
                        break;
                    }
                }
                
                if(!connected)
                {
                    _connections[realIndex].ConnectedModule = null;
                    _connections[realIndex].ConnectedConnectionIndex = 0;
                }
            }
            _lastPosition = this.transform.position;
        }
        
    }

    private void OnDrawGizmos()
    {
        DrawModule();
        DrawConnections();
        DrawBezier();
    }

    private void DrawBezier()
    {
        if (_isBezierGenerated)
        {
            Gizmos.color = Color.yellow;
            Vector3 lastBezierPoint = Vector3.zero;
            for (int step = 0; step < 5; step++)
            {
                float t = step / 4f;
                Vector3 bezierPoint = GetBezierPoint(t);
                if(step > 0)
                {
                    Gizmos.DrawLine(lastBezierPoint, bezierPoint);
                }
                lastBezierPoint = bezierPoint;
            }
        }
    }

    private void DrawConnections()
    {
        for (int index = 0; index < 4; index++)
        {
            int realIndex = (index + _rotation) % 4;
            var connection = _connections[realIndex];
            if (connection.Enabled)
            {
                Vector3 connectionPosition = GetConnectionPosition(index);
                if(connection.ConnectedModule != null)
                {
                    Gizmos.color = Color.green;
                    
                    Vector3 direction = connection.ConnectedModule.transform.position - connectionPosition;
                    ExtraGizmos.DrawArrow(connectionPosition, direction.normalized, 6f, 2f);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(connectionPosition, new Vector3(1f, 1f, 1f));
                }
            }
        }
    }

    private void DrawModule()
    {
        Gizmos.color = Color.red;
        Vector2 realSize = GetSize();
        Gizmos.DrawWireCube(transform.position, new Vector3(realSize.x, 0f, realSize.y));
    }

    private void GenerateBezier()
    {
        int enabledConnections = 0;
        for (int i = 0; i < _connections.Length; i++)
        {
            int realIndex = (i + _rotation) % 4;
            if (_connections[realIndex].Enabled)
            {
                if (_connections[realIndex].ConnectedModule == null)
                {
                    _bezierStartIndex = i;
                }
                else
                {
                    _bezierEndIndex = i;
                }
                enabledConnections++;
            }
        }
        if(enabledConnections == 2 && _bezierStartIndex != -1 && _bezierEndIndex != -1)
        {
            _isBezierGenerated = true;
        }
        else
        {
            _isBezierGenerated = false;
            _bezierStartIndex = -1;
            _bezierEndIndex = -1;
        }
    }
#endif

    [Serializable]
    public class Connection
    {
        public bool Enabled;
        [Range(0f,1f)]
        public float Position = 0.5f;
        public TrackModule ConnectedModule;
        public int ConnectedConnectionIndex;
    }
}
