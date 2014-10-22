using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkingSuite.Common.Models;
using Nancy;
using Nancy.Responses;

namespace BenchmarkingSuite.Coordinator
{
	public class RunModule : NancyModule
	{
		public RunModule ()
		{
			Post ["/run"] = parameters => {
				if (!Request.Query ["recipe.id"].HasValue
					|| !Request.Query ["revision.id"].HasValue
					|| !Request.Query ["run.start_date"].HasValue
					|| !Request.Query ["run.end_date"].HasValue)
					return new TextResponse (HttpStatusCode.BadRequest);

				var start = new DateTime (Int64.Parse (Request.Query ["run.start_date"]));
				var end = new DateTime (Int64.Parse (Request.Query ["run.end_date"]));

				var recipe = Recipe.FindByID (Int64.Parse (Request.Query ["recipe.id"]));
				if (recipe == null)
					return Response.AsJson ("Recipe not found", HttpStatusCode.NotFound);

				var revision = Revision.FindByID (Request.Query ["revision.id"]);
				if (revision == null)
					return Response.AsJson ("Revision not found", HttpStatusCode.NotFound);

				var filename = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName () + ".mlpd");

				Console.Out.WriteLine ("[RunModule] recipe_id {0} start_date {1} end_date {2} duration {3}ms body.length {4} output {5}", recipe.ID, start, end, (end - start).ToString (), Request.Body.Length, filename);

				using (var output = new FileStream (filename, FileMode.Create, FileAccess.Write, FileShare.Read)) {
					Request.Body.CopyTo (output);

					var info = new FileInfo (filename);
					if (info.Length == 0)
						return Response.AsJson ("Empty profiler output", HttpStatusCode.BadRequest);

					if (Request.Body.Length != output.Length)
						throw new Exception ("body.Length != output.Length");

					var run = new Run () { RecipeID = recipe.ID, RevisionID = revision.ID, StartDate = start, EndDate = end }.Save ();

					var inspector = new Common.Inspector.Inspector (filename);
					var cache = new Dictionary<ulong, Counter> ();

					inspector.UpdatedSample += (sender, e) => {
						var timestamp = e.Timestamp;

						foreach (var counter in e.Counters) {
							if (!cache.ContainsKey (counter.CounterID)) {
								var c = Counter.FindBySectionAndName (counter.Section, counter.Name);
								if (c == null) {
									c = new Counter () { Section = counter.Section, Name = counter.Name, Type = (CounterType) counter.Type,
										Unit = (CounterUnit) counter.Unit, Variance = (CounterVariance) counter.Variance};

									c.Save ();
								}

								cache.Add (counter.CounterID, c);
							}

							new Sample () { RunID = run.ID, CounterID = cache [counter.CounterID].ID, Timestamp = timestamp, Value = counter.Value }.Save ();
						}
					};

					inspector.Run ();

					return Response.AsJson (run);
				}
			};

			Get ["/runs"] = parameters => {
				return Response.AsJson (Run.All ());
			};

			OnError += (ctx, e) => {
				Console.Error.WriteLine (e.ToString ());
				return Response.AsJson (e.ToString (), HttpStatusCode.InternalServerError);
			};
		}
	}
}

