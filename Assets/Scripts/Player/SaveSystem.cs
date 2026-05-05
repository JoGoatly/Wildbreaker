using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class SaveSystem
{
    private static string savePath => Path.Combine(Application.persistentDataPath, "savegame.csv");

    public static void SaveCheckpoint(int checkpointIndex)
    {
        int currentHighest = LoadCheckpointIndex();
        if (checkpointIndex <= currentHighest) return;

        List<string> lines = new List<string>
        {
            $"Checkpoint,{checkpointIndex}",
            $"Level,{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}"
        };

        File.WriteAllLines(savePath, lines);
        Debug.Log($"Spiel gespeichert — Checkpoint {checkpointIndex}");
    }

    public static int LoadCheckpointIndex()
    {
        if (!File.Exists(savePath)) return -1;

        string[] lines = File.ReadAllLines(savePath);

        foreach (string line in lines)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2 && parts[0] == "Checkpoint")
                return int.Parse(parts[1]);
        }

        return -1;
    }

    public static string LoadLevelName()
    {
        if (!File.Exists(savePath)) return "";

        string[] lines = File.ReadAllLines(savePath);

        foreach (string line in lines)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2 && parts[0] == "Level")
                return parts[1];
        }

        return "";
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Spielstand gelöscht.");
        }
    }

    public static bool SaveExists()
    {
        return File.Exists(savePath);
    }
}