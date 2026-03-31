using Dapper;
using Npgsql;
using OutboundQMSModel;
using System.Text;

namespace QMSWebApiCore.Services
{
    public interface IDashboardQMSRepository
    {        
        Task<List<M_Dashboard>> GetTimeLine(string ActionDate, string Status, string BigCType, string PlanGroupNo, string DCCode);
        Task<List<M_Dashboard>> GetAllCountPlan(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getStackBarFinishLoadTime(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getStackBarFinish(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getStackBarRemain(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getPlanRemain(string ActionDate, string BigCType, string GroupNo);
        Task<List<M_Dashboard>> getDashboardChartFinishLoad(string ActionDate, string BigCType, string DCCode);


        //Count Plan of process status

        Task<List<M_Dashboard>> getCountPlanPreCool(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getCountPlanTruckOnDock(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getCountPlanStartLoad(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getCountPlanFinishLoad(string ActionDate, string BigCType, string GroupNo, string DCCode);
        Task<List<M_Dashboard>> getCountPlanEDP(string ActionDate, string BigCType, string GroupNo, string DCCode);

    }
    public class DashboardRepository: IDashboardQMSRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
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

        //private readonly string _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
        //    ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
        //private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        //    ?? throw new ArgumentException("Connection string is required");

        //private static string ValidateSchemaName(string schemaName)
        //{
        //    if (string.IsNullOrWhiteSpace(schemaName))
        //        throw new ArgumentException("Schema name cannot be null or empty");

        //    if (!System.Text.RegularExpressions.Regex.IsMatch(
        //        schemaName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        //        throw new ArgumentException($"Invalid schema name: '{schemaName}'");
        //    return schemaName;
        //}
        public async Task<List<M_Dashboard>> GetTimeLine(string ActionDate, string Status, string BigCType, string PlanGroupNo, string DCCode)
        {
            try
            {
                var sql = $@"select
	                            tms_summary_plan_no as PlanNo,
                                TO_CHAR(tms_summary_plan_action_date,  'DD/MM/YYYY HH24:MI') AS PlanDate,
	                            tms_summary_plan_group as PlanGroup,
	                            tms_summary_dock_no as PlanDock,
	                            tms_summary_load_no as Loadno,
	                            substring(tms_summary_plan_no, 1, 2) as PlanBigCType,
	                            gate_type_truck as TruckTypeID,
	                            gate_license as TruckLicense,
	                            gate_driver_name as DriverName,
	                            truck_type_name as TruckTypeText,
	                            tms.tms_plan_pre_cool as PlanPreCool,
	                            tms_summary_plan_actual_pre_cool as ActualPreCool,
	                            tms_summary_plan_actual_pre_cool_status as PreCoolStatus,
	                            tms.tms_plan_on_dock as PlanTruckOnDock,
	                            tms_summary_plan_actual_on_dock as ActualTruckOnDock,
	                            tms_summary_plan_actual_on_dock_status as ActualTruckOnDockStatus,
	                            tms.tms_plan_start_load as PlanStartLoad,
	                            tms_summary_plan_actual_start_load as StartLoad,
	                            tms_summary_plan_actual_start_load_status as StartLoadStatus,
	                            tms.tms_plan_finish_load as PlanFinishLoad,
	                            tms_summary_plan_actual_finish_load as FinishLoad,
	                            tms_summary_plan_actual_finish_load_status as FinishLoadStatus,
	                            tms.tms_plan_dispatch as PlanEDPOut,
	                            tms_summary_plan_actual_edp_in as EDPIn,
	                            tms_summary_plan_actual_edp_in_status as EDPInStatus,
	                            tms_summary_plan_actual_edp_out as EDPOut,
	                            tms_summary_plan_actual_edp_out_status as EDPOutStatus
                            from
	                            {_schema}.tb_tms_summary_plan
                            inner join (
	                            select
		                            tms_plan_no,
		                            tms_plan_action_date ,
		                            tms_plan_pre_cool,
		                            tms_plan_on_dock,
		                            tms_plan_start_load,
		                            tms_plan_finish_load ,
		                            tms_plan_dispatch
	                            from
		                            {_schema}.tb_tms_plan
	                            group by
		                            tms_plan_no,
		                            tms_plan_action_date ,
		                            tms_plan_pre_cool,
		                            tms_plan_on_dock,
		                            tms_plan_start_load,
		                            tms_plan_finish_load ,
		                            tms_plan_dispatch) tms 
                             on
	                            tms.tms_plan_no = tb_tms_summary_plan.tms_summary_plan_no
	                            and tms.tms_plan_action_date::Date = current_date
                            left join (
	                            select
		                            gate_barcode,
		                            gate_license,
		                            gate_driver_name,
		                            gate_type_truck,
		                            trucktype.truck_type_name,
		                            truckondock.truckondock_plan_no
	                            from
		                            {_schema}.tb_tran_gate gatein
	                            left join {_schema}.tb_tran_truckondock truckondock 
                             on
		                            gate_action_date::Date = current_date
		                            and gatein.gate_barcode = truckondock.truckondock_barcode
	                            inner join {_schema}.tb_truck_type trucktype  
                             on
		                            trucktype.truck_type_id = gate_type_truck
	                            where
		                            gatein.gate_action_date::Date = current_date
	                            group by
		                            gate_barcode,
		                            gate_license,
		                            gate_driver_name,
		                            gate_type_truck,
		                            trucktype.truck_type_name,
		                            truckondock.truckondock_plan_no) gate 
                             on gate.truckondock_plan_no  = tb_tms_summary_plan.tms_summary_plan_no
                            where
	                            tms_summary_plan_action_date::Date = current_date";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new { Barcode = ActionDate, DCCode = DCCode });
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
        public async Task<List<M_Dashboard>> GetDashboard(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT gate_barcode as Barcode
                                                ,gate_license as License
                                                ,gate_driver_name as DriverName
                                                ,trucktype.truck_type_name as TruckTypeName
                                                ,to_char(gate.gate_truck_in,'DD/MM/YYYY HH24:MI') as GateInDate
                                                ,to_char(gate.gate_truck_out,'DD/MM/YYYY HH24:MI') as GateOutDate
                                                ,gate.gate_out_remark as Remark
                                                ,rsu.rsu_status_text as RSUStatus 
                                                ,gate.gate_out_lps_name as LPSName
                                                FROM {_schema}.tb_tran_gate gate
                                                INNER JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck 
                                                INNER JOIN {_schema}.tb_rsu_status rsu on rsu.rsu_status_id  = gate.gate_rsu_id 
                                                WHERE gate.gate_dc_code = @DCCode
                                                AND gate.gate_status = '2' ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);
                sql.AppendLine("order by Gate.gate_truck_out desc");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql.ToString(),parameters);
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
        public async Task<List<M_Dashboard>> GetAllCountPlan(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                ActionDate = ActionDate ?? string.Empty;
                BigCType = BigCType ?? string.Empty;
                GroupNo = GroupNo ?? string.Empty;
                DCCode = DCCode ?? string.Empty;

                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                Console.WriteLine($"[DashboardRepository] GetAllCountPlan Parameters: ActionDate={ActionDate}, Type={BigCType}, Group={GroupNo}, DC={DCCode}, Prefix={typePrefix}");

                var sql = $@" SELECT 
                                    COALESCE(Total, 0)::text AS PlanCount,
                                    COALESCE(FINISH, 0)::text AS PlanFinish,
                                    (COALESCE(Total, 0) - COALESCE(FINISH, 0))::text AS PlanRemain
                                FROM (
                                    SELECT 
                                        (
                                            SELECT COUNT(tms_summary_plan_no)
                                            FROM {_schema}.tb_tms_summary_plan 
                                            WHERE tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            AND TRIM(tms_summary_plan_no) LIKE @TypePrefix || '%'
                                            AND tms_summary_plan_group = @GroupNo
                                            AND flag = '1'
                                            AND tms_summary_dc_code = @DCCode
                                        ) AS Total,
                                        (
                                            SELECT COUNT(tms_summary_plan_no)
                                            FROM {_schema}.tb_tms_summary_plan 
                                            WHERE tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            AND TRIM(tms_summary_plan_no) LIKE @TypePrefix || '%'
                                            AND tms_summary_plan_group = @GroupNo
                                            AND COALESCE(tms_summary_plan_actual_finish_load_status, '0') = '1'
                                            AND flag = '1'
                                            AND tms_summary_dc_code = @DCCode
                                        ) AS FINISH
                                ) AS summaryData";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    var list = result.ToList();
                    Console.WriteLine($"[DashboardRepository] GetAllCountPlan Result Count: {list.Count}");
                    if (list.Any())
                    {
                        Console.WriteLine($"[DashboardRepository] Results - TP: {list[0].PlanCount}, FP: {list[0].PlanFinish}, RP: {list[0].PlanRemain}");
                    }
                    return list;
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
        public async Task<List<M_Dashboard>> getStackBarFinishLoadTime(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                ActionDate = ActionDate ?? string.Empty;
                BigCType = BigCType ?? string.Empty;
                GroupNo = GroupNo ?? string.Empty;
                DCCode = DCCode ?? string.Empty;

                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                Console.WriteLine($"[DashboardRepository] getStackBarFinishLoadTime Parameters: ActionDate={ActionDate}, Type={BigCType}, Group={GroupNo}, DC={DCCode}, Prefix={typePrefix}");

                var sql = $@"select  TO_CHAR(plan.tms_plan_finish_load  , 'YYYY-MM-DD HH24:mi') PlanFinishLoadTime ,
                                     TO_CHAR(plan.tms_plan_finish_load  , 'DD/MM/YYYY HH24:mi') PlanFinishLoad 
                                     from {_schema}.tb_tms_plan plan  
                                     where plan.tms_plan_action_date::DATE = @ActionDate::DATE
                                     and TRIM(plan.tms_plan_no) like @TypePrefix || '%' 
                                     and plan.tms_plan_dc_code = @DCCode
                                     and flag = '1' 
                                     group by plan.tms_plan_finish_load 
                                     order by plan.tms_plan_finish_load";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new 
                    { 
                        ActionDate = ActionDate, 
                        TypePrefix = typePrefix, 
                        DCCode = DCCode 
                    });
                    var list = result.ToList();
                    Console.WriteLine($"[DashboardRepository] getStackBarFinishLoadTime Result Count: {list.Count}");
                    return list;
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
        public async Task<List<M_Dashboard>> getStackBarFinish(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                Console.WriteLine($"[DashboardRepository] getStackBarFinish Parameters: ActionDate={ActionDate}, Type={BigCType}, Group={GroupNo}, DC={DCCode}, Prefix={typePrefix}");

                var sql = $@"SELECT TO_CHAR(plan.tms_plan_finish_load, 'DD/MM/YYYY HH24:mi') AS PlanFinishLoad,
                                    COUNT(summary.tms_summary_plan_no) AS PlanFinish
                             FROM {_schema}.tb_tms_summary_plan summary
                             JOIN {_schema}.tb_tms_plan plan ON TRIM(summary.tms_summary_plan_no) = TRIM(plan.tms_plan_no)
                                AND summary.tms_summary_plan_action_date::DATE = plan.tms_plan_action_date::DATE
                             WHERE plan.tms_plan_action_date::DATE = @ActionDate::DATE
                               AND TRIM(plan.tms_plan_no) LIKE @TypePrefix || '%'
                               AND plan.tms_plan_dc_code = @DCCode
                               AND summary.tms_summary_plan_group = @GroupNo
                               AND summary.flag = '1'
                               AND plan.flag = '1'
                               AND COALESCE(summary.tms_summary_plan_actual_finish_load_status, '0') = '1'
                             GROUP BY plan.tms_plan_finish_load
                             ORDER BY plan.tms_plan_finish_load";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new 
                    { 
                        ActionDate = ActionDate, 
                        TypePrefix = typePrefix, 
                        DCCode = DCCode,
                        GroupNo = GroupNo
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getStackBarFinish: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_Dashboard>> getStackBarRemain(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                Console.WriteLine($"[DashboardRepository] getStackBarRemain Parameters: ActionDate={ActionDate}, Type={BigCType}, Group={GroupNo}, DC={DCCode}, Prefix={typePrefix}");

                var sql = $@"SELECT TO_CHAR(plan.tms_plan_finish_load, 'DD/MM/YYYY HH24:mi') AS PlanFinishLoad,
                                    COUNT(summary.tms_summary_plan_no) AS PlanRemain
                             FROM {_schema}.tb_tms_summary_plan summary
                             JOIN {_schema}.tb_tms_plan plan ON TRIM(summary.tms_summary_plan_no) = TRIM(plan.tms_plan_no)
                                AND summary.tms_summary_plan_action_date::DATE = plan.tms_plan_action_date::DATE
                             WHERE plan.tms_plan_action_date::DATE = @ActionDate::DATE
                               AND TRIM(plan.tms_plan_no) LIKE @TypePrefix || '%'
                               AND plan.tms_plan_dc_code = @DCCode
                               AND summary.tms_summary_plan_group = @GroupNo
                               AND summary.flag = '1'
                               AND plan.flag = '1'
                               AND COALESCE(summary.tms_summary_plan_actual_finish_load_status, '0') = '0'
                             GROUP BY plan.tms_plan_finish_load
                             ORDER BY plan.tms_plan_finish_load";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new 
                    { 
                        ActionDate = ActionDate, 
                        TypePrefix = typePrefix, 
                        DCCode = DCCode,
                        GroupNo = GroupNo
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DashboardRepository] getStackBarRemain Error: {ex.Message}");
                throw;
            }
        }
        //All plan count by BigCType for show on dashboard
        public async Task<List<M_Dashboard>> getCountPlan(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                var sql = $@" select count(tms_summary_plan_no)
                                                from {_schema}.tb_tms_summary_plan where tms_summary_plan_action_date::Date = @ActionDate
                                                and tb_tms_summary_plan.tms_summary_plan_no like @BigCType
                                                and tb_tms_summary_plan.tms_summary_plan_group = @GroupNo
                                                and tb_tms_summary_plan.flag = '1'
                                                and tb_tms_summary_plan.tms_summary_dc_code = @DCCode";


                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new { ActionDate = ActionDate, BigCType = BigCType, GroupNo = GroupNo, DCCode = DCCode });
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
        //Finish plan count by BigCType for show on dashboard
        public async Task<List<M_Dashboard>> getCountPlanFinish(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                var sql = $@" select count(loadontruck.loadontruck_plan_no)
                                                from {_schema}.tb_tran_loadontruck loadontruck
                                                where loadontruck.loadontruck_action_date = '{ActionDate}'
                                                and loadontruck.loadontruck_plan_no like '{BigCType}{GroupNo}%'
                                                and loadontruck.loadontruck_status = '2'
                                                and loadontruck.loadontruck_dc_code = '{DCCode}'";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new { ActionDate = ActionDate, BigCType = BigCType, DCCode = DCCode });
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
        public async Task<List<M_Dashboard>> getPlanRemain(string ActionDate, string BigCType, string GroupNo)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@"SELECT tms_summary_plan_no as PlanNo,
                                    tms_summary_plan_group as GroupNo,
                                    tms_summary_load_no as LoadNo,
                                    tms_summary_dock_no as DockNo
                             FROM {_schema}.tb_tms_summary_plan 
                             WHERE tms_summary_plan_action_date::DATE = @ActionDate::DATE
                               AND TRIM(tms_summary_plan_no) LIKE @TypePrefix || '%'
                               AND tms_summary_plan_group = @GroupNo
                               AND CAST(COALESCE(tms_summary_plan_actual_edp_out_status, '0') AS integer) = 0 
                               AND flag = '1'
                             ORDER BY tms_summary_plan_no ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new 
                    { 
                        ActionDate = ActionDate, 
                        TypePrefix = typePrefix, 
                        GroupNo = GroupNo 
                    });
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
        public async Task<List<M_Dashboard>> getDashboardChartFinishLoad(string ActionDate, string BigCType, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                // Returns total remain and total success for the pie chart
                var sql = $@"SELECT 
                                (SELECT COUNT(*) FROM {_schema}.tb_tms_summary_plan 
                                 WHERE tms_summary_plan_action_date::DATE = @ActionDate::DATE 
                                 AND tms_summary_plan_no LIKE @TypePrefix || '%'
                                 AND COALESCE(tms_summary_plan_actual_finish_load_status, '0') = '0'
                                 AND flag = '1' AND tms_summary_dc_code = @DCCode) AS PlanRemain,
                                (SELECT COUNT(*) FROM {_schema}.tb_tms_summary_plan 
                                 WHERE tms_summary_plan_action_date::DATE = @ActionDate::DATE 
                                 AND tms_summary_plan_no LIKE @TypePrefix || '%'
                                 AND COALESCE(tms_summary_plan_actual_finish_load_status, '0') = '1'
                                 AND flag = '1' AND tms_summary_dc_code = @DCCode) AS PlanFinish";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Dashboard>(sql, new { ActionDate = ActionDate, TypePrefix = typePrefix, DCCode = DCCode });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getDashboardChartFinishLoad: {ex.Message}");
                throw;
            }
        }
        //>> Get status process to show on hyper, mini page.
        public async Task<List<M_Dashboard>> getCountPlanPreCool(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@" SELECT 
                                    REMAIN::text as PlanRemain,
                                    FINISH::text as PlanFinish,
                                    (REMAIN + FINISH)::text as PlanCount
                                FROM (
                                    SELECT 
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_pre_cool_status,'0') = '0' and flag = '1'
                                            and tms_summary_dc_code = @DCCode
                                        ) AS REMAIN,
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_pre_cool_status,'0') = '1' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS FINISH
                                ) AS summary  ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getCountPlanPreCool: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_Dashboard>> getCountPlanTruckOnDock(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@" SELECT 
                                    REMAIN::text as PlanRemain,
                                    FINISH::text as PlanFinish,
                                    (REMAIN + FINISH)::text as PlanCount
                                FROM (
                                    SELECT 
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_on_dock_status,'0') = '0' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS REMAIN,
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_on_dock_status,'0') = '1' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS FINISH
                                ) AS summary ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getCountPlanTruckOnDock: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_Dashboard>> getCountPlanStartLoad(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@"  SELECT 
                                    REMAIN::text as PlanRemain,
                                    FINISH::text as PlanFinish,
                                    (REMAIN + FINISH)::text as PlanCount
                                FROM (
                                    SELECT 
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_start_load_status,'0') = '0' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS REMAIN,
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_start_load_status,'0') = '1' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS FINISH
                                ) AS summary ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getCountPlanStartLoad: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_Dashboard>> getCountPlanFinishLoad(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@"  SELECT 
                                    REMAIN::text as PlanRemain,
                                    FINISH::text as PlanFinish,
                                    (REMAIN + FINISH)::text as PlanCount
                                FROM (
                                    SELECT 
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_finish_load_status,'0') = '0' and flag = '1' 
                                            and tms_summary_dc_code  = @DCCode
                                        ) AS REMAIN,
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_finish_load_status,'0') = '1' and flag = '1' 
                                            and tms_summary_dc_code  = @DCCode
                                        ) AS FINISH
                                ) AS summary";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getCountPlanFinishLoad: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_Dashboard>> getCountPlanEDP(string ActionDate, string BigCType, string GroupNo, string DCCode)
        {
            try
            {
                string typePrefix = BigCType.ToUpper().StartsWith("HY") ? "PH" : "PM";
                var sql = $@"  SELECT 
                                    REMAIN::text as PlanRemain,
                                    FINISH::text as PlanFinish,
                                    (REMAIN + FINISH)::text as PlanCount
                                FROM (
                                    SELECT (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_edp_out_status,'0') = '0' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS REMAIN,
                                        (
                                            select count(tms_summary_plan_no)
                                            from {_schema}.tb_tms_summary_plan 
                                            where tms_summary_plan_action_date::DATE = @ActionDate::DATE
                                            and TRIM(tms_summary_plan_no) like @TypePrefix || '%'
                                            and tms_summary_plan_group = @GroupNo
                                            and coalesce(tms_summary_plan_actual_edp_out_status,'0') = '1' and flag = '1' 
                                            and tms_summary_dc_code = @DCCode
                                        ) AS FINISH
                                ) AS summary ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new { ActionDate = ActionDate, TypePrefix = typePrefix, GroupNo = GroupNo, DCCode = DCCode };
                    var result = await connection.QueryAsync<M_Dashboard>(sql, parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in getCountPlanEDP: {ex.Message}");
                throw;
            }
        }
    }
}
