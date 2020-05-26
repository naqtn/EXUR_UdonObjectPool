# EXUR (EXclusive Use and Reusing objects) Manual

### Basic structure 

EXUR object pool structure:

    Object Pool GameObject
     UdonBehaviour hosts EXUR Manager 
   
        Pooled GameObject (1)
         UdonBehaviour hosts EXUR Handler 
         UdonBehaviour hosts User Script for each object
        
        Pooled GameObject (2)
         UdonBehaviour hosts EXUR Handler 
         UdonBehaviour hosts User Script for each object
        
        ...

You can add other components on this structure.


### EXUR Manager
EXUR Manager manages pooled game objects.
It gathers child objects and treats as pool objects.
User program call EXUR Manager custom evnet to operate the pool.


### EXUR Handler
EXUR Handler operates one pooled object.
Target pooled object is a GameObject where the EXUR Handler is attached.
User program call EXUR Handler custom event to operate each pooled object.

It must be the first UdonBehaviour in Pooled GameObject.


### User Script
Library user prepare UserScript if needed.

EXUR expects that has certain custom events and program variables.
Their name and type are defined by EXUR.
So you can make UserScript with UdonGraph and UdonSharp.

These custom events and variables are optional.
You only need to implement what you need because it is just ignored if it doesn't exist.
You just implement you want to know the event happened.

UdonBehaviour components placed in Pooled GameObject are treated as User Script.
Also UdonBehaviours attached to children GameObject can be User Script.
(see `IncludeChildrenToSendEvent` option)


## Event (callback) API

### Handler callback API

UserScripts can react for these events if they implement appropriate custom event.

| Category                              | Custom-Event Name                 | Comment                                  |
|---------------------------------------|-----------------------------------|------------------------------------------|
| Result of initialization              |                                   | spontaneous transition after Start       |
|                                       | InitializedToOwn                  |                                          |
|                                       | InitializedToIdle                 |                                          |
|                                       | InitializedToUsing                |                                          |
| start-stop Transition while not owned |                                   | another players start and stop using     |
|                                       | StartedToUseByOthers              |                                          |
|                                       | StoppedUsingByOthers              |                                          |
| Result of start request               |                                   | happens after  aquire request            |
|                                       | FailedToUseByTimeout              |                                          |
|                                       | FailedToUseByRaceCondition        |                                          |
|                                       | EnterUsingFromWaiting             |                                          |
|                                       | EnterUsingFromOwn                 |                                          |
| Result of stop request                |                                   | happends after release request           |
|                                       | ExitUsingByRequest                |                                          |
|                                       | AttemptToReleaseNotOwnObjectError |                                          |
| Retrieve by Master                    |                                   | InitializedToOwn is not categorized here |
|                                       | RetrievedAfterOwnerLeftWhileUsing |                                          |
|                                       | RetrievedAfterOwnerLeftWhileIdle  |                                          |
| High level simplified API             |                                   |                                          |
|                                       | EXUR_Reinitialize                 |                                          |
|                                       | EXUR_Finalize                     |                                          |


### EXUR_RecieveEvent custom event

Set listener UdonBehaviour to `Iwsd.EXUR.Manager.EventListener`


| EXUR_EventSource | EXUR_EventName               | EXUR_EventAdditionalInfo   | Comment                               |
|------------------|------------------------------|----------------------------|---------------------------------------|
| EXUR.Handler     | (see "Handler callback API") | -                          | Aggregate and propagete Handler event |
| -                | InUseBySelf                  | -                          | Already in use by myself              |
| -                | InUseByOthers                | -                          | Already in use by others              |
| -                | NoFreeObject                 | -                          | No more free object                   |
| -                | ManagerFailure               | human readable description | program error etc.                    |

"-" means null

### ManagerFailure details

This is internal specification so it will be changed.

| description header | possible case                                |
|--------------------|----------------------------------------------|
| InternalError:     |                                              |
|                    | Multiple objects have identical tag          |
|                    | localTagBuffer is not clear though it's free |
| UserProgramError:  |                                              |
|                    | specified tag is null                        |
|                    | specified tag is empty                       |
|                    | No Brother UdonBehaviour                     |
|                    | Does not have EXUR_Tag variable              |
|                    | Does not have EXUR_LastUsedTime variable     |


