using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Common.Models
{
	[Table (Name = "runs"), JsonObject(MemberSerialization.OptOut)]
	public class Run : DatabaseModel<Run>
	{

		[Column (Name = "revision_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "revision_id", ForeignType = typeof (Revision), ForeignKey = "id")]
		public long RevisionID { get; set; }

		[Column (Name = "recipe_id"), JsonIgnoreAttribute] //, ForeignKey (ThisKey = "recipe_id", ForeignType = typeof (Recipe), ForeignKey = "id")]
		public long RecipeID { get; set; }

		[Column (Name = "start_date")]
		public DateTime StartDate { get; set; }

		[Column (Name = "end_date")]
		public DateTime EndDate { get; set; }

		Revision revision = null;
		public Revision Revision {
			get { return revision ?? (revision = IsConnectionOpen ? Revision.FindByID (RevisionID) : null); }
			set { revision = value; }
		}

		Recipe recipe = null;
		public Recipe Recipe {
			get { return recipe ?? (recipe = IsConnectionOpen ? Recipe.FindByID (RecipeID) : null); }
			set { recipe = value; }
		}

		public static void Initialize () {}

		static Run ()
		{
			if (!IsConnectionOpen)
				return;

			new SqliteCommand (@"
				CREATE TABLE IF NOT EXISTS runs (
					id INTEGER PRIMARY KEY AUTOINCREMENT
				      , revision_id INTEGER REFERENCES revisions (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , recipe_id INTEGER REFERENCES recipes (id) ON DELETE CASCADE ON UPDATE CASCADE
				      , start_date DATETIME NOT NULL
				      , end_date DATETIME NOT NULL
				);
			", Connection).ExecuteNonQuery ();
		}

		public Run () : base ()
		{
		}

		internal Run (bool isNew) : base (isNew)
		{
		}
	}
}

