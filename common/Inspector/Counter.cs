using System;

namespace BenchmarkingSuite.Common.Inspector
{
	public struct Counter {
		public readonly string Section;
		public readonly string Name;
		public readonly ulong Type;
		public readonly ulong Unit;
		public readonly ulong Variance;
		public readonly ulong CounterID;

		public readonly object Value;

		public Counter (string section, string name, ulong type, ulong unit, ulong variance, ulong counterID, object value)
		{
			Section = section;
			Name = name;
			Type = type;
			Unit = unit;
			Variance = variance;
			CounterID = counterID;

			Value = value;
		}

		public Counter (Counter o, object value)
		{
			Section = o.Section;
			Name = o.Name;
			Type = o.Type;
			Unit = o.Unit;
			Variance = o.Variance;
			CounterID = o.CounterID;

			Value = value;
		}
	}
}

