using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace syscom.Collections
{
	/// <summary>
	/// 以型別為基礎，為單一TKey型別對照產生TValue型別的快取者 (以static字典為快取)
	/// <para>RazgrizHsu::為獨體模式實作, 請在使用前設定Generator</para>
	/// </summary>
	public static class SingletonCacherBy<TKey, TValue>
	{
		static Object _mutex = new Object();
		static Func<TKey, TValue> _generator;
		static Dictionary<TKey, TValue> _cacheDic = new Dictionary<TKey, TValue>();

		public static TValue GetBy( TKey key )
		{
			if ( _generator == null ) throw new InvalidOperationException( typeof( SingletonCacherBy<TKey, TValue> ).FullName + " 尚未設定 DataGenerator, 無法產生資料." );

			TValue cachedValue;
			if ( _cacheDic.TryGetValue( key, out cachedValue ) ) return cachedValue;


			lock ( _mutex )
			{
				if ( !_cacheDic.TryGetValue( key, out cachedValue ) )
				{
					cachedValue = _generator( key );
					_cacheDic[key] = cachedValue;
				}

				return cachedValue;
			}
		}

		/// <summary>資料產生器, 在使用前必需設定</summary>
		public static Func<TKey, TValue> DataGenerator
		{
			set
			{
				if ( _generator == null ) _generator = value;
			}
		}
	}

	//Todo:[Raz] 想要做個可以以單一TModel為關鍵字, 但有多種回傳型態的SingletonCacher, 例如..
	/*
			SingletonCacherBy<IList<IFTModel>>.DataGenerator = () =>
			{
				return Enumerable.Cast<IFTModel>(ReflectionUtils.GetUserDefineTypes
											.Where( t => t.IsClass && t.GetInterfaces().Contains( typeof( IFTModel ) ) ))
				.ToList();

				//return ReflectionUtils.GetUserDefineTypes
				//.Where( t => t.IsClass && t.GetInterfaces().Contains( typeof( IFTModel ) ) )
				//.Cast<IFTModel>()
				//.ToList();
			};
	 */

	///// <summary>以型別為基礎的快取者 (RazgrizHsu::為獨體模式實作, 請在使用前設定Generator</summary>
	//public static class SingletonCacherBy<TValue>
	//{
	//	static Boolean _inited = false;
	//	static Object _mutex = new Object();
	//	static Func<TValue> _generator;
	//	static TValue _data;

	//	public static TResult GetBy<TResult>()
	//	{
	//		//SingletonCacherBy<SingletonCacherBy<TValue>, TResult>.GetBy
	//	}

	//	public static TValue Data
	//	{
	//		get
	//		{
	//			if ( _generator == null ) throw new InvalidOperationException( typeof( SingletonCacherBy<TValue> ).FullName + " 尚未設定 DataGenerator, 無法產生資料." );
	//			if ( _inited ) return _data;
	//			lock ( _mutex )
	//			{

	//				if ( _inited ) return _data;
	//				_data = _generator();
	//				_inited = true;
	//				return _data;
	//			}
	//		}
	//	}

	//	/// <summary>資料產生器, 在使用前必需設定</summary>
	//	public static Func<TValue> DataGenerator 
	//	{
	//		set 
	//		{ 
	//			if ( _generator == null ) _generator = value; 
	//		} 
	//	}
	//}


	///// <summary>以型別為基礎的快取者 (RazgrizHsu::為獨體模式實作, 請在使用前設定Generator</summary>
	//public static class SingletonTypeCacherBy<TValue>
	//{
	//	static Boolean _inited = false;
	//	static Object _mutex = new Object();
	//	static Func<TValue> _generator;
	//	static TValue _data;

	//	public static TValue Data
	//	{
	//		get
	//		{
	//			if ( _generator == null ) throw new InvalidOperationException( typeof( SingletonCacherBy<TValue> ).FullName + " 尚未設定 DataGenerator, 無法產生資料." );
	//			if ( _inited ) return _data;
	//			lock ( _mutex )
	//			{

	//				if ( _inited ) return _data;
	//				_data = _generator();
	//				_inited = true;
	//				return _data;
	//			}
	//		}
	//	}

	//	/// <summary>資料產生器, 在使用前必需設定</summary>
	//	public static Func<TValue> DataGenerator { set { if ( _generator == null ) _generator = value; } }
	//}
}