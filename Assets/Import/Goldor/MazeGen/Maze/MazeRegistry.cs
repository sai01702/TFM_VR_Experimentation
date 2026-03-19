using System.Collections.Generic;
using UnityEngine;


namespace MazeGen
{
    public class MazeRegistry
    {
        private static readonly int MaxAutoID = 100;
        private Dictionary<int, Dictionary<int, (string, GameObject)>> _representationMap;

        /// <summary>
        /// Create a new registry use to associate GameObject to maze part representation
        /// </summary>
        public MazeRegistry()
        {
            _representationMap = new Dictionary<int, Dictionary<int, (string, GameObject)>>();
        }
        
        /// <summary>
        /// Check if primordial ID is already in the registry
        /// </summary>
        /// <param name="id">Primordial ID</param>
        /// <returns>Return true if primordial ID is already in the registry</returns>
        public bool VerifyPrimordialIdExist(int id)
        {
            if (!_representationMap.ContainsKey(id))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Check if type ID is already in the registry
        /// </summary>
        /// <param name="primordialID">Primordial ID</param>
        /// <param name="typeID">Type ID</param>
        /// <returns>Return true if type ID is already in the registry</returns>
        public bool VerifyTypeIdExist(int primordialPartID, int typeID)
        {
            if (!VerifyPrimordialIdExist(primordialPartID))
                return false;
            
            if (!_representationMap[primordialPartID].ContainsKey(typeID))
                return false;

            return true;
        }
        
        /// <summary>
        /// Add a new primordial associtation to the registry
        /// </summary>
        /// <param name="id">Primordial ID</param>
        /// <param name="nameId">Name ID</param>
        /// <param name="baseRepresentation">GameObject reprensenting this primordial ID</param>
        /// <returns>Return true if primordial ID is successfully added</returns>
        public bool RegisterPrimordial(int id, string nameId, GameObject baseRepresentation)
        {
            if (_representationMap.ContainsKey(id))
            {
                Debug.LogWarning("This id is already used");
                return false;
            }
            _representationMap.Add(id,new Dictionary<int, (string, GameObject)>
            {
                {0, (nameId, baseRepresentation)}
            });
            return true;
        }

        /// <summary>
        /// Add a new type associtation to the registry
        /// </summary>
        /// <param name="primordialPartID">Primordial ID</param>
        /// <param name="typeID">Type ID</param>
        /// <param name="nameId">Name ID</param>
        /// <param name="representation">GameObject reprensenting this type ID</param>
        /// <returns>Return true if type ID is successfully added</returns>
        public bool RegisterType(int primordialPartID, int typeID, string nameId, GameObject representation)
        {
            if (!VerifyPrimordialIdExist(primordialPartID))
                return false;

            Dictionary<int, (string, GameObject)> typeMap = _representationMap[primordialPartID];

            if (typeMap.ContainsKey(typeID))
            {
                typeMap[typeID] = (nameId,representation);
            }
            else
            {
                typeMap.Add(typeID, (nameId, representation));
            }

            return true;
        }
        
        /// <summary>
        /// Override the representation of a primordial ID
        /// </summary>
        /// <param name="id">Primordial ID</param>
        /// <param name="representation">GameObject reprensenting this primordial ID</param>
        /// <returns>Return true if primordial ID is successfully overrided</returns>
        public bool SetPrimordial(int id, GameObject representation)
        {
            if (!VerifyPrimordialIdExist(id))
                return false;

            _representationMap[id][0] = (_representationMap[id][0].Item1, representation);
            return true;
        }
        
        /// <summary>
        /// Override the representation and name ID of a primordial ID
        /// </summary>
        /// <param name="id">Primordial ID</param>
        /// <param name="nameId">new Name ID</param>
        /// <param name="representation">GameObject reprensting this primordial ID</param>
        /// <returns>Return true if primordial ID and name ID is successfully overrided</returns>
        public bool SetPrimordial(int id, string nameId, GameObject representation)
        {
            if (!VerifyPrimordialIdExist(id))
                return false;

            _representationMap[id][0] = (nameId, representation);
            return true;
        }

        /// <summary>
        /// Add a new type associtation to the registry
        /// </summary>
        /// <param name="primordialPartID">Primordial ID the type belong to</param>
        /// <param name="nameId">Name ID of type</param>
        /// <param name="representation">GameObject reprensenting this type ID</param>
        /// <returns>Return the type ID if type is successfully added else return -1</returns>
        public int RegisterType(int primordialPartID, string nameId, GameObject representation)
        {
            if (primordialPartID < 0)
            {
                Debug.LogWarning("Can't handle negative ID please change it");
                return -1;
            }
            
            if (!VerifyPrimordialIdExist(primordialPartID))
                return -1;
            
            Dictionary<int, (string, GameObject)> typeMap = _representationMap[primordialPartID];
            
            for (int i = 0; i < MaxAutoID; i++)
            {
                if (!typeMap.ContainsKey(i))
                {
                    typeMap.Add(i, (nameId, representation));
                    return i;
                }
            }
            Debug.LogWarning("No type id found (maybe maxAutoId value is too low)");
            return -1;
        }
        
        /// <summary>
        /// Override the representation and name ID of a type ID
        /// </summary>
        /// <param name="primordialPartID">Primordial ID the type belong to</param>
        /// <param name="typeID">Type ID</param>
        /// <param name="nameId">new Name ID</param>
        /// <param name="representation">GameObject reprensting this type ID</param>
        /// <returns>Return true if type ID and name ID is successfully overrided</returns>
        public bool SetType(int primordialPartID, int typeID, string nameId, GameObject representation)
        {
            if (!VerifyTypeIdExist(primordialPartID,typeID))
                return false;
            
            _representationMap[primordialPartID][typeID] = (nameId, representation);
            return true;
        }

        /// <summary>
        /// Get the representation of a primordial ID
        /// </summary>
        /// <param name="id">Primordial ID</param>
        /// <returns>Return representation if primordial ID is found else return null</returns>
        public GameObject GetPrimordial(int id)
        {
            if (!VerifyPrimordialIdExist(id))
                return null;

            return _representationMap[id][0].Item2;
        }
        
        /// <summary>
        /// Get the representation of a type ID
        /// </summary>
        /// <param name="primordialPartID">Primordial ID the type belong to</param>
        /// <param name="typeID">Type ID</param>
        /// <returns>Return representation if type ID is found else return null</returns>
        public GameObject GetType(int primordialPartID, int typeID)
        {
            if (!VerifyPrimordialIdExist(primordialPartID))
                return null;

            Dictionary<int, (string, GameObject)> typeMap = _representationMap[primordialPartID];
            
            if (typeMap.ContainsKey(typeID))
            {
                return typeMap[typeID].Item2;
            }
            else
            {
                Debug.LogWarning("can't get typeID base representation will be return");
                return typeMap[0].Item2;
            }
        }
    }
}
