using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System.Data;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace QMSWebApiCore.Services
{
    public interface IPreCoolRepository
    {
        Task<bool> StampPreCoolAsync(M_Precool CLS_PRECOOL);
        Task<bool> CancelPreCoolPass(M_Precool CLS_PRECOOL);
        Task<List<M_Precool>> GetPreCoolFailAsync(M_Precool CLS_PRECOOL);
        Task<List<M_Precool>> GetPreCoolListAsync(M_Precool CLS_PRECOOL);
    }
    public class PreCoolRepository : IPreCoolRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public PreCoolRepository(IConfiguration configuration)
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
        //list precool ที่แสดงหน้า Main Precool
        public async Task<List<M_Precool>> GetPreCoolListAsync(M_Precool CLS_PRECOOL)
        {
            try
            {
                var sql = new StringBuilder($@" SELECT TO_CHAR(summaryPlan.tms_summary_plan_action_date, 'DD/MM/YYYY') AS PlanDate,
	                                                summaryPlan.tms_summary_plan_no AS PlanNo,
	                                                summaryPlan.tms_summary_plan_group AS PlanGroupNo,
	                                                summaryPlan.tms_summary_load_no AS PlanLoadNo ,
	                                                summaryPlan.tms_summary_dock_no AS PlanDockNo,
	                                                TO_CHAR(summaryPlan.tms_summary_plan_pre_cool, 'DD/MM/YYYY HH24:mi') AS PlanPreCool,
	                                                summaryplan.tms_summary_plan_actual_pre_cool_status AS PrecoolStatus ,
	                                                TO_CHAR(summaryplan.tms_summary_plan_actual_pre_cool, 'DD/MM/YYYY HH24:mi') AS LastActionPreCool,
	                                                summaryplan.tms_summary_plan_actual_pre_cool_status AS ActualPreCoolStatus,
	                                                TO_CHAR(plan.PlanPreCool, 'DD/MM/YYYY HH24:mi') AS PlanPreCoolAlias ,
	                                                cast(coalesce(summaryplan.tms_summary_plan_actual_pre_cool_status, '0') AS integer) AS PreCoolStatusID ,
	                                   	            status.precool_status_text AS PreCoolStatusText ,
	                                                CountNotPass AS PlanPreCoolCountNotPass
                                                FROM
	                                                {_schema}.tb_tms_summary_plan summaryPlan
                                                LEFT JOIN {_schema}.tb_tran_truckondock truckondock on
	                                                truckondock.truckondock_plan_no = summaryplan.tms_summary_plan_no
                                                LEFT JOIN (
	                                                SELECT
		                                                max(tms_plan_pre_cool) AS PlanPreCool,
		                                                tms_plan_no
	                                                FROM
		                                                {_schema}.tb_tms_plan
                                                    WHERE
                                                        tms_plan_action_date:: DATE BETWEEN CURRENT_DATE - 1 AND CURRENT_DATE
                                                        AND tms_plan_dc_code = @DCCode
	                                                group by
		                                                tms_plan_no) plan on
	                                                plan.tms_plan_no = summaryPlan.tms_summary_plan_no
                                               LEFT JOIN (
	                                                SELECT
		                                                COUNT(precool_id) AS CountNotPass,
		                                                precool_plan_no
	                                                FROM
		                                                {_schema}.tb_tran_precool
	                                                WHERE
		                                                precool_status = '0'
                                                        AND precool_action_date:: DATE = CURRENT_DATE
                                                        AND precool_dc_code = @DCCode
	                                                GROUP BY
		                                                precool_plan_no) notpass on notpass.precool_plan_no = summaryplan.tms_summary_plan_no
                                                LEFT JOIN {_schema}.tb_precool_status status on
	                                                cast(coalesce(status.precool_status_id, '0') AS integer) = cast(coalesce(summaryplan.tms_summary_plan_actual_pre_cool_status , '0') as integer)
                                                WHERE summaryPlan.flag = '1'
	                                                AND summaryPlan.tms_summary_plan_action_date:: DATE = CURRENT_DATE
	                                                AND tms_summary_dc_code = @DCCode ");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", CLS_PRECOOL.DCCode);

                if (!string.IsNullOrEmpty(CLS_PRECOOL.Search))
                {
                    sql.Append(" AND (summaryPlan.tms_summary_dock_no LIKE @Search OR summaryPlan.tms_summary_plan_group LIKE @Search OR summaryPlan.tms_summary_load_no LIKE @Search)");
                    parameters.Add("Search", $"%{CLS_PRECOOL.Search}%");
                }

                if (!string.IsNullOrEmpty(CLS_PRECOOL.PlanBigCType) && CLS_PRECOOL.PlanBigCType != "All")
                {
                    if (CLS_PRECOOL.PlanBigCType == "HYPER")
                    {
                        sql.AppendLine(" AND substring(trim(summaryPlan.tms_summary_plan_no), 1, 2) = 'PH'");
                    }
                    else
                    {
                        sql.AppendLine(" AND substring(trim(summaryPlan.tms_summary_plan_no), 1, 2) = 'PM'");
                    }
                }

                if (!string.IsNullOrEmpty(CLS_PRECOOL.PlanGroupNo) && CLS_PRECOOL.PlanGroupNo != "All")
                {
                    sql.AppendLine(" AND summaryPlan.tms_summary_plan_group = @PlanGroupNo");
                    parameters.Add("PlanGroupNo", CLS_PRECOOL.PlanGroupNo);
                }

                if (!string.IsNullOrEmpty(CLS_PRECOOL.PreCoolStatusID) && CLS_PRECOOL.PreCoolStatusID != "All")
                {
                    sql.AppendLine(" AND cast(coalesce(summaryplan.tms_summary_plan_actual_pre_cool_status, '0') AS varchar) = @PreCoolStatusID");
                    parameters.Add("PreCoolStatusID", CLS_PRECOOL.PreCoolStatusID);
                }

                if (CLS_PRECOOL.PreCoolStatusID == "All")
                {
                    sql.AppendLine($@"AND summaryPlan.tms_summary_plan_no not in (
	                                            SELECT      precool.precool_plan_no
                                                FROM        {_schema}.tb_tran_precool precool
	                                            WHERE       precool_action_date:: DATE = CURRENT_DATE
                                                AND         precool_dc_code = @DCCode
	                                            GROUP BY    precool.precool_plan_no) ");
                }

                sql.AppendLine(" ORDER BY summaryPlan.tms_summary_plan_no LIMIT 100");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Precool>(
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
        //ไว้ดึงรายการ Precool ที่ไม่ผ่านการตรวจอุณภูมิ
        public async Task<List<M_Precool>> GetPreCoolFailAsync(M_Precool CLS_PRECOOL)
        {
            try
            {
                var sql = $@" SELECT precool_qc_id,precool_licens_container,precool_temperature_truck,
                                     precool_temperature_container,precool_remark as precool_remark_text,
                                     TO_CHAR(created_date , 'DD/MM/YYYY HH24:mi') as PreCoolActionDate
                              FROM   {_schema}.tb_tran_precool tran        
                              WHERE  precool_plan_no  = @PlanNo
                              AND    precool_status = '0'
                              AND    precool_dc_code = @DCCode
                              ORDER BY created_date desc ";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Precool>(sql, new { PlanNo = CLS_PRECOOL.PlanNo, DCCode = CLS_PRECOOL.DCCode });
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
        //pass and fails transaction
        public async Task<List<M_TruckOnDock>> GetPreCoolNotORPass(string barcode, string PlanDate, string DCCode)
        {
            try
            {
                var sql = $@"SELECT PlanDate,
                                    PlanNo,
                                    PlanGroupNo,
                                    PlanLoadNo,
                                    CountStore,
                                    PlanTruckType,
                                    PlanDockNo,
                                    PlanPreCool,
                                    QCID,
                                    LicenseContainer,
                                    TempTruck,
                                    TempContainer,
                                    PrecoolRemark,
                                    StatusID,
                                    StatusStyle,
                                    COALESCE(CountPass, 0) AS CountPass,
                                    COALESCE(CountNotPass, 0) AS CountNotPass,
                                    LastPreCoolAction,
                                    StatusText
                                FROM (
                                    -- Main Plan Query
                                    SELECT 
                                        plan.tms_plan_action_date AS PlanDate,
                                        plan.tms_plan_no AS PlanNo,
                                        plan.tms_plan_group AS PlanGroupNo,
                                        plan.tms_plan_load_no AS PlanLoadNo,
                                        COUNT(plan.tms_plan_store_code) AS CountStore,
                                        plan.tms_plan_truck_type AS PlanTruckType,
                                        plan.tms_plan_dock_no AS PlanDockNo,
                                        MAX(TO_CHAR(plan.tms_plan_pre_cool, 'DD/MM/YYYY HH24:MI')) AS PlanPreCool,
                                        MAX(TO_CHAR(summaryPlan.tms_summary_plan_actual_pre_cool, 'DD/MM/YYYY HH24:MI')) AS LastPreCoolAction,
                                        MAX(precool.precool_qc_id) AS QCID,
                                        MAX(precool.precool_licens_container) AS LicenseContainer,
                                        MAX(precool.precool_temperature_truck) AS TempTruck,
                                        MAX(precool.precool_temperature_container) AS TempContainer,
                                        MAX(precool.precool_remark) AS PreCoolRemark,
                                        summaryPlan.tms_summary_plan_actual_pre_cool_status AS StatusID,
                                        status.status_style AS StatusStyle,
                                        plan.created_date,
                                        PrecoolStatus.precool_status_text AS StatusText
                                    FROM {_schema}.tb_tms_plan plan
                                    INNER JOIN {_schema}.tb_tms_summary_plan summaryPlan 
                                        ON plan.tms_plan_no = summaryPlan.tms_summary_plan_no
                                    LEFT JOIN {_schema}.tb_tran_precool precool 
                                        ON precool.precool_plan_no = plan.tms_plan_no
                                    INNER JOIN {_schema}.tb_status_style status 
                                        ON status.status_id = CAST(COALESCE(summaryPlan.tms_summary_plan_actual_pre_cool_status, '0') AS INTEGER)
                                    INNER JOIN {_schema}.tb_precool_status PrecoolStatus 
                                        ON PrecoolStatus.precool_status_id = CAST(COALESCE(summaryPlan.tms_summary_plan_actual_pre_cool_status, '0') AS INTEGER)
                                    WHERE plan.tms_plan_store_format IN ('Hyper', 'Market', 'BCM', 'BCMM')
                                        AND summaryPlan.flag = '1'
                                    GROUP BY 
                                        plan.tms_plan_action_date,
                                        plan.tms_plan_no,
                                        plan.tms_plan_group,
                                        plan.tms_plan_load_no,
                                        plan.tms_plan_truck_type,
                                        plan.tms_plan_dock_no,
                                        plan.tms_plan_pre_cool,
                                        summaryPlan.tms_summary_plan_actual_pre_cool_status,
                                        status.status_style,
                                        plan.created_date,
                                        summaryPlan.tms_summary_plan_actual_pre_cool,
                                        PrecoolStatus.precool_status_text
                                ) mainPlan
                                LEFT JOIN (
                                    -- Count Not Pass
                                    SELECT 
                                        precool_plan_no,
                                        COUNT(precool_plan_no) AS CountNotPass
                                    FROM {_schema}.tb_tran_precool
                                    WHERE precool_status = '0'
                                    GROUP BY precool_plan_no
                                ) NotPass ON mainPlan.PlanNo = NotPass.precool_plan_no
                                LEFT JOIN (
                                    -- Count Pass
                                    SELECT 
                                        precool_plan_no,
                                        COUNT(precool_plan_no) AS CountPass
                                    FROM {_schema}.tb_tran_precool
                                    WHERE precool_status = '1'
                                    GROUP BY precool_plan_no
                                ) Pass ON mainPlan.PlanNo = Pass.precool_plan_no
                                ORDER BY PlanNo LIMIT 10";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_TruckOnDock>(sql, new { Barcode = barcode, PlanDate = PlanDate, DCCode = DCCode });

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
        
        public async Task<bool> StampPreCoolAsync(M_Precool CLS_PRECOOL)
        {
            if (CLS_PRECOOL == null)
                throw new ArgumentNullException(nameof(CLS_PRECOOL));

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_precool 
                                    (precool_id, precool_plan_no, precool_group_no, precool_load_no,
                                     precool_action_date, precool_dock_no, precool_qc_id,
                                     precool_licens_container, precool_temperature_truck,
                                     precool_temperature_container, precool_remark,
                                     precool_status, created_by, created_date,
                                     updated_by, updated_date, flag,
                                     precool_dc_code, precool_licens_truck)
                                VALUES (
                                    nextval('{_schema}.tb_tran_precool_precool_id_seq'),
                                    @PlanNo, @GroupNo, @LoadNo,
                                    @PlanActionDate::DATE,
                                    @DockNo, @QCID,
                                    @LicenseBehind, @TempFront, @TempBehind,
                                    @Remark, CAST(@PreCoolStatusID AS INTEGER),
                                    @CreateBy, CURRENT_TIMESTAMP,
                                    @CreateBy, CURRENT_TIMESTAMP,
                                    '1',
                                    @DCCode,
                                    @LicenseTruck
                                )";

                var sql_update = $@" UPDATE {_schema}.tb_tms_summary_plan 
                                        SET tms_summary_plan_actual_pre_cool_status = CAST(@PreCoolStatusID AS varchar),
                                            tms_summary_plan_actual_pre_cool = CURRENT_TIMESTAMP
                                        WHERE tms_summary_plan_no = @PlanNo
                                          AND tms_summary_dc_code = @DCCode ";

                if (DateTime.Now.Hour < 8)
                {
                    CLS_PRECOOL.PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                }
                else
                {
                    CLS_PRECOOL.PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                }

                var parameters = new
                {
                    PlanNo = CLS_PRECOOL.PlanNo,
                    GroupNo = CLS_PRECOOL.PlanGroupNo,
                    LoadNo = CLS_PRECOOL.PlanLoadNo,
                    PlanActionDate = CLS_PRECOOL.PlanDate,
                    DockNo = CLS_PRECOOL.PlanDockNo,
                    QCID = CLS_PRECOOL.QCID,
                    LicenseTruck = CLS_PRECOOL.LicenseTruck,
                    LicenseBehind = CLS_PRECOOL.LicenseBehind,
                    TempFront = CLS_PRECOOL.TempuratureFront,
                    TempBehind = CLS_PRECOOL.TempuratureBehind,
                    Remark = CLS_PRECOOL.Remark,
                    PreCoolStatusID = CLS_PRECOOL.PreCoolStatusID ?? "0",
                    CreateBy = CLS_PRECOOL.CreateBy,
                    DCCode = CLS_PRECOOL.DCCode
                };

                var rowInsert = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                var rowUpdate = await connection.ExecuteAsync(sql_update, parameters, transaction);
                if (rowInsert == 0 || rowUpdate == 0)
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
        public async Task<bool> UpdatePreCool(M_Precool CLS_PRECOOL)
        {
            try
            {    
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    using var tran = connection.BeginTransaction();
                    {
                        try
                        {
                            var sql = $@" UPDATE {_schema}.tb_tms_summary_plan 
                              SET   tms_summary_plan_actual_pre_cool_status = @PreCoolStatusID,
                                    tms_summary_plan_actual_pre_cool= now()
                              WHERE tms_summary_plan_no = @PlanNo 
                              AND   tms_summary_dc_code = @DCCode
                              AND   tms_summary_plan_action_date = current_date ";

                            var parameters = new
                            {
                                PreCoolStatusID = CLS_PRECOOL.PlanPreCoolStatusID,
                                precool_plan_no = CLS_PRECOOL.PlanNo,
                                precool_dc_code = CLS_PRECOOL.DCCode
                            };

                            var rowsAffected = await connection.ExecuteAsync(sql, parameters, tran);
                            if (rowsAffected == 0)
                            {
                                await tran.RollbackAsync();
                                return false;
                            }
                            await tran.CommitAsync();
                            return true;
                        }
                        catch
                        {
                            await tran.RollbackAsync();
                            return false;
                        }
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
        public async Task<bool> DeletePreCool(M_Precool CLS_PRECOOL)
        {
            var now = DateTime.UtcNow;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        var sqlDelete = $@" DELETE FROM {_schema}.tb_tran_precool 
                                            WHERE precool_plan_no = @PlanNo 
                                            AND   precool_dc_code = @DCCode ";


                        var sqlUpdate = $@" UPDATE {_schema}.tb_tms_summary_plan 
                                            SET   tms_summary_plan_actual_pre_cool_status = '0', 
                                                  tms_summary_plan_actual_pre_cool = null 
                                            WHERE tms_summary_plan_no = @PlanNo 
                                            AND   tms_summary_plan_no = @DCCode";

                        var parameters = new
                        {
                            PlanNo = CLS_PRECOOL.PlanNo,
                            DCCode = CLS_PRECOOL.DCCode,
                            Now = DateTime.Now
                        };

                        await connection.ExecuteAsync(sqlDelete, parameters, transaction);
                        var rowsAffected = await connection.ExecuteAsync(sqlUpdate, parameters, transaction);
                        await transaction.CommitAsync();
                        return rowsAffected > 0;
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
        // ยกเลิกรายการวัดอุณหภูมิผ่าเกณณ์
        public async Task<bool> CancelPreCoolPass(M_Precool CLS_PRECOOL)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Step: DELETE
                        var sqlDelete = $@" DELETE FROM {_schema}.tb_tran_precool 
                                            WHERE precool_plan_no = @PlanNo 
                                                AND precool_dc_code = @DCCode 
                                                AND precool_status = '1'";


                        // 2. Step: UPDATE (ตัวอย่างการเปลี่ยน Status กลับ)
                        var sqlUpdate = $@"UPDATE {_schema}.tb_tms_summary_plan 
                                   SET tms_summary_plan_actual_pre_cool_status = '0', 
                                       tms_summary_plan_actual_pre_cool = null 
                                   WHERE tms_summary_plan_no = @PlanNo 
                                    AND tms_summary_plan_no = @DCCode";

                        var parameters = new
                        {
                            PlanNo = CLS_PRECOOL.PlanNo,
                            DCCode = CLS_PRECOOL.DCCode,
                            Now = DateTime.Now
                        };

                        var row_delete = await connection.ExecuteAsync(sqlDelete, parameters, transaction);
                        var row_update_date = await connection.ExecuteAsync(sqlUpdate, parameters, transaction);

                        if (row_delete == 0 && row_update_date == 0)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (NpgsqlException ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error: {ex.Message}");
                        return false;
                        throw;
                    }
                }
            }
        }
        // หมายเหตุรายการวัดอุณหภูมิ
        public async Task<List<M_Precool>> GetRemarkPrecool(string DCCode)
        {
            try
            {
                var sql = $@" SELECT precool_remark_id ,precool_remark_text  
                              FROM  {_schema}.tb_remark_precool 
                              WHERE precool_remark_status  = '1'
                              AND   precool_dc_code = @DCCode 
                              ORDER BY precool_remark_id desc";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Precool>(sql, new { DCCode = DCCode });
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
        //หมายเหตุรายการวัดอุณหภูมิที่ผ่านแล้ว
        public async Task<List<M_Precool>> GetRemarkPrecoolPass(string DCCode)
        {
            try
            {
                var sql = $@" SELECT precool_remark_id ,precool_remark_text  
                              FROM  {_schema}.tb_remark_precool_pass 
                              WHERE precool_remark_status  = '1'
                              AND   precool_dc_code = @DCCode 
                              ORDER BY precool_remark_id desc";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Precool>(sql, new { DCCode = DCCode });
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
    }
}
