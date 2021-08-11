﻿#if !NO_RUNTIME
using System;
using System.Collections;
using System.Text;
using libs.ProtoBuf.Serializers;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#if FEAT_COMPILER
using IKVM.Reflection.Emit;
#endif
#else
using System.Reflection;

#if FEAT_COMPILER
using System.Reflection.Emit;
#endif
#endif


namespace libs.ProtoBuf.Meta
{
	/// <summary>
	/// Represents a type at runtime for use with protobuf, allowing the field mappings (etc) to be defined
	/// </summary>
	public class MetaType : ISerializerProxy
	{
		internal sealed class Comparer : IComparer
#if !NO_GENERICS
		                                ,
		                                 System.Collections.Generic.IComparer<MetaType>
#endif
		{
			public static readonly Comparer Default = new Comparer();
			public Int32 Compare( Object x, Object y ) { return Compare( x as MetaType, y as MetaType ); }

			public Int32 Compare( MetaType x, MetaType y )
			{
				if ( ReferenceEquals( x, y ) ) return 0;
				if ( x == null ) return -1;
				if ( y == null ) return 1;

#if FX11
                return string.Compare(x.GetSchemaTypeName(), y.GetSchemaTypeName());
#else
				return String.Compare( x.GetSchemaTypeName(), y.GetSchemaTypeName(), StringComparison.Ordinal );
#endif
			}
		}

		/// <summary>
		/// Get the name of the type being represented
		/// </summary>
		public override String ToString() { return type.ToString(); }

		IProtoSerializer ISerializerProxy.Serializer => Serializer;
		MetaType baseType;

		/// <summary>
		/// Gets the base-type for this type
		/// </summary>
		public MetaType BaseType => baseType;

		internal TypeModel Model => model;

		/// <summary>
		/// When used to compile a model, should public serialization/deserialzation methods
		/// be included for this type?
		/// </summary>
		public Boolean IncludeSerializerMethod
		{
			// negated to minimize common-case / initializer
			get => !HasFlag( OPTIONS_PrivateOnApi );
			set => SetFlag( OPTIONS_PrivateOnApi, !value, true );
		}

		/// <summary>
		/// Should this type be treated as a reference by default?
		/// </summary>
		public Boolean AsReferenceDefault { get => HasFlag( OPTIONS_AsReferenceDefault ); set => SetFlag( OPTIONS_AsReferenceDefault, value, true ); }

		BasicList subTypes;

		Boolean IsValidSubType( Type subType )
		{
#if WINRT
            return typeInfo.IsAssignableFrom(subType.GetTypeInfo());
#else
			return type.IsAssignableFrom( subType );
#endif
		}

		/// <summary>
		/// Adds a known sub-type to the inheritance model
		/// </summary>
		public MetaType AddSubType( Int32 fieldNumber, Type derivedType ) { return AddSubType( fieldNumber, derivedType, DataFormat.Default ); }

		/// <summary>
		/// Adds a known sub-type to the inheritance model
		/// </summary>
		public MetaType AddSubType( Int32 fieldNumber, Type derivedType, DataFormat dataFormat )
		{
			if ( derivedType == null ) throw new ArgumentNullException( "derivedType" );
			if ( fieldNumber < 1 ) throw new ArgumentOutOfRangeException( "fieldNumber" );
#if WINRT
            if (!(typeInfo.IsClass || typeInfo.IsInterface) || typeInfo.IsSealed) {
#else
			if ( !( type.IsClass || type.IsInterface ) || type.IsSealed )
			{
#endif
				throw new InvalidOperationException( "Sub-types can only be added to non-sealed classes" );
			}

			if ( !IsValidSubType( derivedType ) ) throw new ArgumentException( derivedType.Name + " is not a valid sub-type of " + type.Name, "derivedType" );
			var derivedMeta = model[derivedType];
			ThrowIfFrozen();
			derivedMeta.ThrowIfFrozen();
			var subType = new SubType( fieldNumber, derivedMeta, dataFormat );
			ThrowIfFrozen();

			derivedMeta.SetBaseType( this ); // includes ThrowIfFrozen
			if ( subTypes == null ) subTypes = new BasicList();
			subTypes.Add( subType );
			return this;
		}
#if WINRT
        internal static readonly TypeInfo ienumerable = typeof(IEnumerable).GetTypeInfo();
#else
		internal static readonly Type ienumerable = typeof( IEnumerable );
#endif
		void SetBaseType( MetaType baseType )
		{
			if ( baseType == null ) throw new ArgumentNullException( "baseType" );
			if ( this.baseType == baseType ) return;
			if ( this.baseType != null ) throw new InvalidOperationException( "A type can only participate in one inheritance hierarchy" );

			var type = baseType;
			while ( type != null )
			{
				if ( ReferenceEquals( type, this ) ) throw new InvalidOperationException( "Cyclic inheritance is not allowed" );
				type = type.baseType;
			}

			this.baseType = baseType;
		}

		CallbackSet callbacks;

		/// <summary>
		/// Indicates whether the current type has defined callbacks
		/// </summary>
		public Boolean HasCallbacks => callbacks != null && callbacks.NonTrivial;

		/// <summary>
		/// Indicates whether the current type has defined subtypes
		/// </summary>
		public Boolean HasSubtypes => subTypes != null && subTypes.Count != 0;

		/// <summary>
		/// Returns the set of callbacks defined for this type
		/// </summary>
		public CallbackSet Callbacks
		{
			get
			{
				if ( callbacks == null ) callbacks = new CallbackSet( this );
				return callbacks;
			}
		}

		Boolean IsValueType
		{
			get
			{
#if WINRT
                return typeInfo.IsValueType;
#else
				return type.IsValueType;
#endif
			}
		}

		/// <summary>
		/// Assigns the callbacks to use during serialiation/deserialization.
		/// </summary>
		/// <param name="beforeSerialize">The method (or null) called before serialization begins.</param>
		/// <param name="afterSerialize">The method (or null) called when serialization is complete.</param>
		/// <param name="beforeDeserialize">The method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
		/// <param name="afterDeserialize">The method (or null) called when deserialization is complete.</param>
		/// <returns>The set of callbacks.</returns>
		public MetaType SetCallbacks( MethodInfo beforeSerialize, MethodInfo afterSerialize, MethodInfo beforeDeserialize, MethodInfo afterDeserialize )
		{
			var callbacks = Callbacks;
			callbacks.BeforeSerialize = beforeSerialize;
			callbacks.AfterSerialize = afterSerialize;
			callbacks.BeforeDeserialize = beforeDeserialize;
			callbacks.AfterDeserialize = afterDeserialize;
			return this;
		}

		/// <summary>
		/// Assigns the callbacks to use during serialiation/deserialization.
		/// </summary>
		/// <param name="beforeSerialize">The name of the method (or null) called before serialization begins.</param>
		/// <param name="afterSerialize">The name of the method (or null) called when serialization is complete.</param>
		/// <param name="beforeDeserialize">The name of the method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
		/// <param name="afterDeserialize">The name of the method (or null) called when deserialization is complete.</param>
		/// <returns>The set of callbacks.</returns>
		public MetaType SetCallbacks( String beforeSerialize, String afterSerialize, String beforeDeserialize, String afterDeserialize )
		{
			if ( IsValueType ) throw new InvalidOperationException();
			var callbacks = Callbacks;
			callbacks.BeforeSerialize = ResolveMethod( beforeSerialize, true );
			callbacks.AfterSerialize = ResolveMethod( afterSerialize, true );
			callbacks.BeforeDeserialize = ResolveMethod( beforeDeserialize, true );
			callbacks.AfterDeserialize = ResolveMethod( afterDeserialize, true );
			return this;
		}

		internal String GetSchemaTypeName()
		{
			if ( surrogate != null ) return model[surrogate].GetSchemaTypeName();

			if ( !Helpers.IsNullOrEmpty( name ) ) return name;

			var typeName = type.Name;
#if !NO_GENERICS
			if ( type
#if WINRT
                .GetTypeInfo()
#endif
				.IsGenericType )
			{
				var sb = new StringBuilder( typeName );
				var split = typeName.IndexOf( '`' );
				if ( split >= 0 ) sb.Length = split;
				foreach ( var arg in type
#if WINRT
                    .GetTypeInfo().GenericTypeArguments
#else
						.GetGenericArguments()
#endif
				)
				{
					sb.Append( '_' );
					var tmp = arg;
					var key = model.GetKey( ref tmp );
					MetaType mt;
					if ( key >= 0 && ( mt = model[tmp] ) != null && mt.surrogate == null ) // <=== need to exclude surrogate to avoid chance of infinite loop
						sb.Append( mt.GetSchemaTypeName() );
					else
						sb.Append( tmp.Name );
				}

				return sb.ToString();
			}
#endif
			return typeName;
		}

		String name;

		/// <summary>
		/// Gets or sets the name of this contract.
		/// </summary>
		public String Name
		{
			get => name;
			set
			{
				ThrowIfFrozen();
				name = value;
			}
		}

		MethodInfo factory;

		/// <summary>
		/// Designate a factory-method to use to create instances of this type
		/// </summary>
		public MetaType SetFactory( MethodInfo factory )
		{
			model.VerifyFactory( factory, type );
			ThrowIfFrozen();
			this.factory = factory;
			return this;
		}


		/// <summary>
		/// Designate a factory-method to use to create instances of this type
		/// </summary>
		public MetaType SetFactory( String factory ) { return SetFactory( ResolveMethod( factory, false ) ); }

		MethodInfo ResolveMethod( String name, Boolean instance )
		{
			if ( Helpers.IsNullOrEmpty( name ) ) return null;
#if WINRT
            return instance ? Helpers.GetInstanceMethod(typeInfo, name) : Helpers.GetStaticMethod(typeInfo, name);
#else
			return instance ? Helpers.GetInstanceMethod( type, name ) : Helpers.GetStaticMethod( type, name );
#endif
		}

		readonly RuntimeTypeModel model;
		internal static Exception InbuiltType( Type type ) { return new ArgumentException( "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: " + type.FullName ); }
		internal MetaType( RuntimeTypeModel model, Type type, MethodInfo factory )
		{
			this.factory = factory;
			if ( model == null ) throw new ArgumentNullException( "model" );
			if ( type == null ) throw new ArgumentNullException( "type" );

			var coreSerializer = model.TryGetBasicTypeSerializer( type );
			if ( coreSerializer != null ) throw InbuiltType( type );

			this.type = type;
#if WINRT
            this.typeInfo = type.GetTypeInfo();
#endif
			this.model = model;

			if ( Helpers.IsEnum( type ) )
			{
#if WINRT
                EnumPassthru = typeInfo.IsDefined(typeof(FlagsAttribute), false);
#else
				EnumPassthru = type.IsDefined( model.MapType( typeof( FlagsAttribute ) ), false );
#endif
			}
		}
#if WINRT
        private readonly TypeInfo typeInfo;
#endif
		/// <summary>
		/// Throws an exception if the type has been made immutable
		/// </summary>
		protected internal void ThrowIfFrozen()
		{
			if ( ( flags & OPTIONS_Frozen ) != 0 ) throw new InvalidOperationException( "The type cannot be changed once a serializer has been generated for " + type.FullName );
		}
		//internal void Freeze() { flags |= OPTIONS_Frozen; }

		readonly Type type;

		/// <summary>
		/// The runtime type that the meta-type represents
		/// </summary>
		public Type Type => type;

		IProtoTypeSerializer serializer;

		internal IProtoTypeSerializer Serializer
		{
			get
			{
				if ( serializer == null )
				{
					var opaqueToken = 0;
					try
					{
						model.TakeLock( ref opaqueToken );
						if ( serializer == null )
						{
							// double-check, but our main purpse with this lock is to ensure thread-safety with
							// serializers needing to wait until another thread has finished adding the properties
							SetFlag( OPTIONS_Frozen, true, false );
							serializer = BuildSerializer();
#if FEAT_COMPILER && !FX11
                            if (model.AutoCompile) CompileInPlace();
#endif
						}
					}
					finally
					{
						model.ReleaseLock( opaqueToken );
					}
				}

				return serializer;
			}
		}

		internal Boolean IsList
		{
			get
			{
				var itemType = IgnoreListHandling ? null : TypeModel.GetListItemType( model, type );
				return itemType != null;
			}
		}

		IProtoTypeSerializer BuildSerializer()
		{
			if ( Helpers.IsEnum( type ) ) return new TagDecorator( ProtoBuf.Serializer.ListItemTag, WireType.Variant, false, new EnumSerializer( type, GetEnumMap() ) );
			var itemType = IgnoreListHandling ? null : TypeModel.GetListItemType( model, type );
			if ( itemType != null )
			{
				if ( surrogate != null ) throw new ArgumentException( "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot use a surrogate" );
				if ( subTypes != null && subTypes.Count != 0 ) throw new ArgumentException( "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed" );
				Type? defaultType = null;
				ResolveListTypes( model, type, ref itemType, ref defaultType );
				var fakeMember = new ValueMember( model, ProtoBuf.Serializer.ListItemTag, type, itemType, defaultType, DataFormat.Default );
				return new TypeSerializer( model, type, new Int32[] { ProtoBuf.Serializer.ListItemTag }, new IProtoSerializer[] { fakeMember.Serializer }, null, true, true, null, constructType, factory );
			}

			if ( surrogate != null )
			{
				MetaType mt = model[surrogate], mtBase;
				while ( ( mtBase = mt.baseType ) != null ) mt = mtBase;
				return new SurrogateSerializer( model, type, surrogate, mt.Serializer );
			}

			if ( IsAutoTuple )
			{
				MemberInfo[] mapping;
				var ctor = ResolveTupleConstructor( type, out mapping );
				if ( ctor == null ) throw new InvalidOperationException();
				return new TupleSerializer( model, ctor, mapping );
			}


			fields.Trim();
			var fieldCount = fields.Count;
			var subTypeCount = subTypes == null ? 0 : subTypes.Count;
			var fieldNumbers = new Int32[fieldCount + subTypeCount];
			var serializers = new IProtoSerializer[fieldCount + subTypeCount];
			var i = 0;
			if ( subTypeCount != 0 )
				foreach ( SubType subType in subTypes )
				{
#if WINRT
                    if (!subType.DerivedType.IgnoreListHandling && ienumerable.IsAssignableFrom(subType.DerivedType.Type.GetTypeInfo()))
#else
					if ( !subType.DerivedType.IgnoreListHandling && model.MapType( ienumerable ).IsAssignableFrom( subType.DerivedType.Type ) )
#endif
						throw new ArgumentException( "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a subclass" );
					fieldNumbers[i] = subType.FieldNumber;
					serializers[i++] = subType.Serializer;
				}

			if ( fieldCount != 0 )
				foreach ( ValueMember member in fields )
				{
					fieldNumbers[i] = member.FieldNumber;
					serializers[i++] = member.Serializer;
				}

			BasicList? baseCtorCallbacks = null;
			var tmp = BaseType;

			while ( tmp != null )
			{
				var method = tmp.HasCallbacks ? tmp.Callbacks.BeforeDeserialize : null;
				if ( method != null )
				{
					if ( baseCtorCallbacks == null ) baseCtorCallbacks = new BasicList();
					baseCtorCallbacks.Add( method );
				}

				tmp = tmp.BaseType;
			}

			MethodInfo[]? arr = null;
			if ( baseCtorCallbacks != null )
			{
				arr = new MethodInfo[baseCtorCallbacks.Count];
				baseCtorCallbacks.CopyTo( arr, 0 );
				Array.Reverse( arr );
			}

			return new TypeSerializer( model, type, fieldNumbers, serializers, arr, baseType == null, UseConstructor, callbacks, constructType, factory );
		}

		[Flags]
		internal enum AttributeFamily
		{
			None = 0, ProtoBuf = 1, DataContractSerialier = 2, XmlSerializer = 4, AutoTuple = 8
		}

		static Type GetBaseType( MetaType type )
		{
#if WINRT
            return type.typeInfo.BaseType;
#else
			return type.type.BaseType;
#endif
		}

		internal static Boolean GetAsReferenceDefault( RuntimeTypeModel model, Type type )
		{
			if ( type == null ) throw new ArgumentNullException( "type" );
			if ( Helpers.IsEnum( type ) ) return false; // never as-ref
			var typeAttribs = AttributeMap.Create( model, type, false );
			for ( var i = 0; i < typeAttribs.Length; i++ )
				if ( typeAttribs[i].AttributeType.FullName == "ProtoBuf.ProtoContractAttribute" )
				{
					Object tmp;
					if ( typeAttribs[i].TryGet( "AsReferenceDefault", out tmp ) ) return (Boolean) tmp;
				}

			return false;
		}

		internal void ApplyDefaultBehaviour()
		{
			var baseType = GetBaseType( this );
			if ( baseType != null && model.FindWithoutAdd( baseType ) == null
			                      && GetContractFamily( model, baseType, null ) != AttributeFamily.None )
				model.FindOrAddAuto( baseType, true, false, false );

			var typeAttribs = AttributeMap.Create( model, type, false );
			var family = GetContractFamily( model, type, typeAttribs );
			if ( family == AttributeFamily.AutoTuple ) SetFlag( OPTIONS_AutoTuple, true, true );
			var isEnum = !EnumPassthru && Helpers.IsEnum( type );
			if ( family == AttributeFamily.None && !isEnum ) return; // and you'd like me to do what, exactly?
			BasicList? partialIgnores = null, partialMembers = null;
			Int32 dataMemberOffset = 0, implicitFirstTag = 1;
			var inferTagByName = model.InferTagFromNameDefault;
			var implicitMode = ImplicitFields.None;
			String? name = null;
			for ( var i = 0; i < typeAttribs.Length; i++ )
			{
				var item = (AttributeMap) typeAttribs[i];
				Object tmp;
				var fullAttributeTypeName = item.AttributeType.FullName;
				if ( !isEnum && fullAttributeTypeName == "ProtoBuf.ProtoIncludeAttribute" )
				{
					var tag = 0;
					if ( item.TryGet( "tag", out tmp ) ) tag = (Int32) tmp;
					var dataFormat = DataFormat.Default;
					if ( item.TryGet( "DataFormat", out tmp ) ) dataFormat = (DataFormat) (Int32) tmp;
					Type? knownType = null;
					try
					{
						if ( item.TryGet( "knownTypeName", out tmp ) )
							knownType = model.GetType( (String) tmp, type
#if WINRT
                            .GetTypeInfo()
#endif
								                           .Assembly );
						else if ( item.TryGet( "knownType", out tmp ) ) knownType = (Type) tmp;
					}
					catch ( Exception ex )
					{
						throw new InvalidOperationException( "Unable to resolve sub-type of: " + type.FullName, ex );
					}

					if ( knownType == null ) throw new InvalidOperationException( "Unable to resolve sub-type of: " + type.FullName );
					if ( IsValidSubType( knownType ) ) AddSubType( tag, knownType, dataFormat );
				}

				if ( fullAttributeTypeName == "ProtoBuf.ProtoPartialIgnoreAttribute" )
					if ( item.TryGet( "MemberName", out tmp ) && tmp != null )
					{
						if ( partialIgnores == null ) partialIgnores = new BasicList();
						partialIgnores.Add( (String) tmp );
					}

				if ( !isEnum && fullAttributeTypeName == "ProtoBuf.ProtoPartialMemberAttribute" )
				{
					if ( partialMembers == null ) partialMembers = new BasicList();
					partialMembers.Add( item );
				}

				if ( fullAttributeTypeName == "ProtoBuf.ProtoContractAttribute" )
				{
					if ( item.TryGet( "Name", out tmp ) ) name = (String) tmp;
					if ( Helpers.IsEnum( type ) ) // note this is subtly different to isEnum; want to do this even if [Flags]
					{
#if !FEAT_IKVM
						// IKVM can't access EnumPassthruHasValue, but conveniently, InferTagFromName will only be returned if set via ctor or property
						if ( item.TryGet( "EnumPassthruHasValue", false, out tmp ) && (Boolean) tmp )
#endif
							if ( item.TryGet( "EnumPassthru", out tmp ) )
							{
								EnumPassthru = (Boolean) tmp;
								if ( EnumPassthru ) isEnum = false; // no longer treated as an enum
							}
					}
					else
					{
						if ( item.TryGet( "DataMemberOffset", out tmp ) ) dataMemberOffset = (Int32) tmp;

#if !FEAT_IKVM
						// IKVM can't access InferTagFromNameHasValue, but conveniently, InferTagFromName will only be returned if set via ctor or property
						if ( item.TryGet( "InferTagFromNameHasValue", false, out tmp ) && (Boolean) tmp )
#endif
							if ( item.TryGet( "InferTagFromName", out tmp ) )
								inferTagByName = (Boolean) tmp;

						if ( item.TryGet( "ImplicitFields", out tmp ) && tmp != null ) implicitMode = (ImplicitFields) (Int32) tmp; // note that this uses the bizarre unboxing rules of enums/underlying-types

						if ( item.TryGet( "SkipConstructor", out tmp ) ) UseConstructor = !(Boolean) tmp;
						if ( item.TryGet( "IgnoreListHandling", out tmp ) ) IgnoreListHandling = (Boolean) tmp;
						if ( item.TryGet( "AsReferenceDefault", out tmp ) ) AsReferenceDefault = (Boolean) tmp;
						if ( item.TryGet( "ImplicitFirstTag", out tmp ) && (Int32) tmp > 0 ) implicitFirstTag = (Int32) tmp;
					}
				}

				if ( fullAttributeTypeName == "System.Runtime.Serialization.DataContractAttribute" )
					if ( name == null && item.TryGet( "Name", out tmp ) )
						name = (String) tmp;
				if ( fullAttributeTypeName == "System.Xml.Serialization.XmlTypeAttribute" )
					if ( name == null && item.TryGet( "TypeName", out tmp ) )
						name = (String) tmp;
			}

			if ( !Helpers.IsNullOrEmpty( name ) ) Name = name;
			if ( implicitMode != ImplicitFields.None ) family &= AttributeFamily.ProtoBuf; // with implicit fields, **only** proto attributes are important
			MethodInfo[]? callbacks = null;

			var members = new BasicList();

#if WINRT
            System.Collections.Generic.IEnumerable<MemberInfo> foundList;
            if(isEnum) {
                foundList = type.GetRuntimeFields();
            }
            else
            {
                System.Collections.Generic.List<MemberInfo> list = new System.Collections.Generic.List<MemberInfo>();
                foreach(PropertyInfo prop in type.GetRuntimeProperties()) {
                    MethodInfo getter = Helpers.GetGetMethod(prop, false, false);
                    if(getter != null && !getter.IsStatic) list.Add(prop);
                }
                foreach(FieldInfo fld in type.GetRuntimeFields()) if(fld.IsPublic && !fld.IsStatic) list.Add(fld);
                foreach(MethodInfo mthd in type.GetRuntimeMethods()) if(mthd.IsPublic && !mthd.IsStatic) list.Add(mthd);
                foundList = list;
            }
#else
			var foundList = type.GetMembers( isEnum
				                                 ? BindingFlags.Public | BindingFlags.Static
				                                 : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
#endif
			foreach ( var member in foundList )
			{
				if ( member.DeclaringType != type ) continue;
				if ( member.IsDefined( model.MapType( typeof( ProtoIgnoreAttribute ) ), true ) ) continue;
				if ( partialIgnores != null && partialIgnores.Contains( member.Name ) ) continue;

				Boolean forced = false, isPublic, isField;
				Type effectiveType;


				PropertyInfo property;
				FieldInfo field;
				MethodInfo method;
				if ( ( property = member as PropertyInfo ) != null )
				{
					if ( isEnum ) continue; // wasn't expecting any props!

					effectiveType = property.PropertyType;
					isPublic = Helpers.GetGetMethod( property, false, false ) != null;
					isField = false;
					ApplyDefaultBehaviour_AddMembers( model, family, isEnum, partialMembers, dataMemberOffset, inferTagByName, implicitMode, members, member, ref forced, isPublic, isField, ref effectiveType );
				}
				else if ( ( field = member as FieldInfo ) != null )
				{
					effectiveType = field.FieldType;
					isPublic = field.IsPublic;
					isField = true;
					if ( isEnum && !field.IsStatic )
						// only care about static things on enums; WinRT has a __value instance field!
						continue;
					ApplyDefaultBehaviour_AddMembers( model, family, isEnum, partialMembers, dataMemberOffset, inferTagByName, implicitMode, members, member, ref forced, isPublic, isField, ref effectiveType );
				}
				else if ( ( method = member as MethodInfo ) != null )
				{
					if ( isEnum ) continue;
					var memberAttribs = AttributeMap.Create( model, method, false );
					if ( memberAttribs != null && memberAttribs.Length > 0 )
					{
						CheckForCallback( method, memberAttribs, "ProtoBuf.ProtoBeforeSerializationAttribute", ref callbacks, 0 );
						CheckForCallback( method, memberAttribs, "ProtoBuf.ProtoAfterSerializationAttribute", ref callbacks, 1 );
						CheckForCallback( method, memberAttribs, "ProtoBuf.ProtoBeforeDeserializationAttribute", ref callbacks, 2 );
						CheckForCallback( method, memberAttribs, "ProtoBuf.ProtoAfterDeserializationAttribute", ref callbacks, 3 );
						CheckForCallback( method, memberAttribs, "System.Runtime.Serialization.OnSerializingAttribute", ref callbacks, 4 );
						CheckForCallback( method, memberAttribs, "System.Runtime.Serialization.OnSerializedAttribute", ref callbacks, 5 );
						CheckForCallback( method, memberAttribs, "System.Runtime.Serialization.OnDeserializingAttribute", ref callbacks, 6 );
						CheckForCallback( method, memberAttribs, "System.Runtime.Serialization.OnDeserializedAttribute", ref callbacks, 7 );
					}
				}
			}

			var arr = new ProtoMemberAttribute[members.Count];
			members.CopyTo( arr, 0 );

			if ( inferTagByName || implicitMode != ImplicitFields.None )
			{
				Array.Sort( arr );
				var nextTag = implicitFirstTag;
				foreach ( var normalizedAttribute in arr )
					if ( !normalizedAttribute.TagIsPinned ) // if ProtoMember etc sets a tag, we'll trust it
						normalizedAttribute.Rebase( nextTag++ );
			}

			foreach ( var normalizedAttribute in arr )
			{
				var vm = ApplyDefaultBehaviour( isEnum, normalizedAttribute );
				if ( vm != null ) Add( vm );
			}

			if ( callbacks != null )
				SetCallbacks( Coalesce( callbacks, 0, 4 ), Coalesce( callbacks, 1, 5 ),
				              Coalesce( callbacks, 2, 6 ), Coalesce( callbacks, 3, 7 ) );
		}

		static void ApplyDefaultBehaviour_AddMembers( TypeModel model, AttributeFamily family, Boolean isEnum, BasicList partialMembers, Int32 dataMemberOffset, Boolean inferTagByName, ImplicitFields implicitMode, BasicList members, MemberInfo member, ref Boolean forced, Boolean isPublic, Boolean isField, ref Type effectiveType )
		{
			switch ( implicitMode )
			{
				case ImplicitFields.AllFields:
					if ( isField ) forced = true;
					break;
				case ImplicitFields.AllPublic:
					if ( isPublic ) forced = true;
					break;
			}

			// we just don't like delegate types ;p
#if WINRT
            if (effectiveType.GetTypeInfo().IsSubclassOf(typeof(Delegate))) effectiveType = null;
#else
			if ( effectiveType.IsSubclassOf( model.MapType( typeof( Delegate ) ) ) ) effectiveType = null;
#endif
			if ( effectiveType != null )
			{
				var normalizedAttribute = NormalizeProtoMember( model, member, family, forced, isEnum, partialMembers, dataMemberOffset, inferTagByName );
				if ( normalizedAttribute != null ) members.Add( normalizedAttribute );
			}
		}


		static MethodInfo Coalesce( MethodInfo[] arr, Int32 x, Int32 y )
		{
			var mi = arr[x];
			if ( mi == null ) mi = arr[y];
			return mi;
		}

		internal static AttributeFamily GetContractFamily( RuntimeTypeModel model, Type type, AttributeMap[] attributes )
		{
			var family = AttributeFamily.None;

			if ( attributes == null ) attributes = AttributeMap.Create( model, type, false );

			for ( var i = 0; i < attributes.Length; i++ )
				switch ( attributes[i].AttributeType.FullName )
				{
					case "ProtoBuf.ProtoContractAttribute":
						var tmp = false;
						GetFieldBoolean( ref tmp, attributes[i], "UseProtoMembersOnly" );
						if ( tmp ) return AttributeFamily.ProtoBuf;
						family |= AttributeFamily.ProtoBuf;
						break;
					case "System.Xml.Serialization.XmlTypeAttribute":
						if ( !model.AutoAddProtoContractTypesOnly ) family |= AttributeFamily.XmlSerializer;
						break;
					case "System.Runtime.Serialization.DataContractAttribute":
						if ( !model.AutoAddProtoContractTypesOnly ) family |= AttributeFamily.DataContractSerialier;
						break;
				}

			if ( family == AttributeFamily.None )
			{
				// check for obvious tuples
				MemberInfo[] mapping;
				if ( ResolveTupleConstructor( type, out mapping ) != null ) family |= AttributeFamily.AutoTuple;
			}

			return family;
		}

		internal static ConstructorInfo ResolveTupleConstructor( Type type, out MemberInfo[] mappedMembers )
		{
			mappedMembers = null;
			if ( type == null ) throw new ArgumentNullException( "type" );
#if WINRT
            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsAbstract) return null; // as if!
            ConstructorInfo[] ctors = Helpers.GetConstructors(typeInfo, false);
#else
			if ( type.IsAbstract ) return null; // as if!
			var ctors = Helpers.GetConstructors( type, false );
#endif
			// need to have an interesting constructor to bother even checking this stuff
			if ( ctors.Length == 0 || ctors.Length == 1 && ctors[0].GetParameters().Length == 0 ) return null;

			var fieldsPropsUnfiltered = Helpers.GetInstanceFieldsAndProperties( type, true );
			var memberList = new BasicList();
			for ( var i = 0; i < fieldsPropsUnfiltered.Length; i++ )
			{
				var prop = fieldsPropsUnfiltered[i] as PropertyInfo;
				if ( prop != null )
				{
					if ( !prop.CanRead ) return null;                                                       // no use if can't read
					if ( prop.CanWrite && Helpers.GetSetMethod( prop, false, false ) != null ) return null; // don't allow a public set (need to allow non-public to handle Mono's KeyValuePair<,>)
					memberList.Add( prop );
				}
				else
				{
					var field = fieldsPropsUnfiltered[i] as FieldInfo;
					if ( field != null )
					{
						if ( !field.IsInitOnly ) return null; // all public fields must be readonly to be counted a tuple
						memberList.Add( field );
					}
				}
			}

			if ( memberList.Count == 0 ) return null;

			var members = new MemberInfo[memberList.Count];
			memberList.CopyTo( members, 0 );

			var mapping = new Int32[members.Length];
			var found = 0;
			ConstructorInfo? result = null;
			mappedMembers = new MemberInfo[mapping.Length];
			for ( var i = 0; i < ctors.Length; i++ )
			{
				var parameters = ctors[i].GetParameters();

				if ( parameters.Length != members.Length ) continue;

				// reset the mappings to test
				for ( var j = 0; j < mapping.Length; j++ ) mapping[j] = -1;

				for ( var j = 0; j < parameters.Length; j++ )
				{
					var lower = parameters[j].Name.ToLower();
					for ( var k = 0; k < members.Length; k++ )
					{
						if ( members[k].Name.ToLower() != lower ) continue;
						var memberType = Helpers.GetMemberType( members[k] );
						if ( memberType != parameters[j].ParameterType ) continue;

						mapping[j] = k;
					}
				}

				// did we map all?
				var notMapped = false;
				for ( var j = 0; j < mapping.Length; j++ )
				{
					if ( mapping[j] < 0 )
					{
						notMapped = true;
						break;
					}

					mappedMembers[j] = members[mapping[j]];
				}

				if ( notMapped ) continue;
				found++;
				result = ctors[i];
			}

			return found == 1 ? result : null;
		}

		static void CheckForCallback( MethodInfo method, AttributeMap[] attributes, String callbackTypeName, ref MethodInfo[] callbacks, Int32 index )
		{
			for ( var i = 0; i < attributes.Length; i++ )
				if ( attributes[i].AttributeType.FullName == callbackTypeName )
				{
					if ( callbacks == null ) { callbacks = new MethodInfo[8]; }
					else if ( callbacks[index] != null )
					{
#if WINRT || FEAT_IKVM
                        Type reflected = method.DeclaringType;
#else
						var reflected = method.ReflectedType;
#endif
						throw new ProtoException( "Duplicate " + callbackTypeName + " callbacks on " + reflected.FullName );
					}

					callbacks[index] = method;
				}
		}

		static Boolean HasFamily( AttributeFamily value, AttributeFamily required ) { return ( value & required ) == required; }

		static ProtoMemberAttribute NormalizeProtoMember( TypeModel model, MemberInfo member, AttributeFamily family, Boolean forced, Boolean isEnum, BasicList partialMembers, Int32 dataMemberOffset, Boolean inferByTagName )
		{
			if ( member == null || family == AttributeFamily.None && !isEnum ) return null; // nix
			Int32 fieldNumber = Int32.MinValue, minAcceptFieldNumber = inferByTagName ? -1 : 1;
			String? name = null;
			Boolean isPacked = false, ignore = false, done = false, isRequired = false, asReference = false, asReferenceHasValue = false, dynamicType = false, tagIsPinned = false, overwriteList = false;
			var dataFormat = DataFormat.Default;
			if ( isEnum ) forced = true;
			var attribs = AttributeMap.Create( model, member, true );
			AttributeMap attrib;

			if ( isEnum )
			{
				attrib = GetAttribute( attribs, "ProtoBuf.ProtoIgnoreAttribute" );
				if ( attrib != null )
				{
					ignore = true;
				}
				else
				{
					attrib = GetAttribute( attribs, "ProtoBuf.ProtoEnumAttribute" );
#if WINRT || PORTABLE || CF || FX11
                    fieldNumber = Convert.ToInt32(((FieldInfo)member).GetValue(null));
#else
					fieldNumber = Convert.ToInt32( ( (FieldInfo) member ).GetRawConstantValue() );
#endif
					if ( attrib != null )
					{
						GetFieldName( ref name, attrib, "Name" );
#if !FEAT_IKVM // IKVM can't access HasValue, but conveniently, Value will only be returned if set via ctor or property
						if ( (Boolean) Helpers.GetInstanceMethod( attrib.AttributeType
#if WINRT
                             .GetTypeInfo()
#endif
						                                        , "HasValue" ).Invoke( attrib.Target, null ) )
#endif
						{
							Object tmp;
							if ( attrib.TryGet( "Value", out tmp ) ) fieldNumber = (Int32) tmp;
						}
					}
				}

				done = true;
			}

			if ( !ignore && !done ) // always consider ProtoMember
			{
				attrib = GetAttribute( attribs, "ProtoBuf.ProtoMemberAttribute" );
				GetIgnore( ref ignore, attrib, attribs, "ProtoBuf.ProtoIgnoreAttribute" );

				if ( !ignore && attrib != null )
				{
					GetFieldNumber( ref fieldNumber, attrib, "Tag" );
					GetFieldName( ref name, attrib, "Name" );
					GetFieldBoolean( ref isRequired, attrib, "IsRequired" );
					GetFieldBoolean( ref isPacked, attrib, "IsPacked" );
					GetFieldBoolean( ref overwriteList, attrib, "OverwriteList" );
					GetDataFormat( ref dataFormat, attrib, "DataFormat" );

#if !FEAT_IKVM
					// IKVM can't access AsReferenceHasValue, but conveniently, AsReference will only be returned if set via ctor or property
					GetFieldBoolean( ref asReferenceHasValue, attrib, "AsReferenceHasValue", false );
					if ( asReferenceHasValue )
#endif
						asReferenceHasValue = GetFieldBoolean( ref asReference, attrib, "AsReference", true );
					GetFieldBoolean( ref dynamicType, attrib, "DynamicType" );
					done = tagIsPinned = fieldNumber > 0; // note minAcceptFieldNumber only applies to non-proto
				}

				if ( !done && partialMembers != null )
					foreach ( AttributeMap ppma in partialMembers )
					{
						Object tmp;
						if ( ppma.TryGet( "MemberName", out tmp ) && (String) tmp == member.Name )
						{
							GetFieldNumber( ref fieldNumber, ppma, "Tag" );
							GetFieldName( ref name, ppma, "Name" );
							GetFieldBoolean( ref isRequired, ppma, "IsRequired" );
							GetFieldBoolean( ref isPacked, ppma, "IsPacked" );
							GetFieldBoolean( ref overwriteList, attrib, "OverwriteList" );
							GetDataFormat( ref dataFormat, ppma, "DataFormat" );

#if !FEAT_IKVM
							// IKVM can't access AsReferenceHasValue, but conveniently, AsReference will only be returned if set via ctor or property
							GetFieldBoolean( ref asReferenceHasValue, attrib, "AsReferenceHasValue", false );
							if ( asReferenceHasValue )
#endif
								asReferenceHasValue = GetFieldBoolean( ref asReference, ppma, "AsReference", true );
							GetFieldBoolean( ref dynamicType, ppma, "DynamicType" );
							if ( done = tagIsPinned = fieldNumber > 0 ) break; // note minAcceptFieldNumber only applies to non-proto
						}
					}
			}

			if ( !ignore && !done && HasFamily( family, AttributeFamily.DataContractSerialier ) )
			{
				attrib = GetAttribute( attribs, "System.Runtime.Serialization.DataMemberAttribute" );
				if ( attrib != null )
				{
					GetFieldNumber( ref fieldNumber, attrib, "Order" );
					GetFieldName( ref name, attrib, "Name" );
					GetFieldBoolean( ref isRequired, attrib, "IsRequired" );
					done = fieldNumber >= minAcceptFieldNumber;
					if ( done ) fieldNumber += dataMemberOffset; // dataMemberOffset only applies to DCS flags, to allow us to "bump" WCF by a notch
				}
			}

			if ( !ignore && !done && HasFamily( family, AttributeFamily.XmlSerializer ) )
			{
				attrib = GetAttribute( attribs, "System.Xml.Serialization.XmlElementAttribute" );
				if ( attrib == null ) attrib = GetAttribute( attribs, "System.Xml.Serialization.XmlArrayAttribute" );
				GetIgnore( ref ignore, attrib, attribs, "System.Xml.Serialization.XmlIgnoreAttribute" );
				if ( attrib != null && !ignore )
				{
					GetFieldNumber( ref fieldNumber, attrib, "Order" );
					GetFieldName( ref name, attrib, "ElementName" );
					done = fieldNumber >= minAcceptFieldNumber;
				}
			}

			if ( !ignore && !done )
				if ( GetAttribute( attribs, "System.NonSerializedAttribute" ) != null )
					ignore = true;
			if ( ignore || fieldNumber < minAcceptFieldNumber && !forced ) return null;
			var result = new ProtoMemberAttribute( fieldNumber, forced || inferByTagName );
			result.AsReference = asReference;
			result.AsReferenceHasValue = asReferenceHasValue;
			result.DataFormat = dataFormat;
			result.DynamicType = dynamicType;
			result.IsPacked = isPacked;
			result.OverwriteList = overwriteList;
			result.IsRequired = isRequired;
			result.Name = Helpers.IsNullOrEmpty( name ) ? member.Name : name;
			result.Member = member;
			result.TagIsPinned = tagIsPinned;
			return result;
		}

		ValueMember ApplyDefaultBehaviour( Boolean isEnum, ProtoMemberAttribute normalizedAttribute )
		{
			MemberInfo member;
			if ( normalizedAttribute == null || ( member = normalizedAttribute.Member ) == null ) return null; // nix

			var effectiveType = Helpers.GetMemberType( member );


			Type? itemType = null;
			Type? defaultType = null;

			// check for list types
			ResolveListTypes( model, effectiveType, ref itemType, ref defaultType );
			// but take it back if it is explicitly excluded
			if ( itemType != null )
			{
				// looks like a list, but double check for IgnoreListHandling
				var idx = model.FindOrAddAuto( effectiveType, false, true, false );
				if ( idx >= 0 && model[effectiveType].IgnoreListHandling )
				{
					itemType = null;
					defaultType = null;
				}
			}

			var attribs = AttributeMap.Create( model, member, true );
			AttributeMap attrib;

			Object? defaultValue = null;
			// implicit zero default
			if ( model.UseImplicitZeroDefaults )
				switch ( Helpers.GetTypeCode( effectiveType ) )
				{
					case ProtoTypeCode.Boolean:
						defaultValue = false;
						break;
					case ProtoTypeCode.Decimal:
						defaultValue = (Decimal) 0;
						break;
					case ProtoTypeCode.Single:
						defaultValue = (Single) 0;
						break;
					case ProtoTypeCode.Double:
						defaultValue = (Double) 0;
						break;
					case ProtoTypeCode.Byte:
						defaultValue = (Byte) 0;
						break;
					case ProtoTypeCode.Char:
						defaultValue = (Char) 0;
						break;
					case ProtoTypeCode.Int16:
						defaultValue = (Int16) 0;
						break;
					case ProtoTypeCode.Int32:
						defaultValue = (Int32) 0;
						break;
					case ProtoTypeCode.Int64:
						defaultValue = (Int64) 0;
						break;
					case ProtoTypeCode.SByte:
						defaultValue = (SByte) 0;
						break;
					case ProtoTypeCode.UInt16:
						defaultValue = (UInt16) 0;
						break;
					case ProtoTypeCode.UInt32:
						defaultValue = (UInt32) 0;
						break;
					case ProtoTypeCode.UInt64:
						defaultValue = (UInt64) 0;
						break;
					case ProtoTypeCode.TimeSpan:
						defaultValue = TimeSpan.Zero;
						break;
					case ProtoTypeCode.Guid:
						defaultValue = Guid.Empty;
						break;
				}

			if ( ( attrib = GetAttribute( attribs, "System.ComponentModel.DefaultValueAttribute" ) ) != null )
			{
				Object tmp;
				if ( attrib.TryGet( "Value", out tmp ) ) defaultValue = tmp;
			}

			var vm = isEnum || normalizedAttribute.Tag > 0
				? new ValueMember( model, type, normalizedAttribute.Tag, member, effectiveType, itemType, defaultType, normalizedAttribute.DataFormat, defaultValue )
				: null;
			if ( vm != null )
			{
#if WINRT
                TypeInfo finalType = typeInfo;
#else
				var finalType = type;
#endif
				var prop = Helpers.GetProperty( finalType, member.Name + "Specified", true );
				var getMethod = Helpers.GetGetMethod( prop, true, true );
				if ( getMethod == null || getMethod.IsStatic ) prop = null;
				if ( prop != null )
				{
					vm.SetSpecified( getMethod, Helpers.GetSetMethod( prop, true, true ) );
				}
				else
				{
					var method = Helpers.GetInstanceMethod( finalType, "ShouldSerialize" + member.Name, Helpers.EmptyTypes );
					if ( method != null && method.ReturnType == model.MapType( typeof( Boolean ) ) ) vm.SetSpecified( method, null );
				}

				if ( !Helpers.IsNullOrEmpty( normalizedAttribute.Name ) ) vm.SetName( normalizedAttribute.Name );
				vm.IsPacked = normalizedAttribute.IsPacked;
				vm.IsRequired = normalizedAttribute.IsRequired;
				vm.OverwriteList = normalizedAttribute.OverwriteList;
				if ( normalizedAttribute.AsReferenceHasValue ) vm.AsReference = normalizedAttribute.AsReference;
				vm.DynamicType = normalizedAttribute.DynamicType;
			}

			return vm;
		}

		static void GetDataFormat( ref DataFormat value, AttributeMap attrib, String memberName )
		{
			if ( attrib == null || value != DataFormat.Default ) return;
			Object obj;
			if ( attrib.TryGet( memberName, out obj ) && obj != null ) value = (DataFormat) obj;
		}

		static void GetIgnore( ref Boolean ignore, AttributeMap attrib, AttributeMap[] attribs, String fullName )
		{
			if ( ignore || attrib == null ) return;
			ignore = GetAttribute( attribs, fullName ) != null;
			return;
		}

		static void GetFieldBoolean( ref Boolean value, AttributeMap attrib, String memberName ) { GetFieldBoolean( ref value, attrib, memberName, true ); }

		static Boolean GetFieldBoolean( ref Boolean value, AttributeMap attrib, String memberName, Boolean publicOnly )
		{
			if ( attrib == null ) return false;
			if ( value ) return true;
			Object obj;
			if ( attrib.TryGet( memberName, publicOnly, out obj ) && obj != null )
			{
				value = (Boolean) obj;
				return true;
			}

			return false;
		}

		static void GetFieldNumber( ref Int32 value, AttributeMap attrib, String memberName )
		{
			if ( attrib == null || value > 0 ) return;
			Object obj;
			if ( attrib.TryGet( memberName, out obj ) && obj != null ) value = (Int32) obj;
		}

		static void GetFieldName( ref String name, AttributeMap attrib, String memberName )
		{
			if ( attrib == null || !Helpers.IsNullOrEmpty( name ) ) return;
			Object obj;
			if ( attrib.TryGet( memberName, out obj ) && obj != null ) name = (String) obj;
		}

		static AttributeMap GetAttribute( AttributeMap[] attribs, String fullName )
		{
			for ( var i = 0; i < attribs.Length; i++ )
			{
				var attrib = attribs[i];
				if ( attrib != null && attrib.AttributeType.FullName == fullName ) return attrib;
			}

			return null;
		}

		/// <summary>
		/// Adds a member (by name) to the MetaType
		/// </summary>
		public MetaType Add( Int32 fieldNumber, String memberName )
		{
			AddField( fieldNumber, memberName, null, null, null );
			return this;
		}

		/// <summary>
		/// Adds a member (by name) to the MetaType, returning the ValueMember rather than the fluent API.
		/// This is otherwise identical to Add.
		/// </summary>
		public ValueMember AddField( Int32 fieldNumber, String memberName ) { return AddField( fieldNumber, memberName, null, null, null ); }

		/// <summary>
		/// Gets or sets whether the type should use a parameterless constructor (the default),
		/// or whether the type should skip the constructor completely. This option is not supported
		/// on compact-framework.
		/// </summary>
		public Boolean UseConstructor
		{
			// negated to have defaults as flat zero
			get => !HasFlag( OPTIONS_SkipConstructor );
			set => SetFlag( OPTIONS_SkipConstructor, !value, true );
		}

		/// <summary>
		/// The concrete type to create when a new instance of this type is needed; this may be useful when dealing
		/// with dynamic proxies, or with interface-based APIs
		/// </summary>
		public Type ConstructType
		{
			get => constructType;
			set
			{
				ThrowIfFrozen();
				constructType = value;
			}
		}

		Type constructType;

		/// <summary>
		/// Adds a member (by name) to the MetaType
		/// </summary>
		public MetaType Add( String memberName )
		{
			Add( GetNextFieldNumber(), memberName );
			return this;
		}

		Type surrogate;

		/// <summary>
		/// Performs serialization of this type via a surrogate; all
		/// other serialization options are ignored and handled
		/// by the surrogate's configuration.
		/// </summary>
		public void SetSurrogate( Type surrogateType )
		{
			if ( surrogateType == type ) surrogateType = null;
			if ( surrogateType != null )
				// note that BuildSerializer checks the **CURRENT TYPE** is OK to be surrogated
				if ( surrogateType != null && Helpers.IsAssignableFrom( model.MapType( typeof( IEnumerable ) ), surrogateType ) )
					throw new ArgumentException( "Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a surrogate" );
			ThrowIfFrozen();
			surrogate = surrogateType;
			// no point in offering chaining; no options are respected
		}

		internal MetaType GetSurrogateOrSelf()
		{
			if ( surrogate != null ) return model[surrogate];
			return this;
		}

		internal MetaType GetSurrogateOrBaseOrSelf( Boolean deep )
		{
			if ( surrogate != null ) return model[surrogate];
			var snapshot = baseType;
			if ( snapshot != null )
			{
				if ( deep )
				{
					MetaType tmp;
					do
					{
						tmp = snapshot;
						snapshot = snapshot.baseType;
					}
					while ( snapshot != null );

					return tmp;
				}

				return snapshot;
			}

			return this;
		}

		Int32 GetNextFieldNumber()
		{
			var maxField = 0;
			foreach ( ValueMember member in fields )
				if ( member.FieldNumber > maxField )
					maxField = member.FieldNumber;
			if ( subTypes != null )
				foreach ( SubType subType in subTypes )
					if ( subType.FieldNumber > maxField )
						maxField = subType.FieldNumber;
			return maxField + 1;
		}

		/// <summary>
		/// Adds a set of members (by name) to the MetaType
		/// </summary>
		public MetaType Add( params String[] memberNames )
		{
			if ( memberNames == null ) throw new ArgumentNullException( "memberNames" );
			var next = GetNextFieldNumber();
			for ( var i = 0; i < memberNames.Length; i++ ) Add( next++, memberNames[i] );
			return this;
		}


		/// <summary>
		/// Adds a member (by name) to the MetaType
		/// </summary>
		public MetaType Add( Int32 fieldNumber, String memberName, Object defaultValue )
		{
			AddField( fieldNumber, memberName, null, null, defaultValue );
			return this;
		}

		/// <summary>
		/// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists
		/// </summary>
		public MetaType Add( Int32 fieldNumber, String memberName, Type itemType, Type defaultType )
		{
			AddField( fieldNumber, memberName, itemType, defaultType, null );
			return this;
		}

		/// <summary>
		/// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists, returning the ValueMember rather than the fluent API.
		/// This is otherwise identical to Add.
		/// </summary>
		public ValueMember AddField( Int32 fieldNumber, String memberName, Type itemType, Type defaultType ) { return AddField( fieldNumber, memberName, itemType, defaultType, null ); }

		ValueMember AddField( Int32 fieldNumber, String memberName, Type itemType, Type defaultType, Object defaultValue )
		{
			MemberInfo? mi = null;
#if WINRT
            mi = Helpers.IsEnum(type) ? type.GetTypeInfo().GetDeclaredField(memberName) : Helpers.GetInstanceMember(type.GetTypeInfo(), memberName);

#else
			var members = type.GetMember( memberName, Helpers.IsEnum( type ) ? BindingFlags.Static | BindingFlags.Public : BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
			if ( members != null && members.Length == 1 ) mi = members[0];
#endif
			if ( mi == null ) throw new ArgumentException( "Unable to determine member: " + memberName, "memberName" );

			Type miType;
#if WINRT || PORTABLE
            PropertyInfo pi = mi as PropertyInfo;
            if (pi == null)
            {
                FieldInfo fi = mi as FieldInfo;
                if (fi == null)
                {
                    throw new NotSupportedException(mi.GetType().Name);
                }
                else
                {
                    miType = fi.FieldType;
                }
            }
            else
            {
                miType = pi.PropertyType;
            }
#else
			switch ( mi.MemberType )
			{
				case MemberTypes.Field:
					miType = ( (FieldInfo) mi ).FieldType;
					break;
				case MemberTypes.Property:
					miType = ( (PropertyInfo) mi ).PropertyType;
					break;
				default:
					throw new NotSupportedException( mi.MemberType.ToString() );
			}
#endif
			ResolveListTypes( model, miType, ref itemType, ref defaultType );
			var newField = new ValueMember( model, type, fieldNumber, mi, miType, itemType, defaultType, DataFormat.Default, defaultValue );
			Add( newField );
			return newField;
		}

		internal static void ResolveListTypes( TypeModel model, Type type, ref Type itemType, ref Type defaultType )
		{
			if ( type == null ) return;
			// handle arrays
			if ( type.IsArray )
			{
				if ( type.GetArrayRank() != 1 ) throw new NotSupportedException( "Multi-dimension arrays are supported" );
				itemType = type.GetElementType();
				if ( itemType == model.MapType( typeof( Byte ) ) )
					defaultType = itemType = null;
				else
					defaultType = type;
			}

			// handle lists
			if ( itemType == null ) itemType = TypeModel.GetListItemType( model, type );

			// check for nested data (not allowed)
			if ( itemType != null )
			{
				Type? nestedItemType = null, nestedDefaultType = null;
				ResolveListTypes( model, itemType, ref nestedItemType, ref nestedDefaultType );
				if ( nestedItemType != null ) throw TypeModel.CreateNestedListsNotSupported();
			}

			if ( itemType != null && defaultType == null )
			{
#if WINRT
                TypeInfo typeInfo = type.GetTypeInfo();
                if (typeInfo.IsClass && !typeInfo.IsAbstract && Helpers.GetConstructor(typeInfo, Helpers.EmptyTypes, true) != null)
#else
				if ( type.IsClass && !type.IsAbstract && Helpers.GetConstructor( type, Helpers.EmptyTypes, true ) != null )
#endif
					defaultType = type;
				if ( defaultType == null )
				{
#if WINRT
                    if (typeInfo.IsInterface)
#else
					if ( type.IsInterface )
#endif
					{
#if NO_GENERICS
                        defaultType = typeof(ArrayList);
#else
						Type[] genArgs;
#if WINRT
                        if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>)
                            && itemType == typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(genArgs = typeInfo.GenericTypeArguments))
#else
						if ( type.IsGenericType && type.GetGenericTypeDefinition() == model.MapType( typeof( System.Collections.Generic.IDictionary<,> ) )
						                        && itemType == model.MapType( typeof( System.Collections.Generic.KeyValuePair<,> ) ).MakeGenericType( genArgs = type.GetGenericArguments() ) )
#endif
							defaultType = model.MapType( typeof( System.Collections.Generic.Dictionary<,> ) ).MakeGenericType( genArgs );
						else
							defaultType = model.MapType( typeof( System.Collections.Generic.List<> ) ).MakeGenericType( itemType );
#endif
					}
				}

				// verify that the default type is appropriate
				if ( defaultType != null && !Helpers.IsAssignableFrom( type, defaultType ) ) defaultType = null;
			}
		}

		void Add( ValueMember member )
		{
			var opaqueToken = 0;
			try
			{
				model.TakeLock( ref opaqueToken );
				ThrowIfFrozen();
				fields.Add( member );
			}
			finally
			{
				model.ReleaseLock( opaqueToken );
			}
		}

		/// <summary>
		/// Returns the ValueMember that matchs a given field number, or null if not found
		/// </summary>
		public ValueMember this[ Int32 fieldNumber ]
		{
			get
			{
				foreach ( ValueMember member in fields )
					if ( member.FieldNumber == fieldNumber )
						return member;
				return null;
			}
		}

		/// <summary>
		/// Returns the ValueMember that matchs a given member (property/field), or null if not found
		/// </summary>
		public ValueMember this[ MemberInfo member ]
		{
			get
			{
				if ( member == null ) return null;
				foreach ( ValueMember x in fields )
					if ( x.Member == member )
						return x;
				return null;
			}
		}

		readonly BasicList fields = new BasicList();

		/// <summary>
		/// Returns the ValueMember instances associated with this type
		/// </summary>
		public ValueMember[] GetFields()
		{
			var arr = new ValueMember[fields.Count];
			fields.CopyTo( arr, 0 );
			Array.Sort( arr, ValueMember.Comparer.Default );
			return arr;
		}

		/// <summary>
		/// Returns the SubType instances associated with this type
		/// </summary>
		public SubType[] GetSubtypes()
		{
			if ( subTypes == null || subTypes.Count == 0 ) return new SubType[0];
			var arr = new SubType[subTypes.Count];
			subTypes.CopyTo( arr, 0 );
			Array.Sort( arr, SubType.Comparer.Default );
			return arr;
		}

#if FEAT_COMPILER && !FX11
        /// <summary>
        /// Compiles the serializer for this type; this is *not* a full
        /// standalone compile, but can significantly boost performance
        /// while allowing additional types to be added.
        /// </summary>
        /// <remarks>An in-place compile can access non-public types / members</remarks>
        public void CompileInPlace()
        {
#if FEAT_IKVM
            // just no nothing, quietely; don't want to break the API
#else
            serializer = CompiledSerializer.Wrap(Serializer, model);
#endif
        }
#endif

		internal Boolean IsDefined( Int32 fieldNumber )
		{
			foreach ( ValueMember field in fields )
				if ( field.FieldNumber == fieldNumber )
					return true;
			return false;
		}

		internal Int32 GetKey( Boolean demand, Boolean getBaseKey ) { return model.GetKey( type, demand, getBaseKey ); }


		internal EnumSerializer.EnumPair[] GetEnumMap()
		{
			if ( HasFlag( OPTIONS_EnumPassThru ) ) return null;
			var result = new EnumSerializer.EnumPair[fields.Count];
			for ( var i = 0; i < result.Length; i++ )
			{
				var member = (ValueMember) fields[i];
				var wireValue = member.FieldNumber;
				var value = member.GetRawEnumValue();
				result[i] = new EnumSerializer.EnumPair( wireValue, value, member.MemberType );
			}

			return result;
		}


		/// <summary>
		/// Gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
		/// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
		/// </summary>
		public Boolean EnumPassthru { get => HasFlag( OPTIONS_EnumPassThru ); set => SetFlag( OPTIONS_EnumPassThru, value, true ); }

		/// <summary>
		/// Gets or sets a value indicating that this type should NOT be treated as a list, even if it has
		/// familiar list-like characteristics (enumerable, add, etc)
		/// </summary>
		public Boolean IgnoreListHandling { get => HasFlag( OPTIONS_IgnoreListHandling ); set => SetFlag( OPTIONS_IgnoreListHandling, value, true ); }

		internal Boolean Pending { get => HasFlag( OPTIONS_Pending ); set => SetFlag( OPTIONS_Pending, value, false ); }

		const Byte
			OPTIONS_Pending = 1,
			OPTIONS_EnumPassThru = 2,
			OPTIONS_Frozen = 4,
			OPTIONS_PrivateOnApi = 8,
			OPTIONS_SkipConstructor = 16,
			OPTIONS_AsReferenceDefault = 32,
			OPTIONS_AutoTuple = 64,
			OPTIONS_IgnoreListHandling = 128;

		volatile Byte flags;
		Boolean HasFlag( Byte flag ) { return ( flags & flag ) == flag; }

		void SetFlag( Byte flag, Boolean value, Boolean throwIfFrozen )
		{
			if ( throwIfFrozen && HasFlag( flag ) != value ) ThrowIfFrozen();
			if ( value )
				flags |= flag;
			else
				flags = (Byte) ( flags & ~flag );
		}

		internal static MetaType GetRootType( MetaType source )
		{
			while ( source.serializer != null )
			{
				var tmp = source.baseType;
				if ( tmp == null ) return source;
				source = tmp; // else loop until we reach something that isn't generated, or is the root
			}

			// now we get into uncertain territory
			var model = source.model;
			var opaqueToken = 0;
			try
			{
				model.TakeLock( ref opaqueToken );

				MetaType tmp;
				while ( ( tmp = source.baseType ) != null ) source = tmp;
				return source;
			}
			finally
			{
				model.ReleaseLock( opaqueToken );
			}
		}

		internal Boolean IsPrepared()
		{
#if FEAT_COMPILER && !FEAT_IKVM && !FX11
            return serializer is CompiledSerializer;
#else
			return false;
#endif
		}

		internal IEnumerable Fields => fields;

		internal static StringBuilder NewLine( StringBuilder builder, Int32 indent ) { return Helpers.AppendLine( builder ).Append( ' ', indent * 3 ); }
		internal Boolean IsAutoTuple => HasFlag( OPTIONS_AutoTuple );

		internal void WriteSchema( StringBuilder builder, Int32 indent, ref Boolean requiresBclImport )
		{
			if ( surrogate != null ) return; // nothing to write


			var fieldsArr = new ValueMember[fields.Count];
			fields.CopyTo( fieldsArr, 0 );
			Array.Sort( fieldsArr, ValueMember.Comparer.Default );

			if ( IsList )
			{
				var itemTypeName = model.GetSchemaTypeName( TypeModel.GetListItemType( model, type ), DataFormat.Default, false, false, ref requiresBclImport );
				NewLine( builder, indent ).Append( "message " ).Append( GetSchemaTypeName() ).Append( " {" );
				NewLine( builder, indent + 1 ).Append( "repeated " ).Append( itemTypeName ).Append( " items = 1;" );
				NewLine( builder, indent ).Append( '}' );
			}
			else if ( IsAutoTuple )
			{
				// key-value-pair etc
				MemberInfo[] mapping;
				if ( ResolveTupleConstructor( type, out mapping ) != null )
				{
					NewLine( builder, indent ).Append( "message " ).Append( GetSchemaTypeName() ).Append( " {" );
					for ( var i = 0; i < mapping.Length; i++ )
					{
						Type effectiveType;
						if ( mapping[i] is PropertyInfo )
							effectiveType = ( (PropertyInfo) mapping[i] ).PropertyType;
						else if ( mapping[i] is FieldInfo )
							effectiveType = ( (FieldInfo) mapping[i] ).FieldType;
						else
							throw new NotSupportedException( "Unknown member type: " + mapping[i].GetType().Name );
						NewLine( builder, indent + 1 ).Append( "optional " ).Append( model.GetSchemaTypeName( effectiveType, DataFormat.Default, false, false, ref requiresBclImport ).Replace( '.', '_' ) )
						                              .Append( ' ' ).Append( mapping[i].Name ).Append( " = " ).Append( i + 1 ).Append( ';' );
					}

					NewLine( builder, indent ).Append( '}' );
				}
			}
			else if ( Helpers.IsEnum( type ) )
			{
				NewLine( builder, indent ).Append( "enum " ).Append( GetSchemaTypeName() ).Append( " {" );
				if ( fieldsArr.Length == 0 && EnumPassthru )
				{
					if ( type
#if WINRT
                    .GetTypeInfo()
#endif
						.IsDefined( model.MapType( typeof( FlagsAttribute ) ), false ) )
						NewLine( builder, indent + 1 ).Append( "// this is a composite/flags enumeration" );
					else
						NewLine( builder, indent + 1 ).Append( "// this enumeration will be passed as a raw value" );
					foreach ( var field in
#if WINRT
                        type.GetRuntimeFields()
#else
						type.GetFields()
#endif
					)
						if ( field.IsStatic && field.IsLiteral )
						{
							Object enumVal;
#if WINRT || PORTABLE || CF || FX11
                            enumVal = field.GetValue(null);
#else
							enumVal = field.GetRawConstantValue();
#endif
							NewLine( builder, indent + 1 ).Append( field.Name ).Append( " = " ).Append( enumVal ).Append( ";" );
						}
				}
				else
				{
					foreach ( var member in fieldsArr ) NewLine( builder, indent + 1 ).Append( member.Name ).Append( " = " ).Append( member.FieldNumber ).Append( ';' );
				}

				NewLine( builder, indent ).Append( '}' );
			}
			else
			{
				NewLine( builder, indent ).Append( "message " ).Append( GetSchemaTypeName() ).Append( " {" );
				foreach ( var member in fieldsArr )
				{
					var ordinality = member.ItemType != null ? "repeated" : member.IsRequired ? "required" : "optional";
					NewLine( builder, indent + 1 ).Append( ordinality ).Append( ' ' );
					if ( member.DataFormat == DataFormat.Group ) builder.Append( "group " );
					var schemaTypeName = member.GetSchemaTypeName( true, ref requiresBclImport );
					builder.Append( schemaTypeName ).Append( " " )
					       .Append( member.Name ).Append( " = " ).Append( member.FieldNumber );
					if ( member.DefaultValue != null )
					{
						if ( member.DefaultValue is String )
							builder.Append( " [default = \"" ).Append( member.DefaultValue ).Append( "\"]" );
						else if ( member.DefaultValue is Boolean )
							// need to be lower case (issue 304)
							builder.Append( (Boolean) member.DefaultValue ? " [default = true]" : " [default = false]" );
						else
							builder.Append( " [default = " ).Append( member.DefaultValue ).Append( ']' );
					}

					if ( member.ItemType != null && member.IsPacked ) builder.Append( " [packed=true]" );
					builder.Append( ';' );
					if ( schemaTypeName == "bcl.NetObjectProxy" && member.AsReference && !member.DynamicType ) // we know what it is; tell the user
						builder.Append( " // reference-tracked " ).Append( member.GetSchemaTypeName( false, ref requiresBclImport ) );
				}

				if ( subTypes != null && subTypes.Count != 0 )
				{
					NewLine( builder, indent + 1 ).Append( "// the following represent sub-types; at most 1 should have a value" );
					var subTypeArr = new SubType[subTypes.Count];
					subTypes.CopyTo( subTypeArr, 0 );
					Array.Sort( subTypeArr, SubType.Comparer.Default );
					foreach ( var subType in subTypeArr )
					{
						var subTypeName = subType.DerivedType.GetSchemaTypeName();
						NewLine( builder, indent + 1 ).Append( "optional " ).Append( subTypeName )
						                              .Append( " " ).Append( subTypeName ).Append( " = " ).Append( subType.FieldNumber ).Append( ';' );
					}
				}

				NewLine( builder, indent ).Append( '}' );
			}
		}
	}
}
#endif
