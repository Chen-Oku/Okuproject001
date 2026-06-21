using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Escucha eventos de GameManager/ScoreManager/ZoneManager (no los referencia ninguno de ellos)
// y vuelca un log de texto + un CSV de resumen por run en Application.persistentDataPath.
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    string logPath;
    string csvPath;

    DateTime sessionStartTimestamp;
    int runIndex;
    DateTime runStartTimestamp;
    readonly List<string> runZones = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        logPath = Path.Combine(Application.persistentDataPath, "analytics_log.txt");
        csvPath = Path.Combine(Application.persistentDataPath, "analytics_summary.csv");
        EnsureCsvHeader();

        sessionStartTimestamp = DateTime.Now;
        WriteLog("session_start");
    }

    void OnEnable()
    {
        GameManager.OnRunStart     += HandleRunStart;
        GameManager.OnRunEnd       += HandleRunEnd;
        ScoreManager.OnNewComboMax += HandleNewComboMax;
        ZoneManager.OnZoneReached  += HandleZoneReached;
        AbyssEvents.OnSurgeActivated   += HandleSurgeStart;
        AbyssEvents.OnSurgeDeactivated += HandleSurgeEnd;
    }

    void OnDisable()
    {
        GameManager.OnRunStart     -= HandleRunStart;
        GameManager.OnRunEnd       -= HandleRunEnd;
        ScoreManager.OnNewComboMax -= HandleNewComboMax;
        ZoneManager.OnZoneReached  -= HandleZoneReached;
        AbyssEvents.OnSurgeActivated   -= HandleSurgeStart;
        AbyssEvents.OnSurgeDeactivated -= HandleSurgeEnd;
    }

    void HandleRunStart()
    {
        runIndex++;
        runStartTimestamp = DateTime.Now;
        runZones.Clear();
        WriteLog("run_start", $"run_index={runIndex}");
    }

    void HandleRunEnd(int depth, string cause, float durationSeconds)
    {
        WriteLog("run_end", $"run_index={runIndex}", $"depth={depth}", $"cause={cause}", $"duration_s={durationSeconds:F2}");

        int comboMax = ScoreManager.Instance != null ? ScoreManager.Instance.ComboMax : 0;
        AppendCsvRow(runIndex, runStartTimestamp, depth, cause, durationSeconds, comboMax, runZones.Count);
    }

    void HandleNewComboMax(int newMax) => WriteLog("combo_max", $"run_index={runIndex}", $"value={newMax}");

    void HandleZoneReached(string zoneName, int depth)
    {
        runZones.Add(zoneName);
        WriteLog("zone_reached", $"run_index={runIndex}", $"zone={zoneName}", $"depth={depth}");
    }

    void HandleSurgeStart()
    {
        int depth = ZoneManager.Instance != null ? ZoneManager.Instance.RingsPassed : 0;
        WriteLog("surge_started", $"run_index={runIndex}", $"depth={depth}");
    }

    void HandleSurgeEnd()
    {
        int depth = ZoneManager.Instance != null ? ZoneManager.Instance.RingsPassed : 0;
        WriteLog("surge_ended", $"run_index={runIndex}", $"depth={depth}");
    }

    // ── Stub: near_miss es mecanica del plan V1.5, aun no implementada. ──
    // TODO(V1.5): invocar desde el sistema de near-miss cuando exista; en ese momento
    // conviene invertir esto a un evento (como GameManager.OnRunStart) en vez de la llamada directa.
    public static void LogNearMiss(int depth, int comboAtMiss)
    {
        if (Instance == null) return;
        Instance.WriteLog("near_miss", $"depth={depth}", $"combo={comboAtMiss}");
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) WriteSessionLength();
    }

    void OnApplicationQuit() => WriteSessionLength();

    void WriteSessionLength()
    {
        double seconds = (DateTime.Now - sessionStartTimestamp).TotalSeconds;
        WriteLog("session_length", $"seconds={seconds:F2}");
    }

    void WriteLog(string eventName, params string[] fields)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {eventName} {string.Join(" ", fields)}".TrimEnd();
        try { File.AppendAllText(logPath, line + Environment.NewLine); }
        catch (IOException e) { Debug.LogWarning($"AnalyticsManager: no se pudo escribir el log ({e.Message})"); }
    }

    void EnsureCsvHeader()
    {
        if (File.Exists(csvPath)) return;
        try { File.AppendAllText(csvPath, "run_index,start_timestamp,depth,cause,duration_s,combo_max,zones_reached" + Environment.NewLine); }
        catch (IOException e) { Debug.LogWarning($"AnalyticsManager: no se pudo crear el CSV ({e.Message})"); }
    }

    void AppendCsvRow(int index, DateTime startTimestamp, int depth, string cause, float durationSeconds, int comboMax, int zonesReached)
    {
        string row = string.Join(",",
            index.ToString(CultureInfo.InvariantCulture),
            startTimestamp.ToString("o", CultureInfo.InvariantCulture),
            depth.ToString(CultureInfo.InvariantCulture),
            cause,
            durationSeconds.ToString("F2", CultureInfo.InvariantCulture),
            comboMax.ToString(CultureInfo.InvariantCulture),
            zonesReached.ToString(CultureInfo.InvariantCulture));
        try { File.AppendAllText(csvPath, row + Environment.NewLine); }
        catch (IOException e) { Debug.LogWarning($"AnalyticsManager: no se pudo escribir el CSV ({e.Message})"); }
    }
}
