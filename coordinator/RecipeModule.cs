using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkingSuite.Common.Models;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;

namespace BenchmarkingSuite.Coordinator
{
	public class RecipeModule : NancyModule
	{
		public RecipeModule ()
		{
			Get ["/recipes"] = (parameters) => {
				var device_name = Request.Query ["device.name"];
				var device_architecture = Request.Query ["device.architecture"];
				var benchmark_name = Request.Query ["benchmark.name"];

				List<Device> devices;

				if (device_name.HasValue && device_architecture.HasValue)
					devices = new Device [] { Device.FindByNameAndArchitecture (device_name.Value, device_architecture.Value) }
							.Where (d => d != null).ToList ();
				else if (device_name.HasValue)
					devices = Device.FilterByName (device_name.Value);
				else if (device_architecture.HasValue)
					devices = Device.FilterByArchitecture (device_architecture.Value);
				else
					devices = Device.All ();

				List<Benchmark> benchmarks;

				if (benchmark_name.HasValue)
					benchmarks = new Benchmark [] { Benchmark.FindByName (benchmark_name.Value) }
							.Where (b => b != null).ToList ();
				else
					benchmarks = Benchmark.All ();

				return Response.AsJson (Recipe.FilterByDevicesAndBenchmarks (devices, benchmarks).Select (r => r.ID));
			};

			Get ["/recipe/{id}"] = (parameters) => {
				Recipe recipe = Recipe.FindByID (parameters ["id"]);

				if (recipe == null)
					return Response.AsJson ("Revision not found", HttpStatusCode.NotFound);

				return Response.AsJson (recipe);
			};

			OnError += (ctx, e) => {
				Console.Error.WriteLine (e.ToString ());
				return Response.AsJson (e.ToString (), HttpStatusCode.InternalServerError);
			};
		}
	}
}

