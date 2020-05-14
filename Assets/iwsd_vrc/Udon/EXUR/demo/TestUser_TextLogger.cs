
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TestUser_TextLogger : UdonSharpBehaviour
    {
        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void log(string s)
        {
            if (DebugText)
            {
                DebugText.text += "\n USR:" + transform.parent.name + ":" + s;
            }
        }

    
        public void InitializedAsMaster()
        {
            log("InitializedAsMaster");
        }
        public void InitializedAsNotMine()
        {
            log("InitializedAsNotMine");
        }
        public void ExitUsingByPlayerLeft()
        {
            log("ExitUsingByPlayerLeft");
        }
        public void OwnedByMaster()
        {
            log("OwnedByMaster");
        }
        public void EnterUsingFromWaiting()
        {
            log("EnterUsingFromWaiting");
        }
        public void FailedToStartUsing()
        {
            log("FailedToStartUsing");
        }
        public void LostOwnershipOnUsing()
        {
            log("LostOwnershipOnUsing");
        }
        public void LostOwnershipOnUnusing()
        {
            log("LostOwnershipOnUnusing");
        }
        public void EnterUsingFromOwn()
        {
            log("EnterUsingFromOwn");
        }
        public void ExitUsingByRequest()
        {
            log("ExitUsingByRequest");
        }
    }
}
