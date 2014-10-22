using System;
using XamarinProfiler.Core.Reader;
using XamarinProfiler.Core;
using System.Collections.Generic;

namespace BenchmarkingSuite.Common.Inspector
{
	public class Inspector
	{
		LogReader Reader;
		InspectorEventListener Listener;

		public delegate void SampleEventHandler (object sender, SampleEventArgs e);
		public event SampleEventHandler Sample;
		public event SampleEventHandler UpdatedSample;

		public Inspector (string filename)
		{
			Reader = new LogReader (filename, true);
			Listener = new InspectorEventListener (this);
		}

		public void Run ()
		{
			if (!Reader.OpenReader ())
				return;

			foreach (var buf in Reader.ReadBuffer (Listener)) {
				if (Reader.IsStopping)
					break;
				if (buf == null)
					continue;
			}
		}

		public class SampleEventArgs : EventArgs
		{
			public ulong Timestamp { get; internal set; }
			public List<Counter> Counters  { get; internal set; }
		}

		class InspectorEventListener : EventListener
		{
			Inspector Inspector;

			Dictionary<ulong, Counter> Counters = new Dictionary<ulong, Counter> ();

			public InspectorEventListener (Inspector inspector)
			{
				Inspector = inspector;
			}

			public override void HandleSampleCountersDesc (List<Tuple<string, string, ulong, ulong, ulong, ulong>> counters)
			{
				foreach (var t in counters)
					Counters.Add (t.Item6, new Counter (t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, null));
			}

			public override void HandleSampleCounters (ulong timestamp, List<Tuple<ulong, ulong, object>> values)
			{
				var counters = values.ConvertAll<Counter> (t => {
					return Counters [t.Item1] = new Counter (Counters [t.Item1], t.Item3);
				});

				if (Inspector.UpdatedSample != null)
					Inspector.UpdatedSample (this, new SampleEventArgs () { Timestamp = timestamp, Counters = counters });

				if (Inspector.Sample != null)
					Inspector.Sample (this, new SampleEventArgs { Timestamp = timestamp, Counters = new List<Counter> (Counters.Values) });
			}
		}
	}
}

