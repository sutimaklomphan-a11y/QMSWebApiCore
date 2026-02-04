using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System.Numerics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace QMSWebApiCore.Services
{
    public class PreCoolRepository(IConfiguration configuration) : IPreCoolRepository
    {
        private readonly string _schema = ValidateSchemaName(configuration["DatabaseSchema:DBSchema"]
     ?? configuration["Database:Schema"] ?? "outbound_qms_sim");
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentException("Connection string is required");
        private static string ValidateSchemaName(string SCHEMA_NAME)
        {
            if (string.IsNullOrWhiteSpace(SCHEMA_NAME))
                throw new ArgumentException("Schema name cannot be null or empty");

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                SCHEMA_NAME, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                throw new ArgumentException($"Invalid schema name: '{SCHEMA_NAME}'");
            return SCHEMA_NAME;
        }
        public async Task<bool> StampPreCoolAsync(M_Precool CLS_PRECOOL)
        {
            var now = DateTime.UtcNow;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {

                        var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_precool 
                                                    (precool_id,precool_plan_no,precool_group_no,precool_load_no,precool_action_date,precool_dock_no,precool_qc_id
                                                    ,precool_licens_container,precool_temperature_truck,precool_temperature_container,precool_remark
                                                    ,precool_status,created_by,created_date,updated_by,updated_date,flag,precool_dc_code,precool_licens_truck)
                                            VALUES  nextval({_schema}.tb_tran_precool_precool_id_seq ::regclass)
                                                    ,@PlanNo
                                                    ,@GroupNo
                                                    ,@LoadNo
                                                    ,@PlanActionDate
                                                    ,@DockNo
                                                    ,@QCID
                                                    ,@LicenseBehind
                                                    ,@TempFront
                                                    ,@TempBehind
                                                    ,@Remark
                                                    ,@PreCoolStatusID
                                                    ,@CreateBy
                                                    ,now()
                                                    ,@CreateBy
                                                    ,now()    
                                                    ,'1'
                                                    ,@DCCode
                                                    ,@LicenseTruck) ";


                        var sql_update = $@" UPDATE {_schema}.tb_tms_summary_plan 
                                             SET    tms_summary_plan_actual_pre_cool_status = @PreCoolStatusID,
                                                    tms_summary_plan_actual_pre_cool=now()
                                             WHERE  tms_summary_plan_no = @PlanNo and tms_summary_dc_code = @DCCode ";

                        var parameters = new
                        {
                            PlanNo = CLS_PRECOOL.PlanNo,
                            GroupNo = CLS_PRECOOL.PlanGroupNo,
                            LoadNo = CLS_PRECOOL.PlanLoadNo,
                            PlanActionDate = CLS_PRECOOL.ActionDate,
                            DockNo = CLS_PRECOOL.PlanDockNo,
                            QCID = CLS_PRECOOL.QCID,
                            LicenseBehind = CLS_PRECOOL.LicenseBehind,
                            TempFront = CLS_PRECOOL.TempuratureFront,
                            TempBehind = CLS_PRECOOL.TempuratureBehind,
                            Remark = CLS_PRECOOL.Remark,
                            PreCoolStatusID = CLS_PRECOOL.PlanPreCoolStatusID ?? "1",
                            CreateBy = CLS_PRECOOL.CreateBy,
                            DCCode = CLS_PRECOOL.DCCode,
                            LicenseTruck = CLS_PRECOOL.LicenseTruck
                        };

                        await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                        var rowsAffected = await connection.ExecuteAsync(sql_update, parameters, transaction);
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

        public async Task<bool> UpdatePreCool(M_Precool CLS_PRECOOL)
        {
            var now = DateTime.UtcNow;
            try
            {
                var sql = $@" UPDATE {_schema}.tb_tms_summary_plan 
                              SET   tms_summary_plan_actual_pre_cool_status = @PreCoolStatusID,
                                    tms_summary_plan_actual_pre_cool= now()
                              WHERE tms_summary_plan_no = @PlanNo 
                              AND   tms_summary_dc_code = @DCCode";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var parameters = new
                    {
                        PreCoolStatusID = CLS_PRECOOL.PlanPreCoolStatusID,
                        precool_plan_no = CLS_PRECOOL.PlanNo,
                        precool_dc_code = CLS_PRECOOL.DCCode
                    };

                    var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
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

        public async Task<bool> CancelPreCoolPass(M_Precool CLS_PRECOOL)
        {
            var now = DateTime.UtcNow;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
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
                            Now = DateTime.Now // หรือ DateTime.UtcNow ตามมาตรฐานโปรเจกต์
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
    }
}
