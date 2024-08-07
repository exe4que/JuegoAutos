using System;
using RaceGame.Gameplay;
using UnityEngine;

namespace RaceGame.Events
{
    public static class GameplayEvents
    {
        // params: car id, car offset, car transform
        public static Action<int, float, Transform> OnCarRegistered;
    }
}
