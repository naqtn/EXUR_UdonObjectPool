
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
                DebugText.text += "\nUSR:" + transform.name + ":" + s;
            }
        }

    
        // public void ()
        // {
        //     log("");
        // }

        // Result of initialization
        public void InitializedToOwn()
        {
            log("InitializedToOwn");
        }
        public void InitializedToIdle()
        {
            log("InitializedToIdle");
        }
        public void InitializedToUsing()
        {
            log("InitializedToUsing");
        }

        // start-stop while not owned
        public void StartedToUseByOthers()
        {
            log("StartedToUseByOthers");
        }
        public void StoppedUsingByOthers()
        {
            log("StoppedUsingByOthers");
        }

        // Result of start
        public void FailedToUseByTimeout()
        {
            log("FailedToUseByTimeout");
        }
        public void FailedToUseByRaceCondition()
        {
            log("FailedToUseByRaceCondition");
        }
        public void EnterUsingFromWaiting()
        {
            log("EnterUsingFromWaiting");
        }
        public void EnterUsingFromOwn()
        {
            log("EnterUsingFromOwn");
        }

        // Result of stop
        public void ExitUsingByRequest()
        {
            log("ExitUsingByRequest");
        }

        // Lost ownership
        public void LostOwnershipOnUsing()
        {
            log("LostOwnershipOnUsing");
        }
        public void LostOwnershipOnIdle()
        {
            log("LostOwnershipOnIdle");
        }

        // Retrieve by Master (see also InitializedToOwn)
        public void RetrievedAfterOwnerLeftWhileUsing()
        {
            log("RetrievedAfterOwnerLeftWhileUsing");
        }
        public void RetrievedAfterOwnerLeftWhileIdle()
        {
            log("RetrievedAfterOwnerLeftWhileIdle");
        }

        // high level simplified API
        public void EXUR_Reinitialize()
        {
            log("EXUR_Reinitialize");

            log(" IsOwner=" + Networking.IsOwner(this.gameObject));  // for test TODO remove me
        }        
        public void EXUR_Finalize()
        {
            log("EXUR_Finalize");
        }        

        public void EXUR_ReleaseObject()
        {
            log("EXUR_ReleaseObject :!!!!!!! You got worng instance !!!!!"); // for test
        }        
    }
}
