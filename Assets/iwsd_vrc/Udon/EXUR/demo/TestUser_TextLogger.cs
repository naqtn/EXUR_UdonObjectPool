
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


        public void EXUR_InitializedToOwn()
        {
            log(nameof(HandlerListener.EXUR_InitializedToOwn));
        }
        public void EXUR_InitializedToIdle()
        {
            log(nameof(HandlerListener.EXUR_InitializedToIdle));
        }
        public void EXUR_InitializedToUsing()
        {
            log(nameof(HandlerListener.EXUR_InitializedToUsing));
        }
        public void EXUR_StartedBeingUsed()
        {
            log(nameof(HandlerListener.EXUR_StartedBeingUsed));
        }
        public void EXUR_StoppedBeingUsed()
        {
            log(nameof(HandlerListener.EXUR_StoppedBeingUsed));
        }
        public void EXUR_FailedToUseByTimeout()
        {
            log(nameof(HandlerListener.EXUR_FailedToUseByTimeout));
        }
        public void EXUR_FailedToUseByRaceCondition()
        {
            log(nameof(HandlerListener.EXUR_FailedToUseByRaceCondition));
        }
        public void EXUR_EnterUsingFromWaiting()
        {
            log(nameof(HandlerListener.EXUR_EnterUsingFromWaiting));
        }
        public void EXUR_EnterUsingFromOwn()
        {
            log(nameof(HandlerListener.EXUR_EnterUsingFromOwn));
        }
        public void EXUR_ExitUsingByRequest()
        {
            log(nameof(HandlerListener.EXUR_ExitUsingByRequest));
        }
        public void EXUR_TriedToReleaseNotOwnError()
        {
            log(nameof(HandlerListener.EXUR_TriedToReleaseNotOwnError));
        }
        public void EXUR_ExitUsingByLostOwnership()
        {
            log(nameof(HandlerListener.EXUR_ExitUsingByLostOwnership));
        }
        public void EXUR_LostOwnershipOnIdle()
        {
            log(nameof(HandlerListener.EXUR_LostOwnershipOnIdle));
        }
        public void EXUR_RetrievedFromUsing()
        {
            log(nameof(HandlerListener.EXUR_RetrievedFromUsing));
        }
        public void EXUR_RetrievedFromIdle()
        {
            log(nameof(HandlerListener.EXUR_RetrievedFromIdle));
        }
        public void EXUR_Reinitialize()
        {
            log(nameof(HandlerListener.EXUR_Reinitialize));
        }
        public void EXUR_Finalize()
        {
            log(nameof(HandlerListener.EXUR_Finalize));
        }
        public void EXUR_OtherPlayerAcquired()
        {
            log(nameof(HandlerListener.EXUR_OtherPlayerAcquired));
        }
        public void EXUR_OtherPlayerReleased()
        {
            log(nameof(HandlerListener.EXUR_OtherPlayerReleased));
        }


        // for test. not part of callback API
        public void ReleaseObject()
        {
            log("ReleaseObject :!!!!!!! You got worng instance !!!!!"); // for test
        }
    }
}
