using NeroWeNeed.UIECS.Editor;
using UnityEngine;
#if INPUTSYSTEM_EXISTS
using UnityEngine.InputSystem;
#endif
namespace NeroWeNeed.UIECS.Authoring {

    public class UIObject : MonoBehaviour {
        public UIViewAsset view;
        public UIModelAsset model;
        public InputActionAsset actions;
        [SerializeField, HideInInspector]
        internal Mesh mesh;
    }
}