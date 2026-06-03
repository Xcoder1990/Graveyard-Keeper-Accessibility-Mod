using System.Diagnostics;
using System.IO;

namespace GraveyardKeeperAccessibility;

internal static class ScreenReader
{
    private static bool _tolkAvailable;
    private static bool _sapiAvailable;
    private static Process _sapiProcess;
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
        _tolkAvailable = InitTolk();
        if (!_tolkAvailable)
            _sapiAvailable = InitSapi();

        if (!_tolkAvailable && !_sapiAvailable)
            log.LogError("No TTS output available");
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
                "v.Speak WScript.StdIn.ReadLine,3\r\n" +
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
        if (_tolkAvailable)
            try { Tolk_Unload(); } catch { }

        if (_sapiProcess != null && !_sapiProcess.HasExited)
            try
            {
                _sapiStdin?.Close();
                _sapiProcess.Kill();
            }
            catch { }

        _tolkAvailable = false;
        _sapiAvailable = false;
    }

    internal static bool Say(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (_tolkAvailable)
            return Tolk_Output(text, interrupt);

        if (_sapiAvailable)
            return SapiSpeak(text);

        return false;
    }

    private static bool SapiSpeak(string text)
    {
        try
        {
            if (_sapiProcess == null || _sapiProcess.HasExited)
            {
                _sapiAvailable = false;
                return false;
            }

            var clean = text.Replace("\r", "").Replace("\n", " ");
            _sapiStdin.WriteLine(clean);
            return true;
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"SAPI speak failed: {ex.Message}");
            _sapiAvailable = false;
            return false;
        }
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
