using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeroWeNeed.UIECS.Editor {

    public class UIECSDebugger : EditorWindow {
        [MenuItem("Window/UIECS/Debugger")]
        public static UIECSDebugger ShowWindow() {
            var window = GetWindow<UIECSDebugger>();
            window.titleContent = new GUIContent("UIECS Debugger");
            window.minSize = new Vector2(640, 480);
            window.Show();
            return window;
        }

    }
}