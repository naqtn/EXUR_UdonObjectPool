
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
        public void EXUR_InitializedToOwn()
        {
            DispImage.color = MineInactiveColor;
            log("EXUR_InitializedToOwn");
        }
        public void EXUR_InitializedToIdle()
        {
            DispImage.color = OthersInactiveColor;
            log("EXUR_InitializedToIdle");
        }
        public void EXUR_InitializedToUsing()
        {
            DispImage.color = OthersActiveColor;
            log("EXUR_InitializedToUsing");
        }

        // start-stop while not owned
        public void EXUR_StartedBeingUsed()
        {
            DispImage.color = OthersActiveColor;
            log("EXUR_StartedBeingUsed");
        }
        public void EXUR_StoppedBeingUsed()
        {
            DispImage.color = OthersInactiveColor;
            log("EXUR_StoppedBeingUsed");
        }

        // Result of start
        public void EXUR_FailedToUseByTimeout()
        {
            DispImage.color = WarningColor;
            log("EXUR_FailedToUseByTimeout");
        }
        public void EXUR_FailedToUseByRaceCondition()
        {
            DispImage.color = WarningColor;
            log("EXUR_FailedToUseByRaceCondition");
        }
        public void EXUR_EnterUsingFromWaiting()
        {
            DispImage.color = MineActiveColor;
            log("EXUR_EnterUsingFromWaiting");
        }
        public void EXUR_EnterUsingFromOwn()
        {
            DispImage.color = MineActiveColor;
            log("EXUR_EnterUsingFromOwn");
        }

        // Result of stop
        public void EXUR_ExitUsingByRequest()
        {
            DispImage.color = MineInactiveColor;
            log("EXUR_ExitUsingByRequest");
        }
        public void EXUR_TriedToReleaseNotOwnError()
        {
            DispImage.color = WarningColor;
            log("EXUR_TriedToReleaseNotOwnError");
        }

        // Lost ownership
        public void EXUR_ExitUsingByLostOwnership()
        {
            DispImage.color = OthersActiveColor;
            log("EXUR_ExitUsingByLostOwnership");
        }
        public void EXUR_LostOwnershipOnIdle()
        {
            DispImage.color = OthersInactiveColor;
            log("EXUR_LostOwnershipOnIdle");
        }

        public void EXUR_RetrievedFromUsing()
        {
            DispImage.color = MineInactiveColor;
            log("EXUR_RetrievedFromUsing");
        }
        public void EXUR_RetrievedFromIdle()
        {
            DispImage.color = MineInactiveColor;
            log("EXUR_RetrievedFromIdle");
        }
    }
}
