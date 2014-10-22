using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using Mono.Data.Sqlite;


namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "revisions")]
	public class Revision : DatabaseModel<Revision>
	{
		[Column (Name = "project")]
		public string Project { get; set; }

		[Column (Name = "architecture")]
		public string Architecture { get; set; }

		[Column (Name = "sha")]
		public string Commit { get; set; }

		[Column (Name = "creation_date")]
		public DateTime CreationDate { get; set; }

		public string Url {
			get {
				switch (Project) {
				case "mono":      return String.Format ("https://github.com/mono/mono/tree/{0}", Commit);
				case "monodroid": return String.Format ("https://github.com/xamarin/monodroid/tree/{0}", Commit);
				case "monotouch": return String.Format ("https://github.com/xamarin/monotouch/tree/{0}", Commit);
				default:          return null;
				}
			}
		}

		public static void Initialize () {}

		static Revision ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS revisions (
				        id INTEGER PRIMARY KEY AUTOINCREMENT
				      , project TEXT NOT NULL
				      , architecture TEXT NOT NULL
				      , sha TEXT NOT NULL
				      , creation_date DATETIME NOT NULL
				      , UNIQUE (project, architecture, sha)
				);
			", Connection).ExecuteNonQuery ();
		}

		public Revision () : base ()
		{
		}

		protected Revision (bool isNew) : base (isNew)
		{
		}

		public static List<Revision> FindByProjectAndArchitecture (string project, string architecture)
		{
			return FindBy (new SortedDictionary<string, object> () { { "project", project }, { "architecture", architecture } });
		}

		public static Revision FindByProjectArchitectureAndCommit (string project, string architecture, string commit)
		{
			return FindBy (new SortedDictionary<string, object> () { { "project", project }, { "architecture", architecture }, { "sha", commit } }).FirstOrDefault ();
		}
	}
}
