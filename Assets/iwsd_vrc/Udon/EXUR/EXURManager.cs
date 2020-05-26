/*
 * EXUR (EXclusive Use and Reusing objects) Manager
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class Manager : UdonSharpBehaviour
    {
        Handler[] objects;

        public UdonBehaviour EventListener;

        // Intented to be readonly properties. (but there's no way to limit readonly aceess in Udon.)
        public int TotalCount;
        public int FreeCount;


        const string EVENT_NAME_IN_USE_BY_SELF = "InUseBySelf";
        const string EVENT_NAME_IN_USE_BY_OTHERS = "InUseByOthers";
        const string EVENT_NAME_NO_FREE_OBJECT = "NoFreeObject";
        const string EVENT_NAME_MANAGER_FAILURE = "ManagerFailure";
        const string FAILURE_INFO_HEAD_INTERNAL_ERROR = "InternalError";
        const string FAILURE_INFO_HEAD_USER_PROGRAM_ERROR = "UserProgramError";


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

        void error(string message)
        {
            debugLog("ERR", message);
        }

        #endregion


        //////////////////////////////
        #region Internal implementation

        void GatherObjects()
        {
            objects = new Handler[transform.childCount];
            int n = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                var h = child.GetComponent<Handler>();
                if (!h)
                {
                    ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: No Handler found on a child '{child.name}'");
                }
                else
                {
                    debug($"GetComponent i={i}, name='{h.gameObject.name}'");
                    objects[n++] = h;
                }
            }
            
            if (objects.Length != n)
            {
                var tmp = objects;
                objects = new Handler[n];
                for (int i = 0; i < n; i++) // NOTE Array.Copy not exposed
                {
                    objects[i] = tmp[i];
                }
            }
            
            TotalCount = objects.Length;
        }

        Handler FindFreeOwned(bool checkEmptyTag)
        {
            var n = objects.Length;
            for (int i = 0; i < n; i++)
            {
                var obj = objects[i];
                if (obj.IsFreeOwned()
                    && (!checkEmptyTag || IsBrotherHavingValue(obj, "EXUR_Tag", "")))
                {
                    return obj;
                }
            }
            return null;
        }

        Handler FindFreeNotOwned(bool checkEmptyTag)
        {
            // Random for better assignment to avoid race condition
            var n = objects.Length;
            int randOffset = Random.Range(0, n);

            for (int i = 0; i < n; i++)
            {
                var obj = objects[(i + randOffset) % n];
                if (obj.IsFreeNotOwned()
                    && (!checkEmptyTag || IsBrotherHavingValue(obj, "EXUR_Tag", "")))
                {
                    return obj;
                }
            }
            return null;
        }


        // This actually counts. not using event
        void UpdateCounts()
        {
            FreeCount = 0;

            if (objects == null) // not initiated yet
            {
                TotalCount = -1;
                return;
            }

            var n = objects.Length;
            for (int i = 0; i < n; i++)
            {
                if (objects[i].IsFree())
                {
                    FreeCount++;
                }
            }

            TotalCount = n;
        }

        void SendEvent(Handler eventSource, string eventName, string eventInfo)
        {
            if (EventListener)
            {
                EventListener.SetProgramVariable("EXUR_EventSource", eventSource);
                EventListener.SetProgramVariable("EXUR_EventName", eventName);
                EventListener.SetProgramVariable("EXUR_EventAdditionalInfo", eventInfo);
                EventListener.SendCustomEvent("EXUR_RecieveEvent");

                // TODO read variable to check variable exists in user program as kind debug mode (?)
            }
        }

        void ReportFailure(string info)
        {
            warn("Failure occurred. info='{info}'");
            SendEvent(null, EVENT_NAME_MANAGER_FAILURE, info);
        }

        UdonBehaviour GetBrotherBehavior(Handler handler)
        {
            var brothers = handler.transform.GetComponents(typeof(UdonBehaviour));
            if (brothers.Length < 2)
            {
                ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: No brother UdonBehaviour on '{handler.gameObject.name}'");
                return null;
            }

            return (UdonBehaviour)brothers[1];
        }

        bool IsBrotherHavingValue(Handler handler, string name, object value)
        {
            var brother = GetBrotherBehavior(handler);
            if (brother)
            {
                var v = brother.GetProgramVariable(name);
                return value.Equals(v);
            }
            return false;
        }

        void SetValueToBrother(Handler handler, string name, object value)
        {
            var brother = GetBrotherBehavior(handler);
            if (brother)
            {
                brother.SetProgramVariable(name, value);
            }
        }

        object GetValueFromBrother(Handler handler, string name)
        {
            var brother = GetBrotherBehavior(handler);
            if (brother)
            {
                return brother.GetProgramVariable(name);
            }
            return null;
        }

        bool SaveTagToLocalBuffer(Handler handler, string newtag)
        {
            object t = handler.localTagBuffer;
            // NOTE accessing null becomes string.Empty (U# v0.16.2)
            // https://github.com/Merlin-san/UdonSharp/issues/32
            // if (t != null)
            if (!t.Equals(""))
            {
                ReportFailure($"{FAILURE_INFO_HEAD_INTERNAL_ERROR}: unclear localTagBuffer='{t}', tag='{newtag}'");
                return false;
            }
            handler.localTagBuffer = newtag;
            return true;
        }

        // Returns true if candidate object is found.
        bool AcquireEmptyTaggedObject(string newtag)
        {
            Handler targetHandler = FindFreeOwned(true);
            if (!targetHandler)
            {
                targetHandler = FindFreeNotOwned(true);
            }
            if (targetHandler)
            {
                if (SaveTagToLocalBuffer(targetHandler, newtag))
                {
                    debug($"Will call TryToUse empty tagged '{targetHandler.gameObject.name}'");
                    targetHandler.TryToUse();
                }
                return true;
            }
            return false;
        }
        
        // Select free object with Least recently used (LRU) algorithm.
        // This expects 1st brother UdonBehaviour holds GetServerTimeInMilliseconds in a variable named EXUR_LastUsedTime.
        // This returns error string. null for successful case.
        // tag is optional.
        void RecycleLeastRecentlyUsed(string newtag)
        {
            // Search least recently used object
            Handler targetHandler = null;
            int least = 0;
            var n = objects.Length;
            for (int i = 0; i < n; i++) {
                var obj = objects[i];
                if (obj.IsFree())
                {
                    var to = GetValueFromBrother(obj, "EXUR_LastUsedTime");
                    if (to == null)
                    {
                        ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: Does not have EXUR_LastUsedTime variable");
                        return;
                    }
                    var ti = (int)to;
                    // ti could overflow and becomes negative while least is positive.
                    // Even if so, diff is meaningful because underflow occurs
                    int diff = ti - least;
                    if ((targetHandler == null) // first one
                        || (diff < 0))
                    {
                        least = ti;
                        targetHandler = obj;
                    }

                    debug($" LRU: name='{obj.gameObject.name}', t={ti}");
                }
            }

            if (targetHandler == null)
            {
                SendEvent(null, EVENT_NAME_NO_FREE_OBJECT, null);
                return;
            }
            debug($" LRU: selected='{targetHandler.gameObject.name}'");

            if (newtag != null)
            {
                if (!SaveTagToLocalBuffer(targetHandler, newtag))
                {
                    return;
                }
            }

            targetHandler.TryToUse();
        }


        void AcquireObjectWithTagImpl(string tag)
        {
            // Search handlers having specified tag
            int foundCount = 0;
            Handler targetHandler = null;
            var n = objects.Length;
            for (int i = 0; i < n; i++) {
                var obj = objects[i];
                if (IsBrotherHavingValue(obj, "EXUR_Tag", tag))
                {
                    foundCount++;
                    targetHandler = obj;
                }
            }
            debug($" foundCount={foundCount}");

            if (1 < foundCount)
            {
                ReportFailure($"{FAILURE_INFO_HEAD_INTERNAL_ERROR}: Multiple {foundCount} objects have identical tag='{tag}'");
            }
            else if (foundCount == 1)
            {
                if (targetHandler.IsFree())
                {
                    debug($"will call TryToUse");
                    targetHandler.TryToUse();
                }
                else
                {
                    warn($"already in use. name='{targetHandler.gameObject.name}', tag='{tag}'");

                    // TODO check targetHandler.lastState and assert (?)
                    if (Networking.IsOwner(targetHandler.gameObject))
                    {
                        // Don't fire events on targetHandler because user can do same thing easily.
                        SendEvent(targetHandler, EVENT_NAME_IN_USE_BY_SELF, null);
                    }
                    else
                    {
                        SendEvent(targetHandler, EVENT_NAME_IN_USE_BY_OTHERS, null);
                    }
                }
            }
            else // foundCount == 0
            {
                // try to find one with empty tag
                if (!AcquireEmptyTaggedObject(tag))
                {
                    RecycleLeastRecentlyUsed(tag);
                }
            }
        }


        void ReactToEvent(Handler eventSource, string eventName)
        {
            if (eventName.Equals("EnterUsingFromWaiting") || eventName.Equals("EnterUsingFromOwn"))
            {
                var tag = eventSource.localTagBuffer;
                // if (tag != null)
                if (!tag.Equals(string.Empty))
                {
                    var brother = GetBrotherBehavior(eventSource);
                    if (brother)
                    {
                        var time = Networking.GetServerTimeInMilliseconds();
                        brother.SetProgramVariable("EXUR_Tag", tag);
                        brother.SetProgramVariable("EXUR_LastUsedTime", time);
                        debug($"set tag={tag}, time={time} name='{eventSource.gameObject.name}'");
                    }
                    eventSource.localTagBuffer = null;
                }
            }
        }

        #endregion


        //////////////////////////////
        #region Module private interface

        // We use user callback interface also for Manager
        // to avoid circular dependencies between Manager and Handler
        
        [HideInInspector] // Hide in inspector because this is public but only for API.
        public Handler EXUR_EventSource;
        [HideInInspector]
        public string EXUR_EventName;
        [HideInInspector]
        public string EXUR_EventAdditionalInfo;

        public void EXUR_RecieveEvent()
        {
            if (!EXUR_EventSource)
            {
                warn("null EXUR_EventSource");
            }
            else if (EXUR_EventName == null)
            {
                warn("null EXUR_EventName");
            }
            else
            {
                debug("EXUR_RecieveEvent. " + EXUR_EventName + " from '" + EXUR_EventSource.gameObject.name + "'");

                ReactToEvent(EXUR_EventSource, EXUR_EventName);
                UpdateCounts();
                SendEvent(EXUR_EventSource, EXUR_EventName, EXUR_EventAdditionalInfo); // propagate event
            }
            EXUR_EventSource = null;
            EXUR_EventName = null;
            EXUR_EventAdditionalInfo = null;
        }

        #endregion


        //////////////////////////////
        #region Drive by system events

        void Start()
        {
            GatherObjects();
        }

        #endregion


        //////////////////////////////
        #region Public interface

        public void AcquireObject() {
            Handler targetHandler = FindFreeOwned(false);
            if (!targetHandler)
            {
                targetHandler = FindFreeNotOwned(false);
            }

            if (!targetHandler)
            {
                debug("AcquireObject: No free object");
                SendEvent(null, EVENT_NAME_NO_FREE_OBJECT, null);
            }
            else
            {
                log("AcquireObject: selected target='" + targetHandler.gameObject.name + "'");
                targetHandler.TryToUse();
            }
        }


        [HideInInspector] // Hide in inspector because this is a part of API.
        public string AcquireObjectWithTag_tag;

        public void AcquireObjectWithTag()
        {
            string tag = AcquireObjectWithTag_tag;

            if (tag == null)
            {
                ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: Specified tag was null");
            }
            else if (tag.Equals(string.Empty))
            {
                ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: Specified tag was empty string");
            }
            else
            {
                log($"AcquireObjectWithTag Tag='{tag}'");
                AcquireObjectWithTagImpl(tag);
            }
        }

        public void AcquireObjectForEachPlayer()
        {
            AcquireObjectWithTag_tag = Networking.LocalPlayer.displayName;
            AcquireObjectWithTag();
        }

        #endregion
    }

}
