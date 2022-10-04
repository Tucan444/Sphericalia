# Sphericalia

This is a tool for unity to create games and demos in 2D [spherical space](https://www.youtube.com/watch?v=0hgLUIniO08 "video explaining 2D spherical space").
For example heres a [spherical maze](https://www.youtube.com/watch?v=6ry3OA6xEv0 "devlog about the maze").

![spherical maze](/Sphericalia/documentationAssets/thumbnail.png)

## Quick Documentation

This mainly contains tips that are not obvious.

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

To add shapes use object adder window. (should be at the bottom shelf) <br>
If the window isnt presend you can open it by Spherical>AddObject.
