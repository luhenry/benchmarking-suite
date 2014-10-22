using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;

namespace BenchmarkingSuite.Common.Models
{
	[Table(Name = "configurations")]
	public class Configuration : DatabaseModel<Configuration>
	{
		[Column(Name = "arguments")]
		public string Arguments { get; set; }

		[Column(Name = "envvar")]
		public string EnvironmentVariables { get; set; }

		public static void Initialize () {}

		static Configuration ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS configurations (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , arguments TEXT NOT NULL
				      , envvar TEXT NOT NULL
				      , UNIQUE (arguments, envvar)
				);
			", Connection).ExecuteNonQuery();
		}

		public Configuration () : base ()
		{
		}

		internal Configuration (bool isNew) : base (isNew)
		{
		}
    }
}

