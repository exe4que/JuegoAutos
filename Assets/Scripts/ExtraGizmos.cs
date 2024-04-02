using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtraGizmos
{
    public static void DrawArrow(Vector3 pos, Vector3 direction, float arrowLength, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f) {
        var arrowTip = pos + direction * arrowLength;
        Gizmos.DrawLine(pos, arrowTip);
 
        Camera c = Camera.current;
        if (c == null) return;
        Vector3 right = Quaternion.LookRotation(direction, c.transform.forward) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction, c.transform.forward) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawLine(arrowTip, arrowTip + right * arrowHeadLength);
        Gizmos.DrawLine(arrowTip, arrowTip + left * arrowHeadLength);
    }
 
    public static void DrawGizmosCircle(Vector3 pos, Vector3 normal, float radius, int numSegments)
    {
        Vector3 temp = (normal.x < normal.z) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
        Vector3 forward = Vector3.Cross(normal, temp).normalized;
        Vector3 right = Vector3.Cross(forward, normal).normalized;

        Vector3 prevPt = pos + (forward * radius);
        float angleStep = (Mathf.PI * 2f) / numSegments;
        for (int i = 0; i < numSegments; i++)
        {
            float angle = (i == numSegments - 1) ? 0f : (i + 1) * angleStep;
            Vector3 nextPtLocal = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Vector3 nextPt = pos + (right * nextPtLocal.x) + (forward * nextPtLocal.z);
            Gizmos.DrawLine(prevPt, nextPt);
            prevPt = nextPt;
        }
    }

}
