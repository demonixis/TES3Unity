﻿using System;
using System.Collections.Generic;
using System.IO;

namespace TESUnity
{
    using ESM;
    using TESUnity.ESM.Records;
    using TESUnity.NIF;
    using UnityEngine;

    public class MorrowindDataReader : IDisposable
    {
        private string _folderPath;
        private string _dataFilePath;
        private bool _disposing;
        public ESMFile MorrowindESMFile;
        public BSAFile MorrowindBSAFile;
        public ESMFile BloodmoonESMFile;
        public BSAFile BloodmoonBSAFile;
        public ESMFile TribunalESMFile;
        public BSAFile TribunalBSAFile;

        public string DataPath => _dataFilePath;
        public string FolderPath => _folderPath;

        public MorrowindDataReader(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
            _folderPath = string.Empty;

            var tmp = _dataFilePath.Split('\\');

            for (var i = 0; i < tmp.Length - 1; i++)
            {
                _folderPath += tmp[i];

                if (i < tmp.Length - 2)
                {
                    _folderPath += "\\";
                }
            }

            MorrowindESMFile = new ESMFile(dataFilePath + "/Morrowind.esm");
            MorrowindBSAFile = new BSAFile(dataFilePath + "/Morrowind.bsa");

            if (TESManager.instance?.loadExtensions ?? false)
            {
                if (File.Exists(dataFilePath + "/Bloodmoon.esm"))
                {
                    BloodmoonESMFile = new ESMFile(dataFilePath + "/Bloodmoon.esm");
                    BloodmoonBSAFile = new BSAFile(dataFilePath + "/Bloodmoon.bsa");

                    MorrowindESMFile.Merge(BloodmoonESMFile);
                }

                if (File.Exists(dataFilePath + "/Tribunal.esm"))
                {
                    TribunalESMFile = new ESMFile(dataFilePath + "/Tribunal.esm");
                    TribunalBSAFile = new BSAFile(dataFilePath + "/Tribunal.bsa");

                    MorrowindESMFile.Merge(TribunalESMFile);
                }
            }
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposing)
            {
                return;
            }

            _disposing = true;

            Close();
        }

        public void Close()
        {
            TribunalBSAFile?.Close();
            BloodmoonBSAFile?.Close();
            MorrowindBSAFile.Close();
        }

        #region Texture Loading

        public Texture2DInfo LoadTexture(string texturePath)
        {
            return LoadTexture(MorrowindBSAFile, texturePath);
        }

        public Texture2DInfo LoadTexture(BSAFile bsaFile, string texturePath)
        {
            var path = FindTexture(bsaFile, texturePath);
            if (path != null)
            {
                var fileData = bsaFile.LoadFileData(path);
                var fileExtension = Path.GetExtension(path);

                if (fileExtension?.ToLower() == ".dds")
                {
                    return DDS.DDSReader.LoadDDSTexture(new MemoryStream(fileData));
                }
                else
                {
                    Debug.LogWarning($"Unsupported texture type: {fileExtension}");
                }
            }
            else
            {
                Debug.LogWarning("Could not find file \"" + texturePath + "\" in a BSA file.");
            }

            return null;
        }

        private Texture2DInfo LoadLocalTexture(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath);

            if (fileExtension?.ToLower() == ".dds")
            {
                var textureData = File.ReadAllBytes(filePath);
                return DDS.DDSReader.LoadDDSTexture(new MemoryStream(textureData));
            }

            return null;
        }

        #endregion

        #region Model Loading

        public NiFile LoadNif(string filePath)
        {
            return LoadNif(MorrowindBSAFile, filePath);
        }

        public NiFile LoadNif(BSAFile bsaFile, string filePath)
        {
            var fileData = bsaFile.LoadFileData(filePath);
            var file = new NiFile(Path.GetFileNameWithoutExtension(filePath));
            file.Deserialize(new UnityBinaryReader(new MemoryStream(fileData)));
            return file;
        }

        private NiFile LoadLocalNif(string filePath)
        {
            var localPath = Path.Combine(_dataFilePath, filePath);

            if (File.Exists(localPath))
            {
                var fileData = File.ReadAllBytes(localPath);
                var file = new NiFile(Path.GetFileNameWithoutExtension(filePath));
                file.Deserialize(new UnityBinaryReader(new MemoryStream(fileData)));
                return file;
            }

            return null;
        }

        #endregion

        public T[] FindRecords<T>() where T : Record
        {
            return MorrowindESMFile.GetRecords<T>()?.ToArray() ?? null;
        }

        public byte[] LoadData(string path)
        {
            return MorrowindBSAFile.LoadFileData(path);
        }

        public string GetSound(string soundId)
        {
            var path = string.Empty;
            var records = FindRecords<SOUNRecord>();

            foreach (var sound in records)
            {
                if (sound.Id == soundId)
                {
                    path = sound.Name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return $"{_dataFilePath}\\Sound\\{path}";
        }

        private string GetSoundPath(string id)
        {
            var records = FindRecords<SOUNRecord>();

            foreach (var sound in records)
            {
                if (sound.Id == id)
                {
                    return sound.Name;
                }
            }

            return null;
        }

        public LTEXRecord FindLTEXRecord(int index)
        {
            List<Record> records = MorrowindESMFile.GetRecordsOfType<LTEXRecord>();
            LTEXRecord LTEX = null;

            for (int i = 0, l = records.Count; i < l; i++)
            {
                LTEX = (LTEXRecord)records[i];

                if (LTEX.IntValue == index)
                {
                    return LTEX;
                }
            }

            return null;
        }

        public LANDRecord FindLANDRecord(Vector2i cellIndices)
        {
            MorrowindESMFile.LANDRecordsByIndices.TryGetValue(cellIndices, out LANDRecord LAND);
            return LAND;
        }

        public SCPTRecord FindScript(string name)
        {
            var records = MorrowindESMFile.GetRecordsOfType<SCPTRecord>();

            foreach (var record in records)
            {
                var script = (SCPTRecord)record;
                var scriptName = script.Header.Name;

                if (scriptName == name)
                {
                    return script;
                }
            }

            return null;
        }

        public CELLRecord FindExteriorCellRecord(Vector2i cellIndices)
        {
            MorrowindESMFile.ExteriorCELLRecordsByIndices.TryGetValue(cellIndices, out CELLRecord CELL);
            return CELL;
        }

        public CELLRecord FindInteriorCellRecord(string cellName)
        {
            List<Record> records = MorrowindESMFile.GetRecordsOfType<CELLRecord>();
            CELLRecord CELL = null;

            for (int i = 0, l = records.Count; i < l; i++)
            {
                CELL = (CELLRecord)records[i];

                if (CELL.NAME.value == cellName)
                {
                    return CELL;
                }
            }

            return null;
        }

        public CELLRecord[] FindCellsRecordByRegion(string regionName)
        {
            var regions = MorrowindESMFile.GetRecords<REGNRecord>();
            var regionId = string.Empty;
            foreach (var region in regions)
            {
                if (region.Name == regionName)
                {
                    regionId = region.Id;
                }
            }

            if (string.IsNullOrEmpty(regionId))
            {
                return null;
            }

            var cells = MorrowindESMFile.GetRecords<CELLRecord>();
            var result = new List<CELLRecord>();

            foreach (var cell in cells)
            {
                if (cell?.RGNN?.value == regionId)
                {
                    result.Add(cell);
                }
            }

            return result.ToArray();
        }

        public CELLRecord FindCell(Vector2i gridCoords)
        {
            var records = MorrowindESMFile.GetRecordsOfType<CELLRecord>();
            var index = -1;
            var closestDistance = float.MaxValue;
            CELLRecord record = null;

            for (int i = 0, l = records.Count; i < l; i++)
            {
                record = (CELLRecord)records[i];

                var distance = Vector2i.Distance(record.gridCoords, gridCoords);

                if (distance < closestDistance)
                {
                    index = i;
                    closestDistance = distance;
                }
            }

            if (index > -1)
            {
                return (CELLRecord)records[index];
            }

            return null;
        }

        public CELLRecord FindInteriorCellRecord(Vector2i gridCoords)
        {
            List<Record> records = MorrowindESMFile.GetRecordsOfType<CELLRecord>();
            CELLRecord CELL = null;

            for (int i = 0, l = records.Count; i < l; i++)
            {
                CELL = (CELLRecord)records[i];

                if (CELL.gridCoords.X == gridCoords.X && CELL.gridCoords.Y == gridCoords.Y)
                {
                    return CELL;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the actual path of a texture.
        /// </summary>
        private string FindTexture(BSAFile bsaFile, string texturePath)
        {
            var textureName = Path.GetFileNameWithoutExtension(texturePath);
            var textureNameInTexturesDir = "textures/" + textureName;
            var texturePathWithoutExtension = Path.GetDirectoryName(texturePath) + '/' + textureName;

            var filePath = textureNameInTexturesDir + ".dds";
            if (bsaFile.ContainsFile(filePath))
            {
                return filePath;
            }

            filePath = textureNameInTexturesDir + ".tga";
            if (bsaFile.ContainsFile(filePath))
            {
                return filePath;
            }

            filePath = texturePathWithoutExtension + ".dds";
            if (bsaFile.ContainsFile(filePath))
            {
                return filePath;
            }

            filePath = texturePathWithoutExtension + ".tga";
            if (bsaFile.ContainsFile(filePath))
            {
                return filePath;
            }

            // Could not find the file.
            return null;
        }

        private string FindLocalTexture(string texturePath)
        {
            var textureName = Path.GetFileNameWithoutExtension(texturePath);
            var texturePathWithoutExtension = Path.Combine(_dataFilePath, "textures", textureName);
            var filePath = texturePathWithoutExtension + ".dds";

            if (File.Exists(filePath))
            {
                return filePath;
            }

            return null;
        }
    }
}