using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RaceGame.Test
{
    public class TestDragCamera : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset;

        private void Update()
        {
            Vector3 position = this.transform.position;
            position = Target.position;
            this.transform.position = position + Offset;
        }
    }
}
