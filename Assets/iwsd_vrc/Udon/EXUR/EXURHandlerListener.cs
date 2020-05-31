
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    /// <summary>EXUR Handler event listener interface.
    /// <para> It is written as a plain C# class because U# currently (v0.17.0) doesn't support C# <c>interface</c>.
    /// You can use this as a template of your implementation.
    /// </summary>
    public class HandlerListener : UdonSharpBehaviour
    {
        //////////////////////////////
        #region Simplified event API

        public void EXUR_Reinitialize()
        {
        }
        public void EXUR_Finalize()
        {
        }

        public void EXUR_OtherPlayerAcquired()
        {
        }
        public void EXUR_OtherPlayerReleased()
        {
        }
        #endregion


        //////////////////////////////
        #region Detailed event API

        // Result of initialization
        public void EXUR_InitializedToOwn()
        {
        }
        public void EXUR_InitializedToIdle()
        {
        }
        public void EXUR_InitializedToUsing()
        {
        }

        // Transition while not owned
        public void EXUR_StartedBeingUsed()
        {
        }
        public void EXUR_StoppedBeingUsed()
        {
        }

        // Result of start
        public void EXUR_FailedToUseByTimeout()
        {
        }
        public void EXUR_FailedToUseByRaceCondition()
        {
        }
        public void EXUR_EnterUsingFromWaiting()
        {
        }
        public void EXUR_EnterUsingFromOwn()
        {
        }

        // Result of stop
        public void EXUR_ExitUsingByRequest()
        {
        }

        public void EXUR_TriedToReleaseNotOwnError()
        {
        }

        // Lost ownership
        public void EXUR_ExitUsingByLostOwnership()
        {
        }
        public void EXUR_LostOwnershipOnIdle()
        {
        }

        // Retrieve by Master (see also EXUR_InitializedToOwn)
        public void EXUR_RetrievedFromUsing()
        {
        }
        public void EXUR_RetrievedFromIdle()
        {
        }

        #endregion
    }
}
