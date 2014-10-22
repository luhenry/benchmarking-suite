using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using BenchmarkingSuite.Common;
using BenchmarkingSuite.Common.Models;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Agent.Mono
{
	public class MainClass
	{
		static string Coordinator = String.Empty;
		static string BenchmarkerDir = String.Empty;
		static string Architecture = String.Empty;
		static string RevisionCommit = String.Empty;
		static string Device = String.Empty;

		static Revision Revision = null;

		static void Usage ()
		{
			Console.Error.WriteLine ("usage: --coordinator <coordinator> --benchmarker-dir <benchmarker-dir> --architecture <architecture> --device <device> [--revision <revision>] [--recipe-ids <recipe-id>[,<recipe-id>]*]");
		}

		public static void Main (string[] args)
		{
//			Coordinator = "127.0.0.1:8080";
//			BenchmarkerDir = "/Users/ludovic/Xamarin/benchmarker";
//			Architecture = "amd64";
//			Device = "mac mini";
//			RevisionCommit = "a68a79338360b04cd8a302154252b6e01c564a83";

			var recipe_ids = new int[0];

			for (var optindex = 0; optindex < args.Length; ++optindex) {
				if (args [optindex] == "--coordinator" || args [optindex].StartsWith ("--coordinator=")) {
					Coordinator = (args [optindex] == "--coordinator" ? args [++optindex] : args [optindex].Substring ("--coordinator=".Length)).Trim ();
				} else if (args [optindex] == "--benchmarker-dir" || args [optindex].StartsWith ("--benchmarker-dir=")) {
					BenchmarkerDir = (args [optindex] == "--benchmarker-dir" ? args [++optindex] : args [optindex].Substring ("--benchmarker-dir=".Length)).Trim ();
				} else if (args [optindex] == "--architecture" || args [optindex].StartsWith ("--architecture=")) {
					Architecture = (args [optindex] == "--architecture" ? args [++optindex] : args [optindex].Substring ("--architecture=".Length)).Trim ();
				} else if (args [optindex] == "--revision" || args [optindex].StartsWith ("--revision=")) {
					RevisionCommit = (args [optindex] == "--revision" ? args [++optindex] : args [optindex].Substring ("--revision=".Length)).Trim ();
				} else if (args [optindex] == "--recipe-ids" || args [optindex].StartsWith ("--recipe-ids=")) {
					recipe_ids = (args [optindex] == "--recipe-ids" ? args [++optindex] : args [optindex].Substring ("--recipe-ids=".Length))
						.Split (',').Select (s => s.Trim ()).Distinct ().Select (s => Int32.Parse (s)).ToArray ();
				} else if (args [optindex] == "--device" || args [optindex].StartsWith ("--device=")) {
					Device = (args [optindex] == "--device" ? args [++optindex] : args [optindex].Substring ("--device=".Length)).Trim ();
				} else {
					Console.Error.WriteLine ("unknown parameter {0}", args [optindex]);
					Usage ();
					Environment.Exit (1);
				}
			}

			if (String.IsNullOrEmpty (Coordinator)
				|| String.IsNullOrEmpty (BenchmarkerDir)
				|| String.IsNullOrEmpty (Architecture)
				|| String.IsNullOrEmpty (Device))
			{
				Usage ();
				Environment.Exit (1);
			}

			var archive_folder = FetchAndUnpackRevision ();
			var recipes = FetchRecipes (recipe_ids);

			foreach (var recipe in recipes) {
				RunRecipe (recipe, archive_folder);
			}

		}

		static string FetchAndUnpackRevision ()
		{
			if (!String.IsNullOrEmpty (RevisionCommit)) {
				Revision = JsonConvert.DeserializeObject<Revision> (GetHttpContent (String.Format ("http://{0}/revision/mono/{1}/{2}", Coordinator, Architecture, RevisionCommit)));
			} else {
				Revision = JsonConvert.DeserializeObject<Revision> (GetHttpContent (String.Format ("http://{0}/revision/mono/{1}/last", Coordinator, Architecture)));
				RevisionCommit = Revision.Commit;
			}

			Debug.Assert (Revision != null);
			Debug.Assert (Revision.ID != 0);

			Debug.Assert (!String.IsNullOrEmpty (RevisionCommit));

			var archive = GetHttpStream (String.Format ("http://{0}/revision/mono/{1}/{2}.tar.gz", Coordinator, Architecture, RevisionCommit));

			var folder = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());

			if (Directory.Exists (folder))
				Directory.Delete (folder, true);
			Directory.CreateDirectory (folder);

			var filename = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName () + ".tar.gz");

			if (File.Exists (filename))
				File.Delete (filename);

			using (var file = new FileStream (filename, FileMode.Create, FileAccess.Write))
				archive.CopyTo (file);

			Console.Out.WriteLine ("[] Untar revision {0} to {1}", RevisionCommit, folder);

			var process = Process.Start (new ProcessStartInfo () {
				FileName = "tar",
				Arguments = String.Format ("xvzf {0}", filename),
				WorkingDirectory = folder,
				UseShellExecute = true,
			});

			process.WaitForExit ();

			return folder;
		}

		static List<KeyValuePair <int, Recipe>> FetchRecipes (int[] ids)
		{
			var recipes = new List<KeyValuePair <int, Recipe>> ();

			if (ids.Length == 0) {
				ids = JsonConvert.DeserializeObject <int[]> (GetHttpContent (String.Format (
					"http://{0}/recipes?device.architecture={1}&device.name={2}", Coordinator, Architecture, Device)));
			}

			Console.Out.WriteLine ("[] Fetch recipes {0}", String.Join (", ", ids));

			foreach (var id in ids) {
				recipes.Add (new KeyValuePair <int, Recipe> (id, JsonConvert.DeserializeObject <Recipe> (GetHttpContent (String.Format (
					"http://{0}/recipe/{1}", Coordinator, id)))));
			}

			return recipes;
		}

		static void RunRecipe (KeyValuePair<int, Recipe> recipe, string archive_folder)
		{
			try {
				Console.Out.WriteLine ("[] Run recipe {0} with benchmark '{1}' on device '{2}'", recipe.Key, recipe.Value.Benchmark.Name, recipe.Value.Device.Name);

				var output = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName () + ".mlpd");

				if (File.Exists (output))
					File.Delete (output);

				var info = new ProcessStartInfo ();

				info.UseShellExecute = false;
				info.WorkingDirectory = Path.Combine (BenchmarkerDir, "tests");
				info.FileName =  Path.Combine (archive_folder, "mono");	
				info.Arguments = String.Join (" ", new string [] { recipe.Value.Configuration.Arguments, String.Format (
					"--profile=log:counters,nocalls,noalloc,output=-{0}", output), recipe.Value.Benchmark.Executable, recipe.Value.Benchmark.Arguments });

				info.EnvironmentVariables.Add ("MONO_PATH", archive_folder);
				info.EnvironmentVariables.Add ("LD_LIBRARY_PATH", archive_folder);

				foreach (var env in recipe.Value.Configuration.EnvironmentVariables.Split (' ')) {
					if (String.IsNullOrEmpty (env))
						continue;

					var a = env.Split (new char [] { '=' }, 2).Select (s => s.Trim ()).ToArray ();

					if (a [0] == "MONO_PATH" || a [0] == "LD_LIBRARY_PATH")
						continue;

					info.EnvironmentVariables.Add (a [0], a.Length > 1 ? a [1] : null);
				}

				Console.Out.WriteLine ("[] Execute benchmark : MONO_PATH={0} LD_LIBRARY_PATH={1} {2} {3}", archive_folder, archive_folder, info.FileName, info.Arguments);

				var start = DateTime.Now.Ticks;

				Process.Start (info).WaitForExit ();

				var end = DateTime.Now.Ticks;

				Console.Out.WriteLine ("[] Sending profiler output {0}", output);
				using (var stream = new FileStream (output, FileMode.Open, FileAccess.Read))
					PostHttpStream (String.Format ("http://{0}/run?recipe.id={1}&revision.id={2}&run.start_date={3}&run.end_date={4}", Coordinator, recipe.Key, 
								Revision.ID, HttpUtility.UrlEncode (start.ToString ()), HttpUtility.UrlEncode (end.ToString ())), stream);
			} catch (Exception e) {
				Console.Out.WriteLine (e.ToString ());
			}
		}

		static string GetHttpContent (string url)
		{
			try {
				return new StreamReader (HttpWebRequest.CreateHttp (url).GetResponse ().GetResponseStream ()).ReadToEnd ();
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", url, new StreamReader (e.Response.GetResponseStream ()).ReadToEnd ()), e);
			}
		}

		static Stream GetHttpStream (string url)
		{
			try {
				return HttpWebRequest.CreateHttp (url).GetResponse ().GetResponseStream ();
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", url, new StreamReader (e.Response.GetResponseStream ()).ReadToEnd ()), e);
			}
		}

		static string PostHttpStream (string url, Stream content, string content_type = "application/octet-stream") {
			try {
				var request = HttpWebRequest.CreateHttp (url);
				request.Method = "POST";
				request.ContentType = content_type;

				using (var s = request.GetRequestStream ())
					content.CopyTo (s);

				return new StreamReader (request.GetResponse ().GetResponseStream ()).ReadToEnd ();
			} catch (WebException e) {
				throw new WebException (String.Format ("POST {0} : {1}", url, new StreamReader (e.Response.GetResponseStream ()).ReadToEnd ()), e);
			}
		}
	}
}
