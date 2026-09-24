using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MovementLogger : MonoBehaviour
{
    private string logFilePath;
    private float logInterval = 0.1f;
    private float timer = 0f;

    private void Start()
    {
        logFilePath = Application.persistentDataPath + "/MovementLog.txt";
        // Write the header
        string header = "Time\tTarget\tPosition\tMode\tSpeed\n";
        File.WriteAllText(logFilePath, header);
        Debug.Log($"[MovementLogger] Started logging to {logFilePath}");
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= logInterval)
        {
            timer = 0f;
            LogData();
        }
    }

    private void LogData()
    {
        if (SolarSystemManager.Instance == null) return;

        string timeStr = Time.time.ToString("F3");
        List<string> lines = new List<string>();

        // Log Player Ship
        if (SolarSystemManager.Instance.playerShip != null)
        {
            SpaceshipFlightController ship = SolarSystemManager.Instance.playerShip;
            Vector3 pos = ship.transform.position;
            string posStr = $"({pos.x:F3}, {pos.y:F3}, {pos.z:F3})";
            lines.Add($"{timeStr}\tPlayerShip\t{posStr}\t{ship.currentMode}\t{ship.currentSpeed:F3}");
        }

        // Log Celestial Bodies
        if (SolarSystemManager.Instance.allBodies != null)
        {
            foreach (var body in SolarSystemManager.Instance.allBodies)
            {
                if (body != null)
                {
                    Vector3 pos = body.transform.position;
                    string posStr = $"({pos.x:F3}, {pos.y:F3}, {pos.z:F3})";
                    lines.Add($"{timeStr}\t{body.bodyName}\t{posStr}\t-\t-");
                }
            }
        }

        if (lines.Count > 0)
        {
            File.AppendAllLines(logFilePath, lines);
        }
    }
}
