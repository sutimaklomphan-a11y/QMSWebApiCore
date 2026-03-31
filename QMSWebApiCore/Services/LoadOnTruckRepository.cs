using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace QMSWebApiCore.Services
{
    public interface ILoadOnTruckRepository
    {
        Task<List<M_LoadOnTruck>> GetPlanListFinishPreLoadAsync(M_LoadOnTruck filter);
        Task<List<M_LoadOnTruck>> GetLoadInTruckPlanListAsync(M_LoadOnTruck filter);
        Task<List<M_LoadOnTruck>> getFinishLoadPlanList(M_LoadOnTruck CLS_TRUCKONDOCK);
        Task<bool> StampLoadOnTruck(M_LoadOnTruck CLS_ONTRUCK);
        Task<bool> StampFinishLoadOnTruck(string PlanNo, string LoadOnTruckFinishBy, string LoadOnTruckFinishRemark, string DCCode, string ActionBy);
        Task<bool> checkTruckOnDock(string PlanNo, string DCCode);
    }

    public class LoadOnTruckRepository : ILoadOnTruckRepository
    {
        private readonly string _schema;
        private readonly string _connectionString;
        public LoadOnTruckRepository(IConfiguration configuration)
        {
            _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
                ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("Connection string is required");
        }
        private static string ValidateSchemaName(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty");

            if (!System.Text.RegularExpressions.Regex.IsMatch(schemaName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new ArgumentException($"Invalid schema name: '{schemaName}'");

            return schemaName;
        }
        public async Task<List<M_LoadOnTruck>> GetPlanListFinishPreLoadAsync(M_LoadOnTruck filter)
        {
            return await GetPlanListInternal(filter, true);
        }
        public async Task<List<M_LoadOnTruck>> GetLoadInTruckPlanListAsync(M_LoadOnTruck filter)
        {
            return await GetPlanListInternal(filter, false);
        }
        private async Task<List<M_LoadOnTruck>> GetPlanListInternal(M_LoadOnTruck filter, bool isFinishPreLoad)
        {
            try
            {
                var sql = new StringBuilder($@"
                    SELECT
                        summaryplan.tms_summary_plan_no as PlanNo,
                        plan.tms_plan_group as PlanGroupNo,
                        plan.tms_plan_load_no as LoadNo,
                        plan.tms_plan_dock_no as DockNo,
                        summaryplan.tms_summary_plan_actual_finish_preload_status as FinishPreLoadStatus,
                        TO_CHAR(summaryplan.tms_summary_plan_actual_finish_preload, 'DD/MM/YYYY HH24:mi') as FinishPreLoadActionDate,
                        summaryplan.tms_summary_plan_actual_finish_preload as LoadOnTruckFinishDate,
                        coalesce(summaryplan.tms_summary_plan_actual_start_load_status, '0') as LoadOnTruckStatusID,
                        status.loadontruck_status_text as LoadOnTruckStatusText,
                        TO_CHAR(tran.created_date, 'DD/MM/YYYY HH24:mi') as LoadOnTruckActionDate,
                        tran.loadontruck_action_by as LoadOnTruckActionBy,
                        tran.loadontruck_remark as LoadOnTruckRemark,
                        summaryplan.tms_summary_plan_actual_pre_load_status as PreLoadStatusID,
                        statuspreload.preload_status_text as PreLoadStatusText,
                        statusprecool.precool_status_text as PreCoolStatusText,
                        tranpreload.preload_remark as PreLoadRemark
                    FROM
                        {_schema}.tb_tms_summary_plan summaryPlan
                    INNER JOIN {_schema}.tb_tms_plan plan ON
                        summaryplan.tms_summary_plan_no = plan.tms_plan_no
                    LEFT JOIN {_schema}.tb_tran_loadontruck tran ON
                        tran.loadontruck_plan_no = plan.tms_plan_no
                    LEFT JOIN {_schema}.tb_tran_preload tranpreload ON
                        tranpreload.preload_plan_no = plan.tms_plan_no
                    LEFT JOIN {_schema}.tb_loadontruck_status status ON
                        status.loadontruck_status_id = coalesce(summaryPlan.tms_summary_plan_actual_start_load_status, '0')
                    LEFT JOIN {_schema}.tb_preload_status statuspreload ON
                        statuspreload.preload_status_id = coalesce(summaryplan.tms_summary_plan_actual_pre_load_status, '0')
                    LEFT JOIN {_schema}.tb_precool_status statusprecool ON
                        statusprecool.precool_status_id = cast(coalesce(summaryplan.tms_summary_plan_actual_pre_cool_status, '0') as integer)
                    WHERE summaryPlan.flag = '1'
                        AND summaryPlan.tms_summary_plan_action_date::DATE BETWEEN CURRENT_DATE - 1 AND CURRENT_DATE");

                var parameters = new DynamicParameters();

                // Nullable DCCode filter (if it's ever used in SQL, added here for completeness)
                if (!string.IsNullOrEmpty(filter.DCCode))
                {
                    sql.AppendLine(" AND summaryPlan.tms_summary_dc_code = @DCCode");
                    parameters.Add("DCCode", filter.DCCode);
                }

                // Search Filter for DockNo or LoadNo
                if (!string.IsNullOrEmpty(filter.Search))
                {
                    sql.AppendLine(" AND (summaryplan.tms_summary_dock_no LIKE @Search OR summaryplan.tms_summary_load_no LIKE @Search)");
                    parameters.Add("Search", $"%{filter.Search}%");
                }

                // Search Store Type (HYPER, MINI)
                if (!string.IsNullOrEmpty(filter.PlanBigCType) && filter.PlanBigCType != "All")
                {
                    if (filter.PlanBigCType.Equals("HYPER", StringComparison.OrdinalIgnoreCase))
                    {
                        sql.AppendLine(" AND substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PH'");
                    }
                    else if (filter.PlanBigCType.Equals("MINI", StringComparison.OrdinalIgnoreCase))
                    {
                        sql.AppendLine(" AND substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PM'");
                    }
                }

                // Search Group No (G1, G2)
                if (!string.IsNullOrEmpty(filter.PlanGroupNo) && filter.PlanGroupNo != "All")
                {
                    sql.AppendLine(" AND summaryPlan.tms_summary_plan_group = @PlanGroupNo");
                    parameters.Add("PlanGroupNo", filter.PlanGroupNo);
                }

                sql.AppendLine($@" GROUP BY 
                                    summaryplan.tms_summary_plan_no,
                                    plan.tms_plan_group,
                                    plan.tms_plan_load_no,
                                    plan.tms_plan_dock_no,
                                    summaryplan.tms_summary_plan_actual_finish_preload_status,
                                    summaryplan.tms_summary_plan_actual_finish_preload,
                                    summaryplan.tms_summary_plan_actual_start_load,
                                    summaryplan.tms_summary_plan_actual_start_load_status,
                                    tran.created_date,
                                    tran.loadontruck_action_by,
                                    tran.loadontruck_remark,
                                    summaryplan.tms_summary_plan_actual_pre_load_status,
                                    statuspreload.preload_status_text,
                                    statusprecool.precool_status_text,
                                    tranpreload.preload_remark,
                                    status.loadontruck_status_text");

                sql.AppendLine(" ORDER BY summaryplan.tms_summary_plan_no LIMIT 10");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_LoadOnTruck>(sql.ToString(), parameters);
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve load on truck records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_LoadOnTruck>> getFinishLoadPlanList(M_LoadOnTruck CLS_ONTRUCK)
        {
            try
            {
                var sql = new StringBuilder($@" select summaryplan.tms_summary_plan_no as PlanNo,
	                                                    plan.tms_plan_group as PlanGroupNo,
	                                                    plan.tms_plan_load_no as LoadNo,
	                                                    plan.tms_plan_dock_no as DockNo ,
	                                                    summaryplan.tms_summary_plan_actual_start_load_status as LoadOnTruckStatusID ,
	                                                    TO_CHAR(summaryplan.tms_summary_plan_actual_start_load, 'DD/MM/YYYY HH24:mi') as LoadOnTruckActionDate ,
	                                                    TO_CHAR(tran.loadontruck_finish_date, 'DD/MM/YYYY HH24:mi') as LoadOnTruckFinishDate ,
	                                                    tran.loadontruck_finish_by as LoadOnTruckFinishBy ,
	                                                    tran.loadontruck_finish_remark as LoadOnTruckFinishRemark ,
	                                                    tms_summary_plan_actual_finish_load_status as FinishLoadStatus ,
	                                                    status.loadintruck_finish_status_text as FinishLoadStatusText,
                                                        plan.tms_plan_truck_type as TruckType,
                                                        plan.tms_plan_store_format as PlanBigCType
                                                    from
	                                                    {_schema}.tb_tms_summary_plan summaryPlan
                                                    inner join {_schema}.tb_tms_plan plan on
	                                                    summaryplan.tms_summary_plan_no = plan.tms_plan_no
                                                    left join {_schema}.tb_tran_loadontruck tran on
	                                                    tran.loadontruck_plan_no = plan.tms_plan_no
                                                    inner join {_schema}.tb_loadintruck_finish_status status on
	                                                    status.loadintruck_finish_status_id = coalesce (summaryPlan.tms_summary_plan_actual_finish_load_status ,
	                                                    '0')
                                                    where summaryPlan.flag = '1'
	                                                    and tms_summary_plan_action_date::DATE BETWEEN CURRENT_DATE - 1 AND CURRENT_DATE
                                                        and summaryPlan.tms_summary_dc_code = @DCCode
	                                                    group by
		                                                    summaryplan.tms_summary_plan_no,
		                                                    plan.tms_plan_group,
		                                                    plan.tms_plan_load_no,
		                                                    plan.tms_plan_dock_no ,
		                                                    summaryplan.tms_summary_plan_actual_finish_preload_status,
		                                                    summaryplan.tms_summary_plan_actual_finish_preload ,
		                                                    summaryplan.tms_summary_plan_actual_start_load,
		                                                    summaryplan.tms_summary_plan_actual_start_load_status ,
		                                                    tran.loadontruck_finish_date,
		                                                    tran.loadontruck_finish_by,
		                                                    tran.loadontruck_finish_remark ,
		                                                    status.loadintruck_finish_status_id,
		                                                    tms_summary_plan_actual_finish_load_status,
		                                                    status.loadintruck_finish_status_text,
                                                            plan.tms_plan_truck_type,
                                                            plan.tms_plan_store_format
	                                                    order by
		                                                    summaryplan.tms_summary_plan_no ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", CLS_ONTRUCK.DCCode);

                if (!string.IsNullOrEmpty(CLS_ONTRUCK.Search))
                {
                    sql.Append(" AND (summaryplan.tms_summary_dock_no LIKE @Search OR plan.tms_plan_group LIKE @Search OR plan.tms_plan_load_no LIKE @Search)");
                    parameters.Add("Search", $"%{CLS_ONTRUCK.Search}%");
                }

                if (!string.IsNullOrEmpty(CLS_ONTRUCK.LoadOnTruckStatusID) && CLS_ONTRUCK.LoadOnTruckStatusID != "All")
                {
                    sql.Append(" AND coalesce(summaryplan.tms_summary_plan_actual_finish_load_status, '0') = @Status ");
                    parameters.Add("Status", CLS_ONTRUCK.LoadOnTruckStatusID);
                }

                if (!string.IsNullOrEmpty(CLS_ONTRUCK.PlanBigCType) && CLS_ONTRUCK.PlanBigCType != "All")
                {
                    if (CLS_ONTRUCK.PlanBigCType == "HYPER")
                    {
                        sql.AppendLine(" AND substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PH'");
                    }
                    else
                    {
                        sql.AppendLine(" AND substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PM'");
                    }
                }

                if (!string.IsNullOrEmpty(CLS_ONTRUCK.PlanGroupNo) && CLS_ONTRUCK.PlanGroupNo != "All")
                {
                    sql.AppendLine(" AND plan.tms_plan_group = @PlanGroupNo");
                    parameters.Add("PlanGroupNo", CLS_ONTRUCK.PlanGroupNo);
                }

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_LoadOnTruck>(sql.ToString(),parameters);
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

        public async Task<bool> StampLoadOnTruck(M_LoadOnTruck CLS_ONTRUCK)
        {
            if (CLS_ONTRUCK == null)
                throw new ArgumentNullException(nameof(CLS_ONTRUCK));

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_loadontruck
                                    (loadontruck_id 
                                    ,loadontruck_load_no, loadontruck_action_date 
                                    ,loadontruck_remark, loadontruck_status 
                                    ,created_by, created_date, flag, loadontruck_plan_no 
                                    ,loadontruck_employee_1, loadontruck_employee_2, loadontruck_employee_3, loadontruck_employee_4, loadontruck_employee_5 
                                    ,loadontruck_employee_6, loadontruck_employee_7, loadontruck_employee_8, loadontruck_employee_9, loadontruck_employee_10 
                                    ,loadontruck_action_by, loadontruck_dc_code) 
                                    VALUES(nextval('{_schema}.tb_tran_loadontruck_loadontruck_id_seq'::regclass) 
                                    ,@LoadNo, CAST(@PlanActionDate AS DATE)
                                    ,@Remark,@Status 
                                    ,@ActionBy,now(),'1',@PlanNo 
                                    ,@Emp1,@Emp2,@Emp3,@Emp4,@Emp5,@Emp6,@Emp7,@Emp8,@Emp9,@Emp10 
                                    ,@ActionBy,@DCCode) ";

                var sql_update_summary = $@"update {_schema}.tb_tms_summary_plan 
                                    SET tms_summary_plan_actual_start_load_status = '1',
                                    tms_summary_plan_actual_start_load = now() 
                                    WHERE tms_summary_plan_no = @PlanNo and tms_summary_dc_code = @DCCode ";


                var sql_update_tran_gate = $@"update {_schema}.tb_tran_gate
                                     SET last_process = 'Start Load'
                                     where gate_barcode = (select truckondock_barcode from {_schema}.tb_tran_truckondock where truckondock_plan_no = @PlanNo)
                                     and gate_action_date =CAST(@PlanActionDate AS DATE) and gate_status = '1' ";
               
                var sql_update_wip = $@" UPDATE {_schema}.tb_wip_current
                                                 SET status = '7'
                                                 Where plan_no = @PlanNo
                                                 and dc_code = @DCCode ";

                if (DateTime.Now.Hour < 8)
                {
                    CLS_ONTRUCK.PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                }
                else
                {
                    CLS_ONTRUCK.PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                }

                var parameters = new
                {
                    LoadNo = CLS_ONTRUCK.LoadNo,
                    PlanActionDate = CLS_ONTRUCK.PlanDate,
                    Remark = CLS_ONTRUCK.PreLoadRemark,
                    Status = CLS_ONTRUCK.LoadOnTruckStatusID,
                    CreateBy = CLS_ONTRUCK.LoadOnTruckActionBy,
                    ActionBy = CLS_ONTRUCK.LoadOnTruckActionBy,
                    PlanNo = CLS_ONTRUCK.PlanNo,
                    Emp1 = CLS_ONTRUCK.Employee1,
                    Emp2 = CLS_ONTRUCK.Employee2,
                    Emp3 = CLS_ONTRUCK.Employee3,
                    Emp4 = CLS_ONTRUCK.Employee4,
                    Emp5 = CLS_ONTRUCK.Employee5,
                    Emp6 = CLS_ONTRUCK.Employee6,
                    Emp7 = CLS_ONTRUCK.Employee7,
                    Emp8 = CLS_ONTRUCK.Employee8,
                    Emp9 = CLS_ONTRUCK.Employee9,
                    Emp10 = CLS_ONTRUCK.Employee10,
                    DCCode = CLS_ONTRUCK.DCCode
                };

                var rowInsert = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                var rowUpdate_sum = await connection.ExecuteAsync(sql_update_summary, parameters, transaction);
                var rowUpdate_gate = await connection.ExecuteAsync(sql_update_tran_gate,parameters,transaction);
                var rowUpdate_wip = await connection.ExecuteAsync(sql_update_wip, parameters, transaction);

                if (rowInsert == 0 || rowUpdate_sum == 0 || rowUpdate_gate == 0 || rowUpdate_wip == 0)
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
        public async Task<bool> StampFinishLoadOnTruck(string PlanNo, string LoadOnTruckFinishBy, string LoadOnTruckFinishRemark, string DCCode, string ActionBy)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var sql_Upate_load = $@"update {_schema}.tb_tran_loadontruck 
                                set loadontruck_finish_by = @ActionBy, 
                                    loadontruck_finish_remark = @Remark ,
                                    loadontruck_finish_date = now(),loadontruck_status = '2' 
                                where loadontruck_plan_no = @PlanNo 
                                    and loadontruck_dc_code = @DCCode ";

                var sql_update_summary = $@"update {_schema}.tb_tms_summary_plan
                                set tms_summary_plan_actual_finish_load_status = '1',
                                    tms_summary_plan_actual_finish_load = now()
                                where tms_summary_plan_no = @PlanNo 
                                    and tms_summary_dc_code = @DCCode";


                var sql_update_tran_gate = $@" update {_schema}.tb_tran_gate
                                         set last_process = 'Finish Load'
                                         where gate_barcode = (select truckondock_barcode from {_schema}.tb_tran_truckondock 
                                                                where truckondock_plan_no = @PlanNo)
                                         and gate_action_date = CAST(@PlanActionDate AS DATE)
                                         and gate_status = '1'";

                var sql_update_wip = $@" UPDATE {_schema}.tb_wip_current
                                                 SET status = '8'
                                                 Where plan_no = @PlanNo
                                                 and dc_code = @DCCode ";

                string PlanActionDate = "";
                if (DateTime.Now.Hour < 8)
                {
                    PlanActionDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                }
                else
                {
                    PlanActionDate = DateTime.Today.ToString("yyyy-MM-dd");
                }

                var parameters = new
                {
                    Remark = LoadOnTruckFinishRemark,
                    PlanActionDate = PlanActionDate,
                    CreateBy = LoadOnTruckFinishBy,
                    ActionBy = ActionBy,
                    PlanNo = PlanNo,
                    DCCode = DCCode
                };

                var rowUpateload = await connection.ExecuteAsync(sql_Upate_load, parameters, transaction);
                var rowUpdate_sum = await connection.ExecuteAsync(sql_update_summary, parameters, transaction);
                var rowUpdate_gate = await connection.ExecuteAsync(sql_update_tran_gate, parameters, transaction);
                var rowUpdate_wip = await connection.ExecuteAsync(sql_update_wip, parameters, transaction);

                if (rowUpateload == 0 || rowUpdate_sum == 0 || rowUpdate_gate == 0 || rowUpdate_wip == 0)
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
        public async Task<bool> checkTruckOnDock(string PlanNo, string DCCode)
        {
            if (string.IsNullOrWhiteSpace(PlanNo))
                return false;
            try
            {
                var sql = $@"select count(*) from {_schema}.tb_tran_truckondock 
                        where truckondock_plan_no = @PlanNo and truckondock_dc_code = @DCCode ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var count = await connection.ExecuteScalarAsync<int>(sql, new { PlanNo = PlanNo, DCCode = DCCode });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking duplicate barcode: {ex.Message}");
                throw;
            }
        }        
    }
}

