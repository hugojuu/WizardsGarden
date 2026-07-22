using System;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>JSON 세이브 파일 입출력·버전 게이트 검증. 임시 폴더 사용 — 실제 세이브 경로를 건드리지 않는다.</summary>
    public class SaveRepositoryTests
    {
        string _directory;
        SaveRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "WizardGardenTests", Guid.NewGuid().ToString("N"));
            _repository = new SaveRepository(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            var data = SaveData.CreateNew();
            data.eventSeconds = 1234.5;
            data.resourceSeconds = 6789.25;
            data.lastSavedUtcTicks = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc).Ticks;

            _repository.Save(data);

            Assert.IsTrue(_repository.Exists);
            Assert.IsTrue(_repository.TryLoad(out var loaded));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(data.eventSeconds, loaded.eventSeconds);
            Assert.AreEqual(data.resourceSeconds, loaded.resourceSeconds);
            Assert.AreEqual(data.lastSavedUtcTicks, loaded.lastSavedUtcTicks);
        }

        [Test]
        public void TryLoad_NoFile_ReturnsFalse()
        {
            Assert.IsFalse(_repository.Exists);
            Assert.IsFalse(_repository.TryLoad(out var loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void TryLoad_CorruptJson_ReturnsFalseWithoutThrowing()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath, "{이건 JSON이 아님!!");

            Assert.IsFalse(_repository.TryLoad(out _));
        }

        [Test]
        public void TryLoad_FutureVersion_ReturnsFalse()
        {
            var data = SaveData.CreateNew();
            data.version = SaveData.CurrentVersion + 1;
            _repository.Save(data);

            Assert.IsFalse(_repository.TryLoad(out _));
        }

        [Test]
        public void TryLoad_MissingVersionField_ReturnsFalse()
        {
            // 버전 필드 없는 JSON → version 0 → 알 수 없는 구버전으로 거부
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath, "{\"eventSeconds\":10.0}");

            Assert.IsFalse(_repository.TryLoad(out _));
        }

        [Test]
        public void Save_OverwritesExistingFile()
        {
            var first = SaveData.CreateNew();
            first.eventSeconds = 1.0;
            _repository.Save(first);

            var second = SaveData.CreateNew();
            second.eventSeconds = 2.0;
            _repository.Save(second);

            Assert.IsTrue(_repository.TryLoad(out var loaded));
            Assert.AreEqual(2.0, loaded.eventSeconds);
        }

        [Test]
        public void Delete_RemovesFile()
        {
            _repository.Save(SaveData.CreateNew());
            Assert.IsTrue(_repository.Exists);

            _repository.Delete();

            Assert.IsFalse(_repository.Exists);
        }
    }
}
