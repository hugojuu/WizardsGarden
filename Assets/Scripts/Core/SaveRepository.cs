using System;
using System.IO;
using UnityEngine;

namespace WizardGarden.Core
{
    /// <summary>
    /// JSON 세이브 파일 입출력. 디렉터리 경로 주입 — 런타임은 Application.persistentDataPath,
    /// 테스트는 임시 폴더. 쓰기는 임시 파일 → 교체(중간 중단으로 인한 세이브 파손 방지).
    /// 손상·미래 버전 파일은 TryLoad가 false를 돌려줄 뿐 예외를 던지지 않는다.
    /// </summary>
    public sealed class SaveRepository
    {
        public const string FileName = "save.json";

        readonly string _directory;

        public SaveRepository(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("세이브 디렉터리 경로가 비어 있음", nameof(directory));
            _directory = directory;
        }

        public string FilePath => Path.Combine(_directory, FileName);

        public bool Exists => File.Exists(FilePath);

        public void Save(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Directory.CreateDirectory(_directory);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            File.Move(tempPath, FilePath);
        }

        public bool TryLoad(out SaveData data)
        {
            data = null;
            if (!Exists)
                return false;

            try
            {
                string json = File.ReadAllText(FilePath);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null || !SaveMigrator.TryMigrate(loaded))
                    return false;
                data = loaded;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Delete()
        {
            if (Exists)
                File.Delete(FilePath);
        }
    }
}
