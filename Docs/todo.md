# TODOs

Refinement
============================================================

### Tag value Empty and null
How to handle current U# bug?
It's not useful for user program to use empty to describe free.
So EXUR should accept both. (?)

### add more simplified API
* faild to use
* remote started to use
* remote stopped to use

### Rename event name
* to ease to understand. and add prefix to avoid conflict.

### Write testcase
* user program error case

### disable debug log

### check SDK limitation with latest SDK.

### stress test
* late join with many objects
    * delay on initialization based on index amoung children


More samples
============================================================

### Make samples using from UdonGraph 

### sample: without-manager ussage
It will be useful for ownership passing management

### One object for each player case
* Automatically player gets one object
* put object at above and front of head tracking (?)

### sample: Restore player position when rejoin


Idea, Enhancement
============================================================

### add option to allow theft.
* argument or handler switch?

### add a boolean to tag feature to represent it is preserved one or not

### "continue to use when previous owner left" option.
To be able to do finalize operation, for example. 


### Add "pooling object child depth level"

An integer that specifies where to find pooling object.
If there are so many pooling objects, that will be too long child list in Hierarchy view.
So it will be useful if it's possible to bundle them in sub groups.



TODOs Before Release
============================================================

### write documents

Short "howto use" in readme:

* basic ussage
    * prepare object and component
        * structure
    * put your script
        * event based design
    * call master to aquire object
    * call handler to release object
    * implement listener to know error
* advanced ussage
    * deactivate when idle
    * one object for each player

* update introduction part of readme
* update features list in readme


Manual:

* method API
* event details
    * simplified and detailed
* Tag feature
    * tag is a string
    * tag is used to identify objects
    * string.Empty means "not used". it can not be used usual value. (null is also)
    * brother UdonBehavior must have certain variables
    * it try to preserve and use identical object if possible 
    * purge by Least recently used (LRU) algorithm
    * user can update EXUR_LastUsedTime
    * interface note: how to passing argument
* ownership
    * ownership is controled on "Pooled GameObject"
    * Delay for safer sync variable writing
    * handler will be pass to user program by manager event after ownership obtained.
* IncludeChildrenToSendEvent handler option
    * to be exact "descendant"
    * note: child object ownership is not controled by EXUR
* DeavtivateWhenIdle handler option
    * pooled object when it becomes idle.
* additional note for features
    * synced variable free
        * to avoid network load
    * Try to use owned object if possible
* DebugText
    * optional
    * to view internal log
* Assignment algorithm described
    * try to use own object first for speed
    * random to avoid race condition
    * Least recently used (LRU) algorithm for tag
* figures (svg files)
    * this is local view, not system-wide
* internal design note
    * Tag need synced variable. so it implemented on another UdonBehavior.
    to let core (Handler) be synced variable free.
    * method name prefix policy.
    to avoid conflict.


### Create package and deploy test
Doc should be moved to inside Asset

### Research how to create "U# optional installation" release
