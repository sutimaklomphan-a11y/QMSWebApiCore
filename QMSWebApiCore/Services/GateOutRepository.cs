using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using QMSWebApiCore.Models;
using System.Data;
using System.Text;

namespace QMSWebApiCore.Services
{
    public interface IGateOutRepository
    {
        Task<List<M_GateOut>> GetGateOutDirectAsync(string DCCode, string TruckTypeID, string StatusGateIn);
        Task<List<M_GateOut>> GetAllGateOutAsync(string DCCode, string TruckTypeID, string StatusGateOut);
        Task<List<M_GateOut>> GatGateInByBarcodeDetail(string Barcode, string DCCode);  //check barcode detail
        Task<List<M_GateOut>> GatGateOutByBarcodeDirect(string Barcode, string DCCode, string ActionDate);                  
        Task<bool> StampGateOutAsync(M_GateOut ClsGateOut);        //Stamp GateOut
        Task<bool> StampGateOutDirectAsync(M_GateOut ClsGateOut);  //Stamp GateOut Direect
        Task<bool> UpdateGateOutAsync(M_GateOut ClsGateOut);
        Task<bool> DeleteGateOutAsync(M_GateOut ClsGateOut);
        Task<List<M_GateOut>> checkStatusBarcodeDetail(string Barcode, string DCCode);    //check gateout มีการทำ EDP ก่อนรถออกไหม?
    }
    public class GateOutRepository : IGateOutRepository
    {
        //private readonly string _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
        //    ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
        //private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        //    ?? throw new ArgumentException("Connection string is required");

        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public GateOutRepository(IConfiguration configuration)
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

        //check GateOut Direct by Status ไม่ได้ดึง by barcode
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
	                                            EDP.edp_status as EDPStatus,
                                                EDP.edp_out_by as EDPOutBy,
                                                TO_CHAR(EDP.edp_out_date, 'DD/MM/YYYY HH24:mi') as EDPOutDate
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

        //ใช้สำหรับเช็คข้อมูลเพื่อบันทึก Gate Out รถที่จะออกจากคลังสินค้า >>เหมือนกันใน function ของ EDPInRepo
        public async Task<List<M_GateOut>> checkStatusBarcodeDetail(string Barcode, string DCCode)
        {
            try
            {
                var sql = new StringBuilder($@"select
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
		                                                TO_CHAR(RSU.created_date, 'DD/MM/YYYY HH24:mi') as RSUInDateTime,
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
	                                                left join outbound_qms.tb_tran_truckondock truckondock on
		                                                truckondock.truckondock_barcode = gate.gate_barcode
		                                                and truckondock.gate_id::INTEGER = gate.gate_id::INTEGER
	                                                left join {_schema}.tb_tran_preload preload on
		                                                preload.preload_plan_no = truckondock.truckondock_plan_no
	                                                left join {_schema}.tb_tran_loadontruck loadintruck on
		                                                loadintruck.loadontruck_plan_no = truckondock.truckondock_plan_no
	                                                left join {_schema}.tb_tms_summary_plan summary on
		                                                summary.tms_summary_plan_no = truckondock.truckondock_plan_no
	                                                left join {_schema}.tb_tran_edp edp on
		                                                edp.edp_barcode = Gate.gate_barcode
		                                                and edp.edp_plan_no = (
		                                                select
			                                                truckondock_plan_no
		                                                from
			                                                {_schema}.tb_tran_truckondock
		                                                where
			                                                truckondock_barcode = '{Barcode}'
		                                                order by
			                                                created_date desc
		                                                limit 1)
		                                                and edp.edp_gate_id::integer = gate.gate_id::integer
	                                                where
		                                                1 = 1
		                                                and Gate.gate_out_direct = '0'
		                                                and Gate.gate_status = '1'
		                                                --and UPPER(Gate.gate_barcode) = UPPER('{Barcode}')
                                                        and Gate.gate_barcode = '{Barcode}'
		                                                and Gate.gate_dc_code = '{DCCode}'
	                                                order by
		                                                Gate.gate_id desc
	                                                limit 1) as Plandata
                                                left join (
	                                                select
		                                                truckondock_plan_no,
		                                                truckondock_barcode
	                                                from
		                                                {_schema}.tb_tran_truckondock
	                                                where
		                                                truckondock_barcode = '{Barcode}'
	                                                order by
		                                                created_date desc
	                                                limit 1) ondock 
                                                 on
	                                                plandata.Barcode = ondock.truckondock_barcode ");


                var parameters = new DynamicParameters();
                sql.AppendLine("ORDER BY gateindate DESC LIMIT 20");

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
                throw new Exception($"Failed to retrieve gate Out records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<M_GateOut?>> GetGateInByDetailAsync(int Gate_id, string DCCode)
        {
            try
            {
                var sql = new StringBuilder($@"SELECT GATE.gate_id,
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
		                                                    ON GATE.gate_type_truck  = TRUCKTYPE.truck_type_id 
                                                    INNER JOIN {_schema}.tb_tran_edp EDP
		                                                    ON GATE.gate_barcode = EDP.edp_barcode
                                                    LEFT JOIN {_schema}.tb_tran_rsu RSU
		                                                    ON GATE.gate_rsu_id  = RSU.rsu_id 
                                                    INNER JOIN {_schema}.tb_rsu_status RSU_STATUS 
		                                                    ON GATE.gate_rsu_id = RSU_STATUS.rsu_status_id 
                                                    WHERE 1=1 AND GATE.gate_dc_code = @DCCode");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", DCCode);

                if (Gate_id != 0)
                {
                    sql.AppendLine("AND TRUCKTYPE.truck_type_name = @Gate_id");
                    parameters.Add("Gate_id", Gate_id);
                }

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
                    command.Parameters.AddWithValue("@gate_truck_out", now);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", 2); 
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)CLS_GATEOUT.GateOutRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)CLS_GATEOUT.ActionBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if(rowsAffected == 0)
                    {
                        return false;
                    }
                    return true;
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
                    command.Parameters.AddWithValue("@gate_truck_out", now);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", 2);
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)CLS_GATEOUT.GateOutRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)CLS_GATEOUT.ActionBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> UpdateGateOutAsync(M_GateOut CLS_GATEOUT)
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
                    command.Parameters.AddWithValue("@gate_barcode", (object?)CLS_GATEOUT.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)CLS_GATEOUT.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_type_truck", CLS_GATEOUT.TruckTypeID);
                    command.Parameters.AddWithValue("@gate_license", (object?)CLS_GATEOUT.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)CLS_GATEOUT.RSUStatusID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)CLS_GATEOUT.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_by", (object?)CLS_GATEOUT.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;

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
        //private static string ValidateSchemaName(string schemaName)
        //{
        //    if (string.IsNullOrWhiteSpace(schemaName))
        //        throw new ArgumentException("Schema name cannot be null or empty");

        //    if (!System.Text.RegularExpressions.Regex.IsMatch(
        //        schemaName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        //        throw new ArgumentException($"Invalid schema name: '{schemaName}'");
        //    return schemaName;
        //}

    }
}
