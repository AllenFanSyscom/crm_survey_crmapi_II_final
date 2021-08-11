using System;
using System.Reflection;

namespace libs.ProtoBuf
{
	internal enum TimeSpanScale
	{
		Days = 0,
		Hours = 1,
		Minutes = 2,
		Seconds = 3,
		Milliseconds = 4,
		Ticks = 5,

		MinMax = 15
	}

	/// <summary>
	/// Provides support for common .NET types that do not have a direct representation
	/// in protobuf, using the definitions from bcl.proto
	/// </summary>
	[CLSCompliant( false )]
	public
#if FX11
    sealed
#else
		static
#endif
		class BclHelpers
	{
		/// <summary>
		/// Creates a new instance of the specified type, bypassing the constructor.
		/// </summary>
		/// <param name="type">The type to create</param>
		/// <returns>The new instance</returns>
		/// <exception cref="NotSupportedException">If the platform does not support constructor-skipping</exception>
		public static Object GetUninitializedObject( Type type )
		{
#if PLAT_BINARYFORMATTER && !(WINRT || PHONE8)
            return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#else
			throw new NotSupportedException( "Constructor-skipping is not supported on this platform" );
#endif
		}
#if FX11
        private BclHelpers() { } // not a static class for C# 1.2 reasons
#endif
		const Int32 FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02;

		internal static readonly DateTime EpochOrigin = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );

		/// <summary>
		/// Writes a TimeSpan to a protobuf stream
		/// </summary>
		public static void WriteTimeSpan( TimeSpan timeSpan, ProtoWriter dest )
		{
			if ( dest == null ) throw new ArgumentNullException( "dest" );
			Int64 value;
			switch ( dest.WireType )
			{
				case WireType.String:
				case WireType.StartGroup:
					TimeSpanScale scale;
					value = timeSpan.Ticks;
					if ( timeSpan == TimeSpan.MaxValue )
					{
						value = 1;
						scale = TimeSpanScale.MinMax;
					}
					else if ( timeSpan == TimeSpan.MinValue )
					{
						value = -1;
						scale = TimeSpanScale.MinMax;
					}
					else if ( value % TimeSpan.TicksPerDay == 0 )
					{
						scale = TimeSpanScale.Days;
						value /= TimeSpan.TicksPerDay;
					}
					else if ( value % TimeSpan.TicksPerHour == 0 )
					{
						scale = TimeSpanScale.Hours;
						value /= TimeSpan.TicksPerHour;
					}
					else if ( value % TimeSpan.TicksPerMinute == 0 )
					{
						scale = TimeSpanScale.Minutes;
						value /= TimeSpan.TicksPerMinute;
					}
					else if ( value % TimeSpan.TicksPerSecond == 0 )
					{
						scale = TimeSpanScale.Seconds;
						value /= TimeSpan.TicksPerSecond;
					}
					else if ( value % TimeSpan.TicksPerMillisecond == 0 )
					{
						scale = TimeSpanScale.Milliseconds;
						value /= TimeSpan.TicksPerMillisecond;
					}
					else
					{
						scale = TimeSpanScale.Ticks;
					}

					var token = ProtoWriter.StartSubItem( null, dest );

					if ( value != 0 )
					{
						ProtoWriter.WriteFieldHeader( FieldTimeSpanValue, WireType.SignedVariant, dest );
						ProtoWriter.WriteInt64( value, dest );
					}

					if ( scale != TimeSpanScale.Days )
					{
						ProtoWriter.WriteFieldHeader( FieldTimeSpanScale, WireType.Variant, dest );
						ProtoWriter.WriteInt32( (Int32) scale, dest );
					}

					ProtoWriter.EndSubItem( token, dest );
					break;
				case WireType.Fixed64:
					ProtoWriter.WriteInt64( timeSpan.Ticks, dest );
					break;
				default:
					throw new ProtoException( "Unexpected wire-type: " + dest.WireType.ToString() );
			}
		}

		/// <summary>
		/// Parses a TimeSpan from a protobuf stream
		/// </summary>
		public static TimeSpan ReadTimeSpan( ProtoReader source )
		{
			var ticks = ReadTimeSpanTicks( source );
			if ( ticks == Int64.MinValue ) return TimeSpan.MinValue;
			if ( ticks == Int64.MaxValue ) return TimeSpan.MaxValue;
			return TimeSpan.FromTicks( ticks );
		}

		/// <summary>
		/// Parses a DateTime from a protobuf stream
		/// </summary>
		public static DateTime ReadDateTime( ProtoReader source )
		{
			var ticks = ReadTimeSpanTicks( source );
			if ( ticks == Int64.MinValue ) return DateTime.MinValue;
			if ( ticks == Int64.MaxValue ) return DateTime.MaxValue;
			return EpochOrigin.AddTicks( ticks );
		}

		/// <summary>
		/// Writes a DateTime to a protobuf stream
		/// </summary>
		public static void WriteDateTime( DateTime value, ProtoWriter dest )
		{
			if ( dest == null ) throw new ArgumentNullException( "dest" );
			TimeSpan delta;
			switch ( dest.WireType )
			{
				case WireType.StartGroup:
				case WireType.String:
					if ( value == DateTime.MaxValue )
						delta = TimeSpan.MaxValue;
					else if ( value == DateTime.MinValue )
						delta = TimeSpan.MinValue;
					else
						delta = value - EpochOrigin;
					break;
				default:
					delta = value - EpochOrigin;
					break;
			}

			WriteTimeSpan( delta, dest );
		}

		static Int64 ReadTimeSpanTicks( ProtoReader source )
		{
			switch ( source.WireType )
			{
				case WireType.String:
				case WireType.StartGroup:
					var token = ProtoReader.StartSubItem( source );
					Int32 fieldNumber;
					var scale = TimeSpanScale.Days;
					Int64 value = 0;
					while ( ( fieldNumber = source.ReadFieldHeader() ) > 0 )
						switch ( fieldNumber )
						{
							case FieldTimeSpanScale:
								scale = (TimeSpanScale) source.ReadInt32();
								break;
							case FieldTimeSpanValue:
								source.Assert( WireType.SignedVariant );
								value = source.ReadInt64();
								break;
							default:
								source.SkipField();
								break;
						}

					ProtoReader.EndSubItem( token, source );
					switch ( scale )
					{
						case TimeSpanScale.Days:
							return value * TimeSpan.TicksPerDay;
						case TimeSpanScale.Hours:
							return value * TimeSpan.TicksPerHour;
						case TimeSpanScale.Minutes:
							return value * TimeSpan.TicksPerMinute;
						case TimeSpanScale.Seconds:
							return value * TimeSpan.TicksPerSecond;
						case TimeSpanScale.Milliseconds:
							return value * TimeSpan.TicksPerMillisecond;
						case TimeSpanScale.Ticks:
							return value;
						case TimeSpanScale.MinMax:
							switch ( value )
							{
								case 1:  return Int64.MaxValue;
								case -1: return Int64.MinValue;
								default: throw new ProtoException( "Unknown min/max value: " + value.ToString() );
							}
						default:
							throw new ProtoException( "Unknown timescale: " + scale.ToString() );
					}
				case WireType.Fixed64:
					return source.ReadInt64();
				default:
					throw new ProtoException( "Unexpected wire-type: " + source.WireType.ToString() );
			}
		}

		const Int32 FieldDecimalLow = 0x01, FieldDecimalHigh = 0x02, FieldDecimalSignScale = 0x03;

		/// <summary>
		/// Parses a decimal from a protobuf stream
		/// </summary>
		public static Decimal ReadDecimal( ProtoReader reader )
		{
			UInt64 low = 0;
			UInt32 high = 0;
			UInt32 signScale = 0;

			Int32 fieldNumber;
			var token = ProtoReader.StartSubItem( reader );
			while ( ( fieldNumber = reader.ReadFieldHeader() ) > 0 )
				switch ( fieldNumber )
				{
					case FieldDecimalLow:
						low = reader.ReadUInt64();
						break;
					case FieldDecimalHigh:
						high = reader.ReadUInt32();
						break;
					case FieldDecimalSignScale:
						signScale = reader.ReadUInt32();
						break;
					default:
						reader.SkipField();
						break;
				}

			ProtoReader.EndSubItem( token, reader );

			if ( low == 0 && high == 0 ) return Decimal.Zero;

			Int32 lo = (Int32) ( low & 0xFFFFFFFFL ),
			      mid = (Int32) ( ( low >> 32 ) & 0xFFFFFFFFL ),
			      hi = (Int32) high;
			var isNeg = ( signScale & 0x0001 ) == 0x0001;
			var scale = (Byte) ( ( signScale & 0x01FE ) >> 1 );
			return new Decimal( lo, mid, hi, isNeg, scale );
		}

		/// <summary>
		/// Writes a decimal to a protobuf stream
		/// </summary>
		public static void WriteDecimal( Decimal value, ProtoWriter writer )
		{
			var bits = Decimal.GetBits( value );
			UInt64 a = (UInt64) bits[1] << 32, b = (UInt64) bits[0] & 0xFFFFFFFFL;
			var low = a | b;
			var high = (UInt32) bits[2];
			var signScale = (UInt32) ( ( ( bits[3] >> 15 ) & 0x01FE ) | ( ( bits[3] >> 31 ) & 0x0001 ) );

			var token = ProtoWriter.StartSubItem( null, writer );
			if ( low != 0 )
			{
				ProtoWriter.WriteFieldHeader( FieldDecimalLow, WireType.Variant, writer );
				ProtoWriter.WriteUInt64( low, writer );
			}

			if ( high != 0 )
			{
				ProtoWriter.WriteFieldHeader( FieldDecimalHigh, WireType.Variant, writer );
				ProtoWriter.WriteUInt32( high, writer );
			}

			if ( signScale != 0 )
			{
				ProtoWriter.WriteFieldHeader( FieldDecimalSignScale, WireType.Variant, writer );
				ProtoWriter.WriteUInt32( signScale, writer );
			}

			ProtoWriter.EndSubItem( token, writer );
		}

		const Int32 FieldGuidLow = 1, FieldGuidHigh = 2;

		/// <summary>
		/// Writes a Guid to a protobuf stream
		/// </summary>
		public static void WriteGuid( Guid value, ProtoWriter dest )
		{
			var blob = value.ToByteArray();

			var token = ProtoWriter.StartSubItem( null, dest );
			if ( value != Guid.Empty )
			{
				ProtoWriter.WriteFieldHeader( FieldGuidLow, WireType.Fixed64, dest );
				ProtoWriter.WriteBytes( blob, 0, 8, dest );
				ProtoWriter.WriteFieldHeader( FieldGuidHigh, WireType.Fixed64, dest );
				ProtoWriter.WriteBytes( blob, 8, 8, dest );
			}

			ProtoWriter.EndSubItem( token, dest );
		}

		/// <summary>
		/// Parses a Guid from a protobuf stream
		/// </summary>
		public static Guid ReadGuid( ProtoReader source )
		{
			UInt64 low = 0, high = 0;
			Int32 fieldNumber;
			var token = ProtoReader.StartSubItem( source );
			while ( ( fieldNumber = source.ReadFieldHeader() ) > 0 )
				switch ( fieldNumber )
				{
					case FieldGuidLow:
						low = source.ReadUInt64();
						break;
					case FieldGuidHigh:
						high = source.ReadUInt64();
						break;
					default:
						source.SkipField();
						break;
				}

			ProtoReader.EndSubItem( token, source );
			if ( low == 0 && high == 0 ) return Guid.Empty;
			UInt32 a = (UInt32) ( low >> 32 ), b = (UInt32) low, c = (UInt32) ( high >> 32 ), d = (UInt32) high;
			return new Guid( (Int32) b, (Int16) a, (Int16) ( a >> 16 ),
			                 (Byte) d, (Byte) ( d >> 8 ), (Byte) ( d >> 16 ), (Byte) ( d >> 24 ),
			                 (Byte) c, (Byte) ( c >> 8 ), (Byte) ( c >> 16 ), (Byte) ( c >> 24 ) );
		}


		const Int32
			FieldExistingObjectKey = 1,
			FieldNewObjectKey = 2,
			FieldExistingTypeKey = 3,
			FieldNewTypeKey = 4,
			FieldTypeName = 8,
			FieldObject = 10;

		/// <summary>
		/// Optional behaviours that introduce .NET-specific functionality
		/// </summary>
		[Flags]
		public enum NetObjectOptions : byte
		{
			/// <summary>
			/// No special behaviour
			/// </summary>
			None = 0,

			/// <summary>
			/// Enables full object-tracking/full-graph support.
			/// </summary>
			AsReference = 1,

			/// <summary>
			/// Embeds the type information into the stream, allowing usage with types not known in advance.
			/// </summary>
			DynamicType = 2,

			/// <summary>
			/// If false, the constructor for the type is bypassed during deserialization, meaning any field initializers
			/// or other initialization code is skipped.
			/// </summary>
			UseConstructor = 4,

			/// <summary>
			/// Should the object index be reserved, rather than creating an object promptly
			/// </summary>
			LateSet = 8
		}

		/// <summary>
		/// Reads an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
		/// </summary>
		public static Object ReadNetObject( Object value, ProtoReader source, Int32 key, Type type, NetObjectOptions options )
		{
#if FEAT_IKVM
            throw new NotSupportedException();
#else
			var token = ProtoReader.StartSubItem( source );
			Int32 fieldNumber;
			Int32 newObjectKey = -1, newTypeKey = -1, tmp;
			while ( ( fieldNumber = source.ReadFieldHeader() ) > 0 )
				switch ( fieldNumber )
				{
					case FieldExistingObjectKey:
						tmp = source.ReadInt32();
						value = source.NetCache.GetKeyedObject( tmp );
						break;
					case FieldNewObjectKey:
						newObjectKey = source.ReadInt32();
						break;
					case FieldExistingTypeKey:
						tmp = source.ReadInt32();
						type = (Type) source.NetCache.GetKeyedObject( tmp );
						key = source.GetTypeKey( ref type );
						break;
					case FieldNewTypeKey:
						newTypeKey = source.ReadInt32();
						break;
					case FieldTypeName:
						var typeName = source.ReadString();
						type = source.DeserializeType( typeName );
						if ( type == null ) throw new ProtoException( "Unable to resolve type: " + typeName + " (you can use the TypeModel.DynamicTypeFormatting event to provide a custom mapping)" );
						if ( type == typeof( String ) )
						{
							key = -1;
						}
						else
						{
							key = source.GetTypeKey( ref type );
							if ( key < 0 )
								throw new InvalidOperationException( "Dynamic type is not a contract-type: " + type.Name );
						}

						break;
					case FieldObject:
						var isString = type == typeof( String );
						var wasNull = value == null;
						var lateSet = wasNull && ( isString || ( options & NetObjectOptions.LateSet ) != 0 );

						if ( newObjectKey >= 0 && !lateSet )
						{
							if ( value == null )
								source.TrapNextObject( newObjectKey );
							else
								source.NetCache.SetKeyedObject( newObjectKey, value );
							if ( newTypeKey >= 0 ) source.NetCache.SetKeyedObject( newTypeKey, type );
						}

						var oldValue = value;
						if ( isString )
							value = source.ReadString();
						else
							value = ProtoReader.ReadTypedObject( oldValue, key, source, type );

						if ( newObjectKey >= 0 )
						{
							if ( wasNull && !lateSet )
								// this both ensures (via exception) that it *was* set, and makes sure we don't shout
								// about changed references
								oldValue = source.NetCache.GetKeyedObject( newObjectKey );
							if ( lateSet )
							{
								source.NetCache.SetKeyedObject( newObjectKey, value );
								if ( newTypeKey >= 0 ) source.NetCache.SetKeyedObject( newTypeKey, type );
							}
						}

						if ( newObjectKey >= 0 && !lateSet && !ReferenceEquals( oldValue, value ) ) throw new ProtoException( "A reference-tracked object changed reference during deserialization" );
						if ( newObjectKey < 0 && newTypeKey >= 0 )
							// have a new type, but not a new object
							source.NetCache.SetKeyedObject( newTypeKey, type );
						break;
					default:
						source.SkipField();
						break;
				}

			if ( newObjectKey >= 0 && ( options & NetObjectOptions.AsReference ) == 0 ) throw new ProtoException( "Object key in input stream, but reference-tracking was not expected" );
			ProtoReader.EndSubItem( token, source );

			return value;
#endif
		}

		/// <summary>
		/// Writes an *implementation specific* bundled .NET object, including (as options) type-metadata, identity/re-use, etc.
		/// </summary>
		public static void WriteNetObject( Object value, ProtoWriter dest, Int32 key, NetObjectOptions options )
		{
#if FEAT_IKVM
            throw new NotSupportedException();
#else
			if ( dest == null ) throw new ArgumentNullException( "dest" );
			Boolean dynamicType = ( options & NetObjectOptions.DynamicType ) != 0,
			        asReference = ( options & NetObjectOptions.AsReference ) != 0;
			var wireType = dest.WireType;
			var token = ProtoWriter.StartSubItem( null, dest );
			var writeObject = true;
			if ( asReference )
			{
				Boolean existing;
				var objectKey = dest.NetCache.AddObjectKey( value, out existing );
				ProtoWriter.WriteFieldHeader( existing ? FieldExistingObjectKey : FieldNewObjectKey, WireType.Variant, dest );
				ProtoWriter.WriteInt32( objectKey, dest );
				if ( existing ) writeObject = false;
			}

			if ( writeObject )
			{
				if ( dynamicType )
				{
					Boolean existing;
					var type = value.GetType();

					if ( !( value is String ) )
					{
						key = dest.GetTypeKey( ref type );
						if ( key < 0 ) throw new InvalidOperationException( "Dynamic type is not a contract-type: " + type.Name );
					}

					var typeKey = dest.NetCache.AddObjectKey( type, out existing );
					ProtoWriter.WriteFieldHeader( existing ? FieldExistingTypeKey : FieldNewTypeKey, WireType.Variant, dest );
					ProtoWriter.WriteInt32( typeKey, dest );
					if ( !existing )
					{
						ProtoWriter.WriteFieldHeader( FieldTypeName, WireType.String, dest );
						ProtoWriter.WriteString( dest.SerializeType( type ), dest );
					}
				}

				ProtoWriter.WriteFieldHeader( FieldObject, wireType, dest );
				if ( value is String )
					ProtoWriter.WriteString( (String) value, dest );
				else
					ProtoWriter.WriteObject( value, key, dest );
			}

			ProtoWriter.EndSubItem( token, dest );
#endif
		}
	}
}
