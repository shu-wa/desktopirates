using System;
using System.IO;
using UnityEngine;

namespace Desktopirates
{
    public sealed class SaveSystem
    {
        public string SavePath { get; }

        public SaveSystem(string savePath = null)
        {
            SavePath = savePath ?? Path.Combine(Application.persistentDataPath, "voyage.dprs");
        }

        public GameState LoadOrNew()
        {
            try
            {
                return File.Exists(SavePath) ? CompactSaveCodec.Deserialize(File.ReadAllBytes(SavePath)) : new GameState();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save recovery: {exception.Message}");
                string broken = SavePath + ".broken";
                if (File.Exists(SavePath)) File.Copy(SavePath, broken, true);
                return new GameState();
            }
        }

        public int Save(GameState state)
        {
            byte[] bytes = CompactSaveCodec.Serialize(state);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temp = SavePath + ".tmp";
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak", true);
            else File.Move(temp, SavePath);
            return bytes.Length;
        }
    }
}
