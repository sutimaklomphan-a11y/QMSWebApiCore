using Dapper;
using Npgsql;
using OutboundQMSModel;
using QMSWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace QMSWebApiCore.Services
{
    public class PreLoadRepository
    {
        public interface IPreLoadRepository
        {
            Task<List<M_PreLoad>> GetPlanListPreLoad(M_PreLoad CLS_PRELOAD);
            Task<List<M_FinishPreLoad>> getFinishPreLoadPlanList(M_FinishPreLoad CLS_FINISHPRELOAD);    
            Task<bool> StampPreLoad(M_PreLoad CLS_PRELOAD);
            Task<bool> StampFinishPreLoad(M_FinishPreLoad CLS_FINISHPRELOAD);
        }
        public class PreloadRepository : IPreLoadRepository
        {
            private readonly IConfiguration _configuration;
            private readonly string _schema;
            private readonly string _connectionString;

            public PreloadRepository(IConfiguration configuration)
            {
                _configuration = configuration;

                _schema = ValidateSchemaName(
                    _configuration["DatabaseSchema:DBSchema"]
                    ?? _configuration["Database:Schema"]
                    ?? "outbound_qms_sim"
                );

                _connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?? throw new ArgumentException("Connection string is required");
            }
            private static string ValidateSchemaName(string schemaName)
            {
                if (string.IsNullOrWhiteSpace(schemaName))
                    throw new ArgumentException("Schema name cannot be null or empty");

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    schemaName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                    throw new ArgumentException($"Invalid schema name: '{schemaName}'");

                return schemaName;
            }
            public async Task<List<M_PreLoad>> GetPlanListPreLoad(M_PreLoad CLS_PRELOAD)
            {
                try
                {
                    var sql = new StringBuilder($@"select
	                                                    summaryplan.tms_summary_plan_no as PlanNo ,
	                                                    plan.tms_plan_group as GroupNo,
	                                                    plan.tms_plan_load_no as LoadNo,
	                                                    plan.tms_plan_dock_no as Dockno ,
	                                                    summaryplan.tms_summary_plan_actual_pre_cool_status as PreCoolStatusID,
	                                                    summaryplan.tms_summary_plan_actual_pre_load_status as PreLoadStatusID ,
	                                                    TO_CHAR(summaryplan.tms_summary_plan_actual_pre_load, 'DD/MM/YYYY HH24:mi') as PreLoadActionDate ,
	                                                    tran.created_by as PreLoadActionBy ,
	                                                    TO_CHAR(tran.created_date, 'DD/MM/YYYY HH24:mi') as PreLoadActionDate ,
	                                                    tran.preload_remark as PreLoadRemark,
	                                                    statuspreload.preload_status_text as PreLoadStatus ,
	                                                    case
		                                                    when summaryplan.tms_summary_plan_actual_pre_cool_status = '0' then 'ไม่ผ่านการตรวจอุณหภูมิ'
		                                                    else case
			                                                    when summaryplan.tms_summary_plan_actual_pre_load_status = '0' then 'ผ่านการตรวจอุณหภูมิ'
			                                                    else 'ผ่านการตรวจอุณหภูมิ' end
		                                                    end as PreCoolStatusText,
	                                                    case
		                                                    when summaryplan.tms_summary_plan_actual_pre_cool_status = '0' then 'TempNotPass'
		                                                    else case
			                                                    when summaryplan.tms_summary_plan_actual_pre_load_status = '0' then 'NotPreLoad'
			                                                    else 'NotPreLoad' end
		                                                    end as PreLoadStatus,
	                                                    case
		                                                    when summaryplan.tms_summary_plan_actual_pre_cool_status = '0' then 'ไม่พร้อมเตรียมการบรรจุสินค้า'
		                                                    else case
			                                                    when summaryplan.tms_summary_plan_actual_pre_load_status = '0' then 'ไม่พร้อมเตรียมการบรรจุสินค้า'
			                                                    else 'เริ่มเตรียมการบรรจุสินค้า(Start Pre Load)' end
		                                                    end as PreLoadStatusText
	                                                    from
		                                                    {_schema}.tb_tms_summary_plan summaryPlan
	                                                    inner join {_schema}.tb_tms_plan plan on
		                                                    summaryplan.tms_summary_plan_no = plan.tms_plan_no
	                                                    left join {_schema}.tb_tran_preload tran on
		                                                    tran.preload_plan_no = plan.tms_plan_no
	                                                    inner join {_schema}.tb_preload_status statusPreLoad on
		                                                    summaryplan.tms_summary_plan_actual_pre_load_status = statuspreload.preload_status_id
	                                                    where
		                                                    tms_summary_plan_action_date = CAST(@PlanDate AS DATE)
		                                                    and summaryPlan.flag = '1'");
                    string PlanDate = "";
                    if (DateTime.Now.Hour < 10)
                    {
                        PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                    }

                    var parameters = new DynamicParameters();
                    parameters.Add("PlanDate", PlanDate);

                    //Search Group No (G1,G2)
                    if (!string.IsNullOrEmpty(CLS_PRELOAD.GroupNo) && CLS_PRELOAD.GroupNo != "All")
                    {
                        sql.AppendLine(" and plan.tms_plan_group = @planGroupNo");
                        parameters.Add("planGroupNo", CLS_PRELOAD.GroupNo);
                    }

                    //Search Preload Staus
                    if (!string.IsNullOrEmpty(CLS_PRELOAD.PreLoadStatusID) && CLS_PRELOAD.PreLoadStatusID != "All")
                    {
                        sql.AppendLine(" and summaryplan.tms_summary_plan_actual_pre_load_status = @PreLoadStatusID");
                        parameters.Add("PreLoadStatusID", CLS_PRELOAD.PreLoadStatusID);
                    }

                    //Search Filter All
                    if (!string.IsNullOrEmpty(CLS_PRELOAD.Search))
                    {
                        sql.Append("AND (plan.tms_plan_dock_no like '%@Search%' OR plan.tms_plan_group like '%@Search%' OR summaryplan.tms_summary_plan_no like '%@Search%')");
                        parameters.Add("Search", CLS_PRELOAD.Search);
                    }

                    //Search Store Type (HYPER,MINI)
                    if (!string.IsNullOrEmpty(CLS_PRELOAD.PlanBigCType) && CLS_PRELOAD.PlanBigCType != "All")
                    {
                        if (CLS_PRELOAD.PlanBigCType == "HYPER")
                        {
                            sql.AppendLine("AND SUBSTRING(trim(summaryPlan.tms_summary_plan_no), 1, 2) like '%PH%'");
                        }
                        else if (CLS_PRELOAD.PlanBigCType == "MINI")
                        {
                            sql.AppendLine("AND SUBSTRING(trim(summaryPlan.tms_summary_plan_no), 1, 2) like '%PM%'");
                        }
                    }

                    sql.Append($@"group by summaryplan.tms_summary_plan_no,
		                                   plan.tms_plan_group ,
		                                   plan.tms_plan_load_no,
		                                   plan.tms_plan_dock_no ,
		                                   summaryplan.tms_summary_plan_actual_pre_cool_status,
		                                   summaryplan.tms_summary_plan_actual_pre_load_status ,
		                                   summaryplan.tms_summary_plan_actual_pre_load,
		                                   tran.preload_remark ,
		                                   tran.created_by,
		                                   tran.created_date,
		                                   statuspreload.preload_status_text
                                           order by summaryplan.tms_summary_plan_no LIMIT 10");

                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        var result = await connection.QueryAsync<M_PreLoad>(
                            sql.ToString(),
                            parameters
                        );
                        return result.ToList();
                    }
                }
                catch (NpgsqlException ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                    throw new Exception($"Failed to retrieve gate in records: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            }
            public async Task<List<M_FinishPreLoad>> getFinishPreLoadPlanList(M_FinishPreLoad CLS_FINISHPRELOAD)
            {
                try
                {
                    var sql = new StringBuilder($@"select  summaryplan.tms_summary_plan_no as PlanNo 
                                                    ,summaryplan.tms_summary_plan_group  as Group 
                                                    ,summaryplan.tms_summary_load_no  as LoadNo 
                                                    ,summaryplan.tms_summary_dock_no  as Dockno 
                                                    ,summaryplan.tms_summary_plan_actual_pre_load_status as PreLoadStatusID 
                                                    ,coalesce(summaryplan.tms_summary_plan_actual_finish_preload_status, '0') as FinishPreLoadStatusID 
                                                    ,TO_CHAR(summaryplan.tms_summary_plan_actual_finish_preload, 'DD/MM/YYYY HH24:mi') as FinishPreLoadActionDate 
                                                    ,tran.finish_preload_action_by as FinishPreLoadActionBy 
                                                    ,status.finish_preload_status_id as StatusID 
                                                    ,status.finish_preload_status_text as StatusText,
                                                    case when summaryplan.tms_summary_plan_actual_pre_load_status = '0' then 'ไม่พร้อมเตรียมการบรรจุสินค้า'
                                                         else case
			                                                    when summaryplan.tms_summary_plan_actual_finish_preload_status = '0' then 'เริ่มเตรียมการบรรจุสินค้า(Start Pre Load)'
                                                                when summaryplan.tms_summary_plan_actual_finish_preload_status = '1' then 'เตรียมสินค้าที่จุดบรรุเรียบร้อย'
			                                                    else 'เริ่มเตรียมการบรรจุสินค้า(Start Pre Load)' end
		                                                    end as FinishPreLoadStatusText
                                                    ,tran.finish_preload_remark as FinishPreLoadRemark 

                                                     from {_schema}.tb_tms_summary_plan summaryPlan 
                                                     left join {_schema}.tb_tran_finish_preload tran on tran.finish_preload_plan_no = summaryplan.tms_summary_plan_no  
                                                     left join {_schema}.tb_finish_preload_status status  
                                                     on COALESCE (summaryPlan.tms_summary_plan_actual_finish_preload_status,'0') = status.finish_preload_status_id 
                                                     where 1=1 
                                                     and tms_summary_plan_action_date::DATE between current_date-1 and current_date
                                                     and summaryPlan.flag = '1'  ");

                    var parameters = new DynamicParameters();

                    //Search Group No (G1,G2)
                    if (!string.IsNullOrEmpty(CLS_FINISHPRELOAD.GroupNo) && CLS_FINISHPRELOAD.GroupNo != "All")
                    {
                        sql.AppendLine(" and plan.tms_plan_group = @planGroupNo");
                        parameters.Add("planGroupNo", CLS_FINISHPRELOAD.GroupNo);
                    }

                    //Search Preload Staus
                    if (!string.IsNullOrEmpty(CLS_FINISHPRELOAD.PreLoadStatusID) && CLS_FINISHPRELOAD.PreLoadStatusID != "All")
                    {
                        sql.AppendLine(" and summaryplan.tms_summary_plan_actual_pre_load_status = @PreLoadStatusID");
                        parameters.Add("PreLoadStatusID", CLS_FINISHPRELOAD.PreLoadStatusID);
                    }

                    //Search Filter All
                    if (!string.IsNullOrEmpty(CLS_FINISHPRELOAD.Search))
                    {
                        sql.Append("AND (plan.tms_plan_dock_no like '%@Search%' OR plan.tms_plan_group like '%@Search%' OR summaryplan.tms_summary_plan_no like '%@Search%')");
                        parameters.Add("Search", CLS_FINISHPRELOAD.Search);
                    }

                    //Search Store Type (HYPER,MINI)
                    if (!string.IsNullOrEmpty(CLS_FINISHPRELOAD.PlanBigCType) && CLS_FINISHPRELOAD.PlanBigCType != "All")
                    {
                        if (CLS_FINISHPRELOAD.PlanBigCType == "HYPER")
                        {
                            sql.AppendLine("AND SUBSTRING(trim(summaryPlan.tms_summary_plan_no), 1, 2) like '%PH%'");
                        }
                        else if (CLS_FINISHPRELOAD.PlanBigCType == "MINI")
                        {
                            sql.AppendLine("AND SUBSTRING(trim(summaryPlan.tms_summary_plan_no), 1, 2) like '%PM%'");
                        }
                    }

                    sql.Append($@" order by summaryplan.tms_summary_plan_no LIMIT 10");
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        var result = await connection.QueryAsync<M_FinishPreLoad>(
                            sql.ToString(),
                            parameters
                        );
                        return result.ToList();
                    }
                }
                catch (NpgsqlException ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                    throw new Exception($"Failed to retrieve gate in records: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            }
            public async Task<bool> StampPreLoad(M_PreLoad CLS_PRELOAD)
            {
                if (CLS_PRELOAD == null)
                    throw new ArgumentNullException(nameof(CLS_PRELOAD));

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_preload
                                        (preload_id, preload_load_no, preload_action_date, preload_remark
                                        , preload_status, created_by, created_date, flag
                                        , preload_plan_no
                                        , preload_employee_1, preload_employee_2, preload_employee_3, preload_employee_4, preload_employee_5
                                        , preload_employee_6, preload_employee_7, preload_employee_8, preload_employee_9, preload_employee_10
                                        , preload_dc_code) 
                                    values(
                                        nextval('{_schema}.tb_tran_preload_preload_id_seq'::regclass)
                                        , @LoadNo
                                        , CAST(@PlanActionDate AS DATE)
                                        , @PreLoadRemark
                                        , '1'
                                        , @CreateBy
                                        , now(), '1'
                                        , @PlanNo
                                        , @Emp1, @Emp2, @Emp3, @Emp4, @Emp5, @Emp6, @Emp7, @Emp8, @Emp9, @Emp10
                                        , @DCCode)";

                    var sql_update_summary = $@" UPDATE {_schema}.tb_tms_summary_plan 
                                           SET tms_summary_plan_actual_pre_load_status = '1', 
                                               tms_summary_plan_actual_pre_load = now()
                                         where tms_summary_plan_no = @PlanNo
                                         and tms_summary_dc_code = @DCCode ";


                    var sql_update_wip = $@" UPDATE {_schema}.tb_wip_current SET status = '5' Where plan_no = @PlanNo ";

                    if (DateTime.Now.Hour < 8)
                    {
                        CLS_PRELOAD.PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        CLS_PRELOAD.PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                    }

                    var parameters = new
                    {
                        LoadNo = CLS_PRELOAD.LoadNo,
                        PlanActionDate = CLS_PRELOAD.PlanDate,
                        PreLoadRemark = CLS_PRELOAD.PreLoadRemark,
                        CreateBy = CLS_PRELOAD.PreLoadActionBy,
                        ActionBy = CLS_PRELOAD.PreLoadActionBy,
                        PlanNo = CLS_PRELOAD.PlanNo,
                        Emp1 = CLS_PRELOAD.Employee1,
                        Emp2 = CLS_PRELOAD.Employee2,
                        Emp3 = CLS_PRELOAD.Employee3,
                        Emp4 = CLS_PRELOAD.Employee4,
                        Emp5 = CLS_PRELOAD.Employee5,
                        Emp6 = CLS_PRELOAD.Employee6,
                        Emp7 = CLS_PRELOAD.Employee7,
                        Emp8 = CLS_PRELOAD.Employee8,
                        Emp9 = CLS_PRELOAD.Employee9,
                        Emp10 = CLS_PRELOAD.Employee10,
                        DCCode = CLS_PRELOAD.DCCode
                    };

                    var rowInsert = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                    var rowUpdate_sum = await connection.ExecuteAsync(sql_update_summary, parameters, transaction);
                    var rowUpdate_wip = await connection.ExecuteAsync(sql_update_wip, parameters, transaction);

                    if (rowInsert == 0 || rowUpdate_sum == 0 || rowUpdate_wip == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            }
            public async Task<bool> StampFinishPreLoad(M_FinishPreLoad CLS_FINISHPRELOAD)
            {
                if (CLS_FINISHPRELOAD == null)
                    throw new ArgumentNullException(nameof(CLS_FINISHPRELOAD));

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    var sql_Insert_finishpreload = $@"INSERT INTO {_schema}.tb_tran_finish_preload 
                                        (finish_preload_id, finish_preload_load_no, finish_preload_dock_no, finish_preload_plan_no 
                                        ,finish_preload_action_by, finish_preload_action_date 
                                        ,created_by,created_date 
                                        ,finish_preload_remark, flag,finish_preload_dc_code) 
                                        VALUES(nextval('{_schema}.tb_tran_finish_preload_finish_preload_id_seq'::regclass) 
                                        ,@LoadNo,@DockNo,@PlanNo 
                                        ,@ActionBy,@PlanActionDate 
                                        ,@CreateBy,now() 
                                        ,@Remark,'1',@DCCode)";

                    var sql_update_preload = $@"update {_schema}.tb_tran_preload set preload_status = '2',preload_finish_date = now(),preload_finish_action_by = @ActionBy 
                                         where preload_plan_no = @PlanNo";

                    var sql_update_summary = $@" update {_schema}.tb_tms_summary_plan set tms_summary_plan_actual_finish_preload_status = '1' 
                                        ,tms_summary_plan_actual_finish_preload=now() 
                                         where tms_summary_plan_no = @PlanNo and tms_summary_dc_code = @DCCode  ";

                    var sql_update_wip = $@"Update {_schema}.tb_wip_current 
                                         SET status = 5 
                                         Where plan_no = @PlanNo 
                                         and dc_code = @DCCode;  ";

                    if (DateTime.Now.Hour < 8)
                    {
                        CLS_FINISHPRELOAD.PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        CLS_FINISHPRELOAD.PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                    }

                    var parameters = new
                    {
                        LoadNo = CLS_FINISHPRELOAD.LoadNo,
                        DockNo = CLS_FINISHPRELOAD.DockNo,
                        PlanNo = CLS_FINISHPRELOAD.PlanNo,
                        ActionBy = CLS_FINISHPRELOAD.PreLoadActionBy,
                        PlanActionDate = CLS_FINISHPRELOAD.PlanDate,
                        CreateBy = CLS_FINISHPRELOAD.PreLoadActionBy,
                        Remark = CLS_FINISHPRELOAD.FinishPreLoadRemark,
                        DCCode = CLS_FINISHPRELOAD.DCCode
                    };

                    var rowInsert = await connection.ExecuteAsync(sql_Insert_finishpreload, parameters, transaction);
                    var rowUpdate_preload = await connection.ExecuteAsync(sql_update_preload, parameters, transaction);
                    var rowUpdate_sum = await connection.ExecuteAsync(sql_update_summary, parameters, transaction);
                    var rowUpdate_wip = await connection.ExecuteAsync(sql_update_wip, parameters, transaction);

                    if (rowInsert == 0 || rowUpdate_sum == 0 || rowUpdate_wip == 0 || rowUpdate_preload == 0)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
