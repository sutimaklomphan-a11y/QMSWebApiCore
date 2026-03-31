using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace QMSWebApiCore.Services
{
    public interface ITMSPlanRepository
    {
        Task<bool> SaveTMSPlanAsync(List<M_TMSPlan> plans);
        Task<List<M_TMSPlan>> GetTMSPlanListOnDate(M_TMSPlan plans);
        Task<bool> CancelTMSPlan(M_TMSPlan plans);
    }
    public class TMSPlanRepository : ITMSPlanRepository
    {
        private readonly string _schema;
        private readonly string _connectionString;

        public TMSPlanRepository(IConfiguration configuration)
        {
            _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
                ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("Connection string is required");
        }

        //ใช้สำหรับ TMS Plan managment
        public async Task<List<M_TMSPlan>> GetTMSPlanListOnDate(M_TMSPlan plans)
        {
            string PlanDate = "";
            string YesterdayDate = "";
            if (DateTime.Now.Hour < 8)
            {
                YesterdayDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            }
            else
            {
                PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                YesterdayDate = DateTime.Today.ToString("yyyy-MM-dd");
            }

            try
            {
                var sql = new StringBuilder($@" select tms_summary_plan_no as PlanNo,PlanDate as PlanActionDate,
                                                plangroupno as PlanGroupNo,planloadno as PlanLoadNo,plandockno as PlanDockNo,
                                                countstore as CountStore,storecode as PlanStoreCode,storeformat as PlanStoreFormat,storename as PlanStoreName,plantrucktype as PlanTruckType 
                                                from outbound_qms.tb_tms_summary_plan sumPlan 
                                                inner join ( 
                                                select to_char(tms_plan_action_date,'DD/MM/YYYY') as PlanDate,tms_plan_no,min(tms_plan_store_code) as StoreCode,tms_plan_group as PlanGroupNo, 
                                                tms_plan_load_no as PlanLoadNo,count(tms_plan_store_code) as CountStore,min(tms_plan_store_format) as StoreFormat 
                                                ,min(tms_plan_store_name) as StoreName,tms_plan_truck_type as PlanTruckType,tms_plan_dock_no as PlanDockNo, 
                                                tms_plan_pre_cool as PlanPreCool 
                                                 from outbound_qms.tb_tms_plan 
                                                 where  tb_tms_plan.flag = '1' and  tms_plan_dc_code = @DCCode 
                                                group by 
                                                tms_plan_action_date,tms_plan_no,tms_plan_group,tms_plan_load_no,tms_plan_truck_type, 
                                                tms_plan_dock_no,tms_plan_pre_cool ) Plan on sumplan.tms_summary_plan_no = plan.tms_plan_no 
                                                where 1=1 and tms_summary_plan_action_date in ('{YesterdayDate}' ,'{PlanDate}') ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", plans.DCCode);

                if (!string.IsNullOrEmpty(plans.PlanGroupNo) && plans.PlanGroupNo != "All")
                {
                    sql.AppendLine(" AND PlanGroupNo = @PlanGroupNo");
                    parameters.Add("PlanGroupNo", plans.PlanGroupNo);
                }

                if (!string.IsNullOrEmpty(plans.PlanTypeBigC) && plans.PlanTypeBigC != "All")
                {
                    if (plans.PlanTypeBigC == "HYPER")
                    {
                        sql.AppendLine("AND SUBSTRING(tms_summary_plan_no, 1, 2) = 'PH'");
                    }
                    else
                    {
                        sql.AppendLine("AND SUBSTRING(tms_summary_plan_no, 1, 2) = 'PM'");
                    }
                }

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_TMSPlan>(sql.ToString(), parameters);
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
        public async Task<bool> SaveTMSPlanAsync(List<M_TMSPlan> plans)
        {
            if (plans == null || plans.Count == 0) return false;
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var first = plans[0];
                string planDateStr = Convert.ToDateTime(first.PlanActionDate).ToString("yyyyMMdd");
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sqlDeletePlan = $@" DELETE FROM {_schema}.tb_tms_plan
                                                WHERE tms_plan_action_date = CAST(NULLIF(@PlanActionDate, '') AS DATE)
                                                AND   tms_plan_group       = @PlanGroupNo
                                                AND   substring(trim(tms_plan_no), 1, 2) = @PlanTypeBigC
                                                AND   tms_plan_store_code = @PlanStoreCode
                                                AND   tms_plan_dc_code     = @DCCode";

                        await connection.ExecuteAsync(sqlDeletePlan, first, transaction);
                        var sqlInsertPlan = $@"
                                            INSERT INTO {_schema}.tb_tms_plan (
                                                tms_plan_id,        
                                                tms_plan_no,          
                                                tms_plan_group,
                                                tms_plan_load_no,   
                                                tms_plan_dock_no,     
                                                tms_plan_action_date,
                                                tms_plan_store_code,
                                                tms_plan_store_name,  
                                                tms_plan_store_format,
                                                tms_plan_truck_type,
                                                tms_plan_pre_cool,    
                                                tms_plan_on_dock,
                                                tms_plan_start_load,
                                                tms_plan_finish_load, 
                                                tms_plan_dispatch,
                                                tms_plan_type,      
                                                tms_plan_dc_code,   
                                                created_by,         
                                                created_date,
                                                updated_by,         
                                                updated_date, 
                                                tms_plan_status,
                                                flag
                                            ) VALUES (
                                                nextval('{_schema}.tb_tms_plan_tms_plan_id_seq'),
                                                @PlanNo,        
                                                @PlanGroupNo,   
                                                @PlanLoadNo,
                                                @PlanDockNo,    
                                                CAST(NULLIF(@PlanActionDate,  '') AS DATE),
                                                @PlanStoreCode, 
                                                @PlanStoreName, 
                                                @PlanStoreFormat,
                                                @PlanTruckType,
                                                CAST(NULLIF(@PlanPreCool,    '') AS TIMESTAMP),
                                                CAST(NULLIF(@PlanOnDock,     '') AS TIMESTAMP),
                                                CAST(NULLIF(@PlanStartLoad,  '') AS TIMESTAMP),
                                                CAST(NULLIF(@PlanFinishLoad, '') AS TIMESTAMP),
                                                CAST(NULLIF(@PlanDispatch,   '') AS TIMESTAMP),
                                                @PlanPrefix,  
                                                @DCCode,    
                                                @CreateBy,      
                                                NOW(),
                                                @UpdateBy,      
                                                NOW(),      
                                                '1',
                                                '1'
                                            )";

                        var groupedByLoad = plans
                            .GroupBy(p => p.PlanLoadNo?.Trim())
                            .Select((g, loadIndex) => new
                            {
                                LoadCounter = (loadIndex + 1).ToString("000"),
                                Rows = g.ToList()
                            })
                            .ToList();

                        foreach (var loadGroup in groupedByLoad)
                        {
                            int rowCounter = 1;
                            foreach (var plan in loadGroup.Rows)
                            {
                                // PlanNo: PH/PM + G2 + 20260301 + 001(row)
                                var PlanType = "";
                                if(plan.PlanPrefix == "Mini")
                                {
                                    PlanType = "PM";
                                    plan.PlanNo = $"{PlanType}{plan.PlanGroupNo}{planDateStr}{plan.PlanLoadNo:000}";
                                }
                                else
                                {
                                    PlanType = "PH";
                                    plan.PlanNo = $"{PlanType}{plan.PlanGroupNo}{planDateStr}{"001"}{plan.PlanLoadNo:000}";
                                }
                                
                                if (string.IsNullOrEmpty(plan.UpdateBy))
                                    plan.UpdateBy = plan.CreateBy;

                                await connection.ExecuteAsync(sqlInsertPlan, plan, transaction);
                                rowCounter++;
                            }
                        }

                        // ── Commit Step 1 
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // STEP 2 : Select ข้อมูลที่ Insert ไปแล้ว จาก tb_tms_plan
                var sqlSelectSummary = $@"
                                        SELECT
                                            tms_plan_no                                              AS PlanNo,
                                            tms_plan_action_date                                     AS PlanDate,
                                            COUNT(tms_plan_store_code)                               AS CountStore,
                                            '0'                                                      AS PreCoolStatus,
                                            '0'                                                      AS PreLoadStatus,
                                            '0'                                                      AS TruckOnDockStatus,
                                            '0'                                                      AS StartLoadStatus,
                                            '0'                                                      AS FinishLoadStatus,
                                            '0'                                                      AS DispatchStatus,
                                            '0'                                                      AS PlanStatus,
                                            tms_plan_group                                           AS Group,
                                            tms_plan_dock_no                                         AS Dock,
                                            tms_plan_load_no                                         AS Load,
                                            tms_plan_dc_code                                         AS DCCode,
                                            tms_plan_pre_cool                                        AS PlanPreCool,
                                            tms_plan_on_dock                                         AS PlanOnDock,
                                            tms_plan_start_load                                      AS PlanStartLoad,
                                            tms_plan_finish_load                                     AS PlanFinishLoad,
                                            tms_plan_dispatch                                        AS PlanDispatch,
                                            '1'                                                      AS Flag,
                                            NOW()                                                    AS CreatedDate
                                        FROM {_schema}.tb_tms_plan
                                        WHERE tms_plan_action_date::DATE = CAST(NULLIF(@PlanActionDate, '') AS DATE)
                                        AND   tms_plan_group             = @PlanGroupNo
                                        AND   tms_plan_dc_code           = @DCCode
                                        AND   substring(trim(tms_plan_no), 1, 2) = @PlanTypeBigC
                                        GROUP BY
                                            tms_plan_no,          tms_plan_action_date, tms_plan_group,
                                            tms_plan_load_no,     tms_plan_dock_no,     tms_plan_dc_code,
                                            tms_plan_pre_cool,    tms_plan_on_dock,     tms_plan_start_load,
                                            tms_plan_finish_load, tms_plan_dispatch
                                        ORDER BY tms_plan_no";

                var summaryData = (await connection.QueryAsync<M_TMSPlan>(
                    sqlSelectSummary, first
                )).ToList();

                // STEP 3 : 
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sqlDeleteSummary = $@"
                                                DELETE FROM {_schema}.tb_tms_summary_plan
                                                WHERE tms_summary_plan_action_date = CAST(NULLIF(@PlanActionDate, '') AS DATE)
                                                AND   tms_summary_plan_group       = @PlanGroupNo
                                                AND   substring(trim(tms_summary_plan_no), 1, 2) = @PlanTypeBigC
                                                AND   tms_summary_dc_code          = @DCCode";

                        await connection.ExecuteAsync(sqlDeleteSummary, first, transaction);

                        var sqlInsertSummary = $@"
                                                INSERT INTO {_schema}.tb_tms_summary_plan (
                                                    tms_summary_plan_no,            tms_summary_plan_action_date,
                                                    tms_summary_plan_count_store,tms_summary_plan_actual_pre_cool_status,  
                                                    tms_summary_plan_actual_pre_load_status,
                                                    tms_summary_plan_actual_on_dock_status,
                                                    tms_summary_plan_actual_start_load_status,  tms_summary_plan_actual_finish_load_status,
                                                    tms_summary_plan_actual_dispatch_status,    tms_summary_plan_actual_status,
                                                    tms_summary_plan_group,         tms_summary_dock_no,
                                                    tms_summary_load_no,            tms_summary_dc_code,
                                                    tms_summary_plan_pre_cool,      tms_summary_plan_truck_on_dock,
                                                    tms_summary_plan_start_load,    tms_summary_plan_finish_load,
                                                    tms_summary_plan_dispatch,           
                                                    flag,                           created_date
                                                ) VALUES (
                                                    @PlanNo,                        
                                                    CAST(NULLIF(@PlanDate,'') AS TIMESTAMP),
                                                    @CountStore,
                                                    @PreCoolStatus,                 
                                                    @PreLoadStatus,
                                                    @TruckOnDockStatus,
                                                    @StartLoadStatus,               
                                                    @FinishLoadStatus,
                                                    @DispatchStatus,                
                                                    @PlanStatus,
                                                    @Group,                         
                                                    @Dock,
                                                    @Load,                          
                                                    @DCCode,
                                                    CAST(NULLIF(@PlanPreCool, '') AS TIMESTAMP),                   
                                                    CAST(NULLIF(@PlanOnDock, '') AS TIMESTAMP),
                                                    CAST(NULLIF(@PlanStartLoad, '') AS TIMESTAMP),                 
                                                    CAST(NULLIF(@PlanFinishLoad, '') AS TIMESTAMP),
                                                    CAST(NULLIF(@PlanDispatch, '') AS TIMESTAMP),  
                                                    @Flag,                          
                                                    now()
                                                )";

                        foreach (var row in summaryData)
                        {
                            await connection.ExecuteAsync(sqlInsertSummary, row, transaction);
                        }

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveTMSPlanAsync Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CancelTMSPlan(M_TMSPlan plans)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            try
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {

                    try
                    {
                        //Update WIP
                        var sqlWIP = $@" UPDATE {_schema}.tb_wip_current SET status = '3'
                                         WHERE barcode = (select truckondock_barcode from {_schema}.tb_tran_truckondock 
                                                          where truckondock_plan_no = @PlanNo limit 1)
                                         AND   dc_code = @DCCode";

                        await connection.ExecuteAsync(sqlWIP, plans, transaction);

                        //Delete Truck On Dock
                        var sqlDeletePlan = $@"delete from {_schema}.tb_tran_truckondock where truckondock_plan_no = @PlanNo";
                        await connection.ExecuteAsync(sqlDeletePlan, plans, transaction);

                        //Update Plan
                        var sqlUpdatePlan = $@"update {_schema}.tb_tms_plan set flag = '0',updated_by = @UpdateBy where tms_plan_no = @PlanNo and tms_plan_dc_code = @DCCode";
                        await connection.ExecuteAsync(sqlUpdatePlan, plans, transaction);

                        //Update Summaary
                        var sqlUpdateSummaary = $@"update {_schema}.tb_tms_summary_plan set flag = '0',updated_by = @UpdateBy where tms_summary_plan_no = @PlanNo and (tms_summary_dc_code = @DCCode OR tms_summary_dc_code IS NULL)";
                        await connection.ExecuteAsync(sqlUpdateSummaary, plans, transaction);

                        // ── Commit Step 1 ──
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel TMSPlan Error: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> InsertSummaryDashboardGate(M_TMSPlan plans)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            try
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        //Update WIP
                        var sql_Insert1 = $@" delete from  {_schema}.tb_monitor_summary_gate 
                                         where plan_action_date = CAST(NULLIF(@PlanActionDate, '') AS DATE)  
                                            and dc_code = @DCCode and bigc_type = @PlanTypeBigC and group_no = @Group";
                        await connection.ExecuteAsync(sql_Insert1, plans, transaction);

                        var sql_Insert2 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'1', '4W', 0, 0, 0, 0, 0, 0, 0, 0, @DCCode, @PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert2, plans, transaction);

                        var sql_Insert3 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'2', '6Ws', 0, 0, 0, 0, 0, 0, 0, 0, @DCCode,@PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert3, plans, transaction);

                        var sql_Insert4 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'3', '10W', 0, 0, 0, 0, 0, 0, 0, 0, @DCCode,@PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert4, plans, transaction);                       

                        var sql_Insert6 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'4', '18W', 0, 0, 0, 0, 0, 0, 0, 0, @DCCode,@PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert6, plans, transaction);
                        
                        var sql_Insert7 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'5', 'Dog-trailer', 0, 0, 0, 0, 0, 0, 0, 0,@DCCode,@PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert7, plans, transaction);
                        
                        var sql_Insert8 = $@"INSERT INTO {_schema}.tb_monitor_summary_gate
                                            (plan_action_date, truck_type_id, truck_type, truck_in_dc, truck_rsu, truck_precool, truck_ondock, truck_startload, truck_edp, truck_on_hand, truck_out_dc, dc_code, bigc_type,group_no,created_date)
                                            VALUES(@PlanDate,'6', 'Other', 0, 0, 0, 0, 0, 0, 0, 0, @DCCode,@PlanTypeBigC,@Group,now())";
                        await connection.ExecuteAsync(sql_Insert8, plans, transaction);
                       
                        // ── Commit Step 1 ──
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel TMSPlan Error: {ex.Message}");
                throw;
            }
        }
        private static string ValidateSchemaName(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty");

            if (!System.Text.RegularExpressions.Regex.IsMatch(schemaName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new ArgumentException($"Invalid schema name: '{schemaName}'");

            return schemaName;
        }
    }
}
