using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "recipes"), JsonObject(MemberSerialization.OptOut)]
	public class Recipe : DatabaseModel<Recipe>
	{
		[Column (Name = "benchmark_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "benchmark_id", ForeignType = typeof (Benchmark), ForeignKey = "id")]
		public long BenchmarkID { get; set; }

		[Column (Name = "configuration_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "configuration_id", ForeignType = typeof (Configuration), ForeignKey = "id")]
		public long ConfigurationID { get; set; }

		[Column (Name = "device_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "device_id", ForeignType = typeof (Device), ForeignKey = "id")]
		public long DeviceID { get; set; }

		Benchmark benchmark = null;
		public Benchmark Benchmark {
			get { return benchmark == null ? (benchmark = IsConnectionOpen ? Benchmark.FindByID (BenchmarkID) : null) : benchmark; }
			set {
				BenchmarkID = value.ID;
				benchmark = value;
			}
		}

		Configuration configuration = null;
		public Configuration Configuration {
			get { return configuration ?? (configuration = IsConnectionOpen ? Configuration.FindByID (ConfigurationID) : null); }
			set { configuration = value; }
		}

		Device device = null;
		public Device Device {
			get { return device ?? (device = IsConnectionOpen ? Device.FindByID (DeviceID) : null); }
			set { device = value; }
		}

		public static void Initialize () {}

		static Recipe ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS recipes (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , benchmark_id INTEGER REFERENCES benchmarks (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , configuration_id INTEGER REFERENCES configurations (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , device_id INTEGER REFERENCES devices (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , UNIQUE (benchmark_id, configuration_id, device_id)
				);
			", Connection).ExecuteNonQuery ();

			var benchmark1 = new Benchmark () { Name = "ironpython-50k", Executable = "IronPython-2.0B2/ipy.exe", Arguments = "IronPython-2.0B2/pystone.py 50000" }.Save (true);
			var benchmark2 = new Benchmark () { Name = "ironpython-100k", Executable = "IronPython-2.0B2/ipy.exe", Arguments = "IronPython-2.0B2/pystone.py 100000" }.Save (true);
			var benchmark3 = new Benchmark () { Name = "ironpython-500k", Executable = "IronPython-2.0B2/ipy.exe", Arguments = "IronPython-2.0B2/pystone.py 500000" }.Save (true);
			var benchmark4 = new Benchmark () { Name = "ironpython-1m", Executable = "IronPython-2.0B2/ipy.exe", Arguments = "IronPython-2.0B2/pystone.py 1000000" }.Save (true);

			var device1 = new Device () { Name = "mac mini", Architecture = "amd64" }.Save (true);
			var device2 = new Device () { Name = "mac mini", Architecture = "x86" }.Save (true);

			var configuration1 = new Configuration () { Arguments = "", EnvironmentVariables = "" }.Save (true);

			new Recipe () { BenchmarkID = benchmark1.ID, ConfigurationID = configuration1.ID, DeviceID = device1.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark1.ID, ConfigurationID = configuration1.ID, DeviceID = device2.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark2.ID, ConfigurationID = configuration1.ID, DeviceID = device1.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark2.ID, ConfigurationID = configuration1.ID, DeviceID = device2.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark3.ID, ConfigurationID = configuration1.ID, DeviceID = device1.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark3.ID, ConfigurationID = configuration1.ID, DeviceID = device2.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark4.ID, ConfigurationID = configuration1.ID, DeviceID = device1.ID }.Save (true);
			new Recipe () { BenchmarkID = benchmark4.ID, ConfigurationID = configuration1.ID, DeviceID = device2.ID }.Save (true);
		}

		public Recipe () : base ()
		{
		}

		internal Recipe (bool isNew) : base (isNew)
		{
		}

		public static List<Recipe> FilterByDevicesAndBenchmarks (List<Device> devices, List<Benchmark> benchmarks)
		{
			var devices_sql = devices.Count == 0 ? "1 = 1" : String.Format ("device_id IN ({0})", String.Join (",", Enumerable.Repeat ("?", devices.Count)));
			var benchmarks_sql = benchmarks.Count == 0 ? "1 = 1" : String.Format ("benchmark_id IN ({0})", String.Join (",", Enumerable.Repeat ("?", benchmarks.Count)));

			var sql = new SqliteCommand (String.Format ("SELECT id, benchmark_id, configuration_id, device_id FROM recipes WHERE {0} AND {1}",
				devices_sql, benchmarks_sql), Connection);

			if (devices.Count > 0) {
				foreach (var d in devices)
					sql.Parameters.Add (new SqliteParameter () { Value = d.ID });
			}

			if (benchmarks.Count > 0) {
				foreach (var b in benchmarks)
					sql.Parameters.Add (new SqliteParameter () { Value = b.ID });
			}

			using (var reader = sql.ExecuteReader ()) {
				var recipes = new List<Recipe> ();

				while (reader.Read ()) {
					recipes.Add (new Recipe (false) { ID = reader.GetInt64 (0), BenchmarkID = reader.GetInt64 (1),
										ConfigurationID = reader.GetInt64 (2), DeviceID = reader.GetInt64 (3) });
				}

				return recipes;
			}
		}
	}
}

