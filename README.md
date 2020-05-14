# EXUR_UdonObjectPool
EXUR (EXclusive Use and Reusing objects) is yet another VRChat Udon object pooling library.

On VRChat, networked objects are always owned by someone. Ownership doesn't mean it is actually used. This library adds "free or using attribute" to that.

## Functions and Features
* It manages objects placed on the scene at startup.
* It serves exclusive use of each object.
* Player can use multiple objects concurrently.
* Of course, it supports player joining and leaving.
* Optionally it can handle "theft". (planning feature)
* Implemented with U# but UdonGraph friendly interface. (to be tested)

## Technical Features
* Not "owner centralized" design. Each player can weakly hold objects in unused state.
* Synced variable free. Instead, it uses CustomNetworkEvent to tell "using".
* Provides various callbacks to track objects state.
