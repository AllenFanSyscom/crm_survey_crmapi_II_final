using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using syscom;
using System.Collections.Concurrent;
using syscom.data;
using syscom.data.Schema;

namespace System.Data
{
	public static partial class DataReaderExtension
	{
		/// <summary>
		/// 從DataReader中讀取資料，並轉型為指定的Model型別, 若DataReader沒有資料將傳回null
		/// <para>請勿自行呼叫reader.Read(), 將由內部進行呼叫</para>
		/// </summary>
		/// <typeparam name="TModel">與Columns對應的class型別</typeparam>
		public static TModel AutoReadGetMappedModel<TModel>( this IDataReader reader, IList<String>? skipColumns = null ) where TModel : class
		{
			TModel? model = null;
			var method = GetMethodOfPropertiesMapping<TModel>( reader, skipColumns );
			if ( !reader.Read() ) return model;

			model = (TModel) method.DynamicInvoke( reader );
			//if ( models.Count == 0 ) throw Err.Extension( "DataReader中不包含任何資料集，所以無法取得DTOs。" );
			return model;
		}

		/// <summary>
		/// 從DataReader中讀取資料，並轉型為指定的Model型別, 若DataReader沒有資料將傳回空List(數量為0)
		/// <para>請勿自行呼叫reader.Read(), 將由內部進行呼叫</para>
		/// </summary>
		/// <typeparam name="TModel">與Columns對應的class型別</typeparam>
		/// <param name="onModel">每執行一次DataReader.Read()便會呼叫一次的外部Action (可選項)</param>
		public static IList<TModel> AutoReadGetMappedModels<TModel>( this IDataReader reader, Action<IDataReader, TModel>? onModel = null, IList<String>? skipColumns = null ) where TModel : class
		{
			var models = new List<TModel>();
			var method = GetMethodOfPropertiesMapping<TModel>( reader, skipColumns );

			while ( reader.Read() )
			{
				TModel model;
				try
				{
					model = (TModel) method.DynamicInvoke( reader );
				}
				catch ( Exception ex )
				{
					throw Err.Utility( "GetMappedModels執行DataReader對應時發生異常, " + ex.Message, ex );
				}

				try
				{
					onModel?.Invoke( reader, model );
				}
				catch ( Exception ex ) { throw Err.Utility( "GetMappedModels所執行的外部Action發生異常, " + ex.Message, ex ); }

				models.Add( model );
			}

			//if ( models.Count == 0 ) throw Err.Extension( "DataReader中不包含任何資料集，所以無法取得DTOs。" );
			return models;
		}
	}


	static partial class DataReaderExtension
	{
		static List<String> _ConfigSkipColumns = new List<String> { "ElementKey", "LockAttributes", "LockAllAttributesExcept", "LockElements", "LockAllElementsExcept", "LockItem", "ElementInformation", "CurrentConfiguration" };
		static ConcurrentDictionary<String, Delegate> _modelMappingMethods = new ConcurrentDictionary<String, Delegate>();
		static ConcurrentDictionary<Type, List<PropertyInfo>> _modelNeedMapProperties = new ConcurrentDictionary<Type, List<PropertyInfo>>();

		static Delegate GetMethodOfPropertiesMapping<TModel>( IDataReader reader, IList<String>? skipColumns = null ) where TModel : class
		{
			var type = typeof( TModel );

			// //如果是Config類型別, 略過內建的Columns
			// if ( type.IsSubclassOf( typeof( Configuration.ConfigurationElement ) ) )
			// {
			// 	if ( skipColumns == null )
			// 		skipColumns = _ConfigSkipColumns;
			// 	else
			// 		foreach ( var item in _ConfigSkipColumns )
			// 			skipColumns.Add( item );
			// }

			Delegate modelMapExpr;
			if ( !_modelMappingMethods.TryGetValue( type.FullName, out modelMapExpr ) )
			{
				ValidateProperties<TModel>( reader, type, skipColumns );
				modelMapExpr = generateReaderMappingTo<TModel>();
				_modelMappingMethods.TryAdd( type.FullName, modelMapExpr );
			}

			return modelMapExpr;
		}

		static List<PropertyInfo> GetModelNeedMapProperties<TModel>( IList<String>? skipColumns = null )
		{
			var type = typeof( TModel );
			List<PropertyInfo>? list = null;
			if ( !_modelNeedMapProperties.TryGetValue( type, out list ) )
			{
				list = new List<PropertyInfo>();
				var properties = typeof( TModel ).GetProperties();
				foreach ( var prop in properties )
				{
					if ( skipColumns != null && skipColumns.Contains( prop.Name ) ) continue;

					var mapAttr = prop.GetCustomAttribute<DBMapIgnoreAttribute>();
					if ( mapAttr != null ) continue;

					list.Add( prop );
				}

				_modelNeedMapProperties.TryAdd( type, list );
			}

			return list;
		}

		static void ValidateProperties<TModel>( IDataReader reader, Type type, IList<String>? skipColumns = null ) where TModel : class
		{
			var properties = GetModelNeedMapProperties<TModel>( skipColumns );
			var schema = reader.GetSchemaTable();
			if ( schema == null ) throw Err.Extension( "無法從DataReader中取得SchemaTable" );
			var schemaDic = schema.AsEnumerable().ToDictionary( t => t["ColumnName"].ToString() );

			var needRemoveProperties = new List<PropertyInfo>();

			foreach ( var property in properties )
			{
				var mapAttr = property.GetCustomAttribute<DBMapIgnoreAttribute>();

				if ( !schemaDic.ContainsKey( property.Name ) )
				{
					if ( mapAttr != null )
					{
						needRemoveProperties.Add( property );
						continue;
					}

					throw Err.Extension( "資料集中不包括欄位[ " + property.Name + " ]" );
				}

				var row = schemaDic[property.Name];


				var dbDataType = (Type) row["DataType"];
				if ( dbDataType == ( property.PropertyType.Name == "Nullable`1" ? property.PropertyType.GenericTypeArguments[0] : property.PropertyType ) ) continue;

				//db: Char, model:Char
				if ( dbDataType == typeof( String ) && property.PropertyType == typeof( Char ) )
				{
					var columnSize = (Int32) row["ColumnSize"];
					if ( columnSize != 1 ) throw Err.Extension( "DTO的欄位[" + property.Name + "]型態為Char, DB的定義必需為 char(1), 否則請更改Model型別為String" );
					continue;
				}

				//原本要做DBNull檢核 ，因考量DB回傳型態變數太大，因此避免。
				//if(schemaDic.TryGetValue( property.Name, out datarow ))
				//{
				//	bool CheckType = (Type)datarow["DataType"] ==
				//					 ( property.PropertyType.Name == "Nullable`1"
				//						 ? property.PropertyType.GenericTypeArguments[0]
				//						 : property.PropertyType );
				//	bool CheckNullAble = ( (Type)datarow["DataType"] ).IsValueType
				//		? ( ( property.PropertyType.Name == "Nullable`1" ) == (bool)datarow["AllowDBNull"] )
				//		: true;
				//	if ( CheckType && CheckNullAble ) continue;
				//}

				throw Err.Extension( "DTO[" + type + "]之[" + property.Name + "]欄位型態 (" + property.PropertyType + ") 與 DB ( " + dbDataType + " ) 欄位型態無法對應 " );
			}

			foreach ( var p in needRemoveProperties ) properties.Remove( p );
		}


		public static Func<IDataRecord, TModel> generateReaderMappingTo<TModel>() where TModel : class
		{
			//=======================
			// Globals
			//=======================
			var methodExceptionConstructor = typeof( Exception ).GetConstructor( new[] { typeof( String ), typeof( Exception ) } );
			var methodGetMessage = typeof( Exception ).GetProperty( "Message" ).GetGetMethod();
			var methodConcat = typeof( String ).GetMethod( "Concat", new[] { typeof( Object[] ) } );

			//=======================
			// GlobalExprs
			//=======================
			var exprParameterException = Expression.Parameter( typeof( Exception ), "ex" ); //Exception ex
			//=======================

			var modelType = typeof( TModel );
			var properties = GetModelNeedMapProperties<TModel>();
			var allExpressions = new List<Expression>();

			var varModel = Expression.Variable( modelType, "model" );
			var exprCreateInstance = Expression.Assign( varModel, Expression.New( modelType ) );
			allExpressions.Add( exprCreateInstance );

			var typeOfReader = typeof( IDataRecord );
			var exprReaderParameter = Expression.Parameter( typeOfReader, "reader" );

			var propertyInfoOfReaderItem = typeOfReader.GetProperty( "Item", new[] { typeof( String ) } );

			foreach ( var pInfo in properties )
			{
				var exprVarPropInfoName = Expression.Constant( pInfo.Name );
				var exprVarColumnValue = Expression.MakeIndex( exprReaderParameter, propertyInfoOfReaderItem, new[] { exprVarPropInfoName } );
				var exprProperty = Expression.Property( varModel, pInfo );
				var exprAssignProp = Expression.Assign( exprProperty, Expression.Convert( exprVarColumnValue, pInfo.PropertyType ) ); //assignValue.Dump();
				var exprIfThenAssign = Expression.IfThen( Expression.NotEqual( exprVarColumnValue, Expression.Constant( DBNull.Value ) ), exprAssignProp );


				var bodyOfPropertySet = Expression.Block( exprIfThenAssign );

				var exprIGetExMsg = Expression.Call( exprParameterException, methodGetMessage );
				var exprIErrorMsg = Expression.Call
				(
					methodConcat,
					Expression.NewArrayInit
					(
						typeof( String ),
						new Expression[] { Expression.Constant( "Property[ " ), Expression.Constant( pInfo.Name ), Expression.Constant( " ], " ), exprIGetExMsg }
					)
				);
				var exprIThrow = Expression.Throw( Expression.New( methodExceptionConstructor, exprIErrorMsg, exprParameterException ) );
				var exprInnerCatch = Expression.Catch( exprParameterException, exprIThrow );
				var exprInnerTryCatch = Expression.TryCatch( bodyOfPropertySet, exprInnerCatch );

				allExpressions.Add( exprInnerTryCatch );
			}

			allExpressions.Add( varModel ); //if less, ArgumentException: Argument types do not match
			var body = Expression.Block( varModel.Type, new[] { varModel }, allExpressions.ToArray() );


			var exprGetExMsg = Expression.Call( exprParameterException, methodGetMessage );
			var exprErrorMsg = Expression.Call
			(
				methodConcat,
				Expression.NewArrayInit
				(
					typeof( String ),
					new Expression[] { Expression.Constant( "[Mapping Model Error] " ), exprGetExMsg }
				)
			);
			var exprThrow = Expression.Throw
			(
				Expression.New( methodExceptionConstructor, exprErrorMsg, exprParameterException ),
				varModel.Type
			);

			var exprCatch = Expression.Catch( exprParameterException, exprThrow );
			var exprTryCatch = Expression.TryCatch( body, exprCatch );

			var lambda = Expression.Lambda<Func<IDataRecord, TModel>>( exprTryCatch, exprReaderParameter ).Compile();
			return lambda;
		}
	}
}
