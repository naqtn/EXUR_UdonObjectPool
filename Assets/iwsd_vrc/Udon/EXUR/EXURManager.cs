
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class EXURManager : UdonSharpBehaviour
    {
        EXURHandler[] objects;

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
            objects = new EXURHandler[transform.childCount];

            for (int i = 0; i < transform.childCount; i++) {
                Transform child = transform.GetChild(i);
                objects[i] = child.GetComponent<EXURHandler>();
                debug("GetComponent i=" + i);
                debug("  handler=" + objects[i].gameObject.name);
            }

            // TODO check one EXURHandler for each child.
        }

        EXURHandler FindFreeOwned()
        {
            for (int i = 0; i < objects.Length; i++) {
                if (objects[i].IsFreeOwned())
                {
                    return objects[i];
                }
            }
            return null;
        }

        EXURHandler FindFreeNotOwned()
        {
            // TODO better assignment to avoid race condition
            for (int i = 0; i < objects.Length; i++) {
                if (objects[i].IsFreeNotOwned())
                {
                    return objects[i];
                }
            }
            return null;
        }

        #endregion


        //////////////////////////////
        #region Module private interface

        // TODO count used/unsed/tocal objects. and serves it user.
        // TODO reconsider interface design. (event type as method name or method parameter??)

        public EXURHandler eventSource;
        public string eventName;

        public void recieveEvent()
        {
            // TODO propagate to user
            if (!eventSource)
            {
                warn("null eventSource");
            }
            else if (eventName == null)
            {
                warn("null eventName");
            }
            else
            {
                log("recieveEvent. " + eventName + " from " + eventSource.gameObject.name);
            }
            eventSource = null;
            eventName = null;
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

        public void TryToGetOne() {
            EXURHandler targetHandler = FindFreeOwned();
            if (!targetHandler)
            {
                targetHandler = FindFreeNotOwned();
            }

            if (!targetHandler)
            {
                // TODO Returning error. (What interface is good for UdonGraph user?)
                log("TryToGetOne: No free object");
            }
            else
            {
                debug("TryToGetOne: selected target=" + targetHandler.gameObject.name);
                targetHandler.TryToUse();
            }
        }

        #endregion
    }

}

