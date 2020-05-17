
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR.Demo {

    public class PlayerFollower : UdonSharpBehaviour
    {
        [SerializeField]
        UdonBehaviour EXU_Handler;

        [SerializeField]
        Transform ControlTarget;

        Vector3 ControlOrgPos;
        Vector3 OffsetVector;
        UnityEngine.Animations.ParentConstraint Constraint;
        Transform ConstraintSrc;
        Vector3 ConstraintSrcOrgPos;

        bool Working = false;
        float ParkingTimer = 0;
        const float ParkingDuration = 3.0f;
        
        void Start()
        {
            ControlOrgPos = ControlTarget.position;
            OffsetVector = ControlTarget.localPosition;

            Constraint = GetComponent<UnityEngine.Animations.ParentConstraint>();
            ConstraintSrc = Constraint.GetSource(0).sourceTransform;
            ConstraintSrcOrgPos = ConstraintSrc.position;
        }

        void Update()
        {
            if (Working)
            {
                if (ParkingTimer <= 0) // Not in parking mode
                {
                    var playerPos = Networking.LocalPlayer.GetPosition();
                    ControlTarget.position = playerPos + OffsetVector;
                }
                else
                {
                    ParkingTimer -= Time.deltaTime;
                    if (ParkingTimer <= 0)
                    {
                        // Go back to original position and stop.
                        EXU_Handler.SendCustomEvent("StopUsing");
                    }
                    else if (ParkingTimer <= ParkingDuration * 0.1f)
                    {
                        // At end of ParkingDuration
                        ConstraintSrc.position = ConstraintSrcOrgPos;
                    }
                }

            }

        }

        void Interact()
        {
            if (Networking.IsOwner(this.gameObject))
            {
                // Into parking mode
                ParkingTimer = ParkingDuration;
                ControlTarget.position = ControlOrgPos;
            }
        }

        public void EnterUsingFromWaiting()
        {
            // Call SetOwner on this game object to be workable for "synchronize position".
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

            ParkingTimer = 0.0f;
            Working = true;
            Constraint.enabled = true;
        }
        public void EnterUsingFromOwn()
        {
            EnterUsingFromWaiting();
        }

        public void ExitUsingByRequest()
        {
            Working = false;
            Constraint.enabled = false;
        }
        public void LostOwnershipOnUsing()
        {
            ExitUsingByRequest();
        }
    }
}
