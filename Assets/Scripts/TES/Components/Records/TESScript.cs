﻿using TESUnity.ESM;
using UnityEngine;

namespace TESUnity.Components.Records
{
    public class TESScript : RecordComponent
    {
        private SCPTRecord _script;

#if UNITY_EDITOR
        // For Debug Pupose Only
        public string ScriptName;
        [TextArea(4, 10)]
        public string ScriptContent;
#endif

        private void Start()
        {
            _script = (SCPTRecord)record;

#if UNITY_EDITOR
            ScriptName = _script.Header.Name;
            ScriptContent = _script.Text;

            if (TESManager.instance.logEnabled)
            {
                Debug.Log($"Script: {ScriptName} Added!");
            }
#endif
        }
    }
}
