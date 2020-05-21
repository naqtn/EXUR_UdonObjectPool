/*
 * EXUR (EXclusive Use and Reusing objects) Manager
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// TODO idea. add "pooling object child depth level".
// An integer that specifies where to find pooling object.
// If there are so many pooling objects, that will be too long child list in Hierarchy view.
// So it will be useful if it's possible to bundle them in sub groups.

// TODO count used/unsed/total objects. and serves it to user.

namespace Iwsd.EXUR {

    public class Manager : UdonSharpBehaviour
    {
        Handler[] objects;

        public UdonBehaviour EventListener;

        // Intented to be readonly properties. (but there's no way to limit readonly aceess in Udon.)
        public int TotalCount;
        public int FreeCount;
        
        
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

        #endregion


        //////////////////////////////
        #region Internal implementation

        void GatherObjects()
        {
            objects = new Handler[transform.childCount];

            for (int i = 0; i < transform.childCount; i++) {
                Transform child = transform.GetChild(i);
                objects[i] = child.GetComponent<Handler>();
                debug("GetComponent i=" + i);
                debug("  handler=" + objects[i].gameObject.name);
            }

            // TODO check one Handler for each child.

            TotalCount = objects.Length;
        }

        Handler FindFreeOwned()
        {
            var n = objects.Length;
            for (int i = 0; i < n; i++) {
                var obj = objects[i];
                if (obj.IsFreeOwned())
                {
                    return obj;
                }
            }
            return null;
        }

        Handler FindFreeNotOwned()
        {
            // Random for better assignment to avoid race condition
            var n = objects.Length;
            int randOffset = Random.Range(0, n);
            
            for (int i = 0; i < n; i++) {
                var obj = objects[(i + randOffset) % n];
                if (obj.IsFreeNotOwned())
                {
                    return obj;
                }
            }
            return null;
        }


        // This actually counts not using event
        void UpdateCounts()
        {
            FreeCount = 0;

            if (objects == null) // not initiated yet
            {
                TotalCount = -1;
                return;
            }
            
            var n = objects.Length;
            for (int i = 0; i < n; i++) {
                if (objects[i].IsFree())
                {
                    FreeCount++;
                }
            }

            TotalCount = n;
        }

        void PropagateEvent(Handler eventSource, string eventName)
        {
            if (EventListener)
            {
                EventListener.SetProgramVariable("EXUR_EventSource", eventSource);
                EventListener.SetProgramVariable("EXUR_EventName", eventName);
                EventListener.SendCustomEvent("EXUR_RecieveEvent");
            }
        }

        #endregion


        //////////////////////////////
        #region Module private interface

        // TODO reconsider interface design. (event type as method name or method parameter??)

        public Handler EXUR_EventSource;
        public string EXUR_EventName;

        public void EXUR_RecieveEvent()
        {
            // TODO propagate to user
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
                UpdateCounts();

                debug("EXUR_RecieveEvent. " + EXUR_EventName + " from " + EXUR_EventSource.gameObject.name);
                PropagateEvent(EXUR_EventSource, EXUR_EventName);
            }
            EXUR_EventSource = null;
            EXUR_EventName = null;
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

        public void EXUR_AcquireObject() {
            Handler targetHandler = FindFreeOwned();
            if (!targetHandler)
            {
                targetHandler = FindFreeNotOwned();
            }

            if (!targetHandler)
            {
                // TODO Returning error. (What interface is good for UdonGraph user?)
                log("EXUR_AcquireObject: No free object");
            }
            else
            {
                debug("EXUR_AcquireObject: selected target=" + targetHandler.gameObject.name);
                targetHandler.TryToUse();
            }
        }

        #endregion
    }

}

