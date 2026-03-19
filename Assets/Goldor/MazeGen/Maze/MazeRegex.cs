using System.Collections.Generic;
using UnityEngine;


namespace MazeGen
{
    public class MazeRegex
    {
        private static readonly int MAXAutoID = 100;
        private Dictionary<int, Dictionary<int, (string, GameObject)>> _representationMap;

        /// <summary>
        /// Create a new regex use to associate GameObject to maze part representation
        /// </summary>
        public MazeRegex()
        {
            _representationMap = new Dictionary<int, Dictionary<int, (string, GameObject)>>();
        }
        
        public bool VerifyPrimordialIdExist(int id)
        {
            if (!_representationMap.ContainsKey(id))
            {
                return false;
            }

            return true;
        }
        
        public bool VerifyTypeIdExist(int primordialPartID, int typeID)
        {
            if (!VerifyPrimordialIdExist(primordialPartID))
                return false;
            
            if (!_representationMap[primordialPartID].ContainsKey(typeID))
                return false;

            return true;
        }
        
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
        
        public bool SetPrimordial(int id, GameObject representation)
        {
            if (!VerifyPrimordialIdExist(id))
                return false;

            _representationMap[id][0] = (_representationMap[id][0].Item1, representation);
            return true;
        }
        
        public bool SetPrimordial(int id, string nameId, GameObject representation)
        {
            if (!VerifyPrimordialIdExist(id))
                return false;

            _representationMap[id][0] = (nameId, representation);
            return true;
        }

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
            
            for (int i = 0; i < MAXAutoID; i++)
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
        
        public bool SetType(int primordialPartID, int typeID, string nameId, GameObject representation)
        {
            if (!VerifyTypeIdExist(primordialPartID,typeID))
                return false;
            
            _representationMap[primordialPartID][typeID] = (nameId, representation);
            return true;
        }

        public GameObject GetPrimordial(int id)
        {
            if (!VerifyPrimordialIdExist(id))
                return null;

            return _representationMap[id][0].Item2;
        }
        
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
