using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public class GameAnalytics : MonoBehaviour
{
    static GameAnalytics instance;
    static bool ready;
    static int attempt;
    static readonly List<CustomEvent> pending = new List<CustomEvent>();

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        if (instance != this) return;
        await UnityServices.InitializeAsync();
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = ConsentStatus.Granted,
        });
        ready = true;

        foreach (CustomEvent e in pending)
            AnalyticsService.Instance.RecordEvent(e);
        pending.Clear();
    }

    // Call once per round (first load and every restart).
    public static void RoundStarted()
    {
        attempt++;
        Record("roundStarted", "attempt", attempt);
    }

    public static void Record(string eventName, string param, int value)
    {
        CustomEvent e = new CustomEvent(eventName) { { param, value } };
        if (!ready) { pending.Add(e); return; }
        AnalyticsService.Instance.RecordEvent(e);
    }

    public static void Record(string eventName, string param, string value)
    {
        CustomEvent e = new CustomEvent(eventName) { { param, value } };
        if (!ready) { pending.Add(e); return; }
        AnalyticsService.Instance.RecordEvent(e);
    }

    public static void Flush()
    {
        if (ready) AnalyticsService.Instance.Flush();
    }
}