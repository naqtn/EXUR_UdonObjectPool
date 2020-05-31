using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {


    /// <summary>EXUR Manager event listener interface.
    /// <para> It is written as a plain C# class because U# currently (v0.17.0) doesn't support C# <c>interface</c>.
    /// You can use this as a template of your implementation.
    /// </summary>
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
