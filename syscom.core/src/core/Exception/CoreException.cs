using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	/// <summary>異常分類</summary>
	[Serializable]
	public enum CoreExceptionType
	{
		/// <summary>架構層級</summary>
		[Description( "架構層級" )]
		Infrastructure = 0,

		/// <summary>架構層級</summary>
		[Description( "架構層級" )]
		Framework,

		/// <summary>應用程式層級</summary>
		[Description( "應用程式層級" )]
		AppLayer,

		/// <summary>商業邏輯層級</summary>
		[Description( "商業邏輯層級" )]
		BusinessLogic,

		/// <summary>服務模組層級</summary>
		[Description( "服務模組層級" )]
		ServiceModule,

		/// <summary>系統模組層級</summary>
		[Description( "系統模組層級" )]
		Module,

		/// <summary>系統Utility層級</summary>
		[Description( "系統Utility層級" )]
		Utility,

		/// <summary>DotNet底層型別擴充方法層級</summary>
		[Description( "DotNet底層型別擴充方法層級" )]
		Extension,

		/// <summary>測試層級</summary>
		[Description( "測試層級" )]
		Testing,

		//Component
		//Infrastructure
		//ThreeParty

		/// <summary>標記為不支援的</summary>
		[Description( "標記為不支援的" )]
		NoSupport
	}


	/// <summary>拿來放置核心層級的Exception</summary>
	[Serializable]
	public class CoreException : Exception, ISerializable
	{
		/// <summary>最接近目標型別</summary>
		public Type Thrower { get; private set; }

		/// <summary>執行Method</summary>
		public MethodBase ThwoerMethod { get; private set; }

		public CoreExceptionType? ExceptionType { get; private set; }

		internal CoreException( String message ) : base( message ) { }

		internal CoreException( String message, Exception? innerException = null, Type? thower = null, MethodBase? thowerMethod = null, CoreExceptionType? exceptionType = null )
			: base
			(
				exceptionType == null ? message : "[" + exceptionType + "]" + message
				//+ thower.GetNullOr( t => "-" + t.FullName + thowerMethod.GetNullOr( m => ":" + m.Name ) )
			  , innerException
			)
		{
			ExceptionType = exceptionType;
			ThwoerMethod = thowerMethod;
			Thrower = thowerMethod.GetNullOr( m => m.DeclaringType );
		}

		protected CoreException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			Thrower = info.GetValue( "Thrower", typeof( Type ) ) as Type;
			ThwoerMethod = info.GetValue( "ThwoerMethod", typeof( MethodBase ) ) as MethodBase;
			ExceptionType = (CoreExceptionType) info.GetValue( "ExceptionType", typeof( CoreExceptionType ) );
		}

		[SecurityPermission( SecurityAction.Demand, SerializationFormatter = true )]
		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			info.AddValue( "Thrower", Thrower );
			info.AddValue( "ThwoerMethod", ThwoerMethod );
			info.AddValue( "ExceptionType", ExceptionType );
		}
	}
}
