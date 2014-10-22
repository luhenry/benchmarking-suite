using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "counters")]
	public class Counter : DatabaseModel<Counter>
	{
		[Column (Name = "section")]
		public string Section { get; set; }

		[Column (Name = "name")]
		public string Name { get; set; }

		[Column (Name = "type")]
		public CounterType Type { get; set; }

		[Column (Name = "unit")]
		public CounterUnit Unit { get; set; }

		[Column (Name = "variance")]
		public CounterVariance Variance { get; set; }

		public static void Initialize () {}

		static Counter ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS counters (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , section TEXT NOT NULL
				      , name TEXT NOT NULL
				      , type INTEGER NOT NULL
				      , unit INTEGER NOT NULL
				      , variance INTEGER NOT NULL
				      , UNIQUE (section, name)
				);
			", Connection).ExecuteNonQuery ();
		}

		public Counter () : base ()
		{
		}

		protected Counter (bool isNew) : base (isNew)
		{
		}

		public static Counter FindBySectionAndName (string section, string name)
		{
			return FindBy (new SortedDictionary<string, object> () { { "section", section }, { "name", name } }).FirstOrDefault ();
		}
	}

	public enum CounterType : Int64
	{
		Int = 0,
		UInt = 1,
		Word = 2,
		Long = 3,
		ULong = 4,
		Double = 5,
		String = 6,
		TimeInterval = 7,
	}

//	public struct CounterType : Int64
//	{
//		static CounterType Int = 0;
//		static CounterType UInt = 1;
//		static CounterType Word = 2;
//		static CounterType Long = 3;
//		static CounterType ULong = 4;
//		static CounterType Double = 5;
//		static CounterType String = 6;
//		static CounterType TimeInterval = 7;
//
//		public string ToString ()
//		{
//			switch (this) {
//			case Int: return "Int";
//			case UInt: return "UInt";
//			case Word: return "Word";
//			case Long: return "Long";
//			case ULong: return "ULong";
//			case Double: return "Double";
//			case String: return "String";
//			case TimeInterval: return "TimeInterval";
//			default: throw new Exception ("Unknonw CounterType");
//			}
//		}
//	}

	public enum CounterUnit : Int64
	{
		Raw = 0 << 24,
		Bytes = 1 << 24,
		Time = 2 << 24,
		Count = 3 << 24,
		Percentage = 4 << 24,
	}

	public enum CounterVariance : Int64
	{
		Monotonic = 1 << 28,
		Constant = 1 << 29,
		Variable = 1 << 30,
	}
}

