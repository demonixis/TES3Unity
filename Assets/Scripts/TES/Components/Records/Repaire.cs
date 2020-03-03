﻿using TESUnity.ESM;
using TESUnity.ESM.Records;

namespace TESUnity.Components.Records
{
    public class Repaire : RecordComponent
    {
        void Start()
        {
            var REPA = (REPARecord)record;
            //objData.icon = TESUnity.instance.Engine.textureManager.LoadTexture(WPDT.ITEX.value, "icons"); 
            objData.name = REPA.Name;
            objData.weight = REPA.Data.Weight.ToString();
            objData.value = REPA.Data.Value.ToString();
            objData.interactionPrefix = "Take ";

            TryAddScript(REPA.Script);
        }
    }
}