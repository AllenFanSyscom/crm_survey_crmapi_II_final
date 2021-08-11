using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace syscom.utils.win32
{
	internal static partial class Msvcrt
	{
		[DllImport( "msvcrt.dll", CharSet = CharSet.Auto )]
		public static extern Boolean System( String str );
	}
}