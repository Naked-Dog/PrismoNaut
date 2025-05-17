using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class LavaManager : MonoBehaviour
{
    [SerializeField] private Transform lavaTransform;
    [SerializeField] private float finalHeight = -1f;
    [SerializeField] private float initialHeight = -10f;
    [SerializeField] private float riseDuration = -1f;
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private List<LavaPositions> lavaPositions = new List<LavaPositions>();
    [SerializeField] private ShakeScriptable shakeProfile;
    [SerializeField] private DialogueRunner dialogueRunner;

    private bool eventStarted = false;
    private bool eventFinished = false;
    public static LavaManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        initialHeight = lavaTransform.transform.position.y;
    }

    void Update()
    {
        if (eventStarted)
        {
            RiseLava();
        }
    }

    public void moveToLavaGoal()
    {
        lavaPositions.RemoveAt(0);
        timeElapsed = 0;
        initialHeight = lavaTransform.transform.position.y;
    }

    public void StartLava()
    {
        if (eventStarted) return;
        eventStarted = true;
        AudioManager.Instance.Play3DSoundAttached(LevelEventsSoundsEnum.Lava,lavaTransform,1,true);
        if(PlatformManager.Instance) PlatformManager.Instance.StartPlatforms();
    }

    public void Shake()
    {
        ShakeManager.Instance.CameraShake(shakeProfile);
        AudioManager.Instance.Play2DSound(LevelEventsSoundsEnum.Earthquake);
    }

    private void RiseLava()
    {
        timeElapsed += Time.deltaTime;

        riseDuration = lavaPositions[0].time;
        finalHeight = lavaPositions[0].position;

        if (timeElapsed <= riseDuration)
        {
            float riseAmount = Mathf.Lerp(initialHeight, finalHeight, timeElapsed / riseDuration);
            lavaTransform.transform.position = new Vector3(lavaTransform.transform.position.x, riseAmount, lavaTransform.transform.position.z);
        }
        else
        {
            SetLavaGoal();
        }
    }
    private void SetLavaGoal()
    {
        lavaTransform.transform.position = new Vector3(lavaTransform.transform.position.x, finalHeight, lavaTransform.transform.position.z);
        initialHeight = finalHeight;
        timeElapsed = 0;
        lavaPositions.RemoveAt(0);

        if (lavaPositions.Count <= 0)
        {
            eventStarted = false;
            eventFinished = true;
            AudioManager.Instance.Stop(LevelEventsSoundsEnum.Lava);
            return;
        }
    }
    public void FinishEvent()
    {
        if (lavaPositions.Count == 0) return;

        eventStarted = true;

        while (lavaPositions.Count > 1)
        {
            lavaPositions.RemoveAt(0);
        }
        initialHeight = lavaTransform.transform.position.y;
        timeElapsed = 0;
        riseDuration = lavaPositions[0].time;
        finalHeight = lavaPositions[0].position;
    }
}

[Serializable]
public class LavaPositions
{
    public float position;
    public float time;
}