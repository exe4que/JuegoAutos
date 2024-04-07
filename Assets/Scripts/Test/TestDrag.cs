using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RaceGame.Test
{
    public class TestDrag : MonoBehaviour
    {
        public float Speed = 5f;
        public float Strength = 10f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //Move along z
            this.transform.position += new Vector3(0f, 0f, Speed * Time.deltaTime);
            
            //Drag
            float baseRotation = Mathf.Sin(Time.time * Strength);
            float rotation = baseRotation * Strength;
            this.transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        }
    }
}
