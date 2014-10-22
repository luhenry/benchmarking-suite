using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "samples"), JsonObject(MemberSerialization.OptOut)]
	public class Sample : DatabaseModel<Sample>
	{
		[Column (Name = "run_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "run_id", ForeignType = typeof (Run), ForeignKey = "id")]
		public long RunID { get; set; }

		[Column (Name = "counter_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "counter_id", ForeignType = typeof (Counter), ForeignKey = "id")]
		public long CounterID { get; set; }

		[Column (Name = "timestamp")]
		public ulong Timestamp { get; set; }

		[Column (Name = "value")]
		public object Value { get; set; }

		Run run = null;
		public Run Run {
			get { return run ?? (run = IsConnectionOpen ? Run.FindByID (RunID) : null); }
			set { run = value; }
		}

		Counter counter = null;
		public Counter Counter {
			get { return counter ?? (counter = IsConnectionOpen ? Counter.FindByID (CounterID) : null); }
			set { counter = value; }
		}

		public static void Initialize () {}

		static Sample ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS samples (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , run_id INTEGER REFERENCES runs (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , counter_id INTEGER REFERENCES counters (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , timestamp INTEGER NOT NULL
				      , value NUMERIC NOT NULL
				      , UNIQUE (run_id, counter_id, timestamp)
				);
			", Connection).ExecuteNonQuery ();
		}
	}
}

