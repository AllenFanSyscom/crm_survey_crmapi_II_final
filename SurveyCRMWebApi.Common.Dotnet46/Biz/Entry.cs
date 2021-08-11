using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shortener.Biz;
using Shortener.Models;
using syscom;
using syscom.data;

namespace Shortener.Processor
{
	public static class Entry
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLogger( "ProcEntry" );

		/// <summary>
		/// 新增任務, 固定式會立即回傳短網址, 名單型不會回傳
		/// </summary>
		public static String MakeBy( Guid ownerId, String name, Int32 domainId, String urlDst, List<Guid> listIds, Boolean isSecure, String dateS, String dateE,List<string> listNames )
		{
			//讓dates欄位成為空值
			if ( String.IsNullOrEmpty( dateS ) ) throw new NotSupportedException( "未有開始日期" );
			if ( String.IsNullOrEmpty( dateE ) ) throw new NotSupportedException( "未有結束日期" );

			log.Info( $"[Maker] start add name[{name}] domainId[{domainId}] urlDst[{urlDst}]" );


			//==========================================================================================
			// 依是否有ListId做為 固定 & 名單型分別
			//==========================================================================================
			using ( var uow = DB.CreateUnitOfWork( true ) )
			{
				var pOutOPID = new SqlParameter( "@opid", SqlDbType.UniqueIdentifier, 36 ) { Direction = ParameterDirection.Output };
				uow.ExecuteSPBy( "spGetAvailableOPID", new[] { pOutOPID } );

				//取得OPID
				var opid = (Guid) pOutOPID.Value;

				if ( listIds.Count <= 0 )
				{
					try
					{
						//==========================================================================================
						// 固定式
						//==========================================================================================
						var pNewCode = new SqlParameter( "@code", SqlDbType.VarChar, 6 ) { Direction = ParameterDirection.Output };
						uow.ExecuteSPBy( "spGetAvailableUrlCode", new[] { pNewCode } );

						var code = (String) pNewCode.Value;

						const String sql = "Insert Into dbo.Shortener ( OPID, Name, DomainId, UrlDst, UrlShort, IsSecure, DateStart, DateEnd, OwnerId ) Values ( @opid, @name, @domainId, @urlDst, @code, @isSecure, @dateStart, @dateEnd, @ownerId )";
						var parameters = new[]
						{
							new SqlParameter { ParameterName = "@opid", Value = opid },
							new SqlParameter { ParameterName = "@name", Value = name },
							new SqlParameter { ParameterName = "@domainId", Value = domainId },
							new SqlParameter { ParameterName = "@urlDst", Value = urlDst },
							new SqlParameter { ParameterName = "@code", Value = code },
							new SqlParameter { ParameterName = "@isSecure", Value = isSecure },
							new SqlParameter { ParameterName = "@dateStart", Value = dateS },
							new SqlParameter { ParameterName = "@dateEnd", Value = dateE },
							new SqlParameter { ParameterName = "@ownerId", Value = ownerId },
						};
						var addCount = uow.ExecuteSQLBy( sql, parameters );
						log.Debug( $"[固定] newCode[{code}] addCount[{addCount}] parameters: {parameters.Select( p => { return new { Name = p.ParameterName, Value = p.Value }; } ).ToList().ToJson()}" );

						//==========================================================================================
						// 固定型: 直接新增至Redis
						//==========================================================================================
						Res.AddBy( code, domainId, isSecure, urlDst, dateS, dateE );
						log.Info( $"[固定] Make Res success, domainId[{domainId}] code[{code}] dst[{urlDst}]" );

						
						//==========================================================================================
						// 更新狀態
						//==========================================================================================
						var pStatus_OPID = new SqlParameter { ParameterName = "@opid", Value = opid };
						var pStatus_Status = new SqlParameter { ParameterName = "@status", Value = 2 };
						var pStatus_Return = new SqlParameter { ParameterName = "@return", Direction = ParameterDirection.ReturnValue };
						uow.ExecuteSPBy( "spUpdateStatusBy", new[] { pStatus_OPID, pStatus_Status, pStatus_Return } );


						uow.Commit();

						return code;
					}
					catch ( Exception )
					{
						uow.RollBack();
						throw;
					}
				}
				else
				{
					//==========================================================================================
					// 名單型
					//==========================================================================================
					try
					{
						// sam 新增 CRM連線
						var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
						//IDbConnection newconnection = new SqlConnection(CRMConnect);

						//預先檢查

						foreach ( var listId in listIds )
						{
							var pListId = new SqlParameter { ParameterName = "@listId", Value = listId };
							var pUsed = new SqlParameter( "@used", SqlDbType.Bit, 1 ) { Direction = ParameterDirection.Output };
							using (var newconnection = new SqlConnection(CRMConnect))
							{
								using (var uow1 = DB.CreateUnitOfWork(newconnection))
								{
									uow1.ExecuteSPBy("spCheckListIsUsed", new[] { pListId, pUsed });
								}
							}

								var used = (Boolean) pUsed.Value;
							if ( used ) throw new Exception( $"名單Id[{listId}]已被使用" );
						}

						//寫主檔
						int listcount = 0;
						foreach ( var listId in listIds )
						{
							const String sql = "Insert Into dbo.Shortener ( OPID, Name, DomainId, UrlDst, ListId, IsSecure, DateStart, DateEnd, OwnerID,ListName ) Values ( @opid, @name, @domainId, @urlDst, @listId, @isSecure, @dateStart, @dateEnd, @ownerId,@listname )";
							var parameters = new[]
							{
								new SqlParameter { ParameterName = "@opid", Value = opid },
								new SqlParameter { ParameterName = "@name", Value = name },
								new SqlParameter { ParameterName = "@domainId", Value = domainId },
								new SqlParameter { ParameterName = "@urlDst", Value = urlDst },
								new SqlParameter { ParameterName = "@listId", Value = listId },
								new SqlParameter { ParameterName = "@isSecure", Value = isSecure },
								new SqlParameter { ParameterName = "@dateStart", Value = dateS },
								new SqlParameter { ParameterName = "@dateEnd", Value = dateE },
								new SqlParameter { ParameterName = "@ownerId", Value = ownerId },
								new SqlParameter { ParameterName = "@listname", Value = listNames[listcount] },
							};
							var addCount = uow.ExecuteSQLBy( sql, parameters );
							log.Debug( $"[名單] 名單[{listId}] addCount[{addCount}] parameters: {parameters.Select( p => { return new { Name = p.ParameterName, Value = p.Value }; } ).ToList().ToJson()}" );
							listcount++;
						}

						//==========================================================================================
						// 名單型: 排入新增行程
						//==========================================================================================
						//將這段拿到後端透過console處理不在IIS上跑效能不好
						//ListTypeCreateWorker.Add( opid,listIds);

						uow.Commit();
					}
					catch ( Exception )
					{
						uow.RollBack();
						throw;
					}

					return String.Empty;
				}
			}
		}

		public static void RemoveBy( Guid opid )
		{
			log.Info( $"[Remove] start remove suid[{opid}]" );

			//==========================================================================================
			// 依是否有ListId做為 固定 & 名單型分別
			//==========================================================================================
			using ( var uow = DB.CreateUnitOfWork( true ) )
			{
				try
				{
					var pOPID = new SqlParameter { ParameterName = "@opid", Value = opid };
					var shortener = uow.ExecuteGetMapModelBy<Models.Shortener>( "Select * From vwMainList Where OPID=@opid", new[] { pOPID } );
					if ( shortener == null ) throw new Exception( $"找不到符合的主檔資料 SUID[{opid}]" );

					if ( shortener.ListId == null ) //固定式
					{
						Res.DelBy( shortener.UrlShort );
					}
					else
					{
						Res.DelBy( shortener.SNStart, shortener.SNEnd );
					}

					//------------------------------------------------------------------------------------------------
					// 改狀態為完成
					//------------------------------------------------------------------------------------------------
					var pStatus_OPID = new SqlParameter { ParameterName = "@opid", Value = opid };
					var updateRows = uow.ExecuteSQLBy( "Update Shortener Set IsDel=1 Where OPID=@opid", new[] { pStatus_OPID } );

					log.Info( $"[Remove] updateRows[{updateRows}]" );
					if ( updateRows <= 0 ) throw new Exception( "更改主檔刪除狀態失敗" );

					uow.Commit();
					log.Info( $"[Remove] success" );
				}
				catch ( Exception ex )
				{
					log.Info( $"[Remove] failed", ex );
					uow.RollBack();
					throw;
				}
			}
		}

		/// <summary>
		/// (經由獨立Thread呼叫, 勿手動呼叫)
		///
		/// 使用OPID開始產生短網址相關資料
		/// - 使用SP做為相關狀態更新, 更新成功後再更新Redis
		/// </summary>
		internal static void TryMakeListTypeBy( Guid opid,List<Guid> listids )
		{
			var sw = Stopwatch.StartNew();

			log.Info( $"[Maker][List] Start Operation by OPID[ {opid} ]" );

			//先將資料更新為處理中
			Data.UpdateMainStatusBy( opid, 1 );
			
			// sam 新增 CRM連線
			var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
			//IDbConnection newconnection = new SqlConnection(CRMConnect);

			//將true拿掉測試不要db lock
			using ( var uow = DB.CreateUnitOfWork() )
			{
				try
				{
					var parameters = new[]
					{
						new SqlParameter { ParameterName = "@opid", Value = opid },
					};

					//-----------------------------------------------------------------------
					// Sam 新增從CRM抓取資料如果要拆解不同DB待處理
					// 增加取的listIDs
					//-----------------------------------------------------------------------
					var TotaltempListMembers = new List<Models.ListTypeMember>();
					foreach (var listid in listids)
					{

						log.Info($"[Maker][List] [ listid={listid} ] insert tempListMember  start");
						int listcount = 0;
						using (var newconnection = new SqlConnection(CRMConnect))
						{
							using (var uow1 = DB.CreateUnitOfWork(newconnection))
							{
								var tempListMembers = uow1.ExecuteGetMapModelsBy<Models.ListTypeMember>("Select LM.ListId,LM.EntityId as ContactId,CT.MobileNumber,'' as TxtShort From dbo.ListMember LM LEFT JOIN dbo.Contact CT ON LM.EntityId=CT.ContactId Where ListId=@listid", new SqlParameter { ParameterName = "@listid", Value = listid });
								TotaltempListMembers.AddRange(tempListMembers);
								//const String sql = "Insert Into dbo.tempListMember ( ListId,EntityId,MobileNumber ) Values ( @listid, @entityid,@mobilenumber)";

								//foreach (var tempListMember in tempListMembers)
								//{
								//	listcount++;
								//	var parameters_listmember = new[]
								//	{
								//		new SqlParameter { ParameterName = "@listid", Value = tempListMember.ListId },
								//		new SqlParameter { ParameterName = "@entityid", Value = tempListMember.EntityId },
								//		new SqlParameter { ParameterName = "@mobilenumber", Value = tempListMember.MobileNumber },
								//	};
								//	var addCount = uow.ExecuteSQLBy(sql, parameters_listmember);

								//}

							}
						}
						log.Info($"[Maker][List]  [ listid={listid} ] memory tempListMember end 共 [ {TotaltempListMembers.Count()} ]筆");
					}
					//------------------------------------------------------------------------------------------------


					//-----------------------------------------------------------------------
					// Sam 將這部分SP ProcessShortUrl 改寫成程式處理
					// 增加處理速度
					//------------------------------------------------------------------------------------------------

					//init 基本參數
					log.Info($"[Maker][List] opid:[{opid}]");
					var shorteners = Data.GetinitShorteners(1).Where(x=>x.OPID==opid);
					if (shorteners.Count() <= 0) throw new IgnorableException($"查無此主檔OPID[{opid}]資料");

					log.Info($"[Maker][List]  shorteners:[{shorteners.ToJson()} ] opid:[{opid}]");

					//取得主檔資訊=>domainurl
					var f = shorteners.First();
					var pDomainId = new SqlParameter { ParameterName = "@domainid", Value = f.DomainId };
					var pDomainURL = new SqlParameter("@domainurl", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
					uow.ExecuteSPBy("spGetDomainURL", new[] { pDomainId, pDomainURL });
					var strDomainUrl = (string)pDomainURL.Value;
					log.Info($"[Maker][List] strDomainUrl:[{strDomainUrl}]");
					//取得SN
					var sn = new SqlParameter("@sn", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
					uow.ExecuteSPBy("spGetAvailableSN", new[] { sn });
					var SNstart = (long)sn.Value;
					var SNend = SNstart;
					log.Info($"[Maker][List] SNstart:[{SNstart}]");
					//用by listid取得listmember產shorturl
					foreach (var shortener in shorteners)
                    {
						
						const String sql = "Insert Into dbo.ShortenerAnalyze ( EntityId,Shortener ) Values ( @entityid,@shortener)";
						var listcount = 0;
						log.Info($"[Maker][List] loop shortener:[{shortener.ToJson()}]");
						foreach (var member in TotaltempListMembers.Where(x=>x.ListId==shortener.ListId))
						{
							var nowCode = FnToStrSN(SNend);
							var nowlisidstr = member.ListId.ToString();
							var nowcontactdata = nowlisidstr.ToUpper() + "|" + nowCode;
							///更新url
							member.TxtShort = nowCode;
							
							var parameters_listmember = new[]
								{
									new SqlParameter { ParameterName = "@entityid", Value = member.ContactId },
									new SqlParameter { ParameterName = "@shortener", Value = nowcontactdata},
								};
							var addCount = uow.ExecuteSQLBy(sql, parameters_listmember);
							SNend++;
							listcount++;
						}
						log.Info($"[Maker][List] Insert ShortenerAnalyze :[{listcount}]筆");
						const String update_sql = "update dbo.Shortener Set ListCount=@listcount,SNStart=@snstart,SNEnd=@snend where SUID=@suid ";
						var parameters_update = new[]
						{
							new SqlParameter { ParameterName = "@listcount", Value = listcount },
							new SqlParameter { ParameterName = "@snstart", Value = SNstart},
							new SqlParameter { ParameterName = "@snend", Value = SNend},
							new SqlParameter { ParameterName = "@suid", Value = shortener.SUID},
						};
						//先更新欄位
						shortener.SNEnd = SNend;
						shortener.SNStart = SNstart;
						var upCount = uow.ExecuteSQLBy(update_sql, parameters_update);
						SNend++;
						SNstart = SNend;
						
						log.Info($"[Maker][List] update dbo.Shortener suid:[{shortener.SUID}]");
					}
					if (TotaltempListMembers.Count <= 0) throw new IgnorableException("處理名單結果沒有任何成員");

					

					//uow.Commit();
					//------------------------------------------------------------------------------------------------

					////------------------------------------------------------------------------------------------------
					////sam mark 不走DB SP =>因為跑太久
					//var members = uow.ExecuteCmdBy( "ProcessShortUrl", parameters, cmd =>
					//{
					//	cmd.CommandType = CommandType.StoredProcedure;
					//	cmd.CommandText = "spProcessShortUrlListTypeBy";
					//	cmd.CommandTimeout = 0;

					//	using ( var reader = cmd.ExecuteReader() )
					//	{
					//		return reader.AutoReadGetMappedModels<Shortener.Models.ListTypeMember>();
					//	}
					//} );

					//if ( members.Count <= 0 ) throw new IgnorableException( "處理名單結果沒有任何成員" );

					//------------------------------------------------------------------------------------------------
					//------------------------------------------------------------------------------------------------
					sw.Stop();
					log.Info( $"[Maker][List] sp ms[{sw.ElapsedMilliseconds}] members( {TotaltempListMembers.Count} )" );
					sw.Restart();
					sw.Stop();

					//-----------------------------------------------------------------------
					// Sam 新增如果要拆解不同DB待處理
					// 更新update CHT_IMPORT.Contact
					//-----------------------------------------------------------------------
					
					int membercount = 0;
					log.Info($"[Maker][Member] update contact start");

					using (var newconnection = new SqlConnection(CRMConnect))
					{
						using (var uow1 = DB.CreateUnitOfWork(newconnection))
						{
							foreach (var member in TotaltempListMembers)
							{
								membercount++;

								var parameters_contact = new[]
								{
									new SqlParameter { ParameterName = "@contactid", Value = member.ContactId },
									new SqlParameter { ParameterName = "@shortener", Value = strDomainUrl+member.TxtShort },
								};
								var updateRows = uow1.ExecuteSQLBy("Update dbo.Contact Set Shortener=@shortener Where ContactId=@contactid", parameters_contact);
								//log.Info($"[Maker][Member] update contact Shortener:[{strDomainUrl + member.TxtShort}] ContactId:[{member.ContactId}]");
								if (membercount % 10000 == 0) log.Info($"[Maker][CHT_IMPORT] OPID[{opid}]  updating contact 共 [ {membercount} ]筆");
							}

						}
					}

					log.Info($"[Maker][Member] update contact end 共 [ {membercount} ]筆");
					//------------------------------------------------------------------------------------------------


					//------------------------------------------------------------------------------------------------
					// 寫入Redis & BakJson
					//------------------------------------------------------------------------------------------------
					var snS = shorteners.Min( s => s.SNStart );
					var snE = shorteners.Max( s => s.SNEnd );
					var m = shorteners.First();

					sw.Restart();
					foreach (var shortener in shorteners)
					{
						Res.AddBy(TotaltempListMembers.Where(x=>x.ListId== shortener.ListId).ToList(), shortener.SNStart, shortener.SNEnd, m.DomainId, m.IsSecure, m.UrlDst, m.DateStart.ToString("yyyy-MM-dd"), m.DateEnd.ToString("yyyy-MM-dd"));
						log.Info($"[Maker][List] Make Res ms[{sw.ElapsedMilliseconds}]");
					}
					sw.Stop();


					//更新狀態完已完成並且補上測試網址
					var testurl = FnToStrSN(SNend - 1);
					const String end_sql = "update dbo.Shortener Set Status=2,UrlTest=@urltest Where OPID=@opid ";
					var parameters_end = new[]
					{
							new SqlParameter { ParameterName = "@urltest", Value = testurl},
							new SqlParameter { ParameterName = "@opid", Value = opid},
						};
					var endCount = uow.ExecuteSQLBy(end_sql, parameters_end);


					log.Info( $"[Maker][List] Success" );

					//uow.Commit();
				}
				catch ( Exception ex )
				{
					log.Error( $"[Maker][List] Failed OPID[ {opid} ], {ex.Message}", ex );
					//uow.RollBack();

					//將資料更新為失敗
					Data.UpdateMainStatusBy( opid, 3 );
				}
			}

			
		}

		/// <summary>
		/// sam 取的該短網址的redis點擊數
		///
		/// get Redis su-p-xxxxxx-list count
		/// get Redis su-l-19-99-list count
		/// </summary>
		public static long GetHits(string key)
		{
			log.Info($"[GetHits]  Start key[{key}]");
			long hits = 0;
				try
				{
					hits=Res.GetTotalHits(key);
		
				}
				catch (Exception ex)
				{
					log.Info($"[GetHits] failed", ex);
					throw;
				}
			log.Info($"[GetHits] End key[{key}] hits[{hits}]");
			return hits;
			
		}


		/// <summary>
		/// sam SN 轉換短網址 從MS SQL Function FnToStrSN copy 過來
		///
		/// </summary>
		public static string FnToStrSN  (long sn)
		{
			string ret = "";
			long left = sn;
			int nowV;
			char nowC;
			try
			{
                while (left > 0)
                {
					nowV = (int)left % 36;
					if (nowV >= 10)
						nowC = (char)(nowV + 55);
					else
						nowC = (char)(nowV + 48);

					left = left / 36;
					ret = nowC + ret;

				}

				return ret.PadLeft(7,'0');
			}
			catch (Exception ex)
			{
				log.Info($"[FnToStrSN] failed", ex);
				throw;
			}
			
			

		}


	}
}
