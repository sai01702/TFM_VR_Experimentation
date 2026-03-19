@page SetupGuide Setup Guide

# Fast Setup Guide

To start using MazeGen add a maze loader component to an object that is responsible of the maze, then attach a maze representation to the maze loader and a maze instantiator to the maze loader.<br>
If you want to change the maze apparance create a new maze instantiator, put your custom prefabs (make sure there are square and all the same size) specify the prefab size and attach it to the maze loader. <br>
If you want to change the maze generated you can create a new maze representation or modify an existing onen, then change the maze settings as you want, don't forget to put it in the maze loader.

You're now ready ! 

Click "create" button to generate a maze, click "load" button to load the maze in the scene and "bake" to optimize the maze. 

Large mazes need to be loaded with the "load and bake" button to be backed just after the  instantiation otherwise it may crash the editor.

For more customization look at @ref CustomizingGuide !