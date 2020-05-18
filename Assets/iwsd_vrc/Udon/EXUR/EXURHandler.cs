/*
 * EXUR (EXclusive Use and Reusing objects) Handler
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// TODO Rename event name (callback messages) to ease to understand. and add prefix to avoid conflict.
//  Exclusive use and reusing objects with Udon  => EXUR?
//  ex. EXUR_ExitUsingByRequest


// TODO Simplified event (?). {start,stop}-{local,remote}

// TODO check SDK limitation with latest SDK.

// TODO consider inactivating unused target object. (optionally, and only child object??)
// TODO consider "continue to use when previous owner left" option. To be able to do finalize operation, for instance. 

namespace Iwsd.EXUR {

    public class Handler : UdonSharpBehaviour
    {
        // enum State
        // NOTE: U# doesn't support enum. instead, using const.
        const int STATE_UNDEFINED = 0;
        const int STATE_ERROR = 1;
        const int STATE_INITIAL = 2;
        const int STATE_WAITING_OWNER_RESPONCE = 3;
        const int STATE_USED_BY_OTHERS = 4;
        const int STATE_IDLE_NOT_MINE = 5;
        const int STATE_WAITING_OWNERSHIP = 6;
        const int STATE_OWN_AND_USING = 7;
        const int STATE_OWN_AND_IDLE = 8;

        // This state variable is local. This means what state this object is in from this player view.
        int lastState = STATE_INITIAL;

        [SerializeField]
        bool IncludeChildrenToSendEvent = false;
        
        // NOTE: Use Component[] instead of UdonUdonBehaviour[] because of the limitation
        // "Type referenced by 'VRCUdonUdonBehaviourArray' could not be resolved."
        // (VRCSDK3-UDON-2020.04.25.13.00)
        Component[] eventListeners;

        // Though this is intend for EXUR.Manager,
        // we treat it as an plain UdonBehaviour to avoid circular dependency.
        UdonBehaviour aggregatedListener;

        int ownershipTimeout;
        const int OWNERSHIP_TIMEOUT_DURATION = 20;

        // TODO test and remove ownershipDelay.
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

        void SendCallback(string eventName)
        {
            debug("SendCallback " + eventName);

            for (int i = 0; i < eventListeners.Length; i++)
            {
                var l = (UdonBehaviour)eventListeners[i];
                if (l != this)
                {
                    l.SendCustomEvent(eventName);
                }
            }

            if (aggregatedListener)
            {
                // NOTE: currently CustomEvent doesn't have argument. (VRCSDK3-UDON-2020.04.25.13.00)
                aggregatedListener.SetProgramVariable("EXUR_EventSource", this);
                aggregatedListener.SetProgramVariable("EXUR_EventName", eventName);
                aggregatedListener.SendCustomEvent("EXUR_RecieveEvent");
            }
        }


        void SetSyncedUsing(bool b)
        {
            var eventName = b? "set_using_true": "set_using_false";
            this.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, eventName);
        }

        public void set_using_true()
        {
            switch (lastState) {
                case STATE_WAITING_OWNER_RESPONCE:
                    lastState = STATE_USED_BY_OTHERS;
                    SendCallback("InitializedToUsing");
                    break;

                case STATE_IDLE_NOT_MINE:
                    lastState = STATE_USED_BY_OTHERS;
                    SendCallback("StartedToUseByOthers");
                    break;

                case STATE_WAITING_OWNERSHIP:
                    // Failed to start to use. Maybe it was race condition.
                    lastState = STATE_USED_BY_OTHERS;
                    SendCallback("FailedToUseByRaceCondition");
                    break;

                case STATE_OWN_AND_IDLE:
                    // Locally initiated STATE_OWN_AND_USING => STATE_OWN_AND_IDLE is done before calling here.
                    // If it falls into this case, it's incoming unexpected message.
                    assert(false, "!!!!!!!! set_using_true on STATE_OWN_AND_IDLE !!!!!!!!");
                    break;
                    
                default:
                    // empty
                    break;
            }
        }

        public void set_using_false()
        {
            switch (lastState) {
                case STATE_WAITING_OWNER_RESPONCE:
                    lastState = STATE_IDLE_NOT_MINE;
                    SendCallback("InitializedToIdle");
                    break;

                case STATE_USED_BY_OTHERS:
                    // This is for bystanders.
                    // Locally initiated STATE_USED_BY_OTHERS => STATE_OWN_AND_IDLE must be done before calling here.
                    lastState = STATE_IDLE_NOT_MINE;
                    SendCallback("StoppedUsingByOthers");
                    break;

                default:
                    // empty
                    break;
            }
        }


        void SendStateQueryToOwner()
        {
            this.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "query_state");
        }

        public void query_state()
        {
            if (Networking.IsOwner(this.gameObject))
            {
                assert((lastState == STATE_OWN_AND_USING) || (lastState == STATE_OWN_AND_IDLE), "query_state: Illegal state. " + lastState);
                SetSyncedUsing(lastState == STATE_OWN_AND_USING);
            }
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
                            lastState = STATE_OWN_AND_IDLE;
                            SendCallback("InitializedToOwn");
                        }
                        else
                        {
                            assert(false, "Master doesn't have ownership on Initialize");
                        }
                    }
                    else
                    {
                        // We choose synced-variable free. So new joiner needs to be told from owner.
                        SendStateQueryToOwner();
                        lastState = STATE_WAITING_OWNER_RESPONCE;
                    }
                    break;

                case STATE_WAITING_OWNER_RESPONCE:
                    // TODO timeout and retry (?)
                    break;

                case STATE_USED_BY_OTHERS:
                    if (Networking.IsOwner(this.gameObject))
                    {
                        if (Networking.IsMaster)
                        {
                            // Previous owner left from the world instance. So master shelters it.
                            lastState = STATE_OWN_AND_IDLE; // This state change must be before SetSyncedUsing
                            SetSyncedUsing(false);
                            SendCallback("RetrievedAfterOwnerLeftWhileUsing");
                        }
                        else
                        {
                            assert(false, "non-master gains ownership while USED_BY_OTHERS");
                        }
                    }
                    break;

                case STATE_IDLE_NOT_MINE:
                    // See also STATE_USED_BY_OTHERS case. This similar to that.
                    if (Networking.IsOwner(this.gameObject))
                    {
                        if (Networking.IsMaster)
                        {
                            lastState = STATE_OWN_AND_IDLE;
                            SendCallback("RetrievedAfterOwnerLeftWhileIdle");
                        }
                        else
                        {
                            assert(false, "non-master gains ownership while IDLE_NOT_MINE");
                        }
                    }
                    break;

                case STATE_WAITING_OWNERSHIP:
                    if (Networking.IsOwner(this.gameObject))
                    {
                        if (--ownershipDelay < 0)
                        {
                            SetSyncedUsing(true);
                            lastState = STATE_OWN_AND_USING;
                            SendCallback("EnterUsingFromWaiting");
                            SendCallback("EXUR_Reinitialize");
                        }
                    }
                    else
                    {
                        // TODO better timeout
                        if (--ownershipTimeout < 0)
                        {
                            // timeout when lose in getting ownership when race condition.
                            lastState = STATE_IDLE_NOT_MINE;
                            SendCallback("FailedToUseByTimeout");
                        }
                    }
                    break;

                case STATE_OWN_AND_USING:
                    if (!Networking.IsOwner(this.gameObject))
                    {
                        // Theft by others.
                        lastState = STATE_USED_BY_OTHERS;
                        SendCallback("LostOwnershipOnUsing");
                        SendCallback("EXUR_Finalize");
                    }
                    break;

                case STATE_OWN_AND_IDLE:
                    if (!Networking.IsOwner(this.gameObject))
                    {
                        // Other player started to use
                        lastState = STATE_IDLE_NOT_MINE;
                        SendCallback("LostOwnershipOnIdle");
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

            // TODO add option to allow theft.
            assert((lastState == STATE_IDLE_NOT_MINE) || (lastState == STATE_OWN_AND_IDLE),
                   "TryToUse: Illegal state. " + lastState); 

            if (Networking.IsOwner(this.gameObject))
            {
                assert(lastState == STATE_OWN_AND_IDLE, "TryToUse: Ownership mismatched with state=" + lastState);

                lastState = STATE_OWN_AND_USING;
                SetSyncedUsing(true);
                SendCallback("EnterUsingFromOwn");
                SendCallback("EXUR_Reinitialize");
            }
            else
            {
                // We prefer assert to runtime error because this is module private interface.
                assert(lastState == STATE_IDLE_NOT_MINE, "Tried to use while illegal state=" + lastState);

                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                ownershipTimeout = OWNERSHIP_TIMEOUT_DURATION;
                ownershipDelay = OWNERSHIP_DELAY_DURATION;
                lastState = STATE_WAITING_OWNERSHIP;
            }
        }

        public bool IsFreeOwned()
        {
            return lastState == STATE_OWN_AND_IDLE;
        }

        public bool IsFreeNotOwned()
        {
            return lastState == STATE_IDLE_NOT_MINE;
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

        public void EXUR_ReleaseObject()
        {
            log("EXUR_ReleaseObject called");

            if (lastState == STATE_OWN_AND_USING)
            {
                // continue to keep ownership
                lastState = STATE_OWN_AND_IDLE; // This state change must be before SetSyncedUsing
                SetSyncedUsing(false);
                SendCallback("ExitUsingByRequest");
                SendCallback("EXUR_Finalize");
            }
            else
            {
                warn("EXUR_ReleaseObject on not STATE_OWN_AND_USING. ignore");
                // SendCallback("Error");  // TODO How to tell error detail. public error variable?
            }
        }

        #endregion

    }
}
