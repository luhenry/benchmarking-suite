using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BenchmarkingSuite.Common.Models
{
	public static class Database
	{
		static object ConnectionLock = new object ();

		public static SqliteConnection Connection {
			get;
			private set;
		}

		public static void OpenConnection (string source)
		{
			if (Connection == null) {
				lock (ConnectionLock) {
					if (Connection == null) {
						Connection = new SqliteConnection (String.Format ("Data Source={0};Version=3;", source));
						Connection.Open ();
					}
				}
			}
		}
	}

	public abstract class DatabaseModel
	{
		[Column (Name = "id", IsPrimaryKey = true)]
		public long ID { get; set; }

		protected bool IsNew {
			get;
			set;
		}

		protected static bool IsConnectionOpen {
			get {
				return Database.Connection != null
					&& (Database.Connection.State == ConnectionState.Connecting
						|| Database.Connection.State == ConnectionState.Executing
						|| Database.Connection.State == ConnectionState.Fetching
						|| Database.Connection.State == ConnectionState.Open);
			}
		}

		protected static SqliteConnection Connection {
			get {
				if (!IsConnectionOpen)
					throw new SqliteException ("The connection to the database is closed or broken");

				return Database.Connection;
			}
		}

		public DatabaseModel () : this (true)
		{
		}

		protected DatabaseModel (bool isNew)
		{
			IsNew = isNew;
		}
	}

	public abstract class DatabaseModel<T> : DatabaseModel where T : DatabaseModel
	{
		public DatabaseModel () : base (true)
		{
		}

		protected DatabaseModel (bool isNew) : base (isNew)
		{
		}

		public T Save (bool ignore = false)
		{
			var table = GetTable (typeof (T));
			var fields = GetFields (typeof(T)).Where (f => f.Key != "id").ToDictionary (f => f.Key, f => f.Value);

			// Generate SQL query
			SqliteCommand sql;

			if (IsNew) {
				if (fields.Count == 0) {
					sql = new SqliteCommand (String.Format ("INSERT {0} INTO {1} (id) VALUES (NULL)", ignore ? "OR IGNORE" : "", table), Connection);
				} else {
					sql = new SqliteCommand (String.Format ("INSERT {0} INTO {1} ({2}) VALUES ({3})", ignore ? "OR IGNORE" : "", table,
						String.Join (",", fields.Keys), String.Join (",", System.Linq.Enumerable.Repeat<string> ("?", fields.Keys.Count))), Connection);
				}
			} else {
				if (fields.Count == 0)
					return this as T;

				var values = new List<string> ();

				foreach (var column in fields.Keys)
					values.Add (String.Format ("{0} = ?", column));

				sql = new SqliteCommand (String.Format ("UPDATE {0} SET {1} WHERE id = {2}", table, String.Join (",", values), this.ID), Connection);
			}

			// Add parameters to SQL query
			foreach (var field in fields)
				sql.Parameters.Add (new SqliteParameter () { Value = field.Value.GetValue (this, null) });

			sql.ExecuteNonQuery ();

			// Get ID in case it's new
			if (IsNew) {
				var result = new SqliteCommand ("SELECT last_insert_rowid()", Connection).ExecuteScalar ();

				if (result == null)
					throw new SqliteException ();

				ID = (long)result;
				IsNew = false;
			}

			return this as T;
		}

		public static List<T> All ()
		{
			var table = GetTable (typeof(T));
			var fields = GetFields (typeof(T));

			var sql = new SqliteCommand (String.Format ("SELECT {0} FROM {1}", String.Join (", ", fields.Keys), table), Connection);

			using (var reader = sql.ExecuteReader ()) {
				var records = new List<T> ();

				while (reader.Read ())
					records.Add (BuildObject (reader, fields));

				return records;
			}
		}

		public static T FindByID (long id)
		{
			return FindBy (new SortedDictionary<string, object> () { { "id", id } }).FirstOrDefault ();
		}

		public static List<T> FindBy (SortedDictionary<string, object> filters)
		{
			if (filters.Count == 0)
				return All ();

			// TODO: add support for filters with IList value

			var table = GetTable (typeof(T));
			var fields = GetFields (typeof(T));

			var sql = new SqliteCommand (String.Format ("SELECT {0} FROM {1} WHERE {2}", String.Join (", ", fields.Keys), table,
					String.Join (" AND ", filters.Select (f => String.Format ("{0} = ?", f.Key)))), Connection);

			foreach (var f in filters)
				sql.Parameters.Add (new SqliteParameter () { Value = f.Value });

			using (var reader = sql.ExecuteReader ()) {
				var records = new List<T> ();

				while (reader.Read ())
					records.Add (BuildObject (reader, fields));

				return records;
			}
		}

		static T BuildObject (DbDataReader reader, Dictionary<string, PropertyInfo> fields)
		{
			var constructor = typeof (T).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null, new [] { typeof (Boolean) }, null);
			var record = (T) constructor.Invoke (new object[] { false });

			for (int i = 0; i < reader.FieldCount; i++) {
				// Name of the SQL field
				var name = reader.GetName (i);

				if (!fields.ContainsKey (name))
					continue;

				// Get PropertyInfo for given SQL field
				var field = fields [name];

				if (reader.IsDBNull (i)) {
					field.SetValue (record, null, null);
				} else {
					var type = field.PropertyType;

					if (type.IsEnum) {
						field.SetValue (record, Enum.ToObject (type, reader.GetValue (i)), null);
					} else {
						if (type.IsGenericType && type.GetGenericTypeDefinition ().Equals (typeof(Nullable<>)))
							type = new NullableConverter (type).UnderlyingType;

						field.SetValue (record, Convert.ChangeType (reader.GetValue (i), type), null);
					}
				}
			}

			return record;
		}

		static string GetTable (System.Type type)
		{
			var attributes = type.GetCustomAttributes (typeof(TableAttribute), true);

			if (attributes.Length == 0)
				throw new Exception ("Missing Table annotation on this class");

			return ((TableAttribute)attributes.GetValue (0)).Name;
		}

		static Dictionary<string, PropertyInfo> GetFields (System.Type type)
		{
			var fields = new Dictionary<string, PropertyInfo> ();

			foreach (var property in type.GetProperties()) {
				var attributes = property.GetCustomAttributes (typeof(ColumnAttribute), true);

				if (attributes.Length > 0)
					fields.Add (((ColumnAttribute)attributes.GetValue (0)).Name, property);
			}

			return fields;
		}
	}
}
