using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QMSWebApiCore.Models;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace QMSWebApiCore.Services
{
    public interface ITruckOnDockRepository
    {
        Task<List<M_TruckOnDock>> GetPlanListTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK);
        Task<List<M_TruckOnDock>> GetTruckOnDockByBarcode(string Barcode, string DCCode);
        Task<bool> StampTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK);
        Task<bool> DeleteTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK);
        Task<bool> TruckOnDockDuplicateBarcode(string barcode, string PlanDate, string DCCode);
    }
    public class TruckOnDockRepository : ITruckOnDockRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public TruckOnDockRepository(IConfiguration configuration)
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
        //truck on dock on load
        public async Task<List<M_TruckOnDock>> GetPlanListTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT
                                                    TO_CHAR(sp.tms_summary_plan_action_date, 'DD/MM/YYYY')          AS PlanDate,
                                                    sp.tms_summary_plan_no                                           AS PlanNo,
                                                    sp.tms_summary_plan_group                                        AS PlanGroupNo,
                                                    sp.tms_summary_load_no                                           AS PlanLoadNo,
                                                    sp.tms_summary_dock_no                                           AS PlanDockNo,
                                                    tod.truckondock_barcode                                          AS Barcode,
                                                    g.gate_license                                                   AS LicenseTruck,
                                                    g.gate_type_truck                                                AS TruckTypeID,
                                                    tt.truck_type_name                                               AS TruckTypeText,
                                                    tt.truck_type_image                                              AS TruckTypeImage,
                                                    tod.truckondock_remark                                           AS TruckOnDockRemark,
                                                    sp.tms_summary_plan_actual_pre_cool_status                       AS PrecoolStatus,
                                                    COALESCE(sp.tms_summary_plan_actual_on_dock_status, '0')         AS TruckOnDockStatusID,
                                                    TO_CHAR(sp.tms_summary_plan_actual_on_dock, 'DD/MM/YYYY HH24:MI') AS ActualTruckOnDockTime,
                                                    COALESCE(sp.tms_summary_plan_actual_edp_in_status,  '0')         AS EDPINStatus,
                                                    COALESCE(sp.tms_summary_plan_actual_edp_out_status, '0')         AS EDPOutStatus,
                                                    ps.precool_status_text                                           AS PrecoolStatusText
                                                FROM {_schema}.tb_tms_summary_plan sp
                                                LEFT JOIN {_schema}.tb_tran_truckondock tod
                                                    ON tod.truckondock_plan_no = sp.tms_summary_plan_no
                                                LEFT JOIN {_schema}.tb_tran_gate g
                                                    ON  g.gate_barcode = tod.truckondock_barcode
                                                    AND g.gate_status  = '1'
                                                    AND g.gate_out_direct = '0'
                                                    AND g.gate_action_date = CURRENT_DATE
                                                LEFT JOIN {_schema}.tb_truck_type tt
                                                    ON tt.truck_type_id = g.gate_type_truck
                                                LEFT JOIN {_schema}.tb_precool_status ps
                                                    ON CAST(COALESCE(ps.precool_status_id, '0') AS INTEGER) = CAST(COALESCE(sp.tms_summary_plan_actual_pre_cool_status, '0') AS INTEGER)
                                                WHERE
                                                    sp.flag = '1'
                                                    AND sp.tms_summary_dc_code = @DCCode
                                                    AND sp.tms_summary_plan_actual_pre_cool_status = '1'
                                                    AND sp.tms_summary_plan_action_date = CAST(@PlanDate AS DATE) ");

                string PlanDate = "";
                if (DateTime.Now.Hour < 8)
                {
                    PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                }
                else
                {
                    PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                }

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", CLS_TRUCKONDOCK.DCCode);
                parameters.Add("PlanDate", PlanDate);

                if (!string.IsNullOrEmpty(CLS_TRUCKONDOCK.PlanDockNo))
                {
                    sql.AppendLine(" AND sp.tms_summary_dock_no = @PlanDockNo ");
                    parameters.Add("PlanDockNo", CLS_TRUCKONDOCK.PlanDockNo);
                }

                if (!string.IsNullOrEmpty(CLS_TRUCKONDOCK.Search))
                {
                    sql.Append(" AND (sp.tms_summary_dock_no LIKE @Search OR sp.tms_summary_plan_group LIKE @Search OR sp.tms_summary_load_no LIKE @Search)");
                    parameters.Add("Search", $"%{CLS_TRUCKONDOCK.Search}%");
                }

                if (!string.IsNullOrEmpty(CLS_TRUCKONDOCK.PlanBigCType) && CLS_TRUCKONDOCK.PlanBigCType != "All")
                {
                    if (CLS_TRUCKONDOCK.PlanBigCType == "HYPER")
                    {
                        sql.AppendLine(" AND substring(trim(sp.tms_summary_plan_no), 1, 2) = 'PH'");
                    }
                    else
                    {
                        sql.AppendLine(" AND substring(trim(sp.tms_summary_plan_no), 1, 2) = 'PM'");
                    }
                }

                if (!string.IsNullOrEmpty(CLS_TRUCKONDOCK.PlanGroupNo) && CLS_TRUCKONDOCK.PlanGroupNo != "All")
                {
                    sql.AppendLine(" AND sp.tms_summary_plan_group = @PlanGroupNo");
                    parameters.Add("PlanGroupNo", CLS_TRUCKONDOCK.PlanGroupNo);
                }

                if(!string.IsNullOrEmpty(CLS_TRUCKONDOCK.TruckOnDockStatusID ))
                {
                    sql.Append("AND COALESCE(sp.tms_summary_plan_actual_on_dock_status) = @TruckOnDockStatusID ");
                    parameters.Add("TruckOnDockStatusID", CLS_TRUCKONDOCK.TruckOnDockStatusID);
                }

                sql.AppendLine(" ORDER BY sp.tms_summary_plan_action_date desc LIMIT 10");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_TruckOnDock>(
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
        public async Task<List<M_TruckOnDock>> GetTruckOnDockByBarcode(string Barcode, string DCCode)
        {
            try
            {
                var sql = $@"SELECT ROW_NUMBER() OVER (ORDER BY GATE.gate_id) AS Gate_id,
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
                    var result = await connection.QueryAsync<M_TruckOnDock>(sql, new { Barcode = Barcode, DCCode = DCCode });
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
        public async Task<bool> StampTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_truckondock (   
                                                        truckondock_plan_no,
	                                                    truckondock_load_no,
	                                                    truckondock_barcode,
	                                                    truckondock_remark,
	                                                    truckondock_status,
	                                                    created_by,
	                                                    created_date,
	                                                    flag,
	                                                    truckondock_dc_code,
	                                                    gate_id)
                                            VALUES (  
                                                        @PlanNo,
                                                        @LoadNo,
                                                        @Barcode,
                                                        @TruckRemarck,
                                                        '1',
                                                        @created_by,
                                                        current_timestamp,
                                                        '1',
                                                        @truckondock_dc_code,
                                                        (   SELECT
	                                                            gate_id
                                                            FROM
	                                                            {_schema}.tb_tran_gate
                                                            WHERE
	                                                            gate_barcode = @Barcode
	                                                        AND gate_status = '1'
                                                            AND gate_action_date = CAST(@PlanDate AS DATE)
                                                            ORDER BY gate_id desc
                                                            LIMIT 1))";


                        var sql_update_plan = $@" UPDATE {_schema}.tb_tms_summary_plan
                                                SET tms_summary_plan_actual_on_dock_status = '1',
	                                                tms_summary_plan_actual_on_dock = now(),
                                                    tms_summary_barcode = @Barcode
                                                WHERE
                                                    tms_summary_plan_no = @PlanNo
                                                AND tms_summary_dc_code = @DCCode ";

                        var sql_update_gate = $@" UPDATE {_schema}.tb_tran_gate
                                                SET last_process = 'Truck On Dock'
                                                WHERE
                                                    gate_barcode = @Barcode
                                                AND gate_action_date = CAST(@PlanDate AS DATE)
                                                AND gate_status = '1' ";

                        var sql_update_current = $@" UPDATE {_schema}.tb_wip_current
                                                SET status = '4' , plan_no = @PlanNo
                                                WHERE barcode = @Barcode
                                                AND dc_code = @DCCode ";

                        var parameters = new
                        {
                            LoadNo = CLS_TRUCKONDOCK.PlanLoadNo,
                            Barcode = CLS_TRUCKONDOCK.Barcode,
                            DCCode = CLS_TRUCKONDOCK.DCCode,
                            PlanNo = CLS_TRUCKONDOCK.PlanNo,
                            PlanDate = CLS_TRUCKONDOCK.PlanActionDate ?? CLS_TRUCKONDOCK.ActionDate, // fallback just in case
                            TruckRemarck = CLS_TRUCKONDOCK.TruckOnDockRemark,
                            created_by = CLS_TRUCKONDOCK.CreateBy,
                            truckondock_dc_code = CLS_TRUCKONDOCK.DCCode
                        };

                        var insertTask = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                        var updatePlanTask = await connection.ExecuteAsync(sql_update_plan, parameters, transaction);
                        var updateGateTask = await connection.ExecuteAsync(sql_update_gate, parameters, transaction);
                        var updateCurrentTask = await connection.ExecuteAsync(sql_update_current, parameters, transaction);

                        if (insertTask == 0 || updatePlanTask == 0 || updateGateTask == 0 )
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                        await transaction.CommitAsync();
                        return true;
                    }
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
        public async Task<bool> DeleteTruckOnDock(M_TruckOnDock CLS_TRUCKONDOCK)
        {
            //เพิ่ม rollback
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = $@"DELETE FROM {_schema}.tb_tran_gate  WHERE gate_barcode = @gate_barcode";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@gate_barcode", CLS_TRUCKONDOCK.Barcode ?? DBNull.Value.ToString());
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> TruckOnDockDuplicateBarcode(string barcode, string PlanDate, string DCCode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;
            try
            {
                var sql = $@"SELECT
                                    COUNT(1)
                             FROM   {_schema}.tb_tran_truckondock truckondock
                             INNER JOIN 
                                    {_schema}.tb_tms_summary_plan summary on
	                                truckondock.truckondock_plan_no = summary.tms_summary_plan_no
                             INNER JOIN 
                                    {_schema}.tb_tran_gate gate on
	                                gate.gate_barcode = truckondock.truckondock_barcode
                             WHERE
	                                summary.tms_summary_plan_actual_on_dock_status = '1'
	                                and truckondock.truckondock_barcode = @Barcode
	                                and tms_summary_dc_code = @DCCode
                                    and tms_summary_plan_action_date::date = CAST(@PlanDate AS DATE)
	                                and gate.gate_status = '1' ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var count = await connection.ExecuteScalarAsync<int>(sql, new { Barcode = barcode, PlanDate = PlanDate, DCCode = DCCode });
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
