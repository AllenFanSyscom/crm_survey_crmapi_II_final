using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Shortener.Biz;
using Shortener.Models;
using syscom;
using syscom.data;

namespace Shortener.Processor
{
	public static class Entry_List
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLogger("ProcEntry");

		/// <summary>
		/// 新增任務, 固定式會立即回傳短網址, 名單型不會回傳
		/// </summary>
		public static void MakeBy(String txtName, String txtSource, String txtPurpose, String txtDescription, IList<Models.Entitys> entitys, Int32 type, String userid,List<Guid> listids)
		{
			//1. 新增CHT_MSCRM.ListBase,CHT_MSCRM.ListExtensionBase
			var newListMemberId = CreateList(txtName, txtSource, txtPurpose, txtDescription, 16, userid);
			if (newListMemberId == Guid.Empty)
				return;
			//2. InsertImportStatus 跳過
			//if (newListMemberId != Guid.Empty)
			//	InsertImportStatus(newListMemberId, entitys, "");
			//2. 排入新增行程
			ListsCreateWorker.Add(newListMemberId, entitys, type, userid, listids);
		}
		/// <summary>
		/// (經由獨立Thread呼叫, 勿手動呼叫)
		///	匯入名單
		/// </summary>
		internal static void TryMakeListsBy(Guid newListMemberId, IList<Models.Entitys> entitys, Int32 type, String userid,List<Guid> listids)
		{
			// sam 新增 CRM連線
			var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
			using (var newconnection = new SqlConnection(CRMConnect))
			{
				newconnection.Open();
				using (SqlCommand cmd = new SqlCommand())
				{
					//using (SqlTransaction tran = newconnection.BeginTransaction())
					//{
					try
					{
						cmd.Connection = newconnection;
						//cmd.Transaction = tran;

						log.Info($" Process:匯入中 ");
						if (newListMemberId != Guid.Empty)
							UpdateListStatusCode(cmd, newListMemberId, "2");
						log.Info($" Process:匯入資料 ");
						if (newListMemberId != Guid.Empty)
							ImportList(cmd, newListMemberId, entitys, type, userid, listids);
						log.Info("  Process:已匯入");
						if (newListMemberId != Guid.Empty)
							UpdateListStatusCode(cmd, newListMemberId, "3");

						//tran.Commit();
					}
					catch (Exception ex)
					{
						log.Info("  Process:匯入錯誤");
						if (newListMemberId != Guid.Empty)
							UpdateListStatusCode(cmd, newListMemberId, "5");
						log.Error("Error: " + ex.Message);
						//tran.Rollback();
					}
					//}
				}
				newconnection.Close();
			}
		}

		#region 新增 CHT_MSCRM.ListBase & ListExtensionBase
		public static Guid CreateList(string sListName, string sSource, string sPurpose, string sDescription, int sListType, String userid)
		{
			Guid NewListId = SequentialGuid();

			string NewListName = sListName;

			// sam 新增 CRM連線
			var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
			using (var newconnection = new SqlConnection(CRMConnect))
			{
				try
				{

					newconnection.Open();
					using (SqlCommand cmd = newconnection.CreateCommand())
					{
						#region Create a new list in ListBase
						string SQL = "";
						//if (string.IsNullOrEmpty(userid))
						//{
						SQL = @"INSERT INTO CHT_MSCRM.dbo.ListBase (CreatedOn,ModifiedOn,MemberCount,ListName,ListId,StateCode,StatusCode,DeletionStateCode,DoNotSendOnOptOut,Source,
											 Purpose,Description,IgnoreInactiveListMembers,MemberType,CreatedFromCode,LockStatus,CreatedBy,OwningUser,OwningBusinessUnit)
			                VALUES(GETUTCDATE(),GETUTCDATE(),0,@ListName,@NewListId,0,0,0,1,@Source,@Purpose,@Description,1,2,2,@LockStatus,@Owner,@Owner,@BusinessUnit)";
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						cmd.Parameters.Add(new SqlParameter("@ListName", NewListName));
						cmd.Parameters.Add(new SqlParameter("@NewListId", NewListId));
						cmd.Parameters.Add(new SqlParameter("@Source", sSource));
						cmd.Parameters.Add(new SqlParameter("@Purpose", sPurpose));
						cmd.Parameters.Add(new SqlParameter("@LockStatus", true));
						cmd.Parameters.Add(new SqlParameter("@Description", sDescription));
						cmd.Parameters.Add(new SqlParameter("@Owner", userid));
						cmd.Parameters.Add(new SqlParameter("@BusinessUnit", "9C717464-7D43-DE11-879A-00215E40E496"));

						//}
						//else
						//{
						//	SQL = @"INSERT INTO CHT_MSCRM.dbo.ListBase (CreatedOn,ModifiedOn,MemberCount,ListName,ListId,StateCode,StatusCode,DeletionStateCode,DoNotSendOnOptOut,Source,
						//					 Purpose,Description,IgnoreInactiveListMembers,MemberType,CreatedFromCode,LockStatus,OwningUser)
						//            VALUES(GETUTCDATE(),GETUTCDATE(),0,@ListName,@NewListId,0,0,0,1,@Source,@Purpose,@Description,1,2,2,@LockStatus,@OwningUser)";
						//	cmd.CommandType = CommandType.Text;
						//	cmd.CommandText = SQL;
						//	cmd.Parameters.Add(new SqlParameter("@ListName", NewListName));
						//	cmd.Parameters.Add(new SqlParameter("@NewListId", NewListId));
						//	cmd.Parameters.Add(new SqlParameter("@Source", sSource));
						//	cmd.Parameters.Add(new SqlParameter("@Purpose", sPurpose));
						//	cmd.Parameters.Add(new SqlParameter("@LockStatus", true));
						//	cmd.Parameters.Add(new SqlParameter("@Description", sDescription));
						//	cmd.Parameters.Add(new SqlParameter("@OwningUser", userid));
						//}



						log.Debug(SQL);
						log.Debug("ListName:"+ NewListName);
						log.Debug("NewListId:" + NewListId);
						log.Debug("Source:" + sSource);
						log.Debug("sPurpose:" + sPurpose);
						log.Debug("sDescription:" + sDescription);
						log.Debug("Owner:" + userid);


						int iR = cmd.ExecuteNonQuery();
						#endregion
						#region Create a new list in ListExtensionBase
						cmd.Parameters.Clear();
						SQL = string.Format(@"INSERT INTO CHT_MSCRM.dbo.ListExtensionBase (ListId,New_ListType,New_IsImport,New_StatusCode,New_Interface_Editable,New_ExpirationDate)
			                                          VALUES(@NewListId,@ListType,@IsImport,@StatusCode,@Editable,@ExpirationDate)");
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						cmd.Parameters.Add(new SqlParameter("@NewListId", NewListId));
						cmd.Parameters.Add(new SqlParameter("@ExpirationDate", (DateTime.Now.AddMonths(1).ToUniversalTime())));
						cmd.Parameters.Add(new SqlParameter("@StatusCode", 1));
						cmd.Parameters.Add(new SqlParameter("@Editable", false));
						cmd.Parameters.Add(new SqlParameter("@ListType", sListType));
						cmd.Parameters.Add(new SqlParameter("@IsImport", false));
						log.Debug(SQL);
						iR = cmd.ExecuteNonQuery();
						newconnection.Close();
						#endregion
						return NewListId;
					}

				}
				catch (Exception ex)
				{
					log.Error($"[Maker][List] Failed, {ex.Message}", ex);
					return Guid.Empty;
				}
			}
		}
		[DllImport("rpcrt4.dll", SetLastError = true)]
		static extern int UuidCreateSequential(out Guid guid);
		private static Guid SequentialGuid()
		{
			const int RPC_S_OK = 0;
			Guid g;
			if (UuidCreateSequential(out g) != RPC_S_OK)
				return Guid.NewGuid();
			else
				return g;
		}
		#endregion

		#region 於dbo.ImportStatus新增再利用的名單資訊
		public static void InsertImportStatus(Guid NewListId, IList<Models.Entitys> entitys, string UserId)
		{
			string SqlStmt = string.Format(@"INSERT INTO dbo.ImportStatus (StatusId, ListId, UserId, Action, StatusCode, Total, Processed, ErrorCount, StartTime,
                                                             Message, NewListId, Arguments)
                               VALUES (@StatusId, @ListId, @UserId, @Action, @StatusCode, @Total, @Processed, @ErrorCount, @StartTime,
                                      @Message, @NewListId, @Arguments)");
			// sam 新增 CRM連線
			var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
			using (var newconnection = new SqlConnection(CRMConnect))
			{
				try
				{
					using (SqlCommand cmd = newconnection.CreateCommand())
					{
						var lList = (from v in entitys select v.ListId).Distinct().ToList();
						foreach (var tmp in lList)
						{
							List<SqlParameter> param = new List<SqlParameter>(){ new SqlParameter("StatusId", Guid.NewGuid()), new SqlParameter("UserId", UserId)
										, new SqlParameter("Action", "Reuse"), new SqlParameter("StatusCode", 1), new SqlParameter("Processed", Convert.ToInt32(0))
										, new SqlParameter("ErrorCount", Convert.ToInt32(0)), new SqlParameter("StartTime", DateTime.Now)
										, new SqlParameter("Message", string.Empty), new SqlParameter("NewListId", NewListId), new SqlParameter("Arguments", "1 = 1")
									};

							param.Add(new SqlParameter("ListId", tmp));
							param.Add(new SqlParameter("Total", 0));//TODO
							cmd.CommandType = CommandType.Text;
							cmd.CommandText = SqlStmt;
							cmd.ExecuteNonQuery();
						}
					}
				}
				catch (Exception ex)
				{
					log.Error($"[Maker][List] Failed, {ex.Message}", ex);
				}
			}
		}
		#endregion

		// Update List StatusCode
		public static void UpdateListStatusCode(SqlCommand cmd, Guid ListId, string sStatusCode)
		{
			cmd.Parameters.Clear();
			cmd.CommandText = "UPDATE CHT_MSCRM.dbo.ListExtensionBase SET New_StatusCode=@StatusCode WHERE ListId=@ListId";
			cmd.Parameters.AddWithValue("@StatusCode", sStatusCode);
			cmd.Parameters.AddWithValue("@ListId", ListId);
			log.Debug(cmd.CommandText);
			cmd.ExecuteNonQuery();
		}
		// 匯入資料
		public static void ImportList(SqlCommand cmd, Guid newListId, IList<Models.Entitys> entitys, Int32 type, String userid,List<Guid> lList)
		{
			List<Guid> EntityOper = new List<Guid>();
			

			if (type == 2)//未點擊名單
			{
				
				foreach (var tmp in lList)
				{

					//未點擊名單對應的Entity,從ListMember所有的EntityId排除帶過來的List<EntityId>
					string SQL = "select EntityId from ListMember where ListId=@ListId";
					cmd.CommandType = CommandType.Text;
					cmd.CommandText = SQL;
					//重複宣告
					cmd.Parameters.Clear();
					cmd.Parameters.Add(new SqlParameter("@ListId", tmp));
					SqlDataReader sdr = cmd.ExecuteReader();
					List<Guid> EntityAll = new List<Guid>();
					while (sdr.Read())
					{
						Guid entityTmp = (Guid)sdr[0];
						var lEntity = (from v in entitys where v.ListId == tmp && v.EntityId == entityTmp select v.EntityId).Distinct().ToList();
						if (lEntity == null || lEntity.Count == 0)
							EntityAll.Add(entityTmp);
					}
					sdr.Close();

					foreach (var subTmp in EntityAll)
					{
						//去掉重複的entityId
						if (EntityOper.IndexOf(subTmp) >= 0)
							continue;
						else
							EntityOper.Add(subTmp);

						//Contact
						cmd.Parameters.Clear();
						Guid NewContactId = SequentialGuid();
						SQL = @"insert into Contact(ContactId,OwningBusinessUnit,OwningUser,OwningTeam,CreatedBy,CreatedOn,ModifiedBy,ModifiedOn,StatusCode,Name,
											SSN,SN,HN,MD,Telephone,CircuitNumber,MobileNumber,EMailAddress1,EMailAddress2,Address1,Address2,FTTXCode,
											Spare_1,Spare_2,Spare_3,Spare_4,Spare_5,Spare_6,Spare_7,Spare_8,Spare_9,Spare_10,Spare_11,Spare_12,Spare_13,Spare_14,Spare_15,
											Spare_16,Spare_17,Spare_18,Spare_19,Spare_20,Spare_21,Spare_22,Spare_23,Spare_24,Spare_25,Spare_26,Spare_27,Spare_28,Spare_29,Spare_30,
											Date_1,Date_2,Date_3,Date_4,Date_5,Date_6,Date_7,Date_8,Date_9,Date_10,
											Promo_Code1,Promo_StartDate1,Promo_EndDate1,Promo_Code2,Promo_StartDate2,Promo_EndDate2,Promo_Code3,Promo_StartDate3,Promo_EndDate3,
											Promo_Code4,Promo_StartDate4,Promo_EndDate4,Promo_Code5,Promo_StartDate5,Promo_EndDate5,Promo_Code6,Promo_StartDate6,Promo_EndDate6,
											Promo_Code7,Promo_StartDate7,Promo_EndDate7,Promo_Code8,Promo_StartDate8,Promo_EndDate8,Promo_Code9,Promo_StartDate9,Promo_EndDate9,
											Promo_Code10,Promo_StartDate10,Promo_EndDate10,
											Distinct_1,Distinct_2,Distinct_3,Distinct_4,Distinct_5,Distinct_6,Distinct_7,Distinct_8,Distinct_9,Distinct_10,Distinct_11,Distinct_12,
											Distinct_13,Distinct_14,Distinct_15,Distinct_16,Distinct_17,Distinct_18,Distinct_19,Distinct_20,
											ContractID,ContactPerson,FacebookUID,SerialNumber,DeviceNumber,Shortener)  
											select @NewContactId,OwningBusinessUnit,OwningUser,OwningTeam,CreatedBy,CreatedOn,ModifiedBy,ModifiedOn,StatusCode,Name,SSN,SN,HN,MD,
											Telephone,CircuitNumber,MobileNumber,EMailAddress1,EMailAddress2,Address1,Address2,FTTXCode,
											Spare_1,Spare_2,Spare_3,Spare_4,Spare_5,Spare_6,Spare_7,Spare_8,Spare_9,Spare_10,Spare_11,Spare_12,Spare_13,Spare_14,Spare_15,
											Spare_16,Spare_17,Spare_18,Spare_19,Spare_20,Spare_21,Spare_22,Spare_23,Spare_24,Spare_25,Spare_26,Spare_27,Spare_28,Spare_29,Spare_30,
											Date_1,Date_2,Date_3,Date_4,Date_5,Date_6,Date_7,Date_8,Date_9,Date_10,
											Promo_Code1,Promo_StartDate1,Promo_EndDate1,Promo_Code2,Promo_StartDate2,Promo_EndDate2,Promo_Code3,Promo_StartDate3,Promo_EndDate3,
											Promo_Code4,Promo_StartDate4,Promo_EndDate4,Promo_Code5,Promo_StartDate5,Promo_EndDate5,Promo_Code6,Promo_StartDate6,Promo_EndDate6,
											Promo_Code7,Promo_StartDate7,Promo_EndDate7,Promo_Code8,Promo_StartDate8,Promo_EndDate8,Promo_Code9,Promo_StartDate9,Promo_EndDate9,
											Promo_Code10,Promo_StartDate10,Promo_EndDate10,
											Distinct_1,Distinct_2,Distinct_3,Distinct_4,Distinct_5,Distinct_6,Distinct_7,Distinct_8,Distinct_9,Distinct_10,Distinct_11,Distinct_12,
											Distinct_13,Distinct_14,Distinct_15,Distinct_16,Distinct_17,Distinct_18,Distinct_19,Distinct_20,
											ContractID,ContactPerson,FacebookUID,SerialNumber,DeviceNumber,'' Shortener  
											from Contact where ContactId=@EntityId";
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						cmd.Parameters.Add(new SqlParameter("@NewContactId", NewContactId));
						cmd.Parameters.Add(new SqlParameter("@EntityId", subTmp));
						log.Debug(SQL);
						log.Debug("NewContactId:" + NewContactId);
						log.Debug("EntityId:" + subTmp);
						int iR = cmd.ExecuteNonQuery();
						//ListMember
						cmd.Parameters.Clear();
						Guid NewListMemberId = SequentialGuid();
						//if (string.IsNullOrEmpty(userid))
						//{
						SQL = @"insert into ListMember(EntityType,CreatedOn,EntityId,ListId,ListMemberId,ModifiedOn) 
								   select 2,getdate(),@New_EntityId,@newListId,@NewListMemberId,getdate() from ListMember 
								   where EntityId=@EntityId and ListId=@ListId";
						cmd.Parameters.Add(new SqlParameter("@newListId", newListId));
						cmd.Parameters.Add(new SqlParameter("@NewListMemberId", NewListMemberId));
						cmd.Parameters.Add(new SqlParameter("@ListId", tmp));
						cmd.Parameters.Add(new SqlParameter("@New_EntityId", NewContactId));
						cmd.Parameters.Add(new SqlParameter("@EntityId", subTmp));
						//}
						//else
						//{
						//	SQL = @"insert into ListMember(EntityType,CreatedOn,CreatedBy,EntityId,ModifiedBy,ListId,ListMemberId,ModifiedOn) 
						//		   select 2,getdate(),@Userid,EntityId,@Userid,@newListId,@NewListMemberId,getdate() from ListMember 
						//		   where EntityId=@EntityId and ListId=@ListId";
						//	cmd.Parameters.Add(new SqlParameter("@newListId", newListId));
						//	cmd.Parameters.Add(new SqlParameter("@NewListMemberId", NewListMemberId));
						//	cmd.Parameters.Add(new SqlParameter("@ListId", tmp));
						//	cmd.Parameters.Add(new SqlParameter("@EntityId", NewContactId));
						//	cmd.Parameters.Add(new SqlParameter("@Userid", userid));
						//}
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						log.Debug(SQL);
						log.Debug("newListId:" + newListId);
						log.Debug("NewListMemberId:" + NewListMemberId);
						log.Debug("oListId:" + tmp);
						log.Debug("EntityId:" + subTmp);
						iR = cmd.ExecuteNonQuery();

						//Update ListBase.MemberCount
						cmd.Parameters.Clear();
						SQL = @"update CHT_MSCRM.dbo.ListBase set MemberCount=@count where ListId=@ListId";
						cmd.Parameters.Add(new SqlParameter("@count", EntityOper.Count));
						cmd.Parameters.Add(new SqlParameter("@ListId", newListId));
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						log.Debug(SQL);
						log.Debug("ListId:" + newListId);
						iR = cmd.ExecuteNonQuery();
					}

					// sam 新增 CRM連線
					var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
					//IDbConnection newconnection = new SqlConnection(CRMConnect);
					var obj = new Models.ListBase();
					using (var newconnection = new SqlConnection(CRMConnect))
					{
						using (var uow1 = DB.CreateUnitOfWork(newconnection))
						{
							var plistid = new SqlParameter { ParameterName = "@oldListId", Value = tmp };
							obj = uow1.ExecuteGetMapModelBy<Models.ListBase>("Select top 1 new_IsImport, new_Criteria, new_importCriteria, new_MaskCriteria  from CHT_MSCRM.dbo.ListExtensionBase  where ListId = @oldListId ", new[] { plistid });
						}
					}


					//Update ListExtensionBase
					cmd.Parameters.Clear();
					cmd.CommandText = @"update CHT_MSCRM.dbo.ListExtensionBase 
                        set new_IsImport=@new_IsImport, 
                            new_Criteria=@new_Criteria, 
                            new_importCriteria=@new_importCriteria, 
                            new_MaskCriteria=@new_MaskCriteria
                         where ListId=@newListId ";

					cmd.Parameters.Add(new SqlParameter("@newListId", newListId));
					cmd.Parameters.Add(new SqlParameter("@new_IsImport", obj.new_IsImport));
					cmd.Parameters.Add(new SqlParameter("@new_Criteria", obj.new_Criteria));
					cmd.Parameters.Add(new SqlParameter("@new_importCriteria", obj.new_importCriteria));
					cmd.Parameters.Add(new SqlParameter("@new_MaskCriteria", obj.new_MaskCriteria));
					//cmd.CommandType = CommandType.Text;
					//cmd.CommandText = SQL;
					log.Debug("Update ListExtensionBase" + cmd.CommandText);
					log.Debug(newListId + "//" + tmp);
					cmd.ExecuteNonQuery();


				}

			}
			else
			{
				foreach (var tmp in lList)
				{
					log.Debug("oListId:" + tmp);

					var lEntity = (from v in entitys where v.ListId == tmp select v.EntityId).Distinct().ToList();
					foreach (var subTmp in lEntity)
					{
						//去掉重複的entityId
						if (EntityOper.IndexOf((Guid)subTmp) >= 0)
							continue;
						else
							EntityOper.Add((Guid)subTmp);

						//Contact
						cmd.Parameters.Clear();
						Guid NewContactId = SequentialGuid();
						Guid NewListMemberId = SequentialGuid();
						string SQL = @"insert into Contact(ContactId,OwningBusinessUnit,OwningUser,OwningTeam,CreatedBy,CreatedOn,ModifiedBy,ModifiedOn,StatusCode,Name,
											SSN,SN,HN,MD,Telephone,CircuitNumber,MobileNumber,EMailAddress1,EMailAddress2,Address1,Address2,FTTXCode,
											Spare_1,Spare_2,Spare_3,Spare_4,Spare_5,Spare_6,Spare_7,Spare_8,Spare_9,Spare_10,Spare_11,Spare_12,Spare_13,Spare_14,Spare_15,
											Spare_16,Spare_17,Spare_18,Spare_19,Spare_20,Spare_21,Spare_22,Spare_23,Spare_24,Spare_25,Spare_26,Spare_27,Spare_28,Spare_29,Spare_30,
											Date_1,Date_2,Date_3,Date_4,Date_5,Date_6,Date_7,Date_8,Date_9,Date_10,
											Promo_Code1,Promo_StartDate1,Promo_EndDate1,Promo_Code2,Promo_StartDate2,Promo_EndDate2,Promo_Code3,Promo_StartDate3,Promo_EndDate3,
											Promo_Code4,Promo_StartDate4,Promo_EndDate4,Promo_Code5,Promo_StartDate5,Promo_EndDate5,Promo_Code6,Promo_StartDate6,Promo_EndDate6,
											Promo_Code7,Promo_StartDate7,Promo_EndDate7,Promo_Code8,Promo_StartDate8,Promo_EndDate8,Promo_Code9,Promo_StartDate9,Promo_EndDate9,
											Promo_Code10,Promo_StartDate10,Promo_EndDate10,
											Distinct_1,Distinct_2,Distinct_3,Distinct_4,Distinct_5,Distinct_6,Distinct_7,Distinct_8,Distinct_9,Distinct_10,Distinct_11,Distinct_12,
											Distinct_13,Distinct_14,Distinct_15,Distinct_16,Distinct_17,Distinct_18,Distinct_19,Distinct_20,
											ContractID,ContactPerson,FacebookUID,SerialNumber,DeviceNumber,Shortener)  
											select @NewContactId,OwningBusinessUnit,OwningUser,OwningTeam,CreatedBy,CreatedOn,ModifiedBy,ModifiedOn,StatusCode,Name,SSN,SN,HN,MD,
											Telephone,CircuitNumber,MobileNumber,EMailAddress1,EMailAddress2,Address1,Address2,FTTXCode,
											Spare_1,Spare_2,Spare_3,Spare_4,Spare_5,Spare_6,Spare_7,Spare_8,Spare_9,Spare_10,Spare_11,Spare_12,Spare_13,Spare_14,Spare_15,
											Spare_16,Spare_17,Spare_18,Spare_19,Spare_20,Spare_21,Spare_22,Spare_23,Spare_24,Spare_25,Spare_26,Spare_27,Spare_28,Spare_29,Spare_30,
											Date_1,Date_2,Date_3,Date_4,Date_5,Date_6,Date_7,Date_8,Date_9,Date_10,
											Promo_Code1,Promo_StartDate1,Promo_EndDate1,Promo_Code2,Promo_StartDate2,Promo_EndDate2,Promo_Code3,Promo_StartDate3,Promo_EndDate3,
											Promo_Code4,Promo_StartDate4,Promo_EndDate4,Promo_Code5,Promo_StartDate5,Promo_EndDate5,Promo_Code6,Promo_StartDate6,Promo_EndDate6,
											Promo_Code7,Promo_StartDate7,Promo_EndDate7,Promo_Code8,Promo_StartDate8,Promo_EndDate8,Promo_Code9,Promo_StartDate9,Promo_EndDate9,
											Promo_Code10,Promo_StartDate10,Promo_EndDate10,
											Distinct_1,Distinct_2,Distinct_3,Distinct_4,Distinct_5,Distinct_6,Distinct_7,Distinct_8,Distinct_9,Distinct_10,Distinct_11,Distinct_12,
											Distinct_13,Distinct_14,Distinct_15,Distinct_16,Distinct_17,Distinct_18,Distinct_19,Distinct_20,
											ContractID,ContactPerson,FacebookUID,SerialNumber,DeviceNumber,'' Shortener  
											from Contact where ContactId=@EntityId";
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						cmd.Parameters.Add(new SqlParameter("@NewContactId", NewContactId));
						cmd.Parameters.Add(new SqlParameter("@EntityId", subTmp));
						log.Debug(SQL);
						int iR = cmd.ExecuteNonQuery();
						//ListMember
						cmd.Parameters.Clear();
						SQL = @"insert into ListMember(EntityType,CreatedOn,EntityId,ListId,ListMemberId,ModifiedOn) 
								   select 2,getdate(),@EntityId,@newListId,@NewListMemberId,getdate() from ListMember 
								   where EntityId=@oEntityId and ListId=@ListId";
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						cmd.Parameters.Add(new SqlParameter("@newListId", newListId));
						cmd.Parameters.Add(new SqlParameter("@NewListMemberId", NewListMemberId));
						cmd.Parameters.Add(new SqlParameter("@ListId", tmp));
						cmd.Parameters.Add(new SqlParameter("@EntityId", NewContactId));
						cmd.Parameters.Add(new SqlParameter("@oEntityId", subTmp));
						log.Debug(SQL);
						iR = cmd.ExecuteNonQuery();
						//Update ListBase.MemberCount
						cmd.Parameters.Clear();
						SQL = @"update CHT_MSCRM.dbo.ListBase set MemberCount=@count where ListId=@ListId";
						cmd.Parameters.Add(new SqlParameter("@count", EntityOper.Count));
						cmd.Parameters.Add(new SqlParameter("@ListId", newListId));
						cmd.CommandType = CommandType.Text;
						cmd.CommandText = SQL;
						log.Debug(SQL);
						iR = cmd.ExecuteNonQuery();
					}

					// sam 新增 CRM連線
					var CRMConnect = ConfigUtils.Current.ConnectionStrings[1];
					//IDbConnection newconnection = new SqlConnection(CRMConnect);
					var obj = new Models.ListBase();
					using (var newconnection = new SqlConnection(CRMConnect))
					{
						using (var uow1 = DB.CreateUnitOfWork(newconnection))
						{
							var plistid = new SqlParameter { ParameterName = "@oldListId", Value = tmp };
							obj = uow1.ExecuteGetMapModelBy<Models.ListBase>("Select top 1 new_IsImport, new_Criteria, new_importCriteria, new_MaskCriteria  from CHT_MSCRM.dbo.ListExtensionBase  where ListId = @oldListId ", new[] { plistid });
						}
					}


					//Update ListExtensionBase
					cmd.Parameters.Clear();
					cmd.CommandText = @"update CHT_MSCRM.dbo.ListExtensionBase 
                        set new_IsImport=@new_IsImport, 
                            new_Criteria=@new_Criteria, 
                            new_importCriteria=@new_importCriteria, 
                            new_MaskCriteria=@new_MaskCriteria
                         where ListId=@newListId ";

					cmd.Parameters.Add(new SqlParameter("@newListId", newListId));
					cmd.Parameters.Add(new SqlParameter("@new_IsImport", obj.new_IsImport));
					cmd.Parameters.Add(new SqlParameter("@new_Criteria", obj.new_Criteria));
					cmd.Parameters.Add(new SqlParameter("@new_importCriteria", obj.new_importCriteria));
					cmd.Parameters.Add(new SqlParameter("@new_MaskCriteria", obj.new_MaskCriteria));
					//cmd.CommandType = CommandType.Text;
					//cmd.CommandText = SQL;
					log.Debug("Update ListExtensionBase" + cmd.CommandText);
					log.Debug(newListId + "//" + tmp);
					cmd.ExecuteNonQuery();
				}
			}
		}
	}
}
