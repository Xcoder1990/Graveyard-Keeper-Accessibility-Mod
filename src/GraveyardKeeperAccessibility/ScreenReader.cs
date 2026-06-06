using System.Diagnostics;
using System.IO;

namespace GraveyardKeeperAccessibility;

internal static class ScreenReader
{
    private static bool _tolkAvailable;
    private static bool _sapiAvailable;
    private static bool _macAvailable;
    private static Process _sapiProcess;
    private static Process _macProcess;
    private static StreamWriter _sapiStdin;
    private static string _lastMenuText = "";
    private static ManualLogSource _log;

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Load();

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Unload();

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.Bool)] bool trySAPI);

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Tolk_Output(
        [MarshalAs(UnmanagedType.LPWStr)] string str,
        [MarshalAs(UnmanagedType.Bool)] bool interrupt);

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    private static extern string Tolk_DetectScreenReader();

    [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Tolk_IsLoaded();

    internal static void Init(ManualLogSource log)
    {
        _log = log;

        _macAvailable = InitMacSay();
        if (_macAvailable) return;

        _tolkAvailable = InitTolk();
        if (!_tolkAvailable)
            _sapiAvailable = InitSapi();

        if (!_tolkAvailable && !_sapiAvailable && !_macAvailable)
            log.LogError("No TTS output available");
    }

    private static bool InitMacSay()
    {
        try
        {
            if (File.Exists("/usr/bin/say"))
            {
                _log.LogInfo("Mac 'say' command found");
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool InitTolk()
    {
        try
        {
            Tolk_TrySAPI(true);
            Tolk_Load();
            if (!Tolk_IsLoaded()) return false;

            var reader = Tolk_DetectScreenReader();
            _log.LogInfo($"Tolk screen reader: {reader ?? "SAPI fallback"}");
            return true;
        }
        catch (DllNotFoundException)
        {
            _log.LogInfo("Tolk.dll not found, trying SAPI fallback");
            return false;
        }
        catch (Exception ex)
        {
            _log.LogInfo($"Tolk not available: {ex.Message}");
            return false;
        }
    }

    private static bool InitSapi()
    {
        try
        {
            var vbsPath = Path.Combine(Path.GetTempPath(), "gk_accessibility_tts.vbs");
            File.WriteAllText(vbsPath,
                "Set v=CreateObject(\"SAPI.SpVoice\")\r\n" +
                "Do While Not WScript.StdIn.AtEndOfStream\r\n" +
                "On Error Resume Next\r\n" +
                "s=WScript.StdIn.ReadLine\r\n" +
                "If Len(s)>0 Then v.Speak s,3\r\n" +
                "On Error Goto 0\r\n" +
                "Loop\r\n");

            _sapiProcess = new Process();
            _sapiProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "cscript.exe",
                Arguments = "//nologo \"" + vbsPath + "\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            _sapiProcess.Start();
            _sapiStdin = _sapiProcess.StandardInput;
            _sapiStdin.AutoFlush = true;

            _log.LogInfo("SAPI voice process started");
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError($"SAPI init failed: {ex.Message}");
            return false;
        }
    }

    internal static void Shutdown()
    {
        KillMacSay();
        if (_tolkAvailable)
            try { Tolk_Unload(); } catch { }

        KillSapi();
        _tolkAvailable = false;
        _sapiAvailable = false;
        _macAvailable = false;
    }

    internal static bool Say(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        _log?.LogInfo($"[ScreenReader] Reading: \"{text}\"");

        if (_macAvailable)
            return MacSay(text, interrupt);

        if (_tolkAvailable)
        {
            var result = Tolk_Output(text, interrupt);
            if (!result) _log?.LogWarning("[ScreenReader] Tolk_Output returned false");
            return result;
        }

        return SapiSpeak(text);
    }

    private static bool MacSay(string text, bool interrupt)
    {
        if (interrupt) KillMacSay();
        try
        {
            var clean = text.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "").Replace("\0", "");
            _macProcess = new Process();
            _macProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/say",
                Arguments = "\"" + clean + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _macProcess.Start();
            return true;
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"Mac say failed: {ex.Message}");
            return false;
        }
    }

    private static void KillMacSay()
    {
        try { if (_macProcess != null && !_macProcess.HasExited) _macProcess.Kill(); } catch { }
        _macProcess = null;
    }

    private static bool SapiSpeak(string text)
    {
        if (_sapiProcess == null || _sapiProcess.HasExited)
        {
            _log?.LogWarning("SAPI process died, restarting");
            KillSapi();
            _sapiAvailable = InitSapi();
            if (!_sapiAvailable) return false;
        }

        try
        {
            var clean = text.Replace("\r", "").Replace("\n", " ").Replace("\0", "");
            if (clean.Length > 500) clean = clean.Substring(0, 500);
            _sapiStdin.WriteLine(clean);
            return true;
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"SAPI write failed: {ex.Message}, restarting");
            KillSapi();
            _sapiAvailable = InitSapi();
            return false;
        }
    }

    private static void KillSapi()
    {
        try { _sapiStdin?.Close(); } catch { }
        try { if (_sapiProcess != null && !_sapiProcess.HasExited) _sapiProcess.Kill(); } catch { }
        _sapiProcess = null;
        _sapiStdin = null;
    }

    internal static bool SayMenu(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text == _lastMenuText) return false;
        _lastMenuText = text;
        return Say(text, interrupt);
    }

    internal static void ClearMenuContext()
    {
        _lastMenuText = "";
    }

    internal static string StripNguiCodes(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('[')) return text;
        return Regex.Replace(text, @"\[[\da-fA-F]{6}\]|\[-\]", "");
    }
}
