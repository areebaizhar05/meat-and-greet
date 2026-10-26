using UnityEngine;

// Anomaly quirk: random jittery rotation bursts + a creepy stretch. Doesn't touch position,
// so it never fights with the customer's walking.
public class Twitch : MonoBehaviour
{
    public float interval = 0.7f;
    public float angle = 9f;
    float timer, burst;
    Vector3 baseScale; bool init;

    void Update()
    {
        if (!init) { baseScale = transform.localScale; init = true; }
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = Random.Range(interval * 0.4f, interval * 1.6f); burst = 0.12f; }
        if (burst > 0f)
        {
            burst -= Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(-angle, angle));
            var s = transform.localScale; s.y = baseScale.y * (1f + Random.Range(0f, 0.08f));
            transform.localScale = s;
        }
        else
        {
            transform.rotation = Quaternion.identity;
            // keep x sign (Customer flips x when walking)
            var s = transform.localScale; s.y = baseScale.y; transform.localScale = s;
        }
    }
}
