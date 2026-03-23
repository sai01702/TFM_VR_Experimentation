/*
 *  Structure the maze generation
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MazeGen
{
    public abstract class MazeGenerator : ScriptableObject
    {
        protected MazeSettings MazeSettings;
        protected MazeRegistry MazeRegistry;
        protected MazeContainer MazeContainer;
        
        protected bool Generating { get; set; }
        public bool Finish => !Generating;
        
        public void SetGeneratingState(bool generating)
        {
            Generating = generating;
        }
        
        public MazeGenerator SetSettings(MazeSettings settings)
        {
            MazeSettings = settings;
            return this;
        }
            
        public MazeGenerator SetRegex(MazeRegistry registry)
        {
            MazeRegistry = registry;
            return this;
        }

        public MazeSettings GetSettings()
        {
            return MazeSettings;
        }
            
        public MazeRegistry GetRegex()
        {
            return MazeRegistry;
        }
        
        /// <summary>
        /// Generate a maze with the given registry and settings.
        /// (Set property "Generating" to true if generation is async and reset it to false when finished)
        /// </summary>
        /// <returns>return the maze container</returns>
        public abstract MazeContainer Generate();

        public static Type[] GetAllGenerator()
        {
            var parent = typeof(MazeGenerator);
            IEnumerable<Type> generators = Assembly.GetExecutingAssembly().GetTypes().Where(type => parent.IsAssignableFrom(type) && !type.IsAbstract);
            return generators.ToArray();
        }
    }
}
