using System.Collections.Generic;
using UnityEngine;

public class PlayerRewind : MonoBehaviour
{
    [System.Serializable]
    struct Snapshot
    {
        public Vector3 position;
        public Quaternion rotation;

        public Snapshot(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [Header("Rewind Settings")]
    public float rewindDuration = 5f;
    public bool canrecord;

    [Tooltip("Higher = faster rewind")]
    public int rewindSpeed = 3;

    private List<Snapshot> history =
        new List<Snapshot>();

    private Rigidbody2D rb;

    private bool isRewinding;

    private MonoBehaviour movementScript;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        movementScript =
            GetComponent<Movement>();

        canrecord = true;
    }

    void FixedUpdate()
    {
        if (isRewinding)
        {
            Rewind();
        }
        else if(canrecord)
        {
            Record();
        }
    }

    void Record()
    {
        int maxSnapshots =
            Mathf.RoundToInt(
                rewindDuration /
                Time.fixedDeltaTime);

        if (history.Count > maxSnapshots)
        {
            history.RemoveAt(0);
        }

        history.Add(
            new Snapshot(
                transform.position,
                transform.rotation));
    }

    void Rewind()
    {
        for (int i = 0; i < rewindSpeed; i++)
        {
            if (history.Count <= 0)
            {
                StopRewind();
                return;
            }

            Snapshot snapshot =
                history[history.Count - 1];

            transform.position =
                snapshot.position;

            transform.rotation =
                snapshot.rotation;

            history.RemoveAt(
                history.Count - 1);
        }
    }

    public void StartRewind()
    {
        isRewinding = true;

        // Disable physics
        rb.simulated = false;

        // Disable movement
        if (movementScript != null)
        {
            movementScript.enabled = false;
        }
    }

    void StopRewind()
    {
        isRewinding = false;

        // Enable physics
        rb.simulated = true;

        // Enable movement
        if (movementScript != null)
        {
            movementScript.enabled = true;
        }
    }
}