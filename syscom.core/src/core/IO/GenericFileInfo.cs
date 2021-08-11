using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom.models;

namespace syscom.io
{
	public class GenericFileInfo : IFileInfo
	{
		public String DirectoryName { get; set; }
		public String Name { get; set; }
		public String FullName { get; set; }
		public String Extension { get; set; }
		public DateTime CreationTime { get; set; }
		public DateTime LastWriteTime { get; set; }
		public Int64 Length { get; set; }
		public Boolean Exists { get; set; }
		public Byte[] Content { get; set; }
	}


	public static class IFileInfoExtensions
	{
		/// <summary>轉換成GenericFileInfo, 第二個參數表示是否讀取全部的檔案進該結構</summary>
		public static GenericFileInfo ToGenericFileInfo( this System.IO.FileInfo fi, Boolean readAllContent = false )
		{
			var gfi = new GenericFileInfo
			{
				DirectoryName = fi.DirectoryName,
				Name = fi.Name,
				FullName = fi.FullName,
				Extension = fi.Extension,
				CreationTime = fi.CreationTime,
				LastWriteTime = fi.LastWriteTime,
				Length = fi.Length,
				Exists = fi.Exists
			};

			if ( readAllContent )
				using ( var sr = fi.OpenRead() )
				{
					gfi.Content = new Byte[fi.Length];
					sr.Read( gfi.Content, 0, (Int32) gfi.Length );
				}

			return gfi;
		}
	}
}