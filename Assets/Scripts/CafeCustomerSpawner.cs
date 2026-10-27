using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Spawns customers at this object's position (the entrance) while there is a free seat.
/// Each customer gets a random sprite sheet and a random free seat, walks there around the
/// furniture, stays 5-7 seconds, and walks back out through the entrance.
/// </summary>
public sealed class CafeCustomerSpawner : MonoBehaviour
{
    [Tooltip("The customer object you built (root with CafeCustomer + IsoSortable, child with SpriteRenderer).")]
    [SerializeField] private CafeCustomer customerPrefab;

    [Tooltip("3x4 sprite sheets, 48x64 frames. One is picked at random for each customer.")]
    [SerializeField] private Texture2D[] customerSheets;

    [Tooltip("One empty object per stool, placed where the customer's feet should stand.")]
    [SerializeField] private Transform[] seats;

    [Tooltip("Floor tilemap. Leave empty to use the object named 'floor'.")]
    [SerializeField] private Tilemap floor;

    [SerializeField, Min(0f)] private float firstDelay = 1f;
    [SerializeField, Min(0f)] private float minGap = 2f;
    [SerializeField, Min(0f)] private float maxGap = 5f;
    [SerializeField, Min(0f)] private float minStay = 5f;
    [SerializeField, Min(0f)] private float maxStay = 7f;

    [Tooltip("Size of a customer's feet, used to keep paths clear of furniture.")]
    [SerializeField] private Vector2 feetClearance = new Vector2(0.6f, 0.3f);

    private CafeNavGrid grid;
    private bool[] taken;
    private float timer;

    private void Start()
    {
        if (floor == null)
        {
            var go = GameObject.Find("floor");
            if (go != null) floor = go.GetComponent<Tilemap>();
        }
        if (floor != null)
            grid = new CafeNavGrid(floor, feetClearance);
        else
            Debug.LogWarning("CafeCustomerSpawner: no floor tilemap found, customers will walk in straight lines.");

        taken = new bool[seats.Length];
        timer = firstDelay;
    }

    private void Update()
    {
        if (customerPrefab == null || customerSheets == null || customerSheets.Length == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        int seat = RandomFreeSeat();
        if (seat < 0) { timer = 0.5f; return; }   // every seat is taken, check again soon

        Spawn(seat);
        timer = Random.Range(minGap, maxGap);
    }

    private int RandomFreeSeat()
    {
        int free = 0;
        for (int i = 0; i < taken.Length; i++) if (!taken[i] && seats[i] != null) free++;
        if (free == 0) return -1;
        int pick = Random.Range(0, free);
        for (int i = 0; i < taken.Length; i++)
            if (!taken[i] && seats[i] != null) { if (pick == 0) return i; pick--; }
        return -1;
    }

    private void Spawn(int seatIndex)
    {
        var sheet = customerSheets[Random.Range(0, customerSheets.Length)];
        var customer = Instantiate(customerPrefab, transform.position, Quaternion.identity);
        taken[seatIndex] = true;
        customer.Init(this, grid, sheet, seatIndex, seats[seatIndex].position, transform.position,
                      Random.Range(minStay, maxStay));
    }

    public void FreeSeat(int i)
    {
        if (taken != null && i >= 0 && i < taken.Length) taken[i] = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        if (seats == null) return;
        Gizmos.color = Color.cyan;
        foreach (var s in seats)
            if (s != null) Gizmos.DrawWireSphere(s.position, 0.2f);
    }
}
