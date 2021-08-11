using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
	//Raz - 2013-10-02 因為Timers不能密集進行操作,
	//故我們自已實作了ThreadTimer來解決這個問題, 所以不再用這些Timers

	public static class TimersExtensions
	{
		/// <summary>重新啟動 (先Stop再Start)</summary>
		public static void Restart( this Timers.Timer timer )
		{
			//因為有可能在多執行緒時多向觸發
			timer.Enabled = false;
			timer.Enabled = true;
		}
		///// <summary>
		///// 重新啟動 (先Stop再Start)
		///// </summary>
		//public static void Restart( this System.Windows.Forms.Timer timer )
		//{
		//	timer.Enabled = false;
		//	timer.Enabled = true;
		//}


		/// <summary>依輸入條件重新設定, 將會進行Enable=false, 改變屬性完再Enable</summary>
		/// <param name="timer"></param>
		/// <param name="seconds"></param>
		/// <param name="isRepeat"></param>
		public static void ReStartBy( this Timers.Timer timer, Int32 seconds, Boolean isRepeat = false )
		{
			timer.Enabled = false;
			timer.Interval = seconds * 1000;
			timer.AutoReset = isRepeat;
			timer.Enabled = true;
		}
	}
}