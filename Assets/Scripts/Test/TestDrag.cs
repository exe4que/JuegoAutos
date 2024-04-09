using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RaceGame.Test
{
    public class TestDrag : MonoBehaviour
    {
        public float AnimationDuration = 5f;
        public float Speed = 5f;
        [Range(0f, 1f)] public float AnimationTime = 0f;
        public AnimationCurve AmplitudeCurve;
        public AnimationCurve FrequencyCurve;

        private float _timer = 0f;
        private Tween _tween;
        // Update is called once per frame
        void Update()
        {
            //Move along z
            this.transform.position += new Vector3(0f, 0f, Speed * Time.deltaTime);
            
            //Drag
            _timer += Time.deltaTime * FrequencyCurve.Evaluate(AnimationTime);
            float baseRotation = Mathf.Sin(_timer);
            float rotation = baseRotation * AmplitudeCurve.Evaluate(AnimationTime);
            this.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        [Button]
        public void PlayAnimation()
        {
            AnimationTime = 0f;
            _timer = Random.value;
            _tween?.Kill();
            _tween = DOTween.To(() => AnimationTime, x => AnimationTime = x, 1f, AnimationDuration).SetEase(Ease.Linear);
        }
    }
}
