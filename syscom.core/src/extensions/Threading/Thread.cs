using syscom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading
{
	public static class ThreadExtensions
	{
		/// <summary>安全地設定執行緒名稱, 若該執行緒已設定名稱則略過, 否則會出現異常</summary>
		public static void SafetySetNameBy( this Thread thread, String name )
		{
			if ( !String.IsNullOrWhiteSpace( thread.Name ) ) return;
			thread.Name = name;
		}
	}
}