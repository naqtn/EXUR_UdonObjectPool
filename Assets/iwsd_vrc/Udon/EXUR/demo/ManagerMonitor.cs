
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR.Demo {

    public class ManagerMonitor : UdonSharpBehaviour
    {
        // multi line bufffer
        // Udon doesn't expose StringBuilder.
        const int LINES_COUNT = 5;
        string[] lines = new string[LINES_COUNT];
        int nextIdx = 0;
        string addLine(string s)
        {
            lines[nextIdx] = s;
            nextIdx = (nextIdx + 1) % LINES_COUNT;

            string t = "";
            for (int i = 0; i < LINES_COUNT; i++)
            {
                var e = lines[(nextIdx + i) % LINES_COUNT];
                if (e != null)
                {
                    t += e;
                    t += "\n";
                }
            }
            return t;
        }
        
        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void log(string s)
        {
            if (DebugText)
            {
                DebugText.text = addLine(s);
            }
        }

        [SerializeField]
        UnityEngine.UI.Text StatusReport;

        [SerializeField]
        Iwsd.EXUR.Manager MonitorTarget;

        void Start()
        {
            if (MonitorTarget)
            {
                // It can not cast "U# this" to UdonBehaviour. so by SetProgramVariable.
                MonitorTarget.SetProgramVariable("EventListener", this);

                EXUR_RecieveEvent(); // to initialize display 
            }
            else
            {
                log("MonitorTarget is not specified.");
            }
        }
        
        // implements listener interface
        [HideInInspector] public UdonBehaviour EXUR_EventSource;
        [HideInInspector] public string EXUR_EventName;
        [HideInInspector] public string EXUR_EventAdditionalInfo;
        public void EXUR_RecieveEvent()
        {
            if (EXUR_EventName != null)
            {
                if (EXUR_EventSource)
                {
                    log($"'{EXUR_EventName}' on '{EXUR_EventSource.gameObject.name}'");
                }
                else
                {
                    log($"'{EXUR_EventName}'");
                }
                
                if (EXUR_EventAdditionalInfo != null)
                {
                    log($"'{EXUR_EventAdditionalInfo}'");
                }
            }

            var t = MonitorTarget.TotalCount;
            var f = MonitorTarget.FreeCount;
            StatusReport.text = $"Free {f}, Used {t - f}, Total {t}";
        }

    }
}
