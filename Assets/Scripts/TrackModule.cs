using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaceGame
{
    [ExecuteInEditMode]
    public class TrackModule : MonoBehaviour
    {
        private static float _curveStrength = 0.921f;

        [SerializeField] private TrackType _trackType = TrackType.Straight;

        [SerializeField] private Vector2 _size = new Vector2(15, 15);

        [SerializeField] [Range(0, 3)] private int _rotation;

        [SerializeField]
        // 0 = Left, 1 = Up, 2 = Right, 3 = Down
        private Connection[] _connections = new Connection[4];

        private bool _isBezierGenerated = false;
        private int _bezierStartIndex = -1;
        private int _bezierEndIndex = -1;
        private TrackModule _nextModule;

        public TrackModule GetNextModule()
        {
            return _nextModule;
        }

        private void SetNextModule()
        {
            if (_connections.Length == 0)
            {
                Debug.LogError("Connections array is empty.");
                _nextModule = null;
                return;
            }

            foreach (var connection in _connections)
            {
                if (connection.Enabled && connection.ConnectedModule != null)
                {
                    _nextModule = connection.ConnectedModule;
                    return;
                }
            }

            Debug.LogError("No connected module found.");
        }

        public float GetLength(float xOffset)
        {
            if (_trackType == TrackType.Straight)
            {
                if (_rotation % 2 == 0)
                {
                    return GetSize().y;
                }

                return GetSize().x;
            }
            else
            {
                float radius = -1f;
                foreach (var connection in _connections)
                {
                    if (connection.Enabled)
                    {
                        Vector3 midPoint = GetBezierPoint(0.5f, out Vector3 tangent);
                        Vector3 focalPoint = GetCurveFocalPoint();
                        Vector3 inwardsOutwardsDirection = (focalPoint - midPoint).normalized;
                        Vector3 cross = Vector3.Cross(inwardsOutwardsDirection, tangent);

                        if (cross.y > 0)
                        {
                            // Target is to the right
                            radius = (connection.Position * GetSize().x) - xOffset;
                        }
                        else
                        {
                            // Target is to the left
                            radius = (connection.Position * GetSize().x) + xOffset;
                        }

                        break;
                    }
                }

                if (radius > 0f)
                {
                    return 0.5f * Mathf.PI * radius;
                }

                return 0f;
            }
        }

        private Vector2 GetSize()
        {
            Vector2 size = _size;
            if (_rotation % 2 != 0)
            {
                size = new Vector2(_size.y, _size.x);
            }

            return size;
        }

        private Vector3 GetConnectionPosition(int index)
        {
            if (index < 0 || index >= _connections.Length)
            {
                Debug.LogError($"Invalid index {index} for connections array.");
                return Vector3.zero;
            }

            Vector3 position = Vector3.zero;

            int realIndex = (index + _rotation) % 4;
            Vector2 realSize = GetSize();
            switch (index)
            {
                case 0:
                    position = transform.position + new Vector3(-realSize.x * 0.5f, _connections[realIndex].FloorLevel,
                        -realSize.y * 0.5f + (realSize.y * _connections[realIndex].Position));
                    break;
                case 1:
                    position = transform.position +
                               new Vector3(-realSize.x * 0.5f + realSize.x * _connections[realIndex].Position,
                                   _connections[realIndex].FloorLevel, realSize.y * 0.5f);
                    break;
                case 2:
                    position = transform.position + new Vector3(realSize.x * 0.5f, _connections[realIndex].FloorLevel,
                        realSize.y * 0.5f - (realSize.y * _connections[realIndex].Position));
                    break;
                case 3:
                    position = transform.position +
                               new Vector3(realSize.x * 0.5f - realSize.x * _connections[realIndex].Position,
                                   _connections[realIndex].FloorLevel, -realSize.y * 0.5f);
                    break;
            }

            return position;
        }


        public Vector3 GetTrackPoint(float t, float xOffset, out Vector3 tangent)
        {
            switch (_trackType)
            {
                case TrackType.Straight:
                    Vector3 start = GetConnectionPosition(_bezierStartIndex);
                    Vector3 end = GetConnectionPosition(_bezierEndIndex);
                    tangent = (end - start).normalized;
                    Vector3 point = Vector3.Lerp(start, end, t);
                    Vector3 offset = Vector3.Cross(tangent, Vector3.up) * xOffset;
                    return point + offset;
                case TrackType.Curve:
                    Vector3 bezierPoint = GetBezierPoint(t, out tangent);
                    Vector3 offset1 = Vector3.Cross(tangent, Vector3.up) * xOffset;
                    return bezierPoint + offset1;
            }

            tangent = Vector3.zero;
            return default(Vector3);
        }

        private Vector3 GetBezierPoint(float t, out Vector3 tangent)
        {
            Vector3 start = GetConnectionPosition(_bezierStartIndex);
            Vector3 end = GetConnectionPosition(_bezierEndIndex);
            Vector3 center = Vector3.zero;

            if (_bezierStartIndex % 2 != _bezierEndIndex % 2)
            {
                if (_bezierStartIndex % 2 == 0)
                {
                    center = new Vector3(end.x, center.y, start.z);
                }
                else
                {
                    center = new Vector3(start.x, center.y, end.z);
                }
            }

            Vector3 focalPoint = GetCurveFocalPoint();
            Vector3 outwardsVector = center - focalPoint;
            center = focalPoint + outwardsVector * _curveStrength;


            //Vector3 bezierPoint = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * center + t * t * end;
            Vector3 a = Vector3.Lerp(start, center, t);
            Vector3 b = Vector3.Lerp(center, end, t);
            Vector3 bezierPoint = Vector3.Lerp(a, b, t);
            tangent = (b - a).normalized;
            return bezierPoint;
        }

#if UNITY_EDITOR
        private int _lastRotation = -1;
        private Vector3 _lastPosition = Vector3.one * -1;
        private List<TrackModule> _trackModules = new();
#endif

        private void Awake()
        {
            GenerateBezier();
            SetNextModule();
        }

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
            if (_lastRotation == -1)
            {
                _lastRotation = _rotation;
            }

            if (_lastRotation != _rotation)
            {
                _lastRotation = _rotation;
                transform.rotation = Quaternion.Euler(0f, _rotation * -90f, 0f);
                //Clear all connections
                for (int index = 0; index < _connections.Length; index++)
                {
                    _connections[index].ConnectedModule = null;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

        private void HandleAttachment()
        {
            //find all track modules
            if (UnityEditor.Selection.Contains(gameObject))
            {
                if (_trackModules.Count == 0)
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
            if (_lastPosition == Vector3.one * -1)
            {
                _lastPosition = this.transform.position;
            }

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
                            connectionPosition.y = 0;

                            Vector3 otherConnectionPosition = otherModule.GetConnectionPosition(otherIndex);
                            otherConnectionPosition.y = 0;
                            if (Vector3.Distance(connectionPosition, otherConnectionPosition) < 2f)
                            {
                                //Align this gameobject with the other gameobject
                                Vector3 direction = otherConnectionPosition - connectionPosition;
                                transform.position += direction;
                                _connections[realIndex].ConnectedModule = otherModule;
                                UnityEditor.EditorUtility.SetDirty(this);
                                connected = true;
                                break;
                            }
                        }

                        if (connected)
                        {
                            break;
                        }
                    }

                    if (!connected)
                    {
                        _connections[realIndex].ConnectedModule = null;
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }

                _lastPosition = this.transform.position;
            }

        }

        public void OnDrawGizmos()
        {
            var debugOptions = TrackManager.Instance.DebugOption;
            if (debugOptions.HasFlag(TrackManager.DebugOptions.ModuleSize))
            {
                DrawModule();
            }
            if (debugOptions.HasFlag(TrackManager.DebugOptions.ModuleBezier))
            {
                DrawBezier();
            }
            if (debugOptions.HasFlag(TrackManager.DebugOptions.ModuleConnections))
            {
                DrawConnections();
            }
            if (debugOptions.HasFlag(TrackManager.DebugOptions.ModuleCurveRadius))
            {
                DrawCurveRadius();
            }
            //UnityEditor.Handles.Label(this.transform.position, $"Normal Length: {GetLength(0f)}, Offset Length: {GetLength(-3f)}");
        }

        private void DrawCurveRadius()
        {
            if (_trackType == TrackType.Curve)
            {
                var centerPoint = GetCurveFocalPoint();

                float radius = -1f;
                foreach (var connection in _connections)
                {
                    if (connection.Enabled)
                    {
                        radius = connection.Position * _size.x;
                        break;
                    }
                }

                if (radius > 0f)
                {
                    Gizmos.color = Color.cyan;
                    ExtraGizmos.DrawGizmosCircle(centerPoint, Vector3.up, radius, 20);
                }

            }
        }

        private Vector3 GetCurveFocalPoint()
        {
            Vector3 centerPoint = Vector3.zero;
            switch (_rotation)
            {
                case 0:
                    centerPoint = new Vector3(transform.position.x - _size.x * 0.5f, 0f,
                        transform.position.z - _size.y * 0.5f);
                    break;
                case 1:
                    centerPoint = new Vector3(transform.position.x + _size.x * 0.5f, 0f,
                        transform.position.z - _size.y * 0.5f);
                    break;
                case 2:
                    centerPoint = new Vector3(transform.position.x + _size.x * 0.5f, 0f,
                        transform.position.z + _size.y * 0.5f);
                    break;
                case 3:
                    centerPoint = new Vector3(transform.position.x - _size.x * 0.5f, 0f,
                        transform.position.z + _size.y * 0.5f);
                    break;
            }

            return centerPoint;
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
                    Vector3 bezierPoint = GetTrackPoint(t, 0, out Vector3 tangent);
                    if (step > 0)
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
                    if (connection.ConnectedModule != null)
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
            //draw floor level
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _connections.Length; i++)
            {
                int realIndex = (i + _rotation) % 4;
                if (_connections[realIndex].Enabled)
                {
                    Vector3 connectionPosition = GetConnectionPosition(i);
                    Gizmos.DrawLine(connectionPosition + Vector3.down * _connections[realIndex].FloorLevel,
                        connectionPosition);
                }
            }
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

            if (enabledConnections == 2 && _bezierStartIndex != -1 && _bezierEndIndex != -1)
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
            [Range(0f, 1f)] public float Position = 0.5f;
            public TrackModule ConnectedModule;
            public float FloorLevel = 0f;
        }

        public enum TrackType
        {
            Straight = 0,
            Curve = 1
        }
    }
}
