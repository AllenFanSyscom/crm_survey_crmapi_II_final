using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.data.Schema
{
	//public enum DBMapType
	//{
	//	/// <summary>略過</summary>
	//	Ignore = 0,
	//	/// <summary>不存在則略過</summary>
	//	NotExistIgnore
	//}

	///// <summary>用來標示使用DataReader使用自動Mapping時需要略過的欄位</summary>
	//[AttributeUsage( AttributeTargets.Property )]
	//public class DBMapIgnoreAttribute : Attribute
	//{
	//	public DBMapType MapType { get; private set; }
	//	public DBMapIgnoreAttribute()
	//	{
	//		MapType = DBMapType.Ignore;
	//	}

	//	public DBMapIgnoreAttribute( DBMapType mapType )
	//	{
	//		this.MapType = mapType;
	//	}
	//}


	[AttributeUsage( AttributeTargets.Property )]
	public class DBKeyAttribute : Attribute
	{
	}

	[AttributeUsage( AttributeTargets.Property )]
	public class DBMapIgnoreAttribute : Attribute
	{
	}
}
