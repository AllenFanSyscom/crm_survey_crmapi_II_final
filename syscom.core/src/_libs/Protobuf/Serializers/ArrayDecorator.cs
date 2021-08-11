﻿#if !NO_RUNTIME
using System;
using System.Collections;
using libs.ProtoBuf.Meta;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif

namespace libs.ProtoBuf.Serializers
{
	internal sealed class ArrayDecorator : ProtoDecoratorBase
	{
		readonly Int32 fieldNumber;

		const Byte
			OPTIONS_WritePacked = 1,
			OPTIONS_OverwriteList = 2,
			OPTIONS_SupportNull = 4;

		readonly Byte options;
		readonly WireType packedWireType;

		public ArrayDecorator( TypeModel model, IProtoSerializer tail, Int32 fieldNumber, Boolean writePacked, WireType packedWireType, Type arrayType, Boolean overwriteList, Boolean supportNull )
			: base( tail )
		{
			Helpers.DebugAssert( arrayType != null, "arrayType should be non-null" );
			Helpers.DebugAssert( arrayType.IsArray && arrayType.GetArrayRank() == 1, "should be single-dimension array; " + arrayType.FullName );
			itemType = arrayType.GetElementType();
#if NO_GENERICS
            Type underlyingItemType = itemType;
#else
			var underlyingItemType = supportNull ? itemType : Helpers.GetUnderlyingType( itemType ) ?? itemType;
#endif

			Helpers.DebugAssert( underlyingItemType == Tail.ExpectedType, "invalid tail" );
			Helpers.DebugAssert( Tail.ExpectedType != model.MapType( typeof( Byte ) ), "Should have used BlobSerializer" );
			if ( ( writePacked || packedWireType != WireType.None ) && fieldNumber <= 0 ) throw new ArgumentOutOfRangeException( "fieldNumber" );
			if ( !ListDecorator.CanPack( packedWireType ) )
			{
				if ( writePacked ) throw new InvalidOperationException( "Only simple data-types can use packed encoding" );
				packedWireType = WireType.None;
			}

			this.fieldNumber = fieldNumber;
			this.packedWireType = packedWireType;
			if ( writePacked ) options |= OPTIONS_WritePacked;
			if ( overwriteList ) options |= OPTIONS_OverwriteList;
			if ( supportNull ) options |= OPTIONS_SupportNull;
			this.arrayType = arrayType;
		}

		readonly Type arrayType, itemType; // this is, for example, typeof(int[])
		public override Type ExpectedType => arrayType;
		public override Boolean RequiresOldValue => AppendToCollection;
		public override Boolean ReturnsValue => true;
#if FEAT_COMPILER
        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            // int i and T[] arr
            using (Compiler.Local arr = ctx.GetLocalWithValue(arrayType, valueFrom))
            using (Compiler.Local i = new ProtoBuf.Compiler.Local(ctx, ctx.MapType(typeof(int))))
            {
                bool writePacked = (options & OPTIONS_WritePacked) != 0;
                using (Compiler.Local token = writePacked ? new Compiler.Local(ctx, ctx.MapType(typeof(SubItemToken))) : null)
                {
                    Type mappedWriter = ctx.MapType(typeof (ProtoWriter));
                    if (writePacked)
                    {
                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue((int)WireType.String);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(mappedWriter.GetMethod("WriteFieldHeader"));

                        ctx.LoadValue(arr);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(mappedWriter.GetMethod("StartSubItem"));
                        ctx.StoreValue(token);

                        ctx.LoadValue(fieldNumber);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(mappedWriter.GetMethod("SetPackedField"));
                    }
                    EmitWriteArrayLoop(ctx, i, arr);

                    if (writePacked)
                    {
                        ctx.LoadValue(token);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(mappedWriter.GetMethod("EndSubItem"));
                    }
                }
            }
        }

        private void EmitWriteArrayLoop(Compiler.CompilerContext ctx, Compiler.Local i, Compiler.Local arr)
        {
            // i = 0
            ctx.LoadValue(0);
            ctx.StoreValue(i);

            // range test is last (to minimise branches)
            Compiler.CodeLabel loopTest = ctx.DefineLabel(), processItem = ctx.DefineLabel();
            ctx.Branch(loopTest, false);
            ctx.MarkLabel(processItem);

            // {...}
            ctx.LoadArrayValue(arr, i);
            if (SupportNull)
            {
                Tail.EmitWrite(ctx, null);
            }
            else
            {
                ctx.WriteNullCheckedTail(itemType, Tail, null);
            }

            // i++
            ctx.LoadValue(i);
            ctx.LoadValue(1);
            ctx.Add();
            ctx.StoreValue(i);

            // i < arr.Length
            ctx.MarkLabel(loopTest);
            ctx.LoadValue(i);
            ctx.LoadLength(arr, false);
            ctx.BranchIfLess(processItem, false);
        }
#endif
		Boolean AppendToCollection => ( options & OPTIONS_OverwriteList ) == 0;
		Boolean SupportNull => ( options & OPTIONS_SupportNull ) != 0;

#if !FEAT_IKVM
		public override void Write( Object value, ProtoWriter dest )
		{
			var arr = (IList) value;
			var len = arr.Count;
			SubItemToken token;
			var writePacked = ( options & OPTIONS_WritePacked ) != 0;
			if ( writePacked )
			{
				ProtoWriter.WriteFieldHeader( fieldNumber, WireType.String, dest );
				token = ProtoWriter.StartSubItem( value, dest );
				ProtoWriter.SetPackedField( fieldNumber, dest );
			}
			else
			{
				token = new SubItemToken(); // default
			}

			var checkForNull = !SupportNull;
			for ( var i = 0; i < len; i++ )
			{
				var obj = arr[i];
				if ( checkForNull && obj == null ) throw new NullReferenceException();
				Tail.Write( obj, dest );
			}

			if ( writePacked ) ProtoWriter.EndSubItem( token, dest );
		}

		public override Object Read( Object value, ProtoReader source )
		{
			var field = source.FieldNumber;
			var list = new BasicList();
			if ( packedWireType != WireType.None && source.WireType == WireType.String )
			{
				var token = ProtoReader.StartSubItem( source );
				while ( ProtoReader.HasSubValue( packedWireType, source ) ) list.Add( Tail.Read( null, source ) );
				ProtoReader.EndSubItem( token, source );
			}
			else
			{
				do
				{
					list.Add( Tail.Read( null, source ) );
				}
				while ( source.TryReadFieldHeader( field ) );
			}

			var oldLen = AppendToCollection ? value == null ? 0 : ( (Array) value ).Length : 0;
			var result = Array.CreateInstance( itemType, oldLen + list.Count );
			if ( oldLen != 0 ) ( (Array) value ).CopyTo( result, 0 );
			list.CopyTo( result, oldLen );
			return result;
		}
#endif

#if FEAT_COMPILER
        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            Type listType;
#if NO_GENERICS
            listType = typeof(BasicList);
#else
            listType = ctx.MapType(typeof(System.Collections.Generic.List<>)).MakeGenericType(itemType);
#endif
            Type expected = ExpectedType;
            using (Compiler.Local oldArr = AppendToCollection ? ctx.GetLocalWithValue(expected, valueFrom) : null)
            using (Compiler.Local newArr = new Compiler.Local(ctx, expected))
            using (Compiler.Local list = new Compiler.Local(ctx, listType))
            {
                ctx.EmitCtor(listType);
                ctx.StoreValue(list);
                ListDecorator.EmitReadList(ctx, list, Tail, listType.GetMethod("Add"), packedWireType, false);

                // leave this "using" here, as it can share the "FieldNumber" local with EmitReadList
                using(Compiler.Local oldLen = AppendToCollection ? new ProtoBuf.Compiler.Local(ctx, ctx.MapType(typeof(int))) : null) {
                    Type[] copyToArrayInt32Args = new Type[] { ctx.MapType(typeof(Array)), ctx.MapType(typeof(int)) };

                    if (AppendToCollection)
                    {
                        ctx.LoadLength(oldArr, true);
                        ctx.CopyValue();
                        ctx.StoreValue(oldLen);

                        ctx.LoadAddress(list, listType);
                        ctx.LoadValue(listType.GetProperty("Count"));
                        ctx.Add();
                        ctx.CreateArray(itemType, null); // length is on the stack
                        ctx.StoreValue(newArr);

                        ctx.LoadValue(oldLen);
                        Compiler.CodeLabel nothingToCopy = ctx.DefineLabel();
                        ctx.BranchIfFalse(nothingToCopy, true);
                        ctx.LoadValue(oldArr);
                        ctx.LoadValue(newArr);
                        ctx.LoadValue(0); // index in target

                        ctx.EmitCall(expected.GetMethod("CopyTo", copyToArrayInt32Args));
                        ctx.MarkLabel(nothingToCopy);

                        ctx.LoadValue(list);
                        ctx.LoadValue(newArr);
                        ctx.LoadValue(oldLen);
                        
                    }
                    else
                    {
                        ctx.LoadAddress(list, listType);
                        ctx.LoadValue(listType.GetProperty("Count"));
                        ctx.CreateArray(itemType, null);
                        ctx.StoreValue(newArr);

                        ctx.LoadAddress(list, listType);
                        ctx.LoadValue(newArr);
                        ctx.LoadValue(0);
                    }

                    copyToArrayInt32Args[0] = expected; // // prefer: CopyTo(T[], int)
                    MethodInfo copyTo = listType.GetMethod("CopyTo", copyToArrayInt32Args);
                    if (copyTo == null)
                    { // fallback: CopyTo(Array, int)
                        copyToArrayInt32Args[1] = ctx.MapType(typeof(Array));
                        copyTo = listType.GetMethod("CopyTo", copyToArrayInt32Args);
                    }
                    ctx.EmitCall(copyTo);
                }
                ctx.LoadValue(newArr);
            }


        }
#endif
	}
}
#endif