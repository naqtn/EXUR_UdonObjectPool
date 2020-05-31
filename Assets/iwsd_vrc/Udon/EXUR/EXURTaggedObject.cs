using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Iwsd.EXUR {

    public class TaggedObject : UdonSharpBehaviour
    {
        [UdonSynced] [HideInInspector]
        public string EXUR_Tag;
        [UdonSynced] [HideInInspector]
        public int EXUR_LastUsedTime;
    }
}
