using System;
using syscom;

namespace syscom.logging.transports
{
	public class ConsoleTransport : ILoggerTransporter
	{
		public void OnMessage( ILogMessage message )
		{
			Console.ForegroundColor = message.Level.GetColor();
			Console.WriteLine( message );
		}

		public void Dispose() {  }
	}
}
