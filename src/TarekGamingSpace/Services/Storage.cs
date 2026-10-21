using System;
using System.IO;
using System.Runtime.Serialization.Json;
using TarekGamingSpace.Models;

namespace TarekGamingSpace.Services
{
    /// <summary>Saves everything to %AppData%\TarekGamingSpace\state.json.</summary>
    public static class Storage
    {
        // TGS_DATA_DIR lets development runs use a separate folder instead of the real settings.
        public static readonly string Folder =
            Environment.GetEnvironmentVariable("TGS_DATA_DIR") is string dir && dir.Length > 0
                ? dir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TarekGamingSpace");

        static readonly string FilePath = Path.Combine(Folder, "state.json");
        static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(AppState));

        public static bool Exists => File.Exists(FilePath);

        public static AppState Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    // Skip a UTF-8 BOM (Notepad on older Windows 10 adds one); the JSON reader rejects it.
                    byte[] bytes = File.ReadAllBytes(FilePath);
                    int start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                    using (var ms = new MemoryStream(bytes, start, bytes.Length - start))
                    {
                        var state = (AppState)Serializer.ReadObject(ms);
                        state.Normalize();
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not read state.json, starting fresh", ex);
                TryBackupBroken();
            }
            return AppState.CreateDefault();
        }

        public static void Save(AppState state)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                // Write to a temp file first so a power cut never leaves a half-written state.json.
                string tmp = FilePath + ".tmp";
                using (var fs = File.Create(tmp)) Serializer.WriteObject(fs, state);
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
                else File.Move(tmp, FilePath);
            }
            catch (Exception ex)
            {
                Log.Error("Could not save state.json", ex);
            }
        }

        static void TryBackupBroken()
        {
            try { File.Copy(FilePath, FilePath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); }
            catch { /* nothing else to do */ }
        }
    }

    public static class Log
    {
        public static void Error(string message, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Storage.Folder);
                File.AppendAllText(Path.Combine(Storage.Folder, "error.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* logging must never crash the app */ }
        }
    }
}
