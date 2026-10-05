using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Vault
{
    /// <summary>The only scene object. Sets up input and starts the game flow; everything else is built in code.</summary>
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            Random.InitState(System.Environment.TickCount);
            Dev.Init();
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            UI.Font = TMP_Settings.defaultFontAsset;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            gameObject.AddComponent<GameFlow>();
        }
    }
}
