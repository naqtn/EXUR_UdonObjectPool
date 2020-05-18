
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR.Demo {

    public class ManagerVisualizer : UdonSharpBehaviour
    {
        [SerializeField]
        UnityEngine.UI.Text DebugText;
        
        void log(string s)
        {
            if (DebugText)
            {
                DebugText.text = s;
            }
        }


        public UdonBehaviour EXUR_EventSource;
        public string EXUR_EventName;
        public void EXUR_RecieveEvent()
        {
            log($"{EXUR_EventName} on '{EXUR_EventSource.gameObject.name}'");
        }

    }
}
