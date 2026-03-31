using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QMSWebApiCore.Models;

using System.Data;
using System.Reflection.Emit;
using System.Text;

namespace QMSWebApiCore.Services
{
    public interface ILogInRepository
    {
        Task<List<M_Login>> GetLogIn(string AccountName, string AccountPassword);
    }
    public class LoginRepository : ILogInRepository
    {
        //private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        //    ?? throw new ArgumentException("Connection string is required");

        private readonly IConfiguration _configuration;
        private readonly string _schema;
        private readonly string _connectionString;

        public LoginRepository(IConfiguration configuration)
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
        public async Task<List<M_Login>> GetLogIn(string AccountUserName, string AccountPassword)
        {
            try
            {
                var sql = $@" SELECT ur_uname, ur_pass, ur_shortname, ur_site, ur_fullname, 
                                         ur_email, pms.pm_fvar1,pms.pm_fvar2,pms.pm_fvar3,pms.pm_fvar4
                              FROM public.tb_wpusr usr
                                  INNER JOIN public.tb_wppms pms on usr.ur_uname = pms.pm_uname
                              WHERE ur_uname = @AccountUserName
                                  AND ur_pass = @AccountPassword
                                  AND pm_rcode = 'DNSL'";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var result = await connection.QueryAsync<M_Login>(sql, new { AccountUserName = AccountUserName, AccountPassword = AccountPassword });
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
