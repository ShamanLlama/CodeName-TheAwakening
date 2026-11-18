using System;
using System.Collections.Generic;
using Awakening.Core.Playtest;

namespace Awakening.Platform
{
    /// <summary>
    /// The outbox persists by saving the whole game envelope, which holds its batches, through the
    /// controller; storage reports which generation is durable.
    /// </summary>
    public sealed class PlaytestStorageAdapter : IPlaytestStorage
    {
        readonly GameStorage _storage;
        readonly Func<long> _commit;

        public PlaytestStorageAdapter(GameStorage storage, Func<long> commit)
        {
            _storage = storage;
            _commit = commit;
        }

        public bool Ready => _storage.Ready;
        public bool Writable => _storage.Writable;
        public bool Failed => _storage.Failed;
        public long SavedGeneration => _storage.SavedGeneration;
        public void Poll() => _storage.Poll();
        public long Persist() => _commit();
    }

    /// <summary>Playtest POSTs through the browser bridge (WebGL) or UnityWebRequest (native).</summary>
    public sealed class PlaytestTransportAdapter : IPlaytestTransport
    {
        readonly GameStorage _storage;

        public PlaytestTransportAdapter(GameStorage storage) => _storage = storage;

        public void Send(string id, string endpoint, string body) => _storage.Send(id, endpoint, body);

        public IEnumerable<PlaytestResponse> PollResponses()
        {
            foreach (TransportResponse response in _storage.Responses())
                yield return new PlaytestResponse { Id = response.Id, Status = response.Status, Body = response.Body, RetryAfter = response.RetryAfter };
        }

        public void Cancel() => _storage.Cancel();
    }
}
