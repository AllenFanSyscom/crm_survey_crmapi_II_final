using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.data
{
	/// <summary>用來表示資料庫型別的列舉</summary>
	public enum DataBaseType
	{
		Unknown = 0,
		MsSql,
		Oracle,
		Sqlite,
		MySql
	}
}
