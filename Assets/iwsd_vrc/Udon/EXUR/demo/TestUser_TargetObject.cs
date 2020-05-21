
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TestUser_TargetObject : UdonSharpBehaviour
    {
        Iwsd.EXUR.Handler handler;

        void Start()
        {
            handler = transform.parent.GetComponent<Iwsd.EXUR.Handler>();
        }


        void Interact()
        {
            handler.ReleaseObject();
        }



        ///////////////////////////////
        [SerializeField]
        UnityEngine.UI.Image DispImage;

        public Color MineActiveColor = new Color(1.0f, 0.02f, 0.02f, 0.6f);
        public Color MineInactiveColor = new Color(0.8f, 0.5f, 0.5f, 0.3f);
        public Color WarningColor = new Color(0.9f, 0.9f, 0.3f, 0.5f);
        public Color OthersActiveColor = new Color(0.02f, 1.0f, 0.02f, 0.6f);
        public Color OthersInactiveColor = new Color(0.5f, 0.8f, 0.5f, 0.3f);

        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void log(string s)
        {
            if (DebugText)
            {
                DebugText.text = s;
            }
        }


        // Result of initialization
        public void InitializedToOwn()
        {
            DispImage.color = MineInactiveColor;
            log("InitializedToOwn");
        }
        public void InitializedToIdle()
        {
            DispImage.color = OthersInactiveColor;
            log("InitializedToIdle");
        }
        public void InitializedToUsing()
        {
            DispImage.color = OthersActiveColor;
            log("InitializedToUsing");
        }

        // start-stop while not owned
        public void StartedToUseByOthers()
        {
            DispImage.color = OthersActiveColor;
            log("StartedToUseByOthers");
        }
        public void StoppedUsingByOthers()
        {
            DispImage.color = OthersInactiveColor;
            log("StoppedUsingByOthers");
        }

        // Result of start
        public void FailedToUseByTimeout()
        {
            DispImage.color = WarningColor;
            log("FailedToUseByTimeout");
        }
        public void FailedToUseByRaceCondition()
        {
            DispImage.color = WarningColor;
            log("FailedToUseByRaceCondition");
        }
        public void EnterUsingFromWaiting()
        {
            DispImage.color = MineActiveColor;
            log("EnterUsingFromWaiting");
        }
        public void EnterUsingFromOwn()
        {
            DispImage.color = MineActiveColor;
            log("EnterUsingFromOwn");
        }

        // Result of stop
        public void ExitUsingByRequest()
        {
            DispImage.color = MineInactiveColor;
            log("ExitUsingByRequest");
        }

        // Lost ownership
        public void LostOwnershipOnUsing()
        {
            DispImage.color = OthersActiveColor;
            log("LostOwnershipOnUsing");
        }
        public void LostOwnershipOnIdle()
        {
            DispImage.color = OthersInactiveColor;
            log("LostOwnershipOnIdle");
        }

        // Retrieve by Master (see also InitializedToOwn)
        public void RetrievedAfterOwnerLeftWhileUsing()
        {
            DispImage.color = MineInactiveColor;
            log("RetrievedAfterOwnerLeftWhileUsing");
        }
        public void RetrievedAfterOwnerLeftWhileIdle()
        {
            DispImage.color = MineInactiveColor;
            log("RetrievedAfterOwnerLeftWhileIdle");
        }
    }
}
