﻿using TESUnity.ESM;
using TESUnity.ESM.Records;

namespace TESUnity.Components.Records
{
    public class ProbeItem : RecordComponent
    {
        void Start()
        {
            var PROB = (PROBRecord)record;
            //objData.icon = TESUnity.instance.Engine.textureManager.LoadTexture(WPDT.ITEX.value, "icons"); 
            objData.name = PROB.Name;
            objData.weight = PROB.Data.Weight.ToString();
            objData.value = PROB.Data.Value.ToString();
            objData.interactionPrefix = "Take ";

            TryAddScript(PROB.Script);
        }
    }
}