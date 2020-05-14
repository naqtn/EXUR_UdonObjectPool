using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// TODO Rename event name (callback messages) to ease to understand. and add prefix to avoid conflict.
//  Exclusive use and reusing objects with Udon  => EXUR?
//  ex. EXUR_ExitUsingByRequest

// TODO check SDK limitation with latest SDK.

// TODO split STATE_NOT_MINE state to ILDE and USED BY OTHERS (?)

namespace Iwsd.EXUR {

    public class EXURHandler : UdonSharpBehaviour
    {
        // enum State
        // NOTE: U# doesn't support enum. instead, using const.
        const int STATE_UNDEFINED = 0;
        const int STATE_ERROR = 1;
        const int STATE_INITIAL = 2;
        const int STATE_NOT_MINE = 3;
        const int STATE_WAITING_OWNERSHIP = 4;
        const int STATE_USING = 5;
        const int STATE_NOT_USING = 6;

        // This state variable is local. This means what state this object is in from this player view.
        int lastState = STATE_INITIAL;

        // NOTE: This is local variable.
        // Instead of synced variable, use CustomNetworkEvent to reduce network ussage.
        bool syncedUsing = false;


        [SerializeField]
        bool IncludeChildrenToSendEvent = false;
        
        // NOTE: Use Component[] instead of UdonUdonBehaviour[] because of the limitation
        // "Type referenced by 'VRCUdonUdonBehaviourArray' could not be resolved."
        // (VRCSDK3-UDON-2020.04.25.13.00)
        Component[] eventListeners;

        UdonBehaviour aggregatedListener;

        int ownershipTimeout;
        const int OWNERSHIP_TIMEOUT_DURATION = 20;

        // TODO remove ownershipDelay.
        // It is no more needed because we replaced sync variable with network event.
        int ownershipDelay;
        const int OWNERSHIP_DELAY_DURATION = 0;


        //////////////////////////////
        #region Development support

        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void debugLog(string head, string message)
        {
            if (DebugText)
            {
                DebugText.text += string.Format("\n{0}:{1}:{2}", head, this.gameObject.name, message);
            }
        }

        void assert(bool test,  string message)
        {
            if (!test)
            {
                debugLog("ASSERT", message);
                lastState = STATE_ERROR;
            }
        }

        void log(string message)
        {
            debugLog("LOG", message);
        }

        void debug(string message)
        {
            debugLog("DBG", message);
        }

        void warn(string message)
        {
            debugLog("WRN", message);
        }

        #endregion


        //////////////////////////////
        #region Internal implementation

        void SetupListeners()
        {
            // NOTE: Can't use generics version of GetComponent because of the limitation of current implementation:
            // Method VRCUdonCommonInterfacesIUdonEventReceiver.__GetComponent__T is not exposed in Udon
            if (IncludeChildrenToSendEvent)
            {
                eventListeners = GetComponentsInChildren(typeof(UdonBehaviour), false);
            }
            else
            {
                eventListeners = GetComponents(typeof(UdonBehaviour));
            }
            
            aggregatedListener = (UdonBehaviour)transform.parent.GetComponent(typeof(UdonBehaviour));
        }

        void SendCallback(string message)
        {
            debug("SendCallback " + message);

            for (int i = 0; i < eventListeners.Length; i++)
            {
                var l = (UdonBehaviour)eventListeners[i];
                if (l != this)
                {
                    l.SendCustomEvent(message);
                }
            }

            if (aggregatedListener)
            {
                // Access as an plain UdonBehaviour to avoid circular dependency.
                // NOTE: currently CustomEvent doesn't have argument. (VRCSDK3-UDON-2020.04.25.13.00)
                aggregatedListener.SetProgramVariable("eventSource", this);
                aggregatedListener.SetProgramVariable("eventName", message);
                aggregatedListener.SendCustomEvent("recieveEvent");
            }
        }


        void SetSyncedUsing(bool b)
        {
            var message = b? "set_using_true": "set_using_false";
            this.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, message);
        }

        public void set_using_true()
        {
            syncedUsing = true;
        }
        public void set_using_false()
        {
            syncedUsing = false;
        }


        void CheckImplicitTransition()
        {
            switch (lastState)
            {
                case STATE_UNDEFINED:
                    assert(false, "transition check for STATE_UNDEFINED");
                    break;

                case STATE_ERROR:
                    // do nothing
                    break;

                case STATE_INITIAL:
                    if (Networking.IsMaster)
                    {
                        if (Networking.IsOwner(this.gameObject))
                        {
                            SetSyncedUsing(false);
                            lastState = STATE_NOT_USING;
                            SendCallback("InitializedAsMaster");
                        }
                        else
                        {
                            assert(false, "Master doesn't have ownership on Initialize");
                        }
                    }
                    else
                    {
                        lastState = STATE_NOT_MINE;
                        SendCallback("InitializedAsNotMine");
                    }
                    break;

                case STATE_NOT_MINE:
                    if (Networking.IsOwner(this.gameObject))
                    {
                        if (Networking.IsMaster)
                        {
                            // Previous user left from the world instance. So master shelters it.
                            lastState = STATE_NOT_USING;

                            if (syncedUsing)
                            {
                                // This assignment will be overwritten because of delay issue
                                SetSyncedUsing(false);
                                SendCallback("ExitUsingByPlayerLeft");
                            }
                            else
                            {
                                SendCallback("OwnedByMaster");
                            }
                        }
                        else
                        {
                            assert(false, "non-master gains ownership while NOT_MINE");
                        }
                    }
                    break;

                case STATE_WAITING_OWNERSHIP:
                    if (Networking.IsOwner(this.gameObject))
                    {
                        if (--ownershipDelay < 0)
                        {
                            assert(!syncedUsing, "gain ownership on syncedUsing");
                            SetSyncedUsing(true);
                            lastState = STATE_USING;
                            SendCallback("EnterUsingFromWaiting");
                        }
                    }
                    else
                    {
                        // TODO better timeout
                        if (--ownershipTimeout < 0)
                        {
                            // timeout when lose in getting ownership when race condition.
                            lastState = STATE_NOT_MINE;
                            SendCallback("FailedToStartUsing");
                        }
                    }
                    break;

                case STATE_USING:
                    if (!Networking.IsOwner(this.gameObject))
                    {
                        // Theft by others.
                        lastState = STATE_NOT_MINE;
                        SendCallback("LostOwnershipOnUsing");
                    }
                    break;

                case STATE_NOT_USING:
                    if (!Networking.IsOwner(this.gameObject))
                    {
                        // Other player started to use
                        lastState = STATE_NOT_MINE;
                        SendCallback("LostOwnershipOnUnusing");
                    }
                    else
                    {
                        // This assert might fail because of delay issue.
                        // assert(!syncedUsing, "SyncedUsing while STATE_NOT_USING");
                        // so do workaround.
                        if (syncedUsing)
                        {
                            SetSyncedUsing(false);
                        }
                    }
                    break;

                default:
                    assert(false, "Unknown lastState=" + lastState);
                    break;

            }
        }

        #endregion


        //////////////////////////////
        #region Module private interface

        // The method caller must check status before call this method.
        public void TryToUse()
        {
            debug("TryToUse");

            assert(!syncedUsing, "TryToUse on syncedUsing"); // TODO add allow theft option?

            if (Networking.IsOwner(this.gameObject))
            {
                assert(lastState == STATE_NOT_USING, "Tried to use already using by myself. " + lastState);

                SetSyncedUsing(true);
                lastState = STATE_USING;
                SendCallback("EnterUsingFromOwn");
            }
            else
            {
                assert(lastState == STATE_NOT_MINE, "Tried to use while illegal state. " + lastState);

                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                ownershipTimeout = OWNERSHIP_TIMEOUT_DURATION;
                ownershipDelay = OWNERSHIP_DELAY_DURATION;
                lastState = STATE_WAITING_OWNERSHIP;
            }
        }

        public bool IsFreeOwned()
        {
            return lastState == STATE_NOT_USING;
        }

        public bool IsFreeNotOwned()
        {
            return (lastState == STATE_NOT_MINE) && !syncedUsing;
        }

        #endregion


        //////////////////////////////
        #region Drive by system events

        void Start()
        {
            SetupListeners();
        }

        void Update()
        {
            CheckImplicitTransition();
        }

        #endregion


        //////////////////////////////
        #region Public interface

        public void StopUsing()
        {
            log("StopUsing called");

            if (lastState == STATE_USING)
            {
                // continue to keep ownership
                assert(syncedUsing, "STATE_USING but not syncedUsing");
                lastState = STATE_NOT_USING;
                SetSyncedUsing(false);
                SendCallback("ExitUsingByRequest");
            }
            else
            {
                warn("StopUsing on not STATE_USING. ignore");
                SendCallback("Error");  // TODO How to tell error detail. public error variable?
            }
        }

        #endregion

    }
}
