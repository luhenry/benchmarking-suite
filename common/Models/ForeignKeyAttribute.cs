using System;

namespace BenchmarkingSuite.Common.Models
{
	public class ForeignKeyAttribute : Attribute
	{
		public System.Type ForeignType { get; set; }
		public string ForeignKey { get; set; }
		public string ThisKey { get; set; }
	}
}

