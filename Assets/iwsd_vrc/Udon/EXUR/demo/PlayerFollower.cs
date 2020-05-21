
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR.Demo {

    public class PlayerFollower : UdonSharpBehaviour
    {
        UdonBehaviour EXUR_Handler;

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
            // NOTE: Generics version GetComponentInParent<UdonBehaviour>(); can't be used because unexposed.
            // (SDK limitation of VRCSDK3-UDON-2020.05.12.10.33)
            EXUR_Handler = (UdonBehaviour)transform.parent.GetComponentInParent(typeof(UdonBehaviour));
            
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
                        // Stop working when ParkingTimer expired.
                        EXUR_Handler.SendCustomEvent("ReleaseObject");
                    }
                    else if (ParkingTimer <= ParkingDuration * 0.1f)
                    {
                        // At end of ParkingDuration, go back to original position
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


        //////////////////////////////
        // Methods that will be called from EXUR library
        
        public void EXUR_Reinitialize()
        {
            Working = true;

            // Call SetOwner on this game object to be workable for "synchronize position".
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            ParkingTimer = 0.0f;
            Constraint.enabled = true;
        }

        public void EXUR_Finalize()
        {
            Constraint.enabled = false;

            Working = false;
        }

        public void RetrievedAfterOwnerLeftWhileUsing()
        {
            transform.position = ConstraintSrcOrgPos;
        }

    }
}
