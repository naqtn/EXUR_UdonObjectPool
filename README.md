# EXUR_UdonObjectPool
EXUR (EXclusive Use and Reusing objects) is object pooling library for VRChat Udon.

This library provides useful funtions to make synchronized objects in multi player environment.

[Demo world is available](https://vrchat.com/home/launch?worldId=wrld_c05c3ccf-32e5-4233-80f8-4f8bb0d1d0ad)

## Basic Functions and Features
* It manages objects placed on the scene at startup.
* It serves exclusive use of each object for each player.
* Player can use multiple objects concurrently.
* And also supports "One object for each player" usecase.
* Of course, it supports player joining and leaving.
* For rejoined player, It can reassign identical object.
* Implemented with U# but UdonGraph friendly interface. (to be tested)

## Technical Features
* Synced variable free.
* Provides various callbacks to track objects state.
* Includes delay mechanism for synced variables. (A workarond Udon synced variable ownership problem)
* Not "owner centralized" design. Each player can weakly hold objects in unused state.


## How to use (Quick start guide)

### Install

(TODO write me!)


### Basic use

**1. Prepare EXUR object pool structure**

Make following structure in your scene.

    Object Pool GameObject
     UdonBehaviour hosts EXUR Manager

        Pooled GameObject
         UdonBehaviour hosts EXUR Handler
         UdonBehaviour hosts your Udon Program (see next step)


**2. Create your Udon program for a pooled GameObject**

Create your Udon program on Pooled GameObject

Add a custom event named `EXUR_Reinitialize`.
(If you're using UdonSharp, add `public void EXUR_Reinitialize(){}` method.)
This is called when local player acquired the pooled GameObject. So write programs as your needs.

Add a custom event named `EXUR_Finalize`. (UdonSharp `public void EXUR_Finalize(){}` )
This is called when local player release (or lost) the pooled GameObject.


**3. Arrange starter and stopper**

To start using pooled object, call `AcquireObject` custom event on `EXUR Manager`.
To stop using, call `ReleaseObject` on `EXUR Handler`.

(In UdonSharp class names are `Iwsd.EXUR.Manager` and `Iwsd.EXUR.Handler`)


**4.Clone pooled GameObject**

Clone pooled GameObject as many as you need. EXUR Manager treats child GameObjects as pooled objects.


### Error handling

To response the situation in which no more object in the pool when your program tried to acquire, do followings.

1. Create Udon program that have following program variables and custom event:

        [HideInInspector] public UdonBehaviour EXUR_EventSource;
        [HideInInspector] public string EXUR_EventName;
        [HideInInspector] public string EXUR_EventAdditionalInfo;
        public void EXUR_ReceiveEvent()
        {
            // your code 
        }

2. Set that UdonBehaviour to `Iwsd.EXUR.Manager.EventListener`
3. When "no more object" happens, `EXUR_EventName` is set the value with `"NoFreeObject"` and your `EXUR_ReceiveEvent` is called.
  So check `EXUR_EventName` and do the response you want.

Via `EXUR_ReceiveEvent` you can also react to other situation.
Refer [Manager event API](Docs/manual.md#manager-event-api) section of the manual for details.


### Further usage

* One object for each player
    * Call `Iwsd.EXUR.Manager.AcquireObjectForEachPlayer()`
* Deactivate when idle
    * Enable `DeactivateWhenIdle` on Handler
