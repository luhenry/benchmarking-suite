using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkingSuite.Common.Models;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Coordinator
{
	public class RevisionModule : NancyModule
	{
		public static string Root = Path.Combine (Directory.GetCurrentDirectory (), "data");

		public RevisionModule ()
		{
			Get ["/revisions/{project}/{architecture}"] = parameters => {
				List<Revision> revisions = Revision.FindByProjectAndArchitecture (parameters ["project"], parameters ["architecture"]);

				if (revisions.Count == 0)
					return Response.AsJson ("Architecture not found", HttpStatusCode.NotFound);

					var dirname = String.Format ("{0}/{1}/{2}", Root, parameters ["project"], parameters ["architecture"]);

				if (!Directory.Exists (dirname))
					return Response.AsJson ("Directory not found", HttpStatusCode.NotFound);

				IEnumerable<string> files = Directory.EnumerateFiles (dirname, "*.tar.gz", SearchOption.TopDirectoryOnly);

				return Response.AsJson (revisions.Where (r => files.Any (f => f.Contains (r.Commit))).ToList ());
			};

			Get ["/revision/{project}/{architecture}/{commit}.tar.gz"] = parameters => {
							Revision revision = Revision.FindByProjectArchitectureAndCommit (parameters ["project"], parameters ["architecture"], parameters ["commit"]);
				if (revision == null)
					return Response.AsJson ("Revision not found", HttpStatusCode.NotFound);

							var filename = String.Format ("{0}/{1}/{2}/{3}.tar.gz", Root, parameters ["project"], parameters ["architecture"], parameters ["commit"]);
				if (!File.Exists (filename))
					return Response.AsJson ("File not found", HttpStatusCode.NotFound);

				return new Response () {
					StatusCode = HttpStatusCode.OK,
					ContentType = "application/octet-stream",
					Contents = (s) => {
						using (var file = new FileStream (filename, FileMode.Open, FileAccess.Read)) {
							file.CopyTo (s);
						}
					}
				};
			};

			Get ["/revision/{project}/{architecture}/last"] = parameters => {
				var revisions = Revision.All ();
				if (revisions.Count == 0)
					return Response.AsJson ("Revision not found", HttpStatusCode.NotFound);

				return Response.AsJson (revisions.OrderByDescending (r => r.CreationDate).First ());
			};

			Get ["/revision/{project}/{architecture}/{commit}", ctx => !ctx.Request.Path.EndsWith (".tar.gz")] = parameters => {
				Revision revision = Revision.FindByProjectArchitectureAndCommit (parameters ["project"], parameters ["architecture"], parameters ["commit"]);
				if (revision == null)
					return Response.AsJson ("Revision not found", HttpStatusCode.NotFound);

				return Response.AsJson (revision);
			};

			Post ["/revision/{project}/{architecture}/{commit}.tar.gz"] = parameters => {
				var dirname = String.Format ("{0}/{1}/{2}", Root, parameters ["project"], parameters ["architecture"]);

				if (!Directory.Exists (dirname))
					Directory.CreateDirectory (dirname);

				using (var body = Request.Body) {
					using (var file = new FileStream (String.Format ("{0}/{1}.tar.gz", dirname, parameters ["commit"]), FileMode.Create, FileAccess.Write)) {
						body.CopyTo (file);
					}
				}

				Revision revision = Revision.FindByProjectArchitectureAndCommit (parameters ["project"], parameters ["architecture"], parameters ["commit"]);

				if (revision != null)
					revision.CreationDate = DateTime.Now;
				else
					revision = new Revision () { Project = parameters ["project"], Architecture = parameters ["architecture"],
							Commit = parameters ["commit"], CreationDate = DateTime.Now }.Save ();

				return Response.AsJson (revision);
			};

			OnError += (ctx, e) => {
				Console.Error.WriteLine (e.ToString ());
				return Response.AsJson (e.ToString (), HttpStatusCode.InternalServerError);
			};
		}
	}
}

