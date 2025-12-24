using Npgsql;
using NpgsqlTypes;
using QMSWebApiCore.Models;
using System.Data;

namespace QMSWebApiCore.Services
{
    public class GateRepository : IGateRepository
    {
        private readonly string _connectionString;
        private readonly string _schema;
        public GateRepository(IConfiguration configuration)
        {
            _schema = configuration.GetConnectionString("DefaultSchema")
                ?? throw new ArgumentNullException("Connection string not found");

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string not found");    
        }

        // GET All - Raw SQL
        public async Task<IEnumerable<GateIn>> GetAllGateInAsync()
        {
            var gates = new List<GateIn>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT 
                            GATE.gate_barcode,
	  	                    GATE.gate_type_truck,
	  	                    GATE.gate_license,
	  	                    GATE.gate_driver_name,
	  	                    GATE.gate_action_by,
	  	                    GATE.gate_rsu_id,
	  	                    GATE.gate_in_remark,
	  	                    GATE.gate_out_remark,
	  	                    GATE.gate_truck_in,
	  	                    GATE.gate_truck_out,
	  	                    GATE.gate_status,
		                    TRUCKTYPE.truck_type_name,
		                    TRUCKTYPE.truck_type_image,
		                    STATUS.rsu_status_text 
                    FROM outbound_qms_sim.tb_tran_gate GATE 
                    INNER JOIN  outbound_qms_sim.tb_truck_type TRUCKTYPE 
		                    ON GATE.gate_type_truck  = TRUCKTYPE.truck_type_id 
                    INNER JOIN outbound_qms_sim.tb_rsu_status STATUS 
		                    ON GATE.gate_rsu_id = STATUS.rsu_status_id";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            gates.Add(new GateIn
                            {
                                Barcode = reader.GetString(0),
                                TruckTypeID = reader.GetInt32(1),
                                LicenseTruck = reader.GetString(2),
                                DriverName = reader.GetString(3),
                                ActionBy = reader.GetString(4),
                                RSUStatusID = reader.GetInt32(5),
                                GateInRemark = reader.IsDBNull(6) ? null : reader.GetString(6),
                                GateOutRemark = reader.IsDBNull(7) ? null : reader.GetString(7),
                                GateInDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                                GateOutDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                                GateStatus = reader.GetInt32(10),
                                TruckTypeText = reader.GetString(11),
                                TruckTypeImage = reader.GetString(12),
                                RSUStatusText = reader.GetString(13)
                            });
                        }
                    }
                }
            }

            return gates;
        }

        // GET by Barcode - Raw SQL
        public async Task<GateIn?> GetGateInByBarcodeAsync(string Barcode)
        {
            GateIn? gate = null;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT  
                            gate_barcode , 
                            gate_action_by,
                            gate_driver_name , 
                            gate_type_truck, 
                            gate_license, 
                            gate_rsu_id 
                        FROM outbound_qms_sim.tb_tran_gate
                        WHERE gate_barcode = @Barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    // ป้องกัน SQL Injection
                    command.Parameters.AddWithValue("@Barcode", Barcode);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            gate = new GateIn
                            {
                                Barcode = reader.GetString(0),
                                ActionBy = reader.GetString(1),
                                DriverName = reader.GetString(2),
                                TruckTypeID = reader.GetInt32(3),
                                LicenseTruck = reader.GetString(4),
                                RSUStatusID = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }

            return gate;
        }

        // GET by Detail - Raw SQL
        public async Task<GateIn?> GetGateInByDetailAsync(int Gate_id, string DCCode)
        {
            GateIn? gate = null;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT 	GATE.gate_id,
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

                            FROM outbound_qms_sim.tb_tran_gate GATE 
                            INNER JOIN outbound_qms_sim.tb_truck_type TRUCKTYPE 
		                            on GATE.gate_type_truck  = TRUCKTYPE.truck_type_id 
                            INNER JOIN outbound_qms_sim.tb_tran_edp EDP
		                            on GATE.gate_barcode = EDP.edp_barcode
                            LEFT JOIN outbound_qms_sim.tb_tran_rsu RSU
		                            on GATE.gate_rsu_id  = RSU.rsu_id 
                            INNER JOIN outbound_qms_sim.tb_rsu_status RSU_STATUS 
		                            on GATE.gate_rsu_id = RSU_STATUS.rsu_status_id 

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
                            gate = new GateIn
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

        // CREATE - Raw SQL
        public async Task<GateIn> CreateGateInAsync(GateIn ClsGateIn)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                    INSERT INTO outbound_qms_sim.tb_tran_gate (
                                        gate_id ,
	                                    gate_driver_name ,
	                                    gate_type_truck ,
	                                    gate_license ,
	                                    gate_action_by ,
	                                    gate_action_date ,
	                                    gate_rsu_id ,
	                                    gate_barcode ,
	                                    gate_in_remark ,
	                                    gate_truck_in ,
	                                    gate_dc_code ,
	                                    created_by ,
	                                    created_date ,
	                                    flag 
                    ) 
                    VALUES (
                                        nextval('outbound_qms_sim.tb_tran_gate_gate_id_seq'::regclass) ,
                                        @gate_driver_name ,
                                        @gate_type_truck ,
                                        @gate_barcode ,
                                        @gate_license ,
                                        @gate_action_by ,
                                        @gate_action_date ,
                                        @gate_rsu_id ,                                      
                                        @gate_in_remark ,
                                        @gate_truck_in ,
                                        @gate_dc_code ,
                                        @created_by ,
                                        @created_date ,                                       
                                        @flag)";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)ClsGateIn.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_type_truck", ClsGateIn.TruckTypeID);
                    command.Parameters.AddWithValue("@gate_barcode", (object?)ClsGateIn.Barcode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_license", (object?)ClsGateIn.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_action_by", (object?)ClsGateIn.ActionBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_action_date", DateTime.UtcNow.Date);
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)ClsGateIn.RSUID ?? DBNull.Value);         
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)ClsGateIn.GateInRemark ?? DBNull.Value);                                        
                    command.Parameters.AddWithValue("@gate_truck_in", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@gate_dc_code", (object?)ClsGateIn.DCCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@created_by", (object?)ClsGateIn.CreatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@created_date", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@flag",true);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new GateIn
                            {
                                GateID = reader.GetInt32(0),
                                DriverName = reader.GetString(1),
                                TruckTypeID = reader.GetInt32(2),
                                Barcode = reader.GetString(3),
                                LicenseTruck = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ActionBy = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RSUID = reader.GetInt32(6),
                                GateInRemark = reader.GetString(7),
                                GateInDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                                CreatedDate = reader.GetDateTime(9),
                                FLAG = reader.GetBoolean(10)
                            };
                        }
                    }
                }
            }

            throw new Exception("Failed to create gate record");
        }

        // UPDATE - Raw SQL
        public async Task<bool> UpdateGateInAsync(string Barcode, GateIn ClsGateIn)
        {
            var now = DateTime.UtcNow;
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"UPDATE outbound_qms_sim.tb_tran_gate 
                            SET gate_driver_name = @gate_driver_name,
                                gate_license = @gate_license,
                                gate_rsu_id = @gate_rsu_id,
                                gate_in_remark = @gate_in_remark,
                                updated_by = @updated_by,
                                updated_date = @updated_date
                            WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", ClsGateIn.Barcode);
                    command.Parameters.AddWithValue("@gate_driver_name", (object?)ClsGateIn.DriverName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_license", (object?)ClsGateIn.LicenseTruck ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_rsu_id", (object?)ClsGateIn.RSUID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gate_in_remark", (object?)ClsGateIn.GateInRemark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_by", (object?)ClsGateIn.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@updated_date",now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        // DELETE - Raw SQL
        public async Task<bool> DeleteGateInAsync(string Barcode)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = "DELETE FROM {_schema}.tb_tran_gate  WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", Barcode);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}