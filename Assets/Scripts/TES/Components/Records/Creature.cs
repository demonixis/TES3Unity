﻿using TESUnity.ESM;
using TESUnity.ESM.Records;
using UnityEngine;

namespace TESUnity.Components.Records
{
    public class Creature : RecordComponent
    {
        protected CREARecord _creature;

        void Start()
        {
            _creature = (CREARecord)record;

            transform.rotation = Quaternion.Euler(-70, 0, 0);

            TryAddScript(_creature.Script);
        }
    }
}
