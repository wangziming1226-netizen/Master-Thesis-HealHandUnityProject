using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Thesis.Core
{
    public class ServicesLifeProbe : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log($"[LifeProbe] Awake: {name}", this);
        }

        private void OnEnable()
        {
            Debug.Log($"[LifeProbe] OnEnable: {name}", this);
        }

        private void OnDisable()
        {
            Debug.LogWarning($"[LifeProbe] OnDisable: {name}\nActiveScene={SceneManager.GetActiveScene().name}", this);
        }

        private void OnDestroy()
        {
            // Print a stack trace to locate WHO called Destroy(...) on this object.
            // Use Environment.StackTrace so it includes the managed call stack at the time of destruction.
            string stack = Environment.StackTrace;

            Debug.LogError(
                $"[LifeProbe] OnDestroy: {name}\n" +
                $"ActiveScene={SceneManager.GetActiveScene().name}\n" +
                $"Stack:\n{stack}",
                this
            );
        }
    }
}