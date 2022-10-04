# Sphericalia

This is a tool for unity to create games and demos in [2D spherical space](https://www.youtube.com/watch?v=0hgLUIniO08 "video explaining 2D spherical space").
For example heres a [spherical maze](https://www.youtube.com/watch?v=6ry3OA6xEv0 "devlog about the maze").

![spherical maze](/Sphericalia/documentationAssets/thumbnail.png)

## Quick Documentation

This mainly contains tips that are not obvious. <br>
[skip to scripting](#scripting)

### Getting started

To start download the project and open in unity.
You will be meeted by the sample scene.
All the objects are contained under the ___SphericalSpace___ object.

### Core objects

There are 3 core objects that you need to know.

1. Camera

    > Camera decides how the scene is gonna be rendered. Forexample projection type, resolution. (resolution should be multiple of 32 on both axis)
    > Basic player controls are also configured here.

2. Background

    > When theres bg image, the blend between color and image is configured in color's alpha channel.

3. Lighting

    > Under this object are all the lights.
    > As for baked lighting the detail increases the resolution of lightmap exponentially. (8-9 recomended)
    > Soft shadow correnction is not working that well, its better to not have lights colliding with edges instead.
    > Baking fails if game starts.

### Other objects

#### Shapes

To add shapes use object adder window in the bottom shelf. <br>
If the window isnt presend you can open it by Spherical>AddObject. <br>
I suggest first creating object and then configuring position, rotation, scale ... as the drawn preview isnt completely responsive. <br>
Batch generation is possible here. <br>

These shapes are currently available:
- circle
- ngon
- general shape
- uv tiles

Triggers and empty objects will be covered in the script part.

#### Lights

All light objects are located under lighting object. <br>
To add a light open lighting object in inspector and click on add point light. <br>

Most important setting for point lights is **linear** that completly changes behaivour. <br>
The setting 3D in non-linear lights changes fallof from 1/d to 1/(d**2).

<p id="scripting"><p/>

## Scripting

This part looks into usefull functions in the engine.

### Spherical utilities

This part looks at files in the **Utilities** folder.

The files well look into are:
- spherical utilities
- spherical common functions
- spherical adder
- spherical converter

These follow the inheritance *utilities < common functions < adder < converter*.

Spherical common functions contains most functions you might wanna use, some extra less common are in utilities.
There are also functions for collisions, intersections, raycasts in utilities file.
To convert between coordinates look at converter.
Adder contains functions usefull for positioning objects on a sphere.

### Objects

For *circle, ngon, general shape, uv tiles* there are functions for their manipulation:
- move
- rotate (not in circle)
- scale
- change color
- toggle collider/trigger
- toggle invisible/empty

*Move, rotate, scale and change color* require the object to be non-static.
After *toggle collider/trigger* ssm.SortColliderTrigger() has to be used.
After *toggle invisible/empty* one of ssm.Populate() methods need to be used if object is non-static.

### Lights

All point lights are automatically dynamic and every property can be changed excluding *linear, baked lighting*.
Functions for point lights:
- toggle
- move, moveQ

If you change sphPosition call `Setup()` afterwards.

### Spherical Space Manager (ssm)

This file functions as the core of the engine pulling everything together. <br>
Its located on the ___SphericalSpace___ game object. <br>
To get it use public variable or `GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>()`.
