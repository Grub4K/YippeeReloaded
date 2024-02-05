using UnityEngine;
using UnityEngine.Networking;

namespace LC.Grub4K.YippeeReloaded;

record struct HoarderBugInstance(HoarderBugAI AI, AudioClip[] OriginalSFX);

public static class AudioReplacer {
    private static AudioClip[] CustomSFX = Array.Empty<AudioClip>();
    private static List<HoarderBugInstance> BugInstances = new();

    private static bool _enabled = false;
    public static bool IsEnabled {
        get => _enabled;
        set {
            if (value == _enabled)
            {
                return;
            }

            foreach (var instance in BugInstances) {
                instance.AI.chitterSFX = value ? CustomSFX : instance.OriginalSFX;
            }
            _enabled = value;
            if (!value)
            {
                CustomSFX = Array.Empty<AudioClip>();
            }
            Plugin.Log.LogDebug($"bugs={BugInstances.Count}, sfxs={CustomSFX.Length}, enabled={_enabled}");
        }
    }

    public static void AddBug(HoarderBugAI bug) {
        BugInstances.Add(new(bug, bug.chitterSFX));
        if (IsEnabled) {
            bug.chitterSFX = CustomSFX;
            Plugin.Log.LogDebug($"bugs={BugInstances.Count}, sfxs={CustomSFX.Length}, enabled={IsEnabled}");
        }
    }

    public static void RemoveBug(HoarderBugAI bug) {
        var instance = BugInstances.SingleOrDefault(instance => ReferenceEquals(instance.AI, bug));
        BugInstances.Remove(instance);
        Plugin.Log.LogDebug($"bugs={BugInstances.Count}, sfxs={CustomSFX.Length}, enabled={IsEnabled}");
    }

    public static void RemoveAllBugs() {
        BugInstances.Clear();
            Plugin.Log.LogDebug($"bugs={BugInstances.Count}, sfxs={CustomSFX.Length}, enabled={IsEnabled}");
    }

    public static void Load(string path) {
        if (string.IsNullOrEmpty(path)) {
            Plugin.Log.LogInfo("Using original sfx");
            IsEnabled = false;
            return;
        }
        // TODO: GetInvalidPathChars()
        if (!Directory.Exists(path)) {
            Plugin.Log.LogError($"Path does not exist: {path}");
            IsEnabled = false;
            return;
        }
        Plugin.Log.LogDebug($"Loading sfx from {path}");
        List<AudioClip> customSFX = new();
        var directories = Directory.GetFiles(path);
        var remaining = directories.Length;
        foreach (var filePath in directories) {
            var audioType = Path.GetExtension(filePath) switch {
                ".mp3" or ".mp2" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                ".it" => AudioType.IT,
                ".s3m" => AudioType.S3M,
                ".aiff" or ".aif" => AudioType.AIFF,
                ".mod" => AudioType.MOD,
                ".xm" => AudioType.XM,
                _ => AudioType.UNKNOWN,
            };
            if (audioType == AudioType.UNKNOWN)
            {
                remaining -= 1;
                continue;
            }
            var fileUri = $"file:///{filePath.Replace('\\', '/')}";
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType);
            www.SendWebRequest().completed += (action) => {
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Plugin.Log.LogError($"Error loading file: {www.error}");
                }
                else
                {
                    try
                    {
                        customSFX.Add(DownloadHandlerAudioClip.GetContent(www));
                        Plugin.Log.LogDebug($"Loaded sfx from file: {filePath}");
                    }
                    catch (Exception error)
                    {
                        Plugin.Log.LogError($"Error loading file: {error}");
                    }
                }
                www.Dispose();
                remaining -= 1;
                if (remaining == 0) {
                    CustomSFX = customSFX.ToArray();
                    Plugin.Log.LogInfo($"Loaded {CustomSFX.Length} custom sound(s)");
                    IsEnabled = true;
                }
            };
        }
    }
}
