using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using QMSWebApiCore.Models;
using System.Data;

namespace QMSWebApiCore.Services
{
    public class GateOutRepository(IConfiguration configuration) : IGateOutRepository
    {
        private readonly string _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
            ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentException("Connection string is required");
        public async Task<List<M_GateOut>> GetAllGateOutStatusAsync(M_GateOut ClsGateOut)
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
                            GATE.gate_truck_in AS GateInDate,
                            GATE.gate_truck_out AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            STATUS.rsu_status_text AS RSUStatusText,
                            CASE WHEN GATE.gate_status = 1 then 'รถเข้าคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 1 then STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 2 then STATUS.rsu_status_text
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        INNER JOIN {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id
                        WHERE GATE.gate_dc_code = @DCCode
                        ORDER BY GATE.gate_id DESC";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(sql, new { DCCode = ClsGateOut.DCCode });
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
        public async Task<List<M_GateOut>> GetAllGateOutAsync(string DCCode)
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
                            GATE.gate_truck_in AS GateInDate,
                            GATE.gate_truck_out AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage
                        FROM outbound_qms_sim.tb_tran_gate GATE 
                        LEFT JOIN outbound_qms_sim.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        LEFT join outbound_qms_sim.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id 
                        WHERE GATE.gate_dc_code = @DCCode
                               AND gate_status = 2";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(sql, new { DCCode = DCCode });
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
        public async Task<List<M_GateOut>> GatGateOutByBarcodeOnceAsync(string Barcode, string DCCode)
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
                            GATE.gate_truck_in AS GateInDate,
                            GATE.gate_truck_out AS GateOutDate,
                            GATE.gate_status AS GateStatus,
                            TRUCKTYPE.truck_type_name AS TruckTypeText,
                            TRUCKTYPE.truck_type_image AS TruckTypeImage,
                            STATUS.rsu_status_text AS RSUStatusText,
                            CASE WHEN GATE.gate_status = 1 then 'รถเข้าคลังสินค้า'
		                    	 WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 1 then STATUS.rsu_status_text
		                    	 WHEN GATE.gate_status = 1 and GATE.gate_rsu_id = 2 then STATUS.rsu_status_text
		                    	end as gate_status_text
                        FROM {_schema}.tb_tran_gate GATE 
                        INNER JOIN {_schema}.tb_truck_type TRUCKTYPE 
                            ON GATE.gate_type_truck = TRUCKTYPE.truck_type_id 
                        INNER JOIN {_schema}.tb_rsu_status STATUS 
                            ON GATE.gate_rsu_id = STATUS.rsu_status_id
                        WHERE GATE.gate_dc_code = @DCCode
                            AND GATE.gate_barcode = @Barcode
                            ORDER BY GATE.gate_id DESC";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_GateOut>(sql, new { Barcode = Barcode, DCCode = DCCode });
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
        //ต้องลบออกไหมM
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
	  	                    GATE.gate_truck_in,
	  	                    GATE.gate_truck_out,
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
                            where  1=1  ";

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
                                GateInDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                                GateOutDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
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
        public async Task<bool> StampGateOutAsync(M_GateOut ClsGateOut)
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
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateOut.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_truck_out", (object?)ClsGateOut.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", "2");
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)ClsGateOut.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)ClsGateOut.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> StampGateOutDirectAsync(M_GateOut ClsGateOut)
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
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateOut.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_truck_out", (object?)ClsGateOut.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@last_process", "Gate Out");
                    command.Parameters.AddWithValue("@gate_status", "2");
                    command.Parameters.AddWithValue("@gate_out_remark", (object?)ClsGateOut.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_out_lps_name", (object?)ClsGateOut.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> UpdateGateOutAsync(M_GateOut ClsGateOut)
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
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateOut.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)ClsGateOut.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_type_truck", ClsGateOut.TruckTypeID);
                    command.Parameters.AddWithValue("@gate_license", (object?)ClsGateOut.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)ClsGateOut.RSUStatusID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)ClsGateOut.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_by", (object?)ClsGateOut.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date", now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;

                }
            }
        }
        public async Task<bool> DeleteGateOutAsync(M_GateOut ClsGateOut)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"DELETE FROM {_schema}.tb_tran_gate  WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", ClsGateOut.Barcode ?? DBNull.Value.ToString());

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
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
    }
}
