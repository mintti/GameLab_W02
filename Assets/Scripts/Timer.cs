using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float duration;
    private System.Action callback;
    private bool isRunning = false;
    private float elapsedTime = 0f;

    public static Timer CreateTimer(GameObject parent, float duration, System.Action callback)
    {
        var timerObject = new GameObject("TimerInstance");
        timerObject.transform.SetParent(parent.transform);
        var timer = timerObject.AddComponent<Timer>();
        timer.Initialize(duration, callback);
        return timer;
    }

    private void Initialize(float duration, System.Action callback)
    {
        this.duration = duration;
        this.callback = callback;
        isRunning = true;
    }

    private void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= duration)
            {
                isRunning = false;
                callback?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
