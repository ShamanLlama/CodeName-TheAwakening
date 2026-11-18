using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class PlaytestContractTests
    {
        // Set AWAKENING_PLAYTEST_BATCH_OUTPUT to export the exact sealed body strings for validate.py.
        [Fact]
        public void SyntheticRunExportsSealedBatchesForTheRealGtcValidator()
        {
            var r = new Rig(); r.Start(); var c = r.Capture; var state = r.State;
            var bodies = new List<string>();
            Action seal = () => bodies.Add((string)c.SealBatch()["body"]);
            seal();
            state.Status = "playing"; state.NextLogId++;
            c.Tick(1000, "planning"); c.EmitEvent("starting_region_selected", M("region", "africa"), state);
            c.Checkpoint("tutorial_completed", null, state);
            foreach (string action in new[] { "recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "scatter", "silence", "exploit", "convert", "reveal_figure" })
            {
                state.ElapsedMonths++; state.NextLogId++;
                c.Tick(250, "simulation");
                c.EmitEvent("action_performed", M("action", action, "region", "africa", "spent", M("influence", action == "scatter" ? 0 : 12.5, "dread", action == "silence" ? 15 : 0, "followers", 0)), state);
            }
            c.EmitEvent("doctrine_selected", M("doctrine", "terror", "spent", M("influence", 50, "dread", 0, "followers", 0)), state);
            state.Status = "alignment";
            c.EmitEvent("phase_changed", M("from_phase", "playing"), state);
            c.Checkpoint("first_site_ready", "africa", state);
            c.Tick(2000, "background"); c.EmitEvent("time_checkpoint", M("reason", "visibility_changed"), state);
            c.Tick(400, "excluded"); c.EmitEvent("capture_gap", M("reason", "unknown_time", "missing_events", null), state);
            seal();
            string run = (string)c.Data["active"];
            var saved = Saved(c, state); c.Configure(Configuration(), saved); Assert.True(c.Restore()); c.Resume(state);
            Assert.Equal(run, c.Data["active"]);
            state.Status = "awakening";
            c.Tick(500, "planning");
            c.EmitEvent("awakening_started", M("spent", M("influence", 20, "dread", 35, "followers", 0)), state);
            c.Tick(10000, "simulation"); state.ElapsedMonths = 210; state.Status = "won"; state.IsOver = true;
            c.EmitEvent("run_finished", M("outcome", "won", "reason", "awakened"), state);
            seal();
            Assert.True(c.QueueComment("Synthetic observation: dispersión y silencio 🐙"));
            var jobs = A(c.Data, "jobs");
            bodies.Add((string)((Dictionary<string, object>)jobs[jobs.Count - 1])["body"]);
            Assert.Equal(4, bodies.Count);
            var events = bodies.Select(b => (Dictionary<string, object>)JsonReader.Parse(b)).SelectMany(b => A(b, "events")).Cast<Dictionary<string, object>>().ToList();
            Assert.Equal("run_finished", events.Last()["type"]);
            Assert.Equal(JsonWriter.Write(events[events.Count - 2]), JsonWriter.Write(events.Last()));
            Assert.Equal(Enumerable.Range(1, events.Count - 1), events.Take(events.Count - 1).Select(e => (int)N(e, "sequence")));
            string output = Environment.GetEnvironmentVariable("AWAKENING_PLAYTEST_BATCH_OUTPUT");
            if (string.IsNullOrEmpty(output)) return;
            Directory.CreateDirectory(output);
            for (int i = 0; i < bodies.Count; i++) File.WriteAllText(Path.Combine(output, $"batch-{i + 1:00}.json"), bodies[i]);
            var negative = (Dictionary<string, object>)JsonReader.Parse(bodies[1]); negative["adapter"] = "awakening-1";
            File.WriteAllText(Path.Combine(output, "invalid-awakening-1-scatter.json"), JsonWriter.Write(negative));
        }
    }
}
