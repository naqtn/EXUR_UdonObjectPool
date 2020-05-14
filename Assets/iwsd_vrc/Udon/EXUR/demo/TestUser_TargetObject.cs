
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TestUser_TargetObject : UdonSharpBehaviour
    {
        EXURHandler handler;

        void Start()
        {
            handler = transform.parent.GetComponent<EXURHandler>();
        }


        void Interact()
        {
            handler.StopUsing();
        }



        ///////////////////////////////
        [SerializeField]
        UnityEngine.UI.Image DispImage;

        public Color ActiveColor = new Color(1.0f, 0.02f, 0.02f, 0.5f);
        public Color InactiveColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);


        [SerializeField]
        UnityEngine.UI.Text DebugText;

        void log(string s)
        {
            if (DebugText)
            {
                DebugText.text = s;
            }
        }


        public void InitializedAsMaster()
        {
            log("InitializedAsMaster");
        }
        public void InitializedAsNotMine()
        {
            log("InitializedAsNotMine");
        }
        public void ExitUsingByPlayerLeft()
        {
            log("ExitUsingByPlayerLeft");
        }
        public void OwnedByMaster()
        {
            log("OwnedByMaster");
        }
        public void EnterUsingFromWaiting()
        {
            log("EnterUsingFromWaiting");
            DispImage.color = ActiveColor;
        }
        public void FailedToStartUsing()
        {
            log("FailedToStartUsing");
        }
        public void LostOwnershipOnUsing()
        {
            log("LostOwnershipOnUsing");
        }
        public void LostOwnershipOnUnusing()
        {
            log("LostOwnershipOnUnusing");
        }
        public void EnterUsingFromOwn()
        {
            log("EnterUsingFromOwn");
            DispImage.color = ActiveColor;
        }
        public void ExitUsingByRequest()
        {
            log("ExitUsingByRequest");
            DispImage.color = InactiveColor;
        }

    }
}
