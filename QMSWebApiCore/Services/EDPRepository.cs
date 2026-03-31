using Dapper;
using Npgsql;
using QMSWebApiCore.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace QMSWebApiCore.Services
{
    public interface IEDPRepository
    {

        Task<bool> StampEDPInAsync(M_EDP CLS_EDP);
        Task<bool> StampEDPOutAsync(M_EDP CLS_EDP);
        Task<List<M_EDP>> getEDPForEDPPlanList(M_EDP CLS_EDP); 
        Task<List<M_EDP>> getFinshLoadForEDPPlanList(M_EDP CLS_EDP);
        Task<List<M_EDP>> checkStatusBarcodeDetail(string DCcode, string barcode, string ActionDate);
    }
    public class EDPRepository : IEDPRepository
    {
        private readonly IConfiguration _config;
        private readonly string _schema;
        private readonly string _connectionString;

        public EDPRepository(IConfiguration config)
        {
            _config = config;
            _schema = ValidateSchemaName(
                _config["DatabaseSchema:DBSchema"]
                ?? _config["Database:Schema"]
                ?? "outbound_qms_sim"
            );

            _connectionString = _config.GetConnectionString("DefaultConnection")
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

        //ใช้กับ Menu Stamp gate Out เพื่อเช็คว่าผ่านขั้นตอนทั้งหมดหรือยัง
        public async Task<List<M_EDP>> checkStatusBarcodeDetail(string DCcode, string barcode, string ActionDate)
        {
            try
            {
                var sql = $@"select *,ondock.truckondock_plan_no as PlanNo from (select Gate.gate_barcode as Barcode, 
                                    Gate.gate_license as LicenseTruck,Gate.gate_action_by as EDPInActionBy,Gate.gate_driver_name as DriverName, 
                                    summary.tms_summary_plan_action_date as PlanDate,summary.tms_summary_load_no as PlanLoadNo, 
                                    summary.tms_summary_plan_group as PlanGroupNo,summary.tms_summary_dock_no as PlanDockNo,	 
                                    Gate.gate_truck_in as GateInDate,TO_CHAR(Gate.gate_truck_in,'DD/MM/YYYY HH24:mi') as GateInDate, 
                                    TO_CHAR(RSU.created_date,'DD/MM/YYYY HH24:mi') as RSUInDate, 
                                    TO_CHAR(RSU.rsu_out_date,'DD/MM/YYYY HH24:mi') as RSUOutDate, 
                                    TO_CHAR(truckondock.created_date,'DD/MM/YYYY HH24:mi') as TruckOnDockDate, 
                                    TO_CHAR(preload.created_date,'DD/MM/YYYY HH24:mi') as PreLoadDate, 
                                    TO_CHAR(loadintruck.created_date,'DD/MM/YYYY HH24:mi') as StartLoadDate, 
                                    TO_CHAR(loadintruck.loadontruck_finish_date,'DD/MM/YYYY HH24:mi') as FinishLoadDate, 
                                    TruckType.truck_type_id as TruckTypeID,TruckType.truck_type_name as TruckTypeName, 
                                    TruckType.truck_type_style as TruckTypeStyle,TruckType.truck_type_image as TruckTypeImage, 
                                    edp.edp_in_by as EDPInActionBy,TO_CHAR(edp.edp_in_date,'DD/MM/YYYY HH24:mi') as EDPInDate, 
                                    edp.edp_in_remark as EDPRemarkIn,edp.edp_out_by as EDPOutActionBy, 
                                    TO_CHAR(edp.edp_out_date,'DD/MM/YYYY HH24:mi') as EDPOutDate,
                                    edp.edp_out_remark as EDPRemarkOut,
                                    gate.last_process as LastProcess
                                 from outbound_qms_sim.tb_tran_gate Gate 
                                     left join outbound_qms_sim.tb_tran_rsu rsu on rsu.rsu_barcode  = gate.gate_barcode and rsu.rsu_gate_id = gate_id 
                                     inner join outbound_qms_sim.tb_truck_type TruckType on TruckType.truck_type_id = Gate.gate_type_truck 
                                     left join outbound_qms_sim.tb_tran_truckondock truckondock on truckondock.truckondock_barcode  = gate.gate_barcode and truckondock.gate_id::INTEGER = gate.gate_id::INTEGER  
                                     left join outbound_qms_sim.tb_tran_preload preload on preload.preload_plan_no = truckondock.truckondock_plan_no  
                                     left join outbound_qms_sim.tb_tran_loadontruck loadintruck on loadintruck.loadontruck_plan_no = truckondock.truckondock_plan_no  
                                     left join outbound_qms_sim.tb_tms_summary_plan summary on summary.tms_summary_plan_no = truckondock.truckondock_plan_no 
                                     left join outbound_qms_sim.tb_tran_edp edp on edp.edp_barcode = Gate.gate_barcode 
                                     and edp.edp_plan_no = (select truckondock_plan_no from outbound_qms_sim.tb_tran_truckondock where truckondock_barcode = @Barcode order by created_date desc limit 1) 
                                     and edp.edp_gate_id::integer = gate.gate_id::integer 
                                 where 1=1 
                                     and Gate.gate_out_direct = '0' and Gate.gate_status = '1' 
                                     and Gate.gate_barcode = @Barcode	 
                                     and Gate.gate_dc_code = @DCCode 
                                     order by Gate.gate_id desc limit 1) as Plandata 
                                     left join (select truckondock_plan_no,truckondock_barcode from outbound_qms_sim.tb_tran_truckondock where truckondock_barcode = @Barcode order by created_date desc limit 1) ondock 
                                 on plandata.Barcode = ondock.truckondock_barcode";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_EDP>(sql, new { Barcode = barcode, DCCode = DCcode, ActionDate = ActionDate });
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

        //เป็นตัวเดียวกับ getFinshLoadForEDPPlanList แค่แยก status 1,2
        public async Task<List<M_EDP>> getFinshLoadForEDPPlanList(M_EDP CLS_EDP)
        {
            try
            {
                var sql = new StringBuilder($@"select  summaryPlan.tms_summary_plan_no as PlanNo 
                                                ,tms_summary_plan_group as PlanGroupNo 
                                                ,tms_summary_load_no as PlanLoadNo 
                                                ,tms_summary_dock_no  as PlanDockNo 
                                                ,truckondock.truckondock_barcode as Barcode 
                                                ,gate.gate_type_truck as TruckTypeID 
                                                ,trucktype.truck_type_name as TruckTypeName 
                                                ,gate.gate_license as LicenseTruck 
                                                ,gate.gate_driver_name as DriverName 
                                                ,case when tran.edp_status is null then '0' else tran.edp_status end as EDPStatusID
                                                ,case when tran.edp_status = '0' then 'ไม่ได้ยื่นเอกสาร EDP'
                                                  when tran.edp_status = '1' then 'ยื่นเอกสาร EDP'
                                                  when tran.edp_status = '2' then 'ยื่นเอกสาร EDP เรียบร้อย'
                                                 else 'ไม่ได้ยื่นเอกสาร EDP' end as EDPStatusText
                                                ,TO_CHAR(tms_summary_plan_actual_edp_in , 'DD/MM/YYYY HH24:mi') as EDPInDate 
                                                ,tran.edp_in_by as EDPInActionBy 
                                                ,tran.edp_in_remark as EDPRemarkIn 
                                                ,TO_CHAR(tms_summary_plan_actual_edp_out , 'DD/MM/YYYY HH24:mi') as EDPOutDate 
                                                ,tran.edp_out_remark as EDPRemarkOut 
                                                ,tran.edp_out_by as EDPOutActionBy 
                                                 from outbound_qms_sim.tb_tms_summary_plan summaryPlan 
                                                 inner join outbound_qms_sim.tb_tran_truckondock truckondock on truckondock.truckondock_plan_no = summaryplan.tms_summary_plan_no 
                                                 inner join outbound_qms_sim.tb_tran_gate gate on gate.gate_barcode = truckondock.truckondock_barcode and gate.gate_status = '1' 
                                                 left join outbound_qms_sim.tb_tran_edp tran on tran.edp_plan_no = summaryPlan.tms_summary_plan_no 
                                                 left join outbound_qms_sim.tb_truck_type trucktype on trucktype.truck_type_id = gate.gate_type_truck 
                                                 where 1=1 
                                                 and summaryPlan.flag = '1' 
                                                 and gate.gate_out_direct = '0' 
                                                 and gate.gate_status = '1' 
                                                 and gate.gate_action_date between CAST(@Yesterday AS DATE) and CAST(@CurrentDay AS DATE) ");

                string PlanDateAction, CurrentDay, Yesterday = "";
                string PrefixPlan = "";

                if (DateTime.Now.Hour < 10)
                {
                    PlanDateAction = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    PrefixPlan = CLS_EDP.PlanGroupNo + DateTime.Today.AddDays(-1).ToString("yyyyMMdd");
                }
                else
                {
                    PlanDateAction = DateTime.Today.ToString("yyyy-MM-dd");
                    PrefixPlan = CLS_EDP.PlanGroupNo + DateTime.Today.ToString("yyyyMMdd");
                }

                CurrentDay = DateTime.Today.ToString("yyyy-MM-dd");
                Yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

                var parameters = new DynamicParameters();
                parameters.Add("DCCode", CLS_EDP.DCCode);
                parameters.Add("Yesterday", Yesterday);
                parameters.Add("CurrentDay", CurrentDay);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_EDP>(sql.ToString(), parameters);
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
        public async Task<List<M_EDP>> getEDPForEDPPlanList(M_EDP CLS_EDP)
        {
            try
            {
                var sql = new StringBuilder($@"select
	                                                summaryplan.tms_summary_plan_no as PlanNo,
	                                                tms_summary_plan_group as group,
	                                                tms_summary_load_no as LoadNo,
	                                                tms_summary_dock_no as DockNo,
	                                                truckondock.truckondock_barcode as Barcode,
	                                                gate.gate_type_truck as TruckTypeID,
	                                                trucktype.truck_type_name as TruckTypeName,
	                                                gate.gate_license as LicenseTruck,
	                                                gate.gate_driver_name as DriverName,
	                                                edp.edp_status as EDPStatusID,
	                                                case when edp.edp_status = '0' then 'ไม่ได้ยื่นเอกสาร EDP'
	                                                  when edp.edp_status = '1' then 'ยื่นเอกสาร EDP'
	                                                  when edp.edp_status = '2' then 'ยื่นเอกสาร EDP เรียบร้อย'
	                                                 else 'ไม่ได้ยื่นเอกสาร EDP' end as EDPStatusText,
	                                                TO_CHAR(edp.edp_in_date, 'DD/MM/YYYY HH24:mi') as EDPInActionDate,
	                                                edp.edp_in_by as EDPInActionBy,
	                                                edp.edp_in_remark as EDPInRemark,
	                                                TO_CHAR(edp.edp_out_date, 'DD/MM/YYYY HH24:mi') as EDPOutActionDate,
	                                                edp.edp_out_remark as EDPOutRemark,
	                                                edp.edp_out_by as EDPOutActionBy
                                                from
	                                                outbound_qms_sim.tb_tran_edp edp
                                                inner join outbound_qms_sim.tb_tms_summary_plan summaryplan on
	                                                edp.edp_plan_no = summaryplan.tms_summary_plan_no
                                                inner join outbound_qms_sim.tb_tran_truckondock truckondock on
	                                                truckondock.truckondock_plan_no = edp.edp_plan_no
                                                inner join outbound_qms_sim.tb_tran_gate gate on
	                                                gate.gate_barcode = truckondock.truckondock_barcode
	                                                and gate.gate_status = '1'
	                                                and truckondock.gate_id::integer = gate.gate_id::integer
                                                inner join outbound_qms_sim.tb_truck_type trucktype on
	                                                trucktype.truck_type_id = gate.gate_type_truck
                                                where  truckondock.truckondock_dc_code = @DCCode ");


                string PrefixPlan = "";
                var parameters = new DynamicParameters();
                parameters.Add("DCCode", CLS_EDP.DCCode);

                if (!string.IsNullOrEmpty(CLS_EDP.PlanGroupNo) && CLS_EDP.PlanGroupNo != "All")
                {
                    if (!string.IsNullOrEmpty(PrefixPlan))
                    {
                        sql.Append(" and edp.edp_plan_no like '%' || @PrefixPlan || '%' ");
                        parameters.Add("PrefixPlan", PrefixPlan);
                    }
                }

                if (!string.IsNullOrEmpty(CLS_EDP.EDPStatusID) && CLS_EDP.EDPStatusID != "All")
                {
                    sql.Append(" and edp.edp_status = @EDPStatusID ");
                    parameters.Add("EDPStatusID", CLS_EDP.EDPStatusID);
                }

                //sql.Append(" order by edp_plan_no ");

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_EDP>(sql.ToString(), parameters);
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
        public async Task<bool> StampEDPInAsync(M_EDP CLS_EDP)
        {
            var now = DateTime.UtcNow;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        var sql_Insert = $@"INSERT INTO {_schema}.tb_tran_edp (
                                                                        edp_plan_no
                                                                        ,edp_in_by
                                                                        ,edp_in_date
                                                                        ,edp_in_remark
                                                                        ,flag
                                                                        ,edp_barcode
                                                                        ,edp_status
                                                                        ,edp_dc_code
                                                                        ,edp_gate_id
                                                                        ,created_date
                                                                        ,created_by
                                                                        )
                                                                        values(
                                                                        @PlanNo
                                                                        , @ActionBy
                                                                        , now()
                                                                        , @Remark
                                                                        , '1'
                                                                        , @Barcode
                                                                        , '1'
                                                                        , @DCCode
                                                                        , (select gate_id from {_schema}.tb_tran_gate where gate_barcode = @Barcode and gate_status = '1' order by gate_id desc limit 1)
                                                                        ,now()
                                                                        ,@ActionBy )";


                        var sql_update_summary_plan = $@"UPDATE {_schema}.tb_tms_summary_plan 
                                                        SET tms_summary_plan_actual_edp_in_status = '1',
                                                            tms_summary_plan_actual_edp_in=now(),
                                                            tms_summary_dc_code= @DCCode 
                                                        WHERE tms_summary_plan_no = @PlanNo
                                                            and tms_summary_dc_code = @DCCode ";


                        var sql_tb_tran_gate = $@" UPDATE {_schema}.tb_tran_gate
                                                        SET last_process = 'EDP In'
                                                        WHERE gate_barcode = @Barcode
                                                        AND gate_action_date = CAST(@PlanActionDate AS DATE) 
	                                                    AND gate_dc_code = @DCCode
                                                        AND gate_status = '1'";


                        var sql_update_current = $@" UPDATE {_schema}.tb_wip_current
                                                            SET status = 9
                                                       WHERE plan_no = @PlanNo
                                                        AND  dc_code = @DCCode
                                                        AND  barcode = @Barcode";


                        string PlanDate = "";
                        if (DateTime.Now.Hour < 10)
                        {
                            PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                        }

                        var parameters = new
                        {
                            Barcode = CLS_EDP.Barcode,
                            ActionByRSUIn = CLS_EDP.EDPInActionBy,
                            ActionBy = CLS_EDP.EDPInActionBy,
                            Remark = CLS_EDP.EDPRemarkIn,
                            PlanNo = CLS_EDP.PlanNo,
                            DCCode = CLS_EDP.DCCode,
                            PlanActionDate = PlanDate,
                        };

                        var insertTask = await connection.ExecuteAsync(sql_Insert, parameters, transaction);
                        var updatePlanTask = await connection.ExecuteAsync(sql_update_summary_plan, parameters, transaction);
                        var updateCurrentTask = await connection.ExecuteAsync(sql_update_current, parameters, transaction);
                        var updategateTask = await connection.ExecuteAsync(sql_tb_tran_gate, parameters, transaction);

                        if (insertTask == 0 || updatePlanTask == 0 || updateCurrentTask == 0 || updategateTask == 0)
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
        public async Task<bool> StampEDPOutAsync(M_EDP CLS_EDP)
        {
            var now = DateTime.UtcNow;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        var sql_update_edp = $@"UPDATE {_schema}.tb_tran_edp 
                                                    SET edp_status = '2' ,
                                                        edp_out_date = now(), 
                                                        edp_out_by = @ActionBy,
                                                        edp_out_remark = @Remark,
                                                        updated_date = now()
                                                 WHERE edp_plan_no = @PlanNo ";


                        var sql_update_tms_summary = $@" UPDATE {_schema}.tb_tms_summary_plan
                                                SET tms_summary_plan_actual_edp_out_status = '1',
                                                    tms_summary_plan_actual_edp_out = now()
                                                WHERE
                                                    tms_summary_plan_no = @PlanNo
	                                            AND tms_summary_dc_code = @DCCode ";

                        var sql_update_gate = $@" UPDATE {_schema}.tb_tran_gate
                                                 SET last_process = 'EDP Out'
                                                 where gate_barcode = (select truckondock_barcode  from  {_schema}.tb_tran_truckondock where truckondock_plan_no = @PlanNo)
                                                 and gate_action_date = CAST(@PlanActionDate AS DATE) 
                                                 and gate_status = '1' ";


                        var sql_update_wip_current = $@" UPDATE {_schema}.tb_wip_current
                                                            SET status = 10
                                                       WHERE plan_no = @PlanNo
                                                            AND dc_code = @DCCode
                                                            AND barcode = @Barcode ";


                        string PlanDate = "";
                        if (DateTime.Now.Hour < 10)
                        {
                            PlanDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            PlanDate = DateTime.Today.ToString("yyyy-MM-dd");
                        }

                        var parameters = new
                        {
                            Barcode = CLS_EDP.Barcode,
                            PlanNo = CLS_EDP.PlanNo,
                            ActionBy = CLS_EDP.EDPOutActionBy,
                            Remark = CLS_EDP.EDPRemarkOut,
                            DCCode = CLS_EDP.DCCode,
                            PlanActionDate = PlanDate,
                        };

                        var update_Edp = await connection.ExecuteAsync(sql_update_edp, parameters, transaction);
                        var update_summary = await connection.ExecuteAsync(sql_update_tms_summary, parameters, transaction);
                        var upate_gate = await connection.ExecuteAsync(sql_update_gate, parameters, transaction);
                        var update_wip = await connection.ExecuteAsync(sql_update_wip_current, parameters, transaction);

                        if (update_Edp == 0 || update_summary == 0 || upate_gate == 0 || update_wip == 0)
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
    }
}
