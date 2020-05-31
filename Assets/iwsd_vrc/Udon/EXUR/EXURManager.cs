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
        [HideInInspector]
        public int TotalCount;
        [HideInInspector]
        public int FreeCount;


        const string TAG_VALUE_VAR_NAME = nameof(TaggedObject.EXUR_Tag);
        const string TAG_TIME_VAR_NAME = nameof(TaggedObject.EXUR_LastUsedTime);

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
                    && (!checkEmptyTag || IsSiblingHavingInvalidStringValue(obj, TAG_VALUE_VAR_NAME)))
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
                    && (!checkEmptyTag || IsSiblingHavingInvalidStringValue(obj, TAG_VALUE_VAR_NAME)))
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
                EventListener.SetProgramVariable(nameof(ManagerListener.EXUR_EventSource), eventSource);
                EventListener.SetProgramVariable(nameof(ManagerListener.EXUR_EventName), eventName);
                EventListener.SetProgramVariable(nameof(ManagerListener.EXUR_EventAdditionalInfo), eventInfo);
                EventListener.SendCustomEvent(nameof(ManagerListener.EXUR_ReceiveEvent));

                // TODO read variable to check variable exists in user program as kind debug mode (?)
            }
        }

        void ReportFailure(string info)
        {
            warn("Failure occurred. info='{info}'");
            SendEvent(null, EVENT_NAME_MANAGER_FAILURE, info);
        }

        UdonBehaviour GetSiblingBehavior(Handler handler)
        {
            var siblings = handler.transform.GetComponents(typeof(UdonBehaviour));
            if (siblings.Length < 2)
            {
                ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: No sibling UdonBehaviour on '{handler.gameObject.name}'");
                return null;
            }

            return (UdonBehaviour)siblings[1];
        }

        bool IsSiblingHavingValue(Handler handler, string name, object value)
        {
            var sibling = GetSiblingBehavior(handler);
            if (sibling)
            {
                var v = sibling.GetProgramVariable(name);
                return value.Equals(v);
            }
            return false;
        }


        // Accessing null variable of other UdonBehaviour becomes string.Empty on U# v0.16.2
        // https://github.com/Merlin-san/UdonSharp/issues/32
        // This issues/32 is fixed on v0.17.0
        //
        // This is used for tag value.
        // We should (must?) depend on user program for initial value of tag.
        // And we allow both null and empty because we suppose "strict" rule will not work well.
        // So we avoid to use both null and "" as usual tag value.
        //
        // (Current implementation use "" as explicit-not-used mark for writing.)
        bool IsSiblingHavingInvalidStringValue(Handler handler, string name)
        {
            var sibling = GetSiblingBehavior(handler);
            if (sibling)
            {
                var v = sibling.GetProgramVariable(name);
                return (v == null) || v.Equals("");
            }
            return false;
        }

        void SetValueToSibling(Handler handler, string name, object value)
        {
            var sibling = GetSiblingBehavior(handler);
            if (sibling)
            {
                sibling.SetProgramVariable(name, value);
            }
        }

        object GetValueFromSibling(Handler handler, string name)
        {
            var sibling = GetSiblingBehavior(handler);
            if (sibling)
            {
                return sibling.GetProgramVariable(name);
            }
            return null;
        }

        bool SaveTagToLocalBuffer(Handler handler, string newtag)
        {
            object t = handler.localTagBuffer;
            if ((t != null) && !t.Equals(""))
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
        // This expects 1st sibling UdonBehaviour holds GetServerTimeInMilliseconds in a variable named TAG_TIME_VAR_NAME.
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
                    var to = GetValueFromSibling(obj, TAG_TIME_VAR_NAME);
                    if (to == null)
                    {
                        ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: Does not have a variable named {TAG_TIME_VAR_NAME}");
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
                if (IsSiblingHavingValue(obj, TAG_VALUE_VAR_NAME, tag))
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
            if (eventName.Equals(nameof(HandlerListener.EXUR_EnterUsingFromWaiting))
                || eventName.Equals(nameof(HandlerListener.EXUR_EnterUsingFromOwn)))
            {
                var tag = eventSource.localTagBuffer;
                if ((tag != null) && !tag.Equals(""))
                {
                    var sibling = GetSiblingBehavior(eventSource);
                    if (sibling)
                    {
                        var time = Networking.GetServerTimeInMilliseconds();
                        sibling.SetProgramVariable(TAG_VALUE_VAR_NAME, tag);
                        sibling.SetProgramVariable(TAG_TIME_VAR_NAME, time);
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

        public void EXUR_ReceiveEvent()
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
                debug("EXUR_ReceiveEvent. " + EXUR_EventName + " from '" + EXUR_EventSource.gameObject.name + "'");

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

            // Usual tag value must not be null nor "".
            if (tag == null)
            {
                ReportFailure($"{FAILURE_INFO_HEAD_USER_PROGRAM_ERROR}: Specified tag was null");
            }
            else if (tag.Equals(""))
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
