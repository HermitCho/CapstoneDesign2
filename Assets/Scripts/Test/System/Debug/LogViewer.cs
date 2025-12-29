using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// 빌드에서 실시간 로그 확인을 위한 뷰어
/// F1 키로 토글, F2로 로그 파일 저장
/// </summary>
public class LogViewer : MonoBehaviour
{
    private struct LogEntry
    {
        public string logString;
        public string stackTrace;
        public LogType type;
        public float time;
    }

    private List<LogEntry> logs = new List<LogEntry>();
    private Vector2 scrollPosition;
    private bool showLogViewer = false;
    private bool showStackTrace = false;
    private int maxLogs = 500;
    
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle scrollViewStyle;
    
    private Rect windowRect = new Rect(10, 10, 800, 600);
    private bool isInitialized = false;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        // F1 키로 로그 뷰어 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showLogViewer = !showLogViewer;
        }
        
        // F2 키로 로그 파일 저장
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SaveLogsToFile();
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        LogEntry entry = new LogEntry
        {
            logString = logString,
            stackTrace = stackTrace,
            type = type,
            time = Time.time
        };
        
        logs.Add(entry);
        
        // 최대 로그 수 제한
        if (logs.Count > maxLogs)
        {
            logs.RemoveAt(0);
        }
    }

    void OnGUI()
    {
        if (!showLogViewer) return;
        
        if (!isInitialized)
        {
            InitializeStyles();
            isInitialized = true;
        }
        
        windowRect = GUILayout.Window(0, windowRect, DrawLogWindow, "Log Viewer (F1: Toggle, F2: Save)");
    }

    void InitializeStyles()
    {
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.wordWrap = true;
        labelStyle.fontSize = 12;
        
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 11;
        
        scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
    }

    void DrawLogWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // 상단 컨트롤
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", buttonStyle, GUILayout.Width(60)))
        {
            logs.Clear();
        }
        if (GUILayout.Button("Save to File", buttonStyle, GUILayout.Width(100)))
        {
            SaveLogsToFile();
        }
        showStackTrace = GUILayout.Toggle(showStackTrace, "Show Stack Trace", GUILayout.Width(150));
        GUILayout.Label($"Logs: {logs.Count}", labelStyle);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // 로그 스크롤 뷰
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        foreach (var log in logs)
        {
            Color originalColor = GUI.color;
            
            // 로그 타입에 따라 색상 변경
            switch (log.type)
            {
                case LogType.Error:
                case LogType.Exception:
                    GUI.color = Color.red;
                    break;
                case LogType.Warning:
                    GUI.color = Color.yellow;
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }
            
            GUILayout.BeginVertical("box");
            GUILayout.Label($"[{log.time:F2}s] [{log.type}] {log.logString}", labelStyle);
            
            if (showStackTrace && !string.IsNullOrEmpty(log.stackTrace))
            {
                GUI.color = Color.gray;
                GUILayout.Label(log.stackTrace, labelStyle);
            }
            
            GUILayout.EndVertical();
            GUI.color = originalColor;
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.EndVertical();
        
        GUI.DragWindow();
    }

    void SaveLogsToFile()
    {
        string logPath = Path.Combine(Application.persistentDataPath, $"Log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Unity Log Export ===");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine($"Total Logs: {logs.Count}");
        sb.AppendLine($"========================\n");
        
        foreach (var log in logs)
        {
            sb.AppendLine($"[{log.time:F2}s] [{log.type}] {log.logString}");
            if (!string.IsNullOrEmpty(log.stackTrace))
            {
                sb.AppendLine(log.stackTrace);
            }
            sb.AppendLine();
        }
        
        try
        {
            File.WriteAllText(logPath, sb.ToString());
            Debug.Log($"[LogViewer] 로그 저장 완료: {logPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LogViewer] 로그 저장 실패: {e.Message}");
        }
    }
}

