using Dapper;
using Npgsql;
using QMSWebApiCore.Models;

namespace QMSWebApiCore.Services
{
    public interface IRSUInRepository
    {
        Task<List<M_RSUInOut>> SearchRSUInByBarcode(string Barcode, string DCCode);
        Task<List<M_RSUInOut>> SearchRSUIn(string TruckTypeID, string StatusRSU, string DCCode);
        Task<bool> DeleteRSUIn(M_RSUInOut CLS_RSUIn);
        Task<bool> StampRSUIn(M_RSUInOut CLS_RSUIn);
        Task<bool> StampRSUOut(M_RSUInOut CLS_RSUIn);
        Task<bool> StampRSUInOut(M_RSUInOut CLS_RSUIn);
        Task<bool> chkDupRSUInBarcode(string Gateid);

    }
    public class RSUInRepository : IRSUInRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public RSUInRepository(IConfiguration configuration)
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
        public async Task<List<M_RSUInOut>> SearchRSUInByBarcode(string Barcode, string DCCode)
        {
            try
            {
                var sql = $@"SELECT ROW_NUMBER() OVER (ORDER BY RSU.created_date DESC) AS row_num,
                                    Gate.gate_dc_code           AS DCCode,          
                                    Gate.gate_barcode           AS Barcode,          
                                    Gate.gate_license           AS LicenseTruck,     
                                    Gate.gate_driver_name       AS DriverName,       
                                    RSU.rsu_action_by           AS ActionByRSUIn,    
                                    TruckType.truck_type_name   AS TruckTypeName,    
                                    Style.status_style          AS TruckTypeStyle,   
                                    TruckType.truck_type_image  AS TruckTypeImage,       
                                    RSUStatus.rsu_status_id     AS RSUStatusID,      
                                    RSUStatus.rsu_status_text   AS RSUStatusText, 
                                    RSU.rsu_status              AS Status,
                                    CASE WHEN RSU.rsu_status = 1 THEN 'คืนสินทรัพย์'
                                         WHEN RSU.rsu_status = 2 THEN 'คืนสินทรัพย์เรียบร้อย'
                                         END  AS StatusText,           
                                    TO_CHAR(Gate.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                                    TO_CHAR(RSU.created_date,    'DD/MM/YYYY HH24:MI') AS RSUInDate,    
                                    TO_CHAR(RSU.rsu_out_date,    'DD/MM/YYYY HH24:MI') AS RSUOutDate,   
                                    RSU.rsu_in_remark           AS RSUInRemark,      
                                    RSU.rsu_out_remark          AS RSUOutRemark      
                            FROM {_schema}.tb_tran_rsu RSU
                            INNER JOIN {_schema}.tb_tran_gate Gate 
                                ON RSU.rsu_barcode = Gate.gate_barcode 
                                AND Gate.gate_status = '1'                              --->สถานะรถยังอยู่ในคลัง
                                AND Gate.gate_dc_code = @DCCode
                                AND Gate.gate_action_date = current_date
                            INNER JOIN {_schema}.tb_truck_type TruckType 
                                ON TruckType.truck_type_id = Gate.gate_type_truck
                            INNER JOIN {_schema}.tb_rsu_status RSUStatus 
                                ON Gate.gate_rsu_id = RSUStatus.rsu_status_id
                            INNER JOIN {_schema}.tb_status_style Style 
                                ON Style.status_id = RSU.rsu_status
                            WHERE RSU.rsu_status <> '2'                                --->สถานะไม่คืนสินทรัพย์
                               AND RSU.flag = '1'
                               AND RSU.rsu_barcode = @Barcode
                               AND RSU.rsu_in_date::date = current_date
                            ORDER BY RSU.created_date DESC
                            LIMIT 20 ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_RSUInOut>(sql, new { Barcode = Barcode, DCCode = DCCode });
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

        // RSU list on Page , may be to use for RSU Not Return
        public async Task<List<M_RSUInOut>> SearchRSUIn(string? TruckTypeID, string? StatusRSU, string DCCode)
        {
            try
            {
                var sql = $@"SELECT Gate.gate_id AS GateID,
                                    Gate.gate_dc_code           AS DCCode,          
                                    Gate.gate_barcode           AS Barcode,          
                                    Gate.gate_license           AS LicenseTruck,     
                                    Gate.gate_driver_name       AS DriverName,       
                                    RSU.rsu_action_by           AS ActionByRSUIn,    
                                    TruckType.truck_type_name   AS TruckTypeName,    
                                    Style.status_style          AS TruckTypeStyle,   
                                    TruckType.truck_type_image  AS TruckTypeImage,       
                                    RSUStatus.rsu_status_id     AS RSUStatusID,      
                                    RSUStatus.rsu_status_text   AS RSUStatusText,
                                    RSU.rsu_status              AS Status,
                                    CASE WHEN RSU.rsu_status = 1 THEN 'คืนสินทรัพย์'
                                         WHEN RSU.rsu_status = 2 THEN 'คืนสินทรัพย์เรียบร้อย'
                                         END  AS StatusText,           
                                    TO_CHAR(Gate.gate_truck_in,  'DD/MM/YYYY HH24:MI') AS GateInDate,  
                                    TO_CHAR(RSU.created_date,    'DD/MM/YYYY HH24:MI') AS RSUInDate,    
                                    TO_CHAR(RSU.rsu_out_date,    'DD/MM/YYYY HH24:MI') AS RSUOutDate,   
                                    RSU.rsu_in_remark           AS RSUInRemark,      
                                    RSU.rsu_out_remark          AS RSUOutRemark      
                                FROM {_schema}.tb_tran_rsu RSU
                                INNER JOIN {_schema}.tb_tran_gate Gate 
                                    ON RSU.rsu_barcode = Gate.gate_barcode
                                    AND Gate.gate_dc_code = @DCCode
                                    AND Gate.gate_out_direct = '0'
                                    AND Gate.gate_status = '1'
                                INNER JOIN {_schema}.tb_truck_type TruckType 
                                    ON TruckType.truck_type_id = Gate.gate_type_truck
                                INNER JOIN {_schema}.tb_rsu_status RSUStatus 
                                    ON Gate.gate_rsu_id = RSUStatus.rsu_status_id
                                INNER JOIN {_schema}.tb_status_style Style 
                                    ON Style.status_id = RSU.rsu_status
                                WHERE 1=1 and gate.gate_out_direct = '0' 
                                and gate_action_date = current_date
                                and rsu_in_date::date = CURRENT_DATE
                                and gate.gate_status = '1'";

                int? truckTypeParam = null;
                if (TruckTypeID != "All" && !string.IsNullOrEmpty(TruckTypeID))
                {
                    sql += " AND TruckType.truck_type_id = @TruckTypeID";
                    if (int.TryParse(TruckTypeID, out int tid)) truckTypeParam = tid;
                }

                int? statusParam = null;
                if (StatusRSU != "All" && !string.IsNullOrEmpty(StatusRSU))
                {
                    sql += " AND RSU.rsu_status = @StatusRSU";
                    if (int.TryParse(StatusRSU, out int sid)) statusParam = sid;
                }

                sql += " ORDER BY RSU.created_date DESC LIMIT 20";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_RSUInOut>(sql, new
                    {
                        DCCode = DCCode,
                        TruckTypeID = truckTypeParam,
                        StatusRSU = statusParam,
                    });

                    return result.ToList();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw new Exception($"Failed to retrieve RSU records: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> StampRSUIn(M_RSUInOut CLS_RSUIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql_Insert = $@"INSERT INTO
	                                            {_schema}.tb_tran_rsu (
	                                            rsu_id,
	                                            rsu_barcode,
	                                            rsu_action_by,
	                                            rsu_in_date,
	                                            rsu_in_remark,
	                                            created_by,
	                                            created_date,
	                                            flag,
	                                            rsu_status,
	                                            rsu_dc_code,
	                                            rsu_gate_id)
                                            VALUES ( 
	                                            nextval('{_schema}.tb_tran_rsu_rsu_id_seq'::regclass) ,
	                                            @Barcode ,
	                                            @ActionByRSUIn,
	                                            @PlanDate::timestamp,
	                                            @RSUIn_Remark ,
	                                            @CreatedBy ,
	                                            current_timestamp ,
	                                            '1',
	                                            1,
	                                            @DCCode ,
	                                            (
	                                            SELECT
		                                            gate_id
	                                            FROM
		                                            {_schema}.tb_tran_gate
	                                            WHERE
		                                            gate_barcode = @Barcode
		                                            and gate_status = '1'
                                                    and gate_action_date = @PlanDate::date
	                                            ORDER BY
		                                            gate_id desc
	                                            LIMIT 1) )";


                        var sql_update_plan = $@"UPDATE {_schema}.tb_tran_gate 
                                                    SET last_process = 'RSU In' 
                                                 WHERE gate_barcode = @Barcode 
                                                    AND gate_action_date = @PlanDate::date
                                                    AND gate_status = '1' ";

                        //check แล้ว Insert ตอน Geat In 
                        var sql_update_current = $@" UPDATE {_schema}.tb_wip_current
                                                SET status = 2
                                                WHERE
                                                    barcode = @Barcode
	                                            AND dc_code = @DCCode ";

                        //check แล้ว Insert ตอน Gate In ถูก comment
                        var sql_update_sum_date = $@" UPDATE {_schema}.tb_monitor_summary_gate
                                                            SET truck_rsu = truck_rsu + 1
                                                       WHERE dc_code = @DCCode
                                                            AND cast(coalesce(truck_type_id, '0') as integer) = (
                                                    SELECT gate_type_truck 
                                                       FROM {_schema}.tb_tran_gate
                                                       WHERE gate_barcode = @Barcode
                                                         AND gate_status = '1' LIMIT 1)
                                                         AND plan_action_date = @PlanDate";

                        string PlanDate = "";
                        if (DateTime.Now.Hour < 8)
                        {
                            PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                        }

                        var parameters = new
                        {
                            Barcode = CLS_RSUIn.Barcode,
                            ActionByRSUIn = CLS_RSUIn.ActionBy,
                            current_date = DateTime.UtcNow,
                            RSUIn_Remark = CLS_RSUIn.RSUInRemark,
                            CreatedBy = CLS_RSUIn.ActionBy,
                            DCCode = CLS_RSUIn.DCCode,
                            PlanDate = PlanDate
                        };

                        var rowsAffected = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                        await connection.ExecuteAsync(sql_update_plan, parameters, transaction);
                        await connection.ExecuteAsync(sql_update_current, parameters, transaction);
                        //await connection.ExecuteAsync(sql_update_sum_date, parameters, transaction);

                        if (rowsAffected == 0)
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
        public async Task<bool> StampRSUOut(M_RSUInOut CLS_RSUIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql_update_rsu = $@"UPDATE {_schema}.tb_tran_rsu 
                                                SET rsu_out_date = now(),
                                                    rsu_out_remark = @RSUOutRemark,
                                                    updated_date = now(),
                                                    updated_by = @CreatedBy,
                                                    rsu_out_by = @ActionByOut,
                                                    rsu_status = '2'
                                                WHERE rsu_barcode = @Barcode
                                                    AND rsu_gate_id = (
                                                        SELECT gate_id 
                                                        FROM {_schema}.tb_tran_gate 
                                                        WHERE gate_barcode = @Barcode 
                                                          AND gate_status = '1' 
                                                          AND gate_action_date = current_date
                                                        ORDER BY gate_id DESC 
                                                        LIMIT 1
                                                    )
                                                    AND rsu_dc_code = @DCCode
                                                    AND rsu_in_date::date = CURRENT_DATE
                                                    AND rsu_status = '1'";

                        var parameters = new
                        {
                            Barcode = CLS_RSUIn.Barcode,
                            ActionByOut = CLS_RSUIn.ActionBy,
                            RSUOutRemark = CLS_RSUIn.RSUOutRemark,
                            CreatedBy = CLS_RSUIn.ActionBy,
                            DCCode = CLS_RSUIn.DCCode
                        };

                        var rowsAffected = await connection.ExecuteAsync(sql_update_rsu, parameters, transaction);
                        if (rowsAffected == 0)
                        {
                            Console.WriteLine($"Update failed: Barcode={CLS_RSUIn.Barcode}, DC={CLS_RSUIn.DCCode}");
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
        public async Task<bool> StampRSUInOut(M_RSUInOut CLS_RSUIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql_Insert = $@"INSERT INTO
	                                            {_schema}.tb_tran_rsu (
	                                            rsu_id, 
                                                rsu_barcode, 
                                                rsu_action_by, 
                                                rsu_in_date, 
                                                rsu_in_remark, 
                                                created_by, 
                                                created_date, 
                                                flag, 
                                                rsu_out_remark,
                                                rsu_status, 
                                                rsu_dc_code, 
                                                rsu_gate_id)
                                            VALUES ( 
	                                            nextval('{_schema}.tb_tran_rsu_rsu_id_seq'::regclass) ,
	                                            @Barcode ,
	                                            @CreatedBy,
	                                            current_timestamp,
	                                            'เข้า RSU โดยไม่คืนสินทรัพย์' ,
	                                            @CreatedBy ,
	                                            now() ,
	                                            '1',
                                                @Remark,
	                                            @RSU_StatusID,
	                                            @DCCode ,
                                                @GateId )";


                        var sql_update_gate = $@"UPDATE {_schema}.tb_tran_gate 
                                                    SET last_process = 'RSU Out' 
                                                 WHERE gate_barcode = @Barcode  
                                                 AND gate_action_date = @PlanDate";

                        string PlanDate = "";
                        if (DateTime.Now.Hour < 8)
                        {
                            PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                        }

                        var parameters = new
                        {
                            Barcode = CLS_RSUIn.Barcode,
                            GateId = Convert.ToInt16(CLS_RSUIn.GateID),
                            RSU_StatusID = Convert.ToInt16("2"),
                            ActionBy = CLS_RSUIn.ActionBy,
                            CreatedBy = CLS_RSUIn.ActionBy ?? CLS_RSUIn.ActionBy,
                            Remark = CLS_RSUIn.RSUOutRemark ?? "Not Return",
                            DCCode = CLS_RSUIn.DCCode,
                            PlanDate = DateTime.Parse(PlanDate)
                        };

                        var rows_Insert_rsu = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                        var rows_Update_gate = await connection.ExecuteAsync(sql_update_gate, parameters, transaction);


                        if (rows_Insert_rsu == 0 || rows_Update_gate == 0)
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
        public async Task<bool> DeleteRSUIn(M_RSUInOut CLS_RSUIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        var sql_Delete = $@"delete from {_schema}.tb_tran_rsu where rsu_gate_id = @GateID ";

                        var sql_update_gate = $@"Update {_schema}.tb_tran_gate 
                                                    SET last_process = 'Gate In' 
                                                 WHERE gate_id = CAST(@GateID AS INTEGER) 
                                                    AND gate_status = '1'";


                        //var sql_update_rsu = $@" UPDATE {_schema}.tb_wip_current
                        //                            SET status = '1'
                        //                         WHERE 1=1
                        //                            AND barcode = (select gate_barcode from {_schema}.tb_tran_gate
                        //                                              where gate_id = @GateID)
                        //                         AND dc_code = @DCCode ";

                        var parameters = new
                        {
                            GateID = Convert.ToInt16(CLS_RSUIn.GateID),
                            RSUID = int.TryParse(CLS_RSUIn.RSUID, out var rid) ? rid : 0,
                            DCCode = CLS_RSUIn.DCCode
                        };

                        var row_delete = await connection.ExecuteAsync(sql_Delete, parameters, transaction);
                        var row_update_date = await connection.ExecuteAsync(sql_update_gate, parameters, transaction);

                        if (row_delete == 0 || row_update_date == 0)
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
        public async Task<bool> chkDupRSUInBarcode(string Gateid)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = $@"SELECT 1 
                                 FROM {_schema}.tb_tran_rsu 
                                 WHERE rsu_gate_id = @Gateid 
                                 LIMIT 1";

                    var parameters = new
                    {
                        Gateid = Convert.ToInt16(Gateid)
                    };

                    var result = await connection.QueryFirstOrDefaultAsync<int?>(sql, parameters);

                    return result.HasValue;
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
    }
}
