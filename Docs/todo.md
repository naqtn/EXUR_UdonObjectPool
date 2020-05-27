# TODOs

Refinement
============================================================

### Tag value Empty and null
How to handle current U# bug?
It's not useful for user program to use empty to describe free.
So EXUR should accept both. (?)

### add more simplified API
* failed to use

### Rename event name
* to ease to understand. and add prefix to avoid conflict.

### Write test cases
* user program error case

### disable debug log

### check SDK limitation with latest SDK.

### stress test
* late join with many objects
    * delay on initialization based on transform.GetSiblingIndex()


More samples
============================================================

### Make samples using from UdonGraph 

### sample: without-manager usage
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

### Acquire random one

For example, it could be used for dealing cards.

### Reset

Master (or any client?) collects all object's ownership and let them be idle.

### add pooled object with VRCInstantiation
In the future, if VRCInstantiation becomes network supported.


TODOs Before Release
============================================================

### write documents

Short "howto use" in readme:

* basic usage
    * prepare object and component
        * structure
    * put your script
        * event based design
    * call master to acquire object
    * call handler to release object
    * implement listener to know error
* advanced usage

* update introduction part of readme
* update features list in readme


Manual:

* method API
* event details
    * simplified and detailed
    * meaning of "use".
        * Exclusively "control" the object
        * Remote client react to the control
* figures (svg files)
    * this is local view, not system-wide

### make prefabs

### make a simple sample scene

### Create package and deploy test
Doc should be moved to inside Asset

### Research how to create "U# optional installation" release

