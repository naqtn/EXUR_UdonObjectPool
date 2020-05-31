
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TestUser_Tagged : UdonSharpBehaviour
    {
        [HideInInspector] [UdonSynced]
        public string EXUR_Tag;

        [HideInInspector] [UdonSynced]
        public int EXUR_LastUsedTime;

        
        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void log(string s)
        {
            if (DebugText)
            {
                // DebugText.text += "\nUSR:" + transform.name + ":" + s;
                DebugText.text = s;
            }
        }

        public void EXUR_RetrievedFromUsing()
        {
            EXUR_LastUsedTime = Networking.GetServerTimeInMilliseconds();
        }

        void Update()
        {
            log("Tag='" + EXUR_Tag + "' t=" + EXUR_LastUsedTime);
        }
        
    }
}
