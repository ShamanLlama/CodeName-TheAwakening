using System;
using System.IO;
using Awakening.Core.Save;
using Awakening.Platform;
using NUnit.Framework;

namespace Awakening.Tests.Editor
{
    public class GameStorageTests
    {
        [Test]
        public void SecondNativeWindowCannotWriteTheSameSave()
        {
            string directory = Path.Combine(Path.GetTempPath(), "awakening-lock-" + Guid.NewGuid().ToString("N"));
            var first = new GameStorage();
            var second = new GameStorage();
            var recovered = new GameStorage();
            try
            {
                first.Initialize(directory);
                second.Initialize(directory);
                Assert.That(first.Ready && second.Ready, Is.True);
                Assert.That(first.Writable, Is.True, "one writer per save");
                Assert.That(second.Writable, Is.False);
                string envelope = SaveData.EncodeSave(SaveData.DefaultSave());
                Assert.That(second.Persist(envelope), Is.EqualTo(-1), "second window cannot save");
                int generation = first.Persist(envelope);
                Assert.That(generation, Is.GreaterThan(0), "owner can save");
                Assert.That(first.SavedGeneration, Is.EqualTo(generation), "native save is durable before acknowledgement");
                Assert.That(File.ReadAllText(first.FilePath), Is.EqualTo(envelope));
                second.Dispose();
                Assert.That(Directory.Exists(first.FilePath + ".lock"), Is.True, "non-owner cannot release the owner's lease");
                Assert.That(first.Persist(envelope), Is.GreaterThan(generation));
                first.Dispose();
                recovered.Initialize(directory);
                Assert.That(recovered.Writable, Is.True, "closing the owner releases the save");
                Assert.That(recovered.LoadRaw(), Is.EqualTo(envelope));
                Assert.That(recovered.Persist(envelope), Is.GreaterThan(0));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                recovered.Dispose();
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
