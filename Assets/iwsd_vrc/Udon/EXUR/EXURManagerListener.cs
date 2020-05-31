using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class ManagerListener : UdonSharpBehaviour
    {
        [HideInInspector] public UdonBehaviour EXUR_EventSource;
        [HideInInspector] public string EXUR_EventName;
        [HideInInspector] public string EXUR_EventAdditionalInfo;

        public void EXUR_ReceiveEvent()
        {
        }
    }
}
