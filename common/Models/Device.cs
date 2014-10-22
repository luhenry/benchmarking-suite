using System;
using System.Collections.Generic;

using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "devices")]
	public class Device : DatabaseModel<Device>
	{
		[Column (Name = "name")]
		public string Name { get; set; }

		[Column (Name = "description")]
		public string Description { get; set; }

		[Column (Name = "architecture")]
		public string Architecture { get; set; }

		public static void Initialize () {}

		static Device ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS devices (
				        id INTEGER PRIMARY KEY AUTOINCREMENT
				      , name TEXT NOT NULL
				      , description TEXT
				      , architecture TEXT NOT NULL
				      , UNIQUE (name, architecture)
				);
			", Connection).ExecuteNonQuery ();
		}

		public Device () : base ()
		{
		}

		internal Device (bool isNew) : base (isNew)
		{
		}

		public static List<Device> FilterByName (string name)
		{
			return FindBy (new SortedDictionary<string, object> () { { "name", name } });
		}

		public static List<Device> FilterByArchitecture (string architecture)
		{
			return FindBy (new SortedDictionary<string, object> () { { "architecture", architecture } });
		}

		public static Device FindByNameAndArchitecture (string name, string architecture)
		{
			return FindBy (new SortedDictionary<string, object> () { { "name", name }, { "architecture", architecture } }).FirstOrDefault ();
		}
	}
}
