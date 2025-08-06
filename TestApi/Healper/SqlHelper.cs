using Microsoft.Data.SqlClient;
using System.Data;

namespace TestApi.Healper
{
    public class SqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("DefaultConnection string not found in configuration");
        }

        public async Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(
            string storedProcedureName,
            params SqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentException("Stored procedure name is required.", nameof(storedProcedureName));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(storedProcedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                    command.Parameters.AddRange(parameters);

                await connection.OpenAsync();

                using var reader = await command.ExecuteReaderAsync();
                var result = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        dict[columnName] = value;
                    }
                    result.Add(dict);
                }

                return result;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Error executing stored procedure '{storedProcedureName}': {ex.Message}", ex);
            }
        }


        public async Task ExecuteInsertAsync(string tableName, Dictionary<string, object> data)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name is required.", nameof(tableName));

            if (data == null || data.Count == 0)
                throw new ArgumentException("No data provided.", nameof(data));

            // Validate table name to prevent SQL injection
            if (!IsValidIdentifier(tableName))
                throw new ArgumentException("Invalid table name format.", nameof(tableName));

            // Validate column names
            foreach (var key in data.Keys)
            {
                if (!IsValidIdentifier(key))
                    throw new ArgumentException($"Invalid column name format: {key}", nameof(data));
            }

            try
            {
                var columns = string.Join(", ", data.Keys.Select(k => $"[{k}]"));
                var parameters = string.Join(", ", data.Keys.Select(k => "@" + k));
                string query = $"INSERT INTO [{tableName}] ({columns}) VALUES ({parameters})";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                foreach (var kvp in data)
                {
                    command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                }

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Error inserting data into table '{tableName}': {ex.Message}", ex);
            }
        }

        public async Task ExecuteUpdateAsync(string tableName, Dictionary<string, object> data, string keyColumn, object keyValue)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name is required.", nameof(tableName));

            if (data == null || data.Count == 0)
                throw new ArgumentException("No data provided.", nameof(data));

            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Key column is required for WHERE condition.", nameof(keyColumn));

            if (!IsValidIdentifier(tableName))
                throw new ArgumentException("Invalid table name format.", nameof(tableName));

            if (!IsValidIdentifier(keyColumn))
                throw new ArgumentException("Invalid key column name format.", nameof(keyColumn));

            foreach (var key in data.Keys)
            {
                if (!IsValidIdentifier(key))
                    throw new ArgumentException($"Invalid column name format: {key}", nameof(data));
            }

            try
            {
                var setClause = string.Join(", ", data.Keys.Select(k => $"[{k}] = @{k}"));
                string query = $"UPDATE [{tableName}] SET {setClause} WHERE [{keyColumn}] = @KeyValue";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);

                foreach (var kvp in data)
                {
                    command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                }

                command.Parameters.AddWithValue("@KeyValue", keyValue ?? DBNull.Value);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Error updating data in table '{tableName}': {ex.Message}", ex);
            }
        }

        public async Task ExecuteDeleteAsync(string tableName, string keyColumn, object keyValue)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name is required.", nameof(tableName));

            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Key column is required for WHERE condition.", nameof(keyColumn));

            if (!IsValidIdentifier(tableName))
                throw new ArgumentException("Invalid table name format.", nameof(tableName));

            if (!IsValidIdentifier(keyColumn))
                throw new ArgumentException("Invalid key column name format.", nameof(keyColumn));

            try
            {
                string query = $"DELETE FROM [{tableName}] WHERE [{keyColumn}] = @KeyValue";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@KeyValue", keyValue ?? DBNull.Value);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Error deleting data from table '{tableName}': {ex.Message}", ex);
            }
        }

        // Helper method to validate SQL identifiers
        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Simple validation: alphanumeric characters and underscores only
            // Starts with letter or underscore
            return System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }
    }
}
