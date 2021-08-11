using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace syscom.Diagnostics
{
	public class GenericVersionInfo
	{
		public override String ToString() { return $"{FileName} - {FileMajorPart}.{FileMinorPart}.{FileBuildPart}.{FilePrivatePart}"; }

		public String ProductName { get; set; }
		public String FileName { get; set; }
		public String FilePath { get; set; }
		public String FileDescription { get; set; }

		public String Comments { get; set; }
		public String CompanyName { get; set; }

		public String Language { get; set; }
		public String LegalCopyright { get; set; }

		public String FileVersion { get; set; }
		public Int32 FileBuildPart { get; set; }
		public Int32 FileMajorPart { get; set; }
		public Int32 FileMinorPart { get; set; }
		public Int32 FilePrivatePart { get; set; }

		public String ProductVersion { get; set; }
		public Int32 ProductBuildPart { get; set; }
		public Int32 ProductMajorPart { get; set; }
		public Int32 ProductMinorPart { get; set; }
		public Int32 ProductPrivatePart { get; set; }

		public GenericVersionInfo() { }

		public GenericVersionInfo( System.Diagnostics.FileVersionInfo fvi )
		{
			Comments = fvi.Comments;
			CompanyName = fvi.CompanyName;
			FileBuildPart = fvi.FileBuildPart;
			FileDescription = fvi.FileDescription;
			FileMajorPart = fvi.FileMajorPart;
			FileMinorPart = fvi.FileMinorPart;

			FileName = Path.GetFileName( fvi.FileName );
			FilePath = fvi.FileName;

			FilePrivatePart = fvi.FilePrivatePart;
			FileVersion = fvi.FileVersion;
			Language = fvi.Language;
			LegalCopyright = fvi.LegalCopyright;
			//LegalTrademarks = fvi.LegalTrademarks;
			//OriginalFilename = fvi.OriginalFilename;
			//PrivateBuild = fvi.PrivateBuild;
			ProductBuildPart = fvi.ProductBuildPart;
			ProductMajorPart = fvi.ProductMajorPart;
			ProductMinorPart = fvi.ProductMinorPart;
			ProductName = fvi.ProductName;
			ProductPrivatePart = fvi.ProductPrivatePart;
			ProductVersion = fvi.ProductVersion;
			//SpecialBuild = fvi.SpecialBuild;
			//InternalName = fvi.InternalName;
			//IsDebug = fvi.IsDebug;
			//IsPatched = fvi.IsPatched;
			//IsPreRelease = fvi.IsPreRelease;
			//IsPrivateBuild = fvi.IsPrivateBuild;
			//IsSpecialBuild = fvi.IsSpecialBuild;
		}

		public GenericVersionInfo( Assembly assembly )
		{
			var assName = assembly.GetName();
			var version = assName.Version;

			FileMajorPart = ProductMajorPart = version.Major;
			FileMinorPart = ProductMinorPart = version.Minor;
			FileBuildPart = ProductBuildPart = version.Build;
			FilePrivatePart = ProductPrivatePart = version.Revision;
			ProductVersion = FileVersion = version.ToString();

			FileDescription = ProductName = assName.Name;
			FileName = assName.Name;
		}
	}
}

namespace System
{
	using syscom.Diagnostics;

	public static class DllVersionInfoExtensions
	{
		public static GenericVersionInfo GetDllVersionInfo( this Diagnostics.FileVersionInfo fvi )
		{
			var dllInfo = new GenericVersionInfo( fvi );
			return dllInfo;
		}

		public static GenericVersionInfo GetDllVersionInfo( this Assembly assembly )
		{
			var info = new GenericVersionInfo( assembly );
			return info;
		}
	}
}