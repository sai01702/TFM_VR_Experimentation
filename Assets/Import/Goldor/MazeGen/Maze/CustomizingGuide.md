@page CustomizingGuide Customizing Guide

# Customizing

You can customize MazeGen by overriding existing classes to know how scripts are working and how to extend them look at Script Architecture.The most common thing to be customized would be the agent used to generate the maze.

# Scripts architecture

1. Maze representation
    1. Maze generator
        1. Processors
    2. Maze settings
    3. Maze registry
2. Maze instantiator
3. Maze loader

## 1. Maze representation

The maze representation is the root of the maze system. It is responsible for storing the maze data, the generator, and the settings to generate it. The maze representation is a `ScriptableObject`.

### a. Maze generator
The maze generator is a `ScriptableObject` that can be added to the maze representation. The maze generator can be configured in the inspector. In this scriptable object you can add maze processor to customize the generated maze.

#### i. Processors

Maze processors are additional script that run over the maze when the first generation is finished to modify it, they can be used for example to replace some part. To use it you need to attach the maze processor scriptable object to the maze generator, and it will run automatically.

### b. Maze settings

The maze settings is a component that is attached to the maze representation. It is responsible for storing the maze settings. The maze settings is a `ScriptableObject` that can be added to the maze representation. The maze settings can be configured in the inspector.

### c. Maze registry

The Maze registry is an object that store the association between part id and gameobject. At each generation a new registry is created if no registry is provided. The registry is used to instantiate the maze.

## 2. Maze instantiator

The maze instantiator determine how you maze will be represented in the scene by storingthe different basics parts prefab of the maze. Maze part have direction the Z axis of each prefab must be the exit way of the prefab except for the 3 and 4 way parts the 4 way part don’t care about the direction but the 3 way also name “split path” in the maze instantiator must have the Z axis pointing to the new path created.

The maze instancier also determine the size parametter depending on your prefabs size, your prefab must be square and all must be the same size, the 2 previous part are 2 unit size so the size parametter of the maze instancier must be 2.

The last parametter is the group resolution it is used for optimisation, if you chose to bake the maze all part will be combined to limit the number of draw calls with a group resolution of 50 it will get a 50 by 50 maze part square and group them then if you bake the maze all these part will be combined.

## 3. Maze loader

The maze loader is a component to load the maze representation created by all the scripts described before, you can load a maze by your own script, but it is a basic script if you don’t want specific loading method