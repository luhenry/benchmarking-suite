using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "benchmarks")]
	public class Benchmark : DatabaseModel<Benchmark>
	{
		[Column (Name = "name")]
		public string Name { get; set; }

		[Column (Name = "executable")]
		public string Executable { get; set; }

		[Column (Name = "arguments")]
		public string Arguments { get; set; }

		public static void Initialize () {}

		static Benchmark ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS benchmarks (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , name TEXT NOT NULL
				      , executable TEXT NOT NULL
				      , arguments TEXT NOT NULL
				      , UNIQUE (name)
				);", Connection).ExecuteNonQuery ();
		}

		public Benchmark () : base ()
		{
		}

		internal Benchmark (bool isNew) : base (isNew)
		{
		}

		public static Benchmark FindByName (string name)
		{
			return FindBy (new SortedDictionary<string, object> () { { "name", name } }).FirstOrDefault ();
		}
	}
}
