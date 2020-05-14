
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TestUser_InteractToMethod : UdonSharpBehaviour
    {
        public UdonBehaviour Target;
        public string EventName;


        void Interact() {
            Target.SendCustomEvent(EventName);
        }

    }
}
