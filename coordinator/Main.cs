using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using BenchmarkingSuite.Common.Models;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Coordinator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Database.OpenConnection (Path.Combine (Environment.CurrentDirectory, "data", "database.sqlite"));

			Benchmark.Initialize ();
			Configuration.Initialize ();
			Counter.Initialize ();
			Device.Initialize ();
			Recipe.Initialize ();
			Revision.Initialize ();
			Run.Initialize ();
			Sample.Initialize ();

			using (var nancy = new NancyHost (NancyBootstrapperLocator.Bootstrapper, new Uri ("http://127.0.0.1:8080/"))) {
				nancy.Start ();

				StaticConfiguration.DisableErrorTraces = false;
				StaticConfiguration.Caching.EnableRuntimeViewUpdates = true;

				Console.WriteLine ("Nancy now listening on http://127.0.0.1:8080/. Press key to stop");

				while (true) {
					if (Console.ReadKey ().Key == ConsoleKey.Enter)
						break;

					Thread.Sleep (100);
				}

				nancy.Stop ();
			}

			Console.WriteLine ("The End;");
		}
	}
}
