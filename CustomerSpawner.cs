using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject normalPrefab;
    public GameObject anomalyPrefab;
    public Transform[] seats;       // Seat1..Seat6
    public Transform exit;

    public float firstDelay = 1f;
    public float minGap = 1f, maxGap = 10f;

    // The first anomaly is customer #1..#5 (random). After that one every 3-5 customers.
    int spawnedCount;
    int nextAnomalyAt;
    bool[] taken;
    float timer;

    static readonly Color[] tableColors = {
        new Color(0.95f,0.35f,0.35f), new Color(0.35f,0.65f,0.95f), new Color(0.40f,0.85f,0.45f),
        new Color(0.98f,0.85f,0.30f), new Color(0.75f,0.45f,0.95f), new Color(0.98f,0.60f,0.25f) };

    void Start()
    {
        taken = new bool[seats.Length];
        nextAnomalyAt = Random.Range(1, 6);      // 1..5 inclusive
        timer = firstDelay;
    }

    void Update()
    {
        if (GameManager.I == null || GameManager.I.state != GameManager.State.Playing) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        int free = FreeSeatIndex();
        if (free < 0) { timer = 0.5f; return; }      // customers only enter if a table is free

        Spawn(free);
        timer = Random.Range(minGap, maxGap);
    }

    int FreeSeatIndex()
    {
        // random free seat
        int count = 0;
        for (int i = 0; i < taken.Length; i++) if (!taken[i]) count++;
        if (count == 0) return -1;
        int pick = Random.Range(0, count);
        for (int i = 0; i < taken.Length; i++)
            if (!taken[i]) { if (pick == 0) return i; pick--; }
        return -1;
    }

    void Spawn(int seatIdx)
    {
        spawnedCount++;
        bool anomaly = spawnedCount == nextAnomalyAt;
        if (anomaly) nextAnomalyAt = spawnedCount + Random.Range(3, 6);

        var prefab = anomaly ? anomalyPrefab : normalPrefab;
        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        var c = go.GetComponent<Customer>();
        c.isAnomaly = anomaly;     // safety: prefab flag is overridden by the spawner
        taken[seatIdx] = true;
        c.Init(this, seatIdx, seats[seatIdx].position, exit.position, tableColors[seatIdx % tableColors.Length]);
    }

    public void FreeSeat(int i) { if (i >= 0 && i < taken.Length) taken[i] = false; }
}
