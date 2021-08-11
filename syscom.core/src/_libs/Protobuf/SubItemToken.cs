using System;

namespace libs.ProtoBuf
{
	/// <summary>
	/// Used to hold particulars relating to nested objects. This is opaque to the caller - simply
	/// give back the token you are given at the end of an object.
	/// </summary>
	public struct SubItemToken
	{
		internal readonly Int32 value;
		internal SubItemToken( Int32 value ) { this.value = value; }
	}
}