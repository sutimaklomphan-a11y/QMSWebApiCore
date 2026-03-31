using Dapper;
using Npgsql;
using OutboundQMSModel;
using QMSWebApiCore.Models;
using System.Text;

namespace QMSWebApiCore.Services
{
    public interface IReportQMSRepository
    {
        Task<List<M_ReportGateIn>> GetReportGateIn(string DCCode, string? startDate = null, string? endDate = null);
        Task<List<M_ReportGateOut>> GetReportGateOut(string DCCode, string? startDate = null, string? endDate = null);
        Task<List<M_ReportRSUIn>> GetReportRSUIn(string DCCode,  string? startDate = null, string? endDate = null);
        Task<List<M_ReportRSUIn>> GetReportRSUOut(string DCCode, string? startDate = null, string? endDate = null);
        Task<List<M_ReportPreCool>> GetReportPrecool(string DCCode, string? startDate = null, string? endDate = null);
        Task<List<M_ReportPreCool>> GetReportPreCoolShowStore(string DCCode, string Status, string? startDate = null, string? endDate = null);
        Task<List<M_ReportTruckOnDock>> GetReportTruckOnDock(string DCCode,string? startDate = null, string? endDate = null);
        Task<List<M_ReportPreLoad>> GetReportPreLoad(string DCCode, string Status, string? startDate = null, string? endDate = null);
        Task<List<M_ReportLoad>> GetReportLoad(string DCCode, string Status, string? startDate = null, string? endDate = null);
        Task<List<M_ReportEDP>> GetReportEDP(string DCCode, string? startDate = null, string? endDate = null);
        Task<List<M_ReportSummary>> GetReportSummary(string DCCode, string? startDate = null, string? endDate = null);
    }
    public class ReportQMSRepository : IReportQMSRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public ReportQMSRepository(IConfiguration configuration)
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
        public async Task<List<M_ReportGateIn>> GetReportGateIn(string DCCode, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT gate_barcode 
                                                ,gate_license
                                                ,gate_driver_name
                                                ,trucktype.truck_type_name 
                                                ,to_char(gate.gate_truck_in,'DD/MM/YYYY HH24:MI') as GateIn
                                                ,gate.gate_in_remark 
                                                ,gate.gate_rsu_id 
                                                ,rsu.rsu_status_text 
                                                FROM {_schema}.tb_tran_gate gate
                                                INNER JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck 
                                                INNER JOIN {_schema}.tb_rsu_status rsu on rsu.rsu_status_id  = gate.gate_rsu_id 
                                                where gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("gate.gate_truck_in between '@startDate 00:00:00' and '@endDate 23:59:59' ");
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine("AND gate.gate_truck_in::DATE >= @startDate::DATE");
                    parameters.Add("startDate", startDate);
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND gate.gate_truck_in::DATE <= @endDate::DATE");
                    parameters.Add("endDate", endDate);
                }
                else
                {
                    sql.AppendLine("AND GATE.gate_action_date::DATE = CURRENT_DATE");
                }

                sql.AppendLine("order by Gate.gate_truck_in desc");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportGateIn>(
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
        public async Task<List<M_ReportGateOut>> GetReportGateOut(string DCCode, string? startDate = null, string? endDate = null)
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

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out BETWEEN '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND gate.gate_truck_out::DATE = CURRENT_DATE");
                }

                sql.AppendLine("order by Gate.gate_truck_out desc");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportGateOut>(
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
        public async Task<List<M_ReportRSUIn>> GetReportRSUIn(string DCCode,  string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT gate_barcode as Barcode
                                                ,gate_license as License
                                                ,gate_driver_name as DriverName
                                                ,trucktype.truck_type_name as TruckTypeName
                                                ,to_char(gate.gate_truck_in,'DD/MM/YYYY HH24:MI') as RSUInDate
                                                ,to_char(gate.gate_truck_out,'DD/MM/YYYY HH24:MI') as RSUOutDate
                                                ,gate.gate_out_remark as Remark
                                                ,rsu.rsu_status_text as RSUStatus
                                                FROM {_schema}.tb_tran_gate gate
                                                LEFT JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck 
                                                LEFT JOIN {_schema}.tb_rsu_status rsu on rsu.rsu_status_id  = gate.gate_rsu_id 
                                                WHERE gate.gate_dc_code = @DCCode
                                                AND gate.gate_status = '2' ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out BETWEEN '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND gate.gate_truck_out::DATE = CURRENT_DATE");
                }

                sql.AppendLine("ORDER BY gate.gate_truck_out DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportRSUIn>(
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
        public async Task<List<M_ReportRSUIn>> GetReportRSUOut(string DCCode,  string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT
	                                                rsu.rsu_barcode as Barcode,
	                                                gate_license as License,
	                                                gate_driver_name as DriverName,
	                                                trucktype.truck_type_name as TruckTypeName,  
                                                    rsustatus.rsu_status_text as RSUStatus,
	                                                to_char(rsu.created_date , 'DD/MM/YYYY HH24:MI') as RSUInDate, 
	                                                to_char(rsu.rsu_out_date , 'DD/MM/YYYY HH24:MI') as RSUOutDate,
	                                                rsu.rsu_in_remark as Remark 
                                                FROM {_schema}.tb_tran_rsu rsu
                                                LEFT JOIN {_schema}.tb_tran_gate gate on rsu.rsu_barcode = gate.gate_barcode 
                                                LEFT JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck
                                                LEFT JOIN {_schema}.tb_rsu_status rsustatus on rsustatus.rsu_status_id = gate.gate_rsu_id
                                                WHERE gate.gate_dc_code = @DCCode
	                                                AND rsu.rsu_out_date is not null ");
                                                
                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out BETWEEN '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND gate.gate_truck_out <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND gate.gate_truck_out::DATE = CURRENT_DATE");
                }

                sql.AppendLine("ORDER BY rsu.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportRSUIn>(
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
        public async Task<List<M_ReportPreCool>> GetReportPrecool(string DCCode, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT
	                                            to_char(precool.precool_action_date, 'DD/MM/YYYY HH24:MI') as PreCoolActionTime,
	                                            precool.precool_qc_id as QCID,
	                                            precool.precool_temperature_truck as TempTruck,
	                                            precool.precool_temperature_container as TempContainer,
                                                case when substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PH' then 'Hyper' else 'Mini' end as BigCType,
                                                summaryplan.tms_summary_plan_group as ""Group"",
                                                truckondock.truckondock_barcode as Barcode,
	                                            precool.precool_load_no as LoadNo,
	                                            precool.precool_dock_no as DockNo,
	                                            precool.precool_licens_truck as LicenseTruck,
	                                            precool.precool_licens_container as LicenseContainer,
                                                gate.gate_driver_name as DriverName,
                                                trucktype.truck_type_name as TruckTypeName,
	                                            precool.precool_status as PreCoolStatus,
	                                            precool.precool_remark as Remark,
                                                plan.tms_plan_store_code as StoreCode,
                                                plan.tms_plan_store_name as StoreName
                                            FROM
	                                            {_schema}.tb_tran_precool precool
                                            LEFT JOIN {_schema}.tb_precool_status status on
	                                            cast(coalesce(precool.precool_status, '0') as integer) = status.precool_status_id
                                            LEFT JOIN {_schema}.tb_tran_truckondock truckondock on
	                                            truckondock.truckondock_plan_no = precool.precool_plan_no
                                            INNER JOIN {_schema}.tb_tran_gate gate on
	                                            gate.gate_id = truckondock.gate_id::integer
                                            LEFT JOIN {_schema}.tb_tms_summary_plan summaryplan on summaryplan.tms_summary_plan_no = precool.precool_plan_no
                                            LEFT JOIN {_schema}.tb_tms_plan plan on plan.tms_plan_no = precool.precool_plan_no
                                            LEFT JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck
                                            where gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date between '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND precool.precool_action_date::DATE = CURRENT_DATE");
                }

                sql.AppendLine(" order by precool.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportPreCool>(
                        sql.ToString(),
                        parameters
                    );
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve precool report records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        
        public async Task<List<M_ReportPreCool>> GetReportPreCoolShowStore(string DCCode,string Status, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT
	                                            to_char(precool.precool_action_date, 'DD/MM/YYYY HH24:MI') as PreCoolActionTime,
	                                            precool.precool_qc_id as QCID,
	                                            precool.precool_temperature_truck as TempTruck,
	                                            precool.precool_temperature_container as TempContainer,
                                                case when substring(trim(summaryplan.tms_summary_plan_no), 1, 2) = 'PH' then 'Hyper' else 'Mini' end as BigCType,
                                                summaryplan.tms_summary_plan_group as ""Group"",
                                                truckondock.truckondock_barcode as Barcode,
	                                            precool.precool_load_no as LoadNo,
	                                            precool.precool_dock_no as DockNo,
	                                            precool.precool_licens_truck as LicenseTruck,
	                                            precool.precool_licens_container as LicenseContainer,
                                                gate.gate_driver_name as DriverName,
                                                trucktype.truck_type_name as TruckTypeName,
	                                            precool.precool_status as PreCoolStatus,
	                                            precool.precool_remark as Remark,
                                                plan.tms_plan_store_code as StoreCode,
                                                plan.tms_plan_store_name as StoreName
                                            FROM
	                                            {_schema}.tb_tran_precool precool
                                            LEFT JOIN {_schema}.tb_precool_status status on
	                                            cast(coalesce(precool.precool_status, '0') as integer) = status.precool_status_id
                                            LEFT JOIN {_schema}.tb_tran_truckondock truckondock on
	                                            truckondock.truckondock_plan_no = precool.precool_plan_no
                                            INNER JOIN {_schema}.tb_tran_gate gate on
	                                            gate.gate_id = truckondock.gate_id::integer
                                            LEFT JOIN {_schema}.tb_tms_summary_plan summaryplan on summaryplan.tms_summary_plan_no = precool.precool_plan_no
                                            LEFT JOIN {_schema}.tb_tms_plan plan on plan.tms_plan_no = precool.precool_plan_no
                                            LEFT JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck
                                            where gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date between '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND precool.precool_action_date <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND precool.precool_action_date::DATE = CURRENT_DATE");
                }

                if (!string.IsNullOrEmpty(Status) && Status != "All")
                {
                    if (Status == "Pass")
                    {
                        sql.AppendLine("and precool.precool_status = '0'");
                    }
                    else
                    {
                        sql.AppendLine("and precool.precool_status = '0'");
                    }
                    sql.AppendLine("AND gate.gate_status = @Status");
                    parameters.Add("Status", Status);
                }
                sql.AppendLine(" order by precool.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportPreCool>(
                        sql.ToString(),
                        parameters
                    );
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve precool report show store records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_ReportTruckOnDock>> GetReportTruckOnDock(string DCCode, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT
	                                                to_char(truckondock.created_date, 'DD/MM/YYYY HH24:MI') as TruckOnDockTime ,
	                                                truckondock.truckondock_plan_no as PlanNo ,
	                                                CONCAT (SUBSTRING (truckondock.truckondock_plan_no,11,2),'-',
			                                                SUBSTRING (truckondock.truckondock_plan_no,9,2),'-',
			                                                SUBSTRING (truckondock.truckondock_plan_no,5,4)) as PlanActionDate ,
	                                                truckondock.truckondock_barcode as Barcode  ,
	                                                truckondock_load_no as LoadNo ,
	                                                summary.tms_summary_dock_no as DockNo ,
	                                                summary.tms_summary_plan_group as Group ,
                                                    'Big C' as BigCType,
	                                                gate.gate_license as License ,
	                                                trucktype.truck_type_name as TruckTypeName ,
	                                                gate.gate_driver_name as DriverName ,
	                                                truckondock.truckondock_status as TruckOnDockStatus ,
	                                                truckondock.truckondock_remark as Remark
                                                FROM
	                                                {_schema}.tb_tran_truckondock truckondock
                                                INNER JOIN {_schema}.tb_tms_summary_plan summary on
	                                                summary.tms_summary_plan_no = truckondock_plan_no
                                                INNER JOIN {_schema}.tb_tran_gate gate on
	                                                gate.gate_id = cast (truckondock.gate_id as INTEGER)
                                                INNER JOIN {_schema}.tb_truck_type trucktype on
	                                                trucktype.truck_type_id = gate.gate_type_truck
                                                WHERE gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND truckondock.created_date BETWEEN '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND truckondock.created_date >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND truckondock.created_date <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND truckondock.created_date::DATE = CURRENT_DATE");
                }

                sql.AppendLine("ORDER BY truckondock.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportTruckOnDock>(
                        sql.ToString(),
                        parameters
                    );
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve truck on dock records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_ReportPreLoad>> GetReportPreLoad(string DCCode, string Status, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"SELECT
                                                    to_char(preload.created_date, 'DD/MM/YYYY HH24:MI') as PreLoadActionTime,
                                                    to_char(preload.preload_finish_date, 'DD/MM/YYYY HH24:MI') as FinishPreLoadActionTime,
                                                    truckondock.truckondock_barcode as Barcode,
                                                    case when substring(trim(summary.tms_summary_plan_no), 1, 2) = 'PH' then 'Hyper' else 'Mini' end as BigCType,
                                                    summary.tms_summary_plan_group as ""Group"",
                                                    summary.tms_summary_load_no as LoadNo,
                                                    summary.tms_summary_dock_no as DockNo,
                                                    gate.gate_license as LicenseTruck,
                                                    trucktype.truck_type_name as TruckTypeName,
                                                    preload.preload_employee_1 as Loader,
                                                    preload.preload_status as PreLoadStatus,
                                                    preload.preload_remark as Remark,
                                                    preload.preload_finish_remark as FinishPreloadRemark
                                                FROM
                                                    {_schema}.tb_tran_preload preload
                                                INNER JOIN {_schema}.tb_tms_summary_plan summary on
                                                    summary.tms_summary_plan_no = preload.preload_plan_no
                                                LEFT JOIN {_schema}.tb_tran_truckondock truckondock on
                                                    truckondock.truckondock_plan_no = preload.preload_plan_no
                                                LEFT JOIN {_schema}.tb_tran_gate gate on
                                                    gate.gate_id = truckondock.gate_id::integer
                                                LEFT JOIN {_schema}.tb_truck_type trucktype on
                                                    trucktype.truck_type_id = gate.gate_type_truck
                                                WHERE gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND preload.created_date BETWEEN @StartDate AND @EndDate");
                    parameters.Add("StartDate", DateTime.Parse(startDate).Date);
                    parameters.Add("EndDate", DateTime.Parse(endDate).Date.AddDays(1).AddTicks(-1));
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine("AND preload.created_date >= @StartDate");
                    parameters.Add("StartDate", DateTime.Parse(startDate).Date);
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND preload.created_date <= @EndDate");
                    parameters.Add("EndDate", DateTime.Parse(endDate).Date.AddDays(1).AddTicks(-1));
                }
                else
                {
                    sql.AppendLine("AND preload.created_date::DATE = CURRENT_DATE");
                }

                if (!string.IsNullOrEmpty(Status) && Status != "All")
                {
                    sql.AppendLine("AND preload.preload_status = @Status");
                    parameters.Add("Status", Status);
                }

                sql.AppendLine("ORDER BY preload.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportPreLoad>(
                        sql.ToString(),
                        parameters
                    );
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve preload report records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_ReportLoad>> GetReportLoad(string DCCode, string Status, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"SELECT
                                                    to_char(loadintruck.created_date, 'DD/MM/YYYY HH24:MI') as LoadActionTime,
                                                    to_char(loadintruck.loadontruck_finish_date, 'DD/MM/YYYY HH24:MI') as FinishLoadActionTime,
                                                    truckondock.truckondock_barcode as Barcode,
                                                    case when substring(trim(summary.tms_summary_plan_no), 1, 2) = 'PH' then 'Hyper' else 'Mini' end as BigCType,
                                                    summary.tms_summary_plan_group as ""Group"",
                                                    summary.tms_summary_load_no as LoadNo,
                                                    summary.tms_summary_dock_no as DockNo,
                                                    gate.gate_license as LicenseTruck,
                                                    trucktype.truck_type_name as TruckTypeName,
                                                    loadintruck.loadontruck_employee_1 as Loader,
                                                    loadintruck.loadontruck_status as LoadStatus,
                                                    loadintruck.loadontruck_remark as Remark,
                                                    loadintruck.loadontruck_finish_remark as FinishLoadRemark
                                                FROM
                                                    {_schema}.tb_tran_loadontruck loadintruck
                                                INNER JOIN {_schema}.tb_tms_summary_plan summary on
                                                    summary.tms_summary_plan_no = loadintruck.loadontruck_plan_no
                                                LEFT JOIN {_schema}.tb_tran_truckondock truckondock on
                                                    truckondock.truckondock_plan_no = loadintruck.loadontruck_plan_no
                                                LEFT JOIN {_schema}.tb_tran_gate gate on
                                                    gate.gate_id = truckondock.gate_id::integer
                                                LEFT JOIN {_schema}.tb_truck_type trucktype on
                                                    trucktype.truck_type_id = gate.gate_type_truck
                                                WHERE gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND loadintruck.created_date BETWEEN @StartDate AND @EndDate");
                    parameters.Add("StartDate", DateTime.Parse(startDate).Date);
                    parameters.Add("EndDate", DateTime.Parse(endDate).Date.AddDays(1).AddTicks(-1));
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine("AND loadintruck.created_date >= @StartDate");
                    parameters.Add("StartDate", DateTime.Parse(startDate).Date);
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND loadintruck.created_date <= @EndDate");
                    parameters.Add("EndDate", DateTime.Parse(endDate).Date.AddDays(1).AddTicks(-1));
                }
                else
                {
                    sql.AppendLine("AND loadintruck.created_date::DATE = CURRENT_DATE");
                }

                if (!string.IsNullOrEmpty(Status) && Status != "All")
                {
                    sql.AppendLine("AND loadintruck.loadontruck_status = @Status");
                    parameters.Add("Status", Status);
                }

                sql.AppendLine("ORDER BY loadintruck.created_date DESC");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportLoad>(
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
        public async Task<List<M_ReportEDP>> GetReportEDP(string DCCode, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT edp.edp_plan_no as PlanNo
                                                        ,edp.edp_barcode as Barcode
                                                        ,gate.gate_license  as LicenseTruck
                                                        ,gate.gate_type_truck as TruckTypeID
                                                        ,trucktype.truck_type_name as TruckTypeName
                                                        ,summary.tms_summary_load_no as LoadNo
                                                        ,summary.tms_summary_dock_no as DockNo
                                                        ,summary.tms_summary_plan_group as GroupNo
                                                        ,to_char(edp.edp_in_date,'DD/MM/YYYY HH24:MI') as EDPInTime
                                                        ,edp.edp_in_remark as EDPInRemark
                                                        ,to_char(edp.edp_out_date,'DD/MM/YYYY HH24:MI') as EDPOutTime
                                                        ,edp.edp_out_remark as EDPOutRemark
                                                FROM {_schema}.tb_tran_edp edp
                                                INNER JOIN {_schema}.tb_tms_summary_plan summary on summary.tms_summary_plan_no = edp.edp_plan_no 
                                                INNER JOIN {_schema}.tb_tran_gate gate on gate.gate_id = cast(edp.edp_gate_id as integer)
                                                INNER JOIN {_schema}.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck 
                                                where edp_in_date between {startDate} and {endDate} order by edp_in_date ");

                var parameters = new DynamicParameters();
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportEDP>(
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
        public async Task<List<M_ReportSummary>> GetReportSummary(string DCCode, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@"  SELECT  TO_CHAR(tms_plan_action_date , 'DD/MM/YYYY') as PlanActionDate
                    ,plan.tms_plan_no  as PlanNo
                    ,plan.tms_plan_group as ""Group""
                    ,plan.tms_plan_load_no as LoadNo
                    ,tms_plan_store_code as StoreCode
                    ,tms_plan_store_format as StoreFormat
                    ,tms_plan_store_name as StoreName
                    ,plan.tms_plan_dock_no as DockNo
                    ,gate_type_truck as TruckTypeID
                    ,tb_truck_type.truck_type_name  as TruckTypeName
                    ,gate.gate_license as LicenseTruck
                    ,gate.gate_barcode as Barcode
                    ,to_char(gate.gate_truck_in,'DD/MM/YYYY hh24:mi') as GateInDate
                    ,to_char(rsu.created_date,'DD/MM/YYYY hh24:mi') as RSUInDate
                    ,to_char(rsu.rsu_out_date,'DD/MM/YYYY hh24:mi') as RSUOutDate
                    ,to_char(plan.tms_plan_pre_cool,'DD/MM/YYYY hh24:mi') as PlanPreCool
                    ,to_char(tms_summary_plan_actual_pre_cool,'DD/MM/YYYY hh24:mi') as ActualPreCool
                    ,to_char(plan.tms_plan_on_dock,'DD/MM/YYYY hh24:mi') as PlanOnDock
                    ,to_char(tms_summary_plan_actual_on_dock,'DD/MM/YYYY hh24:mi') as ActualOnDock
                    ,to_char(tms_summary_plan_actual_finish_preload,'DD/MM/YYYY hh24:mi') as ActualPreLoad
                    ,to_char(plan.tms_plan_start_load,'DD/MM/YYYY hh24:mi') as PlanStartLoad
                    ,to_char(tms_summary_plan_actual_start_load,'DD/MM/YYYY hh24:mi') as ActualStartLoad
                    ,to_char(plan.tms_plan_finish_load,'DD/MM/YYYY hh24:mi') as PlanFinishLoad
                    ,to_char(tms_summary_plan_actual_finish_load,'DD/MM/YYYY hh24:mi') as ActualFinishLoad
                    ,to_char(plan.tms_plan_dispatch,'DD/MM/YYYY hh24:mi') as PlanDisPatch
                    ,to_char(tms_summary_plan_actual_edp_in,'DD/MM/YYYY hh24:mi') as ActualEDPIn
                    ,to_char(tms_summary_plan_actual_edp_out,'DD/MM/YYYY hh24:mi') as ActualEDPOut
                    ,to_char(tms_summary_plan_actual_finish_load,'DD/MM/YYYY hh24:mi') as ActualGateOut
                    ,CASE 
                       WHEN to_char(gate.gate_truck_in,'DD/MM/YYYY hh24:mi') is null then 'Wait'
                       WHEN to_char(tms_summary_plan_actual_edp_out,'DD/MM/YYYY hh24:mi') is null THEN 'Processing'   
                       ELSE 'Finished'
                    END as Status
                    ,'' as RemarkPreCool
                    ,precool.precool_qc_id as PreCoolQCID 
                    FROM {_schema}.tb_tms_plan plan
                    left join {_schema}.tb_tran_truckondock truckondock on truckondock.truckondock_plan_no = plan.tms_plan_no 
                    left join {_schema}.tb_tran_gate gate on gate.gate_barcode = truckondock.truckondock_barcode and gate.gate_id  = truckondock.gate_id::integer
                    left join {_schema}.tb_tran_rsu rsu on rsu.rsu_gate_id  = truckondock.gate_id::integer
                    left join {_schema}.tb_truck_type on tb_truck_type.truck_type_id = gate_type_truck
                    left join {_schema}.tb_tms_summary_plan summaryplan on plan.tms_plan_no = summaryplan.tms_summary_plan_no 
                    left join (SELECT precool_qc_id,precool_plan_no  FROM {_schema}.tb_tran_precool where precool_status = '1') precool on precool.precool_plan_no = plan.tms_plan_no
                    where gate.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND plan.tms_plan_action_date between '{startDate} 00:00:00' AND '{endDate} 23:59:59'");
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine($"AND plan.tms_plan_action_date >= '{startDate} 00:00:00'");
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine($"AND plan.tms_plan_action_date <= '{endDate} 23:59:59'");
                }
                else
                {
                    sql.AppendLine("AND plan.tms_plan_action_date::DATE = CURRENT_DATE");
                }
                sql.AppendLine(" ORDER BY plan.tms_plan_no ");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_ReportSummary>(
                        sql.ToString(),
                        parameters
                    );
                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve summary records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
}
