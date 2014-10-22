using System;
using System.Collections.Generic;
using XamarinProfiler.Core;

namespace BenchmarkingSuite.Common.Inspector
{
	public class History
	{
		SortedDictionary<ulong, Dictionary<ulong, Counter>> history;
		Inspector inspector;

		public History (Inspector inspector)
		{
			this.history = new SortedDictionary<ulong, Dictionary<ulong, Counter>> ();

			this.inspector = inspector;
			this.inspector.Sample += OnInspectorSampleTick;
		}

		public SortedDictionary<ulong, List<Counter>> this [ulong since, ulong limit] {
			get {
				var updated = new SortedDictionary<ulong, List<Counter>> ();

				lock (this.history) {
					ulong first = 0;

					foreach (var e in this.history) {
						if (e.Key > since) {
							if (first == 0)
								first = e.Key;
							else if (e.Key > first + limit)
								break;

							updated.Add (e.Key, new List<Counter> (e.Value.Values));
						}
					}
				}

				return updated;
			}
		}

		public ulong LastTimestamp {
			get {
				ulong last = 0;

				lock (this.history) {
					foreach (var e in this.history) {
						if (e.Key > last) {
							last = e.Key;
						}
					}
				}

				return last;
			}
		}

		public void Clear ()
		{
			this.history.Clear ();
		}

		private void OnInspectorSampleTick (object sender, Inspector.SampleEventArgs a)
		{
			Dictionary<ulong, Counter> counters;

			if (this.history.Count == 0) {
				counters = new Dictionary<ulong, Counter> ();

				foreach (var c in a.Counters)
					counters.Add (c.CounterID, c);
			} else {
				counters = new Dictionary<ulong, Counter> (this.history [LastTimestamp]);

				foreach (var c in a.Counters)
					counters [c.CounterID] = c;
			}

			lock (this.history) {
				this.history.Add (a.Timestamp, counters);
			}
		}
	}
}

