using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RaceGame
{
    public static class Utils
    {
        /// <summary>
        ///     Add color decoration to a string.
        ///     i.e. "<color=#00 FF00>hello world</color>"
        /// </summary>
        /// <param name="str"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string Color(this string str, Color c)
        {
            var hexColor = ColorUtility.ToHtmlStringRGB(c);
            return Color(str, hexColor);
        }

        /// <summary>
        ///     Add color decoration to a string.
        ///     i.e. "<color=#00 FF00>hello world</color>"
        /// </summary>
        /// <param name="str"></param>
        /// <param name="hexColor"></param>
        /// <returns></returns>
        public static string Color(this string str, string hexColor)
        {
            return $"<color=#{hexColor}>{str}</color>";
        }

        /// <summary>
        ///     Returns whether or not this Rectransform contains another completely or in certain axis.
        /// </summary>
        public static bool ContainsRectTransform(this RectTransform rt, RectTransform otherRectTransform,
            Axis axis = Axis.None)
        {
            var r = new Vector3[4];
            rt.GetWorldCorners(r);

            var a = new Vector3[4];
            otherRectTransform.GetWorldCorners(a);

            switch (axis)
            {
                case Axis.X:
                    return r[0].x <= a[0].x && r[2].x >= a[2].x;
                case Axis.Y:
                    return r[0].y <= a[0].y && r[2].y >= a[2].y;
                default:
                    return r[0].x <= a[0].x && r[0].y <= a[0].y && r[2].x >= a[2].x && r[2].y >= a[2].y;
            }
        }

        /// <summary>
        ///     Calls a method delayed by a certain number of frames
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="frames">Amount of frames to wait</param>
        public static Coroutine DelayedCallInFrames(this MonoBehaviour caller, int frames, Action method)
        {
            if (caller.isActiveAndEnabled)
            {
                return caller.StartCoroutine(DelayedCallInFramesCo(frames, method));
            }

            return null;
        }

        /// <summary>
        ///     Calls a method delayed in seconds
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="method"></param>
        /// <param name="frames">Time to wait in seconds</param>
        public static Coroutine DelayedCallInSeconds(this MonoBehaviour caller, float seconds, Action method,
            bool unscalled = false)
        {
            if (caller.isActiveAndEnabled)
            {
                return caller.StartCoroutine(DelayedCallInSecondsCo(seconds, method, unscalled));
            }

            return null;
        }

        /// <summary>
        ///     Calculates the minimum distance between a ray (i.e. the camera direction), and a point.
        ///     Useful for clicking stuff on screen without using an actual raycast.
        /// </summary>
        /// <param name="ray">Input ray.</param>
        /// <param name="point">Input point</param>
        /// <returns></returns>
        public static float DistanceToRay(Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }

        /// <summary>
        ///     Returns a random element within an Array.
        /// </summary>
        public static T GetRandomElement<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
            {
                return default;
            }

            return array[Random.Range(0, array.Length)];
        }

        /// <summary>
        ///     Returns a random element within a List.
        /// </summary>
        public static T GetRandomElement<T>(this IEnumerable<T> list)
        {
            IEnumerable<T> enumerable = list as T[] ?? list.ToArray();
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default;
            }

            int count = 1;
            while (enumerator.MoveNext())
            {
                count++;
            }
            enumerator = enumerable.GetEnumerator();
            int randomIndex = Random.Range(0, count);
            int i = 0;
            while (i <= randomIndex)
            {
                enumerator.MoveNext();
                i++;
            }

            T returnValue = enumerator.Current;
            enumerator.Dispose();
            return returnValue;
        }

        /// <summary>
        ///     Get relative position of this RectTransform as if it was child of another transform.
        /// </summary>
        /// <param name="thisRT">RectTransform whose anchored position will be reevaluated</param>
        /// <param name="newParentRT">RectTransform taken as reference "parent" to reevaluate the anchored position.</param>
        /// <returns></returns>
        public static Vector2 GetRelativeAnchoredPosition(this RectTransform thisRT, RectTransform newParentRT,
            UnityEngine.Camera camera)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(thisRT.TransformPoint(Vector3.zero));
            Vector2 originLocalPoint = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(newParentRT, screenPoint,
                camera, out originLocalPoint);
            return originLocalPoint;
        }

        /// <summary>
        ///     Returns whether or not this Rectransform overlaps another in one or both axis.
        /// </summary>
        public static bool OverlapsRectTransform(this RectTransform rt, RectTransform otherRectTransform,
            Axis axis = Axis.None)
        {
            var r = new Vector3[4];
            rt.GetWorldCorners(r);

            var a = new Vector3[4];
            otherRectTransform.GetWorldCorners(a);

            switch (axis)
            {
                case Axis.X:
                    return (r[0].x > a[0].x && r[0].x < a[2].x) || (a[0].x > r[0].x && a[0].x < r[2].x);
                case Axis.Y:
                    return (r[0].y > a[0].y && r[0].y < a[2].y) || (a[0].y > r[0].y && a[0].y < r[2].y);
                default:
                    return rt.OverlapsRectTransform(otherRectTransform, Axis.X) &&
                           rt.OverlapsRectTransform(otherRectTransform, Axis.Y);
            }
        }

        /// <summary>
        ///     Re-scales a value between a new float range.
        /// </summary>
        public static float Remap(float oldMin, float oldMax, float newMin, float newMax, float oldValue,
            bool clamp = true)
        {
            bool invert = false;
            if (oldMin > oldMax)
            {
                (oldMin, oldMax) = (oldMax, oldMin); //fancy swap.
                invert = true;
            }

            if (clamp)
            {
                oldValue = Mathf.Clamp(oldValue, oldMin, oldMax);
            }

            float ret = (oldValue - oldMin) * (newMax - newMin) / (oldMax - oldMin) + newMin;
            if (invert)
            {
                ret = 1 - ret;
            }
            return ret;
        }

        /// <summary>
        ///     Get a texture 2d from a render texture.
        /// </summary>
        /// <param name="rTex"></param>
        /// <returns></returns>
        public static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            var tex = new Texture2D(rTex.height, rTex.width, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        private static IEnumerator DelayedCallInFramesCo(int frames, Action method)
        {
            var frameCount = 0;
            while (frameCount < frames)
            {
                yield return null;
                frameCount++;
            }

            method.Invoke();
        }

        private static IEnumerator DelayedCallInSecondsCo(float seconds, Action method, bool unscaled = false)
        {
            var startTime = unscaled ? Time.unscaledTime : Time.time;
            float time = 0;
            do
            {
                yield return null;
                time = unscaled ? Time.unscaledTime : Time.time;
            } while (time < startTime + seconds);

            method.Invoke();
        }
        
        public static void Shuffle<T> (this T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = Random.Range(0, n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
        
        /// <summary>
        /// 3D Parabola normalized movement for use with DoTween
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="end">End point</param>
        /// <param name="height">Parabola height at its midpoint</param>
        /// <param name="t">Normalized point to sample</param>
        /// <returns>Point along the parabola for t</returns>
        public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
        {
            float Func(float x) => 4 * (-height * x * x + height * x);

            var mid = Vector3.LerpUnclamped(start, end, t);

            return new Vector3(mid.x, Func(t) + Mathf.LerpUnclamped(start.y, end.y, t), mid.z);
        }
        
        /// <summary>
        /// Rotates a point around another point and an axis.
        /// </summary>
        /// <param name="point">The point you want to rotate around</param>
        /// <param name="anchorPoint">The reference anchor point</param>
        /// <param name="axis">The axis around which the rotation will be performed</param>
        /// <param name="angle">Angle that determines the rotation amplitude</param>
        /// <returns></returns>
        public static Vector3 RotateAround(Vector3 point, Vector3 anchorPoint, Vector3 axis, float angle)
        {
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            var v = point - anchorPoint;
            v = q * v;
            v = anchorPoint + v;
            
            return v;
        }
        
        /// <summary>
        /// Converts from anchored position to normalized position in a ScrollRect.
        /// Useful for focusing on a specific element in a ScrollRect.
        /// </summary>
        /// <param name="scrollRect"></param>
        /// <param name="anchoredPositionY"></param>
        /// <returns>Normalized cursor vertical position</returns>
        public static float GetScrollCursorYPosition(this ScrollRect scrollRect, float anchoredPositionY)
        {
            float viewportHeight = scrollRect.viewport.rect.height;
            float contentHeight = scrollRect.content.rect.height;
            float normalizedPosition = Remap(viewportHeight * 0.5f, contentHeight - viewportHeight * 0.5f, 0, 1, anchoredPositionY);
            return normalizedPosition;
        }



#if UNITY_EDITOR
        /// <summary>
        ///     Allows to save the current contents of a render texture to a png.
        ///     Useful for debugging.
        /// </summary>
        [MenuItem("Assets/Save Render Texture to png")]
        private static void SaveRenderTexture()
        {
            var rt = Selection.activeObject as RenderTexture;
            var bytes = rt.ToTexture2D().EncodeToPNG();
            var completePath =
                Path.Combine(Application.dataPath, $"RenderTexture - {DateTime.Now.Ticks}.png");
            File.WriteAllBytes(completePath, bytes);
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Save Render Texture to png", true)]
        private static bool SaveRenderTextureValidation()
        {
            if (Selection.activeObject != null)
            {
                return Selection.activeObject is RenderTexture;
            }

            return false;
        }
#endif
    }
}