using System;
using Xunit.Abstractions;

namespace EnjentUnitTests
{

    /// <summary>
	/// Base class for XUnit tests for Enjent
    /// </summary>
    public abstract class EnjentTest
    {
    	private readonly ITestOutputHelper output;

		protected static Random rand = new Random();

		public EnjentTest(ITestOutputHelper o)
		{
			this.output = o;
		}

		public void Log(string message)
		{
			if (this.output != null)
			{
				this.output.WriteLine(message);
			}
		}
	}
}
