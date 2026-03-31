using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System.Reflection;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace QMSWebApiCore.Services
{
    public interface IGateInRepository
    {
        // --- Gate In Process Service ---
        Task<List<M_GateIn>> GetAllGateInStatusAsync(M_GateIn ClsGateIn);
        Task<List<M_GateIn>> GetAllGateInAsync(string DCCode, string TruckTypeID, string StatusGateIn, string? startDate = null, string? endDate = null);
        Task<List<M_GateIn>> GatGateInByBarcodeOnceAsync(string Barcode, string DCCode);
        Task<List<M_GateIn>> checkStatusBarcodeDetail(string Barcode, string DCCode);    //ใช่check Barcode detail และ check gateout มีการทำ EDP ก่อนรถออกไหม?
        Task<M_GateIn> CreateGateInAsync(M_GateIn ClsGateIn);
        Task<bool> StampGateIn(M_GateIn ClsGateIn);
        Task<bool> CheckDuplicateBarcode(string barcode, string DCCode);
        Task<bool> UpdateGateInAsync(M_GateIn ClsGateIn);
        Task<bool> DeleteGateInAsync(M_GateIn ClsGateIn);
    }
    public class GateInRepository : IGateInRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public GateInRepository(IConfiguration configuration)
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
        public async Task<List<M_GateIn>> GetAllGateInStatusAsync(M_GateIn ClsGateIn)
        {
            try
            {
                var sql = $@"
                            SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
                            GATE.gate_barcode AS Barcode,
                            GATE.gate_dc_code ,
                            GATE.gate_type_truck AS TruckTypeID,
                            GATE.gate_license AS LicenseTruck,
                            GATE.gate_driver_name AS DriverName,
                            GATE.gate_action_by AS ActionBy,
                            GATE.gate_rsu_id AS RSUStatusID,
                            GATE.gate_in_remark AS GateInRemark,
                            GATE.gate_out_remark AS GateOutRemark,
                            TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                            TO_CHAR(GATE.gate_truck_out,  'DD/MM/YYYY HH24:MI') AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            STATUS.rsu_status_text AS RSUStatusText,
                            CASE WHEN GATE.gate_status = 1 then 'รถอยู่ในคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 THEN GATE.gate_rsu_id = 1 THEN STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 THEN GATE.gate_rsu_id = 2 THEN STATUS.rsu_status_text
                                 WHEN GATE.gate_status = 2 THEN 'รถเข้าและออกจากคลังสินค้า'
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        INNER JOIN {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id
                        WHERE GATE.gate_dc_code = @DCCode
                             AND GATE.gate_action_date:: DATE = CURRENT_DATE
                        ORDER BY GATE.gate_id DESC LIMIT 10 ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateIn>(sql, new { DCCode = ClsGateIn.DCCode });
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
        public async Task<List<M_GateIn>> GetAllGateInAsync(string DCCode, string TruckTypeID, string StatusGateIn, string? startDate = null, string? endDate = null)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
                                    GATE.gate_barcode AS Barcode,
                                    GATE.gate_dc_code ,
                                    GATE.gate_type_truck AS TruckTypeID,
                                    GATE.gate_license AS LicenseTruck,
                                    GATE.gate_driver_name AS DriverName,
                                    GATE.gate_action_by AS ActionBy,
                                    GATE.gate_rsu_id AS RSUStatusID,
                                    GATE.gate_in_remark AS GateInRemark,
                                    GATE.gate_out_remark AS GateOutRemark,
                                    TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                                    TO_CHAR(GATE.gate_truck_out,  'DD/MM/YYYY HH24:MI') AS GateOutDate,
                                    GATE.gate_status AS GateStatus,
                                    TRUCKTYPE.truck_type_name AS TruckTypeText,
                                    TRUCKTYPE.truck_type_image AS TruckTypeImage,
                                    STATUS.rsu_status_text AS RSUStatusText,
                                    CASE WHEN GATE.gate_status = 1 THEN 'รถอยู่ในคลังสินค้า'
		                    	        WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 1 THEN STATUS.rsu_status_text
		                    	        WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 2 THEN STATUS.rsu_status_text
                                        WHEN GATE.gate_status = 2 THEN 'รถเข้าและออกจากคลังสินค้า'
		                    	        END AS gate_status_text
                                FROM {_schema}.tb_tran_gate GATE 
                                INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                                    ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                                INNER JOIN {_schema}.tb_rsu_status STATUS 
                                    ON GATE.gate_rsu_id = STATUS.rsu_status_id
                                WHERE GATE.gate_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(TruckTypeID) && TruckTypeID != "All")
                {
                    sql.AppendLine("AND TRUCKTYPE.truck_type_name = @TruckTypeID");
                    parameters.Add("TruckTypeID", TruckTypeID);
                }

                if (!string.IsNullOrEmpty(StatusGateIn) && StatusGateIn != "All")
                {
                    sql.AppendLine("AND GATE.gate_status = @StatusGateIn");
                    parameters.Add("StatusGateIn", Convert.ToInt16(StatusGateIn));
                }

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND GATE.gate_action_date::DATE BETWEEN @startDate::DATE AND @endDate::DATE");
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    sql.AppendLine("AND GATE.gate_action_date::DATE >= @startDate::DATE");
                    parameters.Add("startDate", startDate);
                }
                else if (!string.IsNullOrEmpty(endDate))
                {
                    sql.AppendLine("AND GATE.gate_action_date::DATE <= @endDate::DATE");
                    parameters.Add("endDate", endDate);
                }
                else
                {
                    sql.AppendLine("AND GATE.gate_action_date::DATE = CURRENT_DATE");
                }

                sql.AppendLine("ORDER BY GATE.Gate_id DESC LIMIT 10");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateIn>(
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
        public async Task<List<M_GateIn>> GatGateInByBarcodeOnceAsync(string Barcode, string DCCode)
        {
            try
            {
                var sql = $@"SELECT GATE.gate_id AS GateID,
                                        GATE.gate_barcode AS Barcode,
                                        GATE.gate_dc_code ,
                                        GATE.gate_type_truck AS TruckTypeID,
                                        GATE.gate_license AS LicenseTruck,
                                        GATE.gate_driver_name AS DriverName,
                                        GATE.gate_action_by AS ActionBy,
                                        GATE.gate_rsu_id AS RSUStatusID,
                                        GATE.gate_in_remark AS GateInRemark,
                                        GATE.gate_out_remark AS GateOutRemark,
                                        TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                                        TO_CHAR(GATE.gate_truck_out,  'DD/MM/YYYY HH24:MI') AS GateOutDate,
                                        GATE.gate_status AS GateStatus,
                                        TRUCKTYPE.truck_type_name AS TruckTypeText,
                                        TRUCKTYPE.truck_type_image AS TruckTypeImage,
                                        STATUS.rsu_status_text AS RSUStatusText,
                                        CASE WHEN GATE.gate_status = 1 then 'รถอยู่ในคลังสินค้า'
		                    	             WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 1 then STATUS.rsu_status_text
		                    	             WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 2 then STATUS.rsu_status_text
                                             WHEN GATE.gate_status = 2 then 'รถเข้าและออกจากคลังสินค้า'
		                    	            end as gate_status_text
                                    FROM {_schema}.tb_tran_gate GATE 
                                    INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                                        ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                                    INNER JOIN {_schema}.tb_rsu_status STATUS 
                                        ON GATE.gate_rsu_id = STATUS.rsu_status_id
                                    WHERE GATE.gate_dc_code = @DCCode
                                        AND GATE.gate_barcode = @Barcode
                                        AND GATE.gate_action_date = current_date
                                        AND GATE.gate_status in (1,2)
                                        ORDER BY GATE.gate_id DESC LIMIT 1";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateIn>(sql, new { Barcode = Barcode, DCCode = DCCode });
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

        //<<Barcode Detail Page>> check status barcode detail page >>         
        public async Task<List<M_GateIn>> checkStatusBarcodeDetail(string Barcode, string DCCode)
        {
            try
            {
                var sql = $@"
                            select
                                *,
                                ondock.truckondock_plan_no as PlanNo
                            from
                                (
                                select
                                                        Gate.gate_barcode as Barcode,
                                                        Gate.gate_license as License,
                                                        Gate.gate_action_by as ActionBy,
                                                        Gate.gate_driver_name as DriverName,
                                                        summary.tms_summary_plan_action_date as PlanDate,
                                                        summary.tms_summary_load_no as PlanLoadNo,
                                                        summary.tms_summary_plan_group as PlanGroupNo,
                                                        summary.tms_summary_dock_no as PlanDockNo,	
                                                        Gate.gate_truck_in as GateInDate,    
                                                        TO_CHAR(Gate.gate_truck_in, 'DD/MM/YYYY HH24:mi') as GateInDateTime,
                                                        TO_CHAR(RSU.rsu_in_date, 'DD/MM/YYYY HH24:mi') as RSUInDateTime,
                                                        TO_CHAR(RSU.rsu_out_date, 'DD/MM/YYYY HH24:mi') as RSUOutDateTime,
                                                        TO_CHAR(truckondock.created_date, 'DD/MM/YYYY HH24:mi') as TruckOnDockDate,
                                                        TO_CHAR(preload.created_date, 'DD/MM/YYYY HH24:mi') as PreLoadDate,
                                                        TO_CHAR(loadintruck.created_date, 'DD/MM/YYYY HH24:mi') as LoadInTruckDate,
                                                        TO_CHAR(loadintruck.loadontruck_finish_date, 'DD/MM/YYYY HH24:mi') as LoadInTruckFinishDate,
                                                        TruckType.truck_type_id as TruckTypeID,
                                                        TruckType.truck_type_name as TruckTypeText,
                                                        TruckType.truck_type_style as TruckTypeStyle,
                                                        TruckType.truck_type_image as TruckTypeImage,
                                                        edp.edp_in_by as EDPInBy,
                                                        TO_CHAR(edp.edp_in_date, 'DD/MM/YYYY HH24:mi') as EDPInDate,
                                                        edp.edp_in_remark as EDPInRemark,
                                                        edp.edp_out_by as EDPOutBy,
                                                        TO_CHAR(edp.edp_out_date, 'DD/MM/YYYY HH24:mi') as EDPOutDate,
                                                        edp.edp_out_remark as EDPOutRemark,
                                                        gate.last_process as LastProcess
                                from
                                    {_schema}.tb_tran_gate Gate
                                left join {_schema}.tb_tran_rsu rsu on
                                                        rsu.rsu_barcode = gate.gate_barcode
                                    and rsu.rsu_gate_id = gate_id
                                inner join {_schema}.tb_truck_type TruckType on
                                                            TruckType.truck_type_id = Gate.gate_type_truck
                                inner join {_schema}.tb_tran_truckondock truckondock on
                                                            truckondock.truckondock_barcode = gate.gate_barcode
                                    and truckondock.gate_id::INTEGER = gate.gate_id::INTEGER
                                inner join {_schema}.tb_tran_preload preload on
                                                            preload.preload_plan_no = truckondock.truckondock_plan_no
                                inner join {_schema}.tb_tran_loadontruck loadintruck on
                                                            loadintruck.loadontruck_plan_no = truckondock.truckondock_plan_no
                                inner join {_schema}.tb_tms_summary_plan summary on 
                                                            summary.tms_summary_plan_no = truckondock.truckondock_plan_no
                                left join {_schema}.tb_tran_edp edp on
                                                            edp.edp_barcode = Gate.gate_barcode
                                    and edp.edp_plan_no = (
                                    select
                                        truckondock_plan_no
                                    from
                                        {_schema}.tb_tran_truckondock
                                    where
                                        truckondock_barcode = @Barcode
                                    order by
                                        truckondock.truckondock_id desc
                                    limit 1)
                                    and edp.edp_gate_id::integer = gate.gate_id::integer
                                where
                                    1 = 1
                                    and UPPER(Gate.gate_barcode) = UPPER(@Barcode)
                                order by
                                    Gate.gate_id desc
                                limit 1	) as Plandata
                            inner join (
                                select
                                    truckondock_plan_no,
                                    truckondock_barcode
                                from
                                    {_schema}.tb_tran_truckondock
                                where
                                    truckondock_barcode = @Barcode
                                order by
                                    truckondock_id desc
                                limit 1) ondock on
                                plandata.Barcode = ondock.truckondock_barcode ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateIn>(sql, new { Barcode = Barcode, DCCode = DCCode });
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

        public async Task<M_GateIn?> GetGateInByDetailAsync(int Gate_id, string DCCode)
        {
            M_GateIn? gate = null;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"
                            SELECT GATE.gate_id,
		                    GATE.gate_barcode,
	  	                    GATE.gate_license,
	  	                    GATE.gate_action_by,
	  	                    GATE.gate_driver_name,
	  	                    GATE.gate_rsu_id,
	  	                    GATE.gate_in_remark,
	  	                    GATE.gate_out_remark,	  	
                            TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                            TO_CHAR(GATE.gate_truck_out,  'DD/MM/YYYY HH24:MI') AS GateOutDate,
	  	                    GATE.gate_status,
	  	                    EDP.edp_status,
	  	                    TRUCKTYPE.truck_type_id,
		                    TRUCKTYPE.truck_type_name,
		                    TRUCKTYPE.truck_type_style,
		                    TRUCKTYPE.truck_type_image,
		                    RSU_STATUS.rsu_status_text
                            FROM {_schema}.tb_tran_gate GATE 
                            INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
		                            ON GATE.gate_type_truck  = TRUCKTYPE.truck_type_id 
                            INNER JOIN {_schema}.tb_tran_edp EDP
		                            ON GATE.gate_barcode = EDP.edp_barcode
                            LEFT JOIN {_schema}.tb_tran_rsu RSU
		                            ON GATE.gate_rsu_id  = RSU.rsu_id 
                            INNER JOIN {_schema}.tb_rsu_status RSU_STATUS 
		                            ON GATE.gate_rsu_id = RSU_STATUS.rsu_status_id 
                            where  1=1  AND GATE.gate_action_date:: DATE = CURRENT_DATE ";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    if (Gate_id != 0)
                    {
                        command.CommandText += " AND GATE.gate_id = @Gate_id";
                        command.Parameters.AddWithValue("@Gate_id", Gate_id);
                    }

                    if (!string.IsNullOrWhiteSpace(DCCode))
                    {
                        command.CommandText += " AND GATE.gate_dc_code = @DCCode";
                        command.Parameters.AddWithValue("@DCCode", DCCode);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            gate = new M_GateIn
                            {
                                GateID = reader.GetInt32(0),
                                Barcode = reader.GetString(1),
                                LicenseTruck = reader.GetString(2),
                                ActionBy = reader.GetString(3),
                                DriverName = reader.GetString(4),
                                RSUID = reader.GetInt32(5),
                                GateInRemark = reader.IsDBNull(6) ? null : reader.GetString(6),
                                GateOutRemark = reader.IsDBNull(7) ? null : reader.GetString(7),
                                GateInDate = reader.IsDBNull(8) ? null : reader.GetString(8),
                                GateOutDate = reader.IsDBNull(9) ? null : reader.GetString(9),
                                GateStatus = reader.GetInt32(10),
                                EDPStatusID = reader.GetString(11),
                                TruckTypeID = reader.GetInt32(12),
                                TruckTypeName = reader.GetString(13),
                                TruckTypeStyle = reader.GetString(14),
                                TruckTypeImage = reader.GetString(15),
                                RSUStatusText = reader.GetString(16)
                            };
                        }
                    }
                }
            }

            return gate;
        }

        public async Task<bool> CheckDuplicateBarcode(string barcode, string DCCode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;
            try
            {
                var sql = $@"SELECT EXISTS(
                                        SELECT 1 
                                        FROM {_schema}.tb_tran_gate 
                                        WHERE UPPER(gate_barcode) = UPPER(@gate_barcode)
                                        AND   gate_dc_code = @gate_dc_code
                                        AND   gate_action_date:: DATE = CURRENT_DATE)";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var isDuplicate = await connection.ExecuteScalarAsync<bool>(sql, new { gate_barcode = barcode, gate_dc_code = DCCode });
                    return isDuplicate;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking duplicate barcode: {ex.Message}");
                throw;
            }
        }
        // CREATE, UPDATE, DELETE  - Raw SQL
        public async Task<M_GateIn> CreateGateInAsync(M_GateIn ClsGateIn)
        {
            var now = DateTime.UtcNow;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"INSERT INTO {_schema}.tb_tran_gate (
                                     gate_id ,
                                     gate_driver_name ,
                                     gate_type_truck ,
                                     gate_license ,
                                     gate_action_by ,
                                     gate_status,
                                     gate_action_date ,
                                     gate_rsu_id ,
                                     gate_barcode ,
                                     gate_in_remark ,
                                     gate_truck_in ,
                                     gate_dc_code ,
                                     created_by ,
                                     created_date ,
                                     last_process,
                                     flag 
                    ) 
                    VALUES (
                                        nextval('outbound_qms_sim.tb_tran_gate_gate_id_seq'::regclass) ,
                                        @gate_driver_name ,
                                        @gate_type_truck ,                                        
                                        @gate_license ,
                                        @gate_action_by ,
                                        @gate_status,
                                        CAST(@gate_action_date AS DATE),
                                        @gate_rsu_id , 
                                        @gate_barcode ,
                                        @gate_in_remark ,
                                        @gate_truck_in,
                                        @gate_dc_code ,
                                        @created_by ,   
                                        @created_date,
                                        @last_process,
                                        @flag)

                     RETURNING       gate_id,
                                     gate_driver_name ,
                                     gate_type_truck ,
                                     gate_license ,
                                     gate_action_by ,
                                     gate_status,
                                     gate_action_date ,
                                     gate_rsu_id ,
                                     gate_barcode ,
                                     gate_in_remark ,
                                     gate_truck_in ,
                                     gate_dc_code ,
                                     created_by ,
                                     created_date ,
                                     last_process,
                                     flag ";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)ClsGateIn.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_type_truck", ClsGateIn.TruckTypeID);
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateIn.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_license", (object?)ClsGateIn.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_action_by", (object?)ClsGateIn.ActionBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_status", 1);
                    command.Parameters.AddWithValue("@gate_action_date", now.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)ClsGateIn.RSUStatusID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)ClsGateIn.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_truck_in", now);
                    command.Parameters.AddWithValue("@gate_dc_code", (object?)ClsGateIn.DCCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@created_by", (object?)ClsGateIn.CreatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@created_date", now);
                    command.Parameters.AddWithValue("@last_process", "Gate In");
                    command.Parameters.AddWithValue("@flag", 1);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new M_GateIn
                            {
                                GateID = reader.GetInt32(0),
                            };
                        }
                    }
                }
            }

            throw new Exception("Failed to create gate record");
        }

        public async Task<bool> StampGateIn(M_GateIn ClsGateIn)
        {
            if (ClsGateIn == null)
                throw new ArgumentNullException(nameof(ClsGateIn));

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_gate (
                                     gate_id ,
                                     gate_driver_name ,
                                     gate_type_truck ,
                                     gate_license ,
                                     gate_action_by ,
                                     gate_status,
                                     gate_action_date ,
                                     gate_rsu_id ,
                                     gate_barcode ,
                                     gate_in_remark ,
                                     gate_truck_in ,
                                     gate_dc_code ,
                                     created_by ,
                                     created_date ,
                                     last_process,
                                     flag 
                    ) 
                    VALUES (    nextval('outbound_qms_sim.tb_tran_gate_gate_id_seq'::regclass) ,
                                @gate_driver_name ,
                                @gate_type_truck ,                                        
                                @gate_license ,
                                @gate_action_by ,
                                @gate_status,
                                CAST(@PlanDate AS DATE),
                                @gate_rsu_id , 
                                @gate_barcode ,
                                @gate_in_remark ,
                                current_timestamp,
                                @DCCode ,
                                @created_by ,   
                                @created_date,
                                @last_process,
                                @flag)";

                var sql_update_wip = $@"INSERT INTO {_schema}.tb_wip_current
                                                 (barcode, status, plan_action_date, dc_code) 
                                                 VALUES(@gate_barcode, @gate_status, CAST(@PlanDate AS DATE), @DCCode) ";

                if (DateTime.Now.Hour < 8)
                {
                    ClsGateIn.PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                }
                else
                {
                    ClsGateIn.PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                }

                var parameters = new
                {
                    gate_driver_name = (object?)ClsGateIn.DriverName ?? DBNull.Value,
                    gate_type_truck = ClsGateIn.TruckTypeID,
                    gate_barcode = (object?)ClsGateIn.Barcode ?? DBNull.Value,
                    gate_license = (object?)ClsGateIn.LicenseTruck ?? DBNull.Value,
                    gate_action_by = (object?)ClsGateIn.ActionBy ?? DBNull.Value,
                    gate_status = 1,
                    PlanDate = ClsGateIn.PlanDate,
                    gate_rsu_id = (object?)ClsGateIn.RSUStatusID ?? DBNull.Value,
                    gate_in_remark = (object?)ClsGateIn.GateInRemark ?? DBNull.Value,
                    gate_truck_in = DateTime.UtcNow,
                    DCCode = (object?)ClsGateIn.DCCode ?? DBNull.Value,
                    created_by = (object?)ClsGateIn.CreatedBy ?? DBNull.Value,
                    created_date = DateTime.UtcNow,
                    last_process = "Gate In",
                    flag = "1"
                };

                var rowInsert = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                var rowUpdate_wip = await connection.ExecuteAsync(sql_update_wip, parameters, transaction);

                if (rowInsert == 0 || rowUpdate_wip == 0)
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
        public async Task<bool> UpdateGateInAsync(M_GateIn ClsGateIn)
        {
            var now = DateTime.UtcNow;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"UPDATE {_schema}.tb_tran_gate 
                            SET gate_driver_name = @gate_driver_name,
                                gate_type_truck = @gate_type_truck,
                                gate_license = @gate_license,
                                gate_rsu_id = @gate_rsu_id,
                                gate_in_remark = @gate_in_remark,
                                updated_by = @updated_by,
                                updated_date = @updated_date
                            WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateIn.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)ClsGateIn.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_type_truck", ClsGateIn.TruckTypeID);
                    command.Parameters.AddWithValue("@gate_license", (object?)ClsGateIn.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)ClsGateIn.RSUStatusID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)ClsGateIn.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_by", (object?)ClsGateIn.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> DeleteGateInAsync(M_GateIn ClsGateIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql_delete_gate = $@"DELETE FROM {_schema}.tb_tran_gate  WHERE gate_barcode = @Barcode AND gate_action_date:: DATE = CURRENT_DATE";
                        var sql_delete_rsu = $@"DELETE FROM {_schema}.tb_tran_rsu WHERE rsu_barcode = @Barcode AND rsu_in_date:: DATE = CURRENT_DATE";
                        var sql_delete_wip = $@"DELETE FROM {_schema}.tb_wip_current WHERE 1=1 and barcode = @Barcode and dc_code = @DCCode";

                        var parameters = new
                        {
                            Barcode = ClsGateIn.Barcode,
                            DCCode = ClsGateIn.DCCode
                        };

                        var row_delete = await connection.ExecuteAsync(sql_delete_gate, parameters, transaction);
                        var row_delete_rsu = await connection.ExecuteAsync(sql_delete_rsu, parameters, transaction);
                        var row_delete_wip = await connection.ExecuteAsync(sql_delete_wip, parameters, transaction);

                        if (row_delete == 0 || row_delete_rsu == 0 )
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
                        return false;
                        throw;
                    }
                }
            }
        }

        //function สำหรับค้นหา GateIn by Barcode ที่ต้องการ Stamp Gate Out Direct
        public async Task<M_GateIn> SearchGateInByBarcode(string DCcode, string barcdoe, string ActionDate)
        {
            M_GateIn? gate = null;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"SELECT GATE.gate_id,
		                            GATE.gate_barcode,
	  	                            GATE.gate_license,
	  	                            GATE.gate_action_by,
	  	                            GATE.gate_driver_name,
	  	                            GATE.gate_rsu_id,
	  	                            GATE.gate_in_remark,
	  	                            GATE.gate_out_remark,	  	
                                    TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                                    TO_CHAR(GATE.gate_truck_out, 'DD/MM/YYYY HH24:MI') AS GateOutDate,
	  	                            GATE.gate_status,
	  	                            EDP.edp_status,
	  	                            TRUCKTYPE.truck_type_id,
		                            TRUCKTYPE.truck_type_name,
		                            TRUCKTYPE.truck_type_style,
		                            TRUCKTYPE.truck_type_image,
		                            RSU_STATUS.rsu_status_text                            
                            FROM {_schema}.tb_tran_gate GATE 
                            INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
		                            on GATE.gate_type_truck  = TRUCKTYPE.truck_type_id 
                            INNER JOIN {_schema}.tb_tran_edp EDP
		                            on GATE.gate_barcode = EDP.edp_barcode
                            LEFT JOIN {_schema}.tb_tran_rsu RSU
		                            on GATE.gate_rsu_id  = RSU.rsu_id 
                            INNER JOIN {_schema}.tb_rsu_status RSU_STATUS 
		                            on GATE.gate_rsu_id = RSU_STATUS.rsu_status_id 
                            where  1=1  ";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    if (!string.IsNullOrWhiteSpace(DCcode))
                    {
                        command.CommandText += " AND GATE.gate_dc_code = @DCCode";
                        command.Parameters.AddWithValue("@DCCode", DCcode);
                    }

                    if (!string.IsNullOrWhiteSpace(barcdoe))
                    {
                        command.CommandText += " AND GATE.gate_barcode = @barcdoe";
                        command.Parameters.AddWithValue("@barcdoe", barcdoe);
                    }

                    if (!string.IsNullOrWhiteSpace(ActionDate))
                    {
                        command.CommandText += " AND CAST(GATE.gate_action_date AS DATE) = @ActionDate";
                        command.Parameters.AddWithValue("@ActionDate", ActionDate.ToString());
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            gate = new M_GateIn
                            {
                                GateID = reader.GetInt32(0),
                                Barcode = reader.GetString(1),
                                LicenseTruck = reader.GetString(2),
                                ActionBy = reader.GetString(3),
                                DriverName = reader.GetString(4),
                                RSUID = reader.GetInt32(5),
                                GateInRemark = reader.IsDBNull(6) ? null : reader.GetString(6),
                                GateOutRemark = reader.IsDBNull(7) ? null : reader.GetString(7),
                                GateInDate = reader.IsDBNull(8) ? null : reader.GetString(8),
                                GateOutDate = reader.IsDBNull(9) ? null : reader.GetString(9),
                                GateStatus = reader.GetInt32(10),
                                EDPStatusID = reader.GetString(11),
                                TruckTypeID = reader.GetInt32(12),
                                TruckTypeName = reader.GetString(13),
                                TruckTypeStyle = reader.GetString(14),
                                TruckTypeImage = reader.GetString(15),
                                RSUStatusText = reader.GetString(16)
                            };
                        }
                    }
                    return gate;
                }
            }
        }
        //check GateOut direct list ไม่ได้ดึง by barcode
        public async Task<List<M_GateOut>> GetGateOutDirectAsync(string DCCode, string TruckTypeID, string StatusGateIn)
        {
            try
            {
                var sql = new StringBuilder
                    ($@" SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
                            GATE.gate_barcode AS Barcode,
                            GATE.gate_dc_code AS dcCode ,
                            GATE.gate_type_truck AS TruckTypeID,
                            GATE.gate_license AS LicenseTruck,
                            GATE.gate_driver_name AS DriverName,
                            GATE.gate_action_by AS ActionBy,
                            GATE.gate_rsu_id AS RSUStatusID,
                            GATE.gate_in_remark AS GateInRemark,
                            GATE.gate_out_remark AS GateOutRemark,
                            TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:mi') AS GateInDate,  
                            TO_CHAR(GATE.gate_truck_out, 'DD/MM/YYYY HH24:mi') AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            STATUS.rsu_status_text AS RSUStatusText,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            CASE WHEN GATE.gate_status = 1 THEN 'รถเข้าคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 1 THEN STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 2 THEN STATUS.rsu_status_text
                                 WHEN GATE.gate_status = 2 THEN 'รถเข้าและออกจากคลังสินค้า'
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        LEFT JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        LEFT join {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id 
                        WHERE GATE.gate_dc_code = @DCCode
                            AND GATE.last_process = 'RSU OUT'
                            AND GATE.gate_action_date:: DATE = CURRENT_DATE ");

                // Dynamic parameters
                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                // ✅ Truck Type Filter
                if (!string.IsNullOrEmpty(TruckTypeID) && TruckTypeID != "All")
                {
                    sql.AppendLine("AND TRUCKTYPE.truck_type_name = @TruckTypeID");
                    parameters.Add("TruckTypeID", TruckTypeID);
                }

                // ✅ Status Filter
                if (!string.IsNullOrEmpty(StatusGateIn) && StatusGateIn != "All")
                {
                    sql.AppendLine("AND GATE.gate_status = @StatusGateIn");
                    parameters.Add("StatusGateIn", Convert.ToInt16(StatusGateIn));
                }

                // Order & Limit
                sql.AppendLine("ORDER BY GATE.Gate_id DESC LIMIT 10");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(
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
        public async Task<List<M_GateOut>> GetAllGateOutAsync(string DCCode, string TruckTypeID, string StatusGateIn)
        {
            try
            {
                var sql = new StringBuilder
                    ($@" SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
                            GATE.gate_barcode AS Barcode,
                            GATE.gate_dc_code AS dcCode ,
                            GATE.gate_type_truck AS TruckTypeID,
                            GATE.gate_license AS LicenseTruck,
                            GATE.gate_driver_name AS DriverName,
                            GATE.gate_action_by AS ActionBy,
                            GATE.gate_rsu_id AS RSUStatusID,
                            GATE.gate_in_remark AS GateInRemark,
                            GATE.gate_out_remark AS GateOutRemark,
                            TO_CHAR(GATE.gate_truck_in,  'DD-MM-YYYY HH24:MI') AS GateInDate,  
                            TO_CHAR(GATE.gate_truck_out,  'DD-MM-YYYY HH24:MI') AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            STATUS.rsu_status_text AS RSUStatusText,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            CASE WHEN GATE.gate_status = 1 THEN 'รถเข้าคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 1 THEN STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 2 THEN STATUS.rsu_status_text
                                 WHEN GATE.gate_status = 2 THEN 'รถเข้าและออกจากคลังสินค้า'
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        LEFT JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        LEFT join {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id 
                        WHERE GATE.gate_dc_code = @DCCode
                            AND GATE.gate_action_date:: DATE = CURRENT_DATE ");

                // Dynamic parameters
                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                // ✅ Truck Type Filter
                if (!string.IsNullOrEmpty(TruckTypeID) && TruckTypeID != "All")
                {
                    sql.AppendLine("AND TRUCKTYPE.truck_type_name = @TruckTypeID");
                    parameters.Add("TruckTypeID", TruckTypeID);
                }

                // ✅ Status Filter
                if (!string.IsNullOrEmpty(StatusGateIn) && StatusGateIn != "All")
                {
                    sql.AppendLine("AND GATE.gate_status = @StatusGateIn");
                    parameters.Add("StatusGateIn", Convert.ToInt16(StatusGateIn));
                }

                // Order & Limit
                sql.AppendLine("ORDER BY GATE.Gate_id DESC LIMIT 10");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(
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
        public async Task<List<M_GateOut>> GatGateInByBarcodeDetail(string Barcode, string DCCode)
        {
            try
            {
                var sql = new StringBuilder($@"SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
                            GATE.gate_barcode AS Barcode,
                            GATE.gate_dc_code ,
                            GATE.gate_type_truck AS TruckTypeID,
                            GATE.gate_license AS LicenseTruck,
                            GATE.gate_driver_name AS DriverName,
                            GATE.gate_action_by AS ActionBy,
                            GATE.gate_rsu_id AS RSUStatusID,
                            GATE.gate_in_remark AS GateInRemark,
                            GATE.gate_out_remark AS GateOutRemark,
                            TO_CHAR(GATE.gate_truck_in,  'DD/MM/YYYY HH24:mi') AS GateInDate,  
                            TO_CHAR(GATE.gate_truck_out, 'DD/MM/YYYY HH24:mi') AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            STATUS.rsu_status_text AS RSUStatusText,
                            CASE WHEN GATE.gate_status = 1 THEN 'รถอยู่ในคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 1 then STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 AND GATE.gate_rsu_id = 2 then STATUS.rsu_status_text
                                 WHEN GATE.gate_status = 2 THEN 'รถเข้าและออกจากคลังสินค้า'
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        INNER JOIN {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id
                        WHERE GATE.gate_dc_code = @DCCode AND GATE.gate_status in (1,2) ");


                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (!string.IsNullOrEmpty(Barcode) && Barcode != "All")
                {
                    sql.AppendLine("AND GATE.gate_barcode = @Barcode");
                    parameters.Add("Barcode", Barcode);
                }

                sql.AppendLine("ORDER BY GATE.gate_truck_in DESC LIMIT 20");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(
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
        //ใช้สำหรับเช็คข้อมูลเพื่อบันทึก Gate Out Direct รถที่จะออกจากคลังสินค้า ไม่ผ่าน EDP
        public async Task<List<M_GateOut>> GatGateOutByBarcodeDirect(string Barcode, string DCCode, string ActionDate)
        {
            try
            {
                string PlanActionDateYYYYMMDD = Convert.ToDateTime(ActionDate).ToString("yyyyMMdd");
                var sql = new StringBuilder($@"select
	                                            Gate.gate_id as GateID,
	                                            Gate.gate_barcode as Barcode ,
	                                            TruckType.truck_type_id as TruckTypeID ,
	                                            TruckType.truck_type_name as TruckTypeText ,
	                                            TruckType.truck_type_style as TruckTypeStyle ,
	                                            TruckType.truck_type_image as TruckTypeImage ,
	                                            Gate.gate_license as LicenseTruck ,
	                                            Gate.gate_action_by as ActionBy ,
	                                            Gate.gate_driver_name as DriverName ,
	                                            Gate.gate_rsu_id as RSUStatusID ,
	                                            RSUStatus.rsu_status_text as RSUStatusText ,
	                                            TO_CHAR(Gate.gate_truck_in, 'DD/MM/YYYY HH24:mi') as GateIn ,
	                                            TO_CHAR(RSU.rsu_in_date, 'DD/MM/YYYY HH24:mi') as RSUInDate ,
	                                            TO_CHAR(RSU.rsu_out_date, 'DD/MM/YYYY HH24:mi') as RSUOutDate          ,
	                                            Gate.gate_in_remark as GateInRemark,
	                                            Gate.gate_out_remark as GateOutRemark ,
	                                            Gate.gate_status as GateStatus ,
	                                            EDP.edp_status as EDPStatus
                                            from
	                                            {_schema}.tb_tran_gate Gate
                                            inner join {_schema}.tb_truck_type TruckType on
	                                            TruckType.truck_type_id = Gate.gate_type_truck
                                            inner join {_schema}.tb_rsu_status RSUStatus on
	                                            Gate.gate_rsu_id = rsustatus.rsu_status_id
                                            left join {_schema}.tb_tran_edp edp on
	                                            edp.edp_barcode = gate.gate_barcode
	                                            and edp.edp_plan_no like '%' || @PlanDate || '%'
                                            left join {_schema}.tb_tran_rsu rsu on
	                                            rsu.rsu_barcode = Gate.gate_barcode
                                            where
	                                            1 = 1
	                                            and UPPER(Gate.gate_barcode) = UPPER(@Barcode)
	                                            and Gate.gate_dc_code = @DCCode
	                                            and Gate.gate_status = '1'
                                            order by
	                                            Gate.created_date  desc
                                            limit 1 ");

                var parameters = new DynamicParameters();
                parameters.Add("PlanDate", PlanActionDateYYYYMMDD);
                parameters.Add("Barcode", Barcode);
                parameters.Add("DCCode", DCCode);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(
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
        public async Task<bool> StampGateOutAsync(M_GateOut CLS_GATEOUT)
        {
            var now = DateTime.UtcNow;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"UPDATE {_schema}.tb_tran_gate 
                            SET gate_truck_out = @gate_truck_out,
                                last_process = @last_process,
                                gate_status = @gate_status,
                                gate_out_remark = @gate_out_remark,
                                gate_out_lps_name = @gate_out_lps_name,
                                updated_date = @updated_date
                            WHERE gate_status = 1
                            AND gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", (object?)CLS_GATEOUT.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_truck_out", (object?)CLS_GATEOUT.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", "2");
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)CLS_GATEOUT.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)CLS_GATEOUT.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> StampGateOutDirectAsync(M_GateOut CLS_GATEOUT)
        {
            var now = DateTime.UtcNow;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"UPDATE {_schema}.tb_tran_gate 
                            SET gate_truck_out = @gate_truck_out,
                                last_process = @last_process,
                                gate_status = @gate_status,
                                gate_out_remark = @gate_out_remark,
                                gate_out_lps_name = @gate_out_lps_name,
                                updated_date = @updated_date
                            WHERE gate_status = 1
                            AND gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", (object?)CLS_GATEOUT.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_truck_out", (object?)CLS_GATEOUT.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", "2");
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)CLS_GATEOUT.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)CLS_GATEOUT.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> UpdateGateOutAsync(M_GateOut CLS_GATEOUT)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                try
                {
                    var sql = $@"UPDATE {_schema}.tb_tran_gate 
                            SET gate_driver_name = @gate_driver_name,
                                gate_type_truck = @gate_type_truck,
                                gate_license = @gate_license,
                                gate_rsu_id = @gate_rsu_id,
                                gate_in_remark = @gate_in_remark,
                                updated_by = @updated_by,
                                updated_date = @updated_date
                            WHERE gate_barcode = @gate_barcode";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@gate_barcode", (object?)CLS_GATEOUT.Barcode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@gate_driver_name", (object?)CLS_GATEOUT.DriverName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@gate_type_truck", CLS_GATEOUT.TruckTypeID);
                        command.Parameters.AddWithValue("@gate_license", (object?)CLS_GATEOUT.LicenseTruck ?? DBNull.Value);
                        command.Parameters.AddWithValue("@gate_rsu_id", (object?)CLS_GATEOUT.RSUStatusID ?? DBNull.Value);
                        command.Parameters.AddWithValue("@gate_in_remark", (object?)CLS_GATEOUT.GateInRemark ?? DBNull.Value);
                        command.Parameters.AddWithValue("@updated_by", (object?)CLS_GATEOUT.UpdatedBy ?? DBNull.Value);
                        command.Parameters.AddWithValue("@updated_date", DateTime.UtcNow.ToString());

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            }
        }
        public async Task<bool> DeleteGateOutAsync(M_GateOut CLS_GATEOUT)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"DELETE FROM {_schema}.tb_tran_gate  WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", CLS_GATEOUT.Barcode ?? DBNull.Value.ToString());

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        private string m_exePath = string.Empty;
        public void LogWriter(string logMessage, string logFileName)
        {
            LogWrite(logMessage, logFileName);
        }
        public void LogWrite(string logMessage, string logFileName)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                string logFile = logFileName;
                using (StreamWriter w = File.AppendText(logFileName))
                //using (StreamWriter w = File.AppendText(m_exePath + "\\" + logFile))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }
        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                //txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
            }
        }
    }
}