using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	public class ConsoleProgresser
	{
		String template = "\r";
		Boolean cancel = false;
		Task progressTask;
		Int32 CountOfDottedMax;

		public ConsoleProgresser( String message, Int32 countOfdotted = 10 )
		{
			template += message;
			CountOfDottedMax = countOfdotted;
		}

		public void Stop() { cancel = true; }

		public void Start()
		{
			progressTask = new Task( () =>
			{
				var count = 0;
				while ( !cancel )
				{
					count++;

					Console.Write( template.PadRight( template.Length + count, '.' ) );
					System.Threading.Thread.Sleep( 1000 );
					if ( cancel ) return;
					if ( count > CountOfDottedMax )
					{
						Console.Write( template.PadRight( template.Length + CountOfDottedMax, ' ' ) );
						count = 0;
					}
				}
			} );
			progressTask.Start();
		}
	}
}