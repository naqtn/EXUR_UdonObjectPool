/*
 * EXUR (EXclusive Use and Reusing objects) Handler
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// TODO Rename event name (callback messages) to ease to understand. and add prefix to avoid conflict.

// TODO check SDK limitation with latest SDK.

// TODO consider "continue to use when previous owner left" option. To be able to do finalize operation, for example. 

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
        const int STATE_TEMPORARY = 9;

        // This state variable is local. This means what state this object is in from this player view.
        int lastState = STATE_INITIAL;

        [SerializeField]
        bool IncludeChildrenToSendEvent = false;

        [SerializeField]
        bool DeavtivateWhenIdle = false;

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


        void SetActiveObject(bool b)
        {
            set_active_true(); // For the present, force active because SendCustomNetworkEvent doesn't work when inactive

            var eventName = b? "set_active_true": "set_active_false";
            this.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, eventName);
        }

        public void set_active_true()
        {
            debug("recieve set_active_true.");
            this.gameObject.SetActive(true);
        }
        
        public void set_active_false()
        {
            debug("recieve set_active_false.");
            this.gameObject.SetActive(false);
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
                    // For DeavtivateWhenIdle usecase, if set_active_true and set_using_true are incoming successively,
                    // detecting to-lost-ownership transition in Update is not done yet. So do it here.
                    if (DeavtivateWhenIdle && !Networking.IsOwner(gameObject))
                    {
                        lastState = STATE_IDLE_NOT_MINE;
                        SendCallback("LostOwnershipOnIdle");
                        lastState = STATE_USED_BY_OTHERS;
                        SendCallback("StartedToUseByOthers");
                    }
                    else
                    {
                        // Locally initiated STATE_OWN_AND_IDLE => STATE_OWN_AND_USING is done before calling here.
                        
                        // If it falls into here, it's an incoming unexpected set_using_true message.
                        // Maybe it means receiving set_using_true before lost-ownership comes
                        // when other player is switching to use.
                        // If so, it is communication message ordering issue between SetOwner and SendCustomNetworkEvent.
                        // It's weired but not actual problem. Probably we can resolve it in a way introduce new waiting state.
                        // But we are not sure if it really happens. So, we put assert here to investigae for now.
                        assert(false, "!!!!!!!! set_using_true on STATE_OWN_AND_IDLE !!!!!!!!");
                    }
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
            // It needs to be temporary active to work Networking.IsOwner properly.
            bool temporaryActivated = false;
            if (DeavtivateWhenIdle && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                temporaryActivated = true;
            }

            if (Networking.IsOwner(this.gameObject))
            {
                assert((lastState == STATE_OWN_AND_USING) || (lastState == STATE_OWN_AND_IDLE),
                       "query_state: Illegal state. " + lastState);

                if (temporaryActivated) // when gameObject was inactive
                {
                    // Respond "it's inactive". It sends only when inactive because initial state is active.
                    // Call SetActiveObject before calling SetSyncedUsing for user script 
                    // to know inactive state when InitializedToIdle event.
                    SetActiveObject(false); 
                }
                SetSyncedUsing(lastState == STATE_OWN_AND_USING);
            }

            if (temporaryActivated)
            {
                gameObject.SetActive(false); // restore
            }
        }


        // NOTE: Instead of OnOwnershipTransfer, it uses Networking.IsOwner to examine ownership.
        // Because OnOwnershipTransfer fires only for the previous owner. (VRCSDK3-UDON-2020.05.12.10.33)
        // https://vrchat.canny.io/vrchat-udon-closed-alpha-feedback/p/request-change-onownershiptransfer-behaviour
        
        void CheckImplicitTransition()
        {
            switch (lastState)
            {
                case STATE_UNDEFINED:
                    assert(false, "Unexpected transition check for STATE_UNDEFINED");
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
                            if (DeavtivateWhenIdle)
                            {
                                // Do this before callback InitializedToOwn for user program to be able to know inactive.
                                SetActiveObject(false);
                            }
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

                            if (DeavtivateWhenIdle)
                            {
                                SetActiveObject(false);
                            }
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

                            if (DeavtivateWhenIdle)
                            {
                                SetActiveObject(false);
                            }
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
                            // To skip set_using_true sent by myself
                            // NOTE: There's no option like NetworkEventTarget.Other for SendCustomNetworkEvent.
                            lastState = STATE_TEMPORARY;
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
            log("TryToUse: state=" + lastState);

            // TODO add option to allow theft.
            assert((lastState == STATE_IDLE_NOT_MINE) || (lastState == STATE_OWN_AND_IDLE),
                   "TryToUse: Illegal state. " + lastState); 

            if (DeavtivateWhenIdle)
            {
                SetActiveObject(true); // This must be before calling IsOwner
            }
            
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

        public bool IsFree()
        {
            return (lastState == STATE_OWN_AND_IDLE) || (lastState == STATE_IDLE_NOT_MINE);
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

        public void ReleaseObject()
        {
            log("ReleaseObject called");

            if (lastState == STATE_OWN_AND_USING)
            {
                // continue to keep ownership
                lastState = STATE_OWN_AND_IDLE; // This state change must be before SetSyncedUsing
                SetSyncedUsing(false);
                SendCallback("ExitUsingByRequest");
                SendCallback("EXUR_Finalize");

                if (DeavtivateWhenIdle)
                {
                    SetActiveObject(false);
                }
            }
            else
            {
                warn("ReleaseObject on not STATE_OWN_AND_USING. ignore");
                // SendCallback("Error");  // TODO How to tell error detail. public error variable?
            }
        }

        #endregion

    }
}
