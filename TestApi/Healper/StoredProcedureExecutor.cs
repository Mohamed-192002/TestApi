using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Reflection;

namespace TestApi.Healper
{
    public class StoredProcedureExecutor
    {
        private readonly string _connectionString;
        public StoredProcedureExecutor(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public string ExecuteStoredProcedure(string storedProcedureName, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }
            var table = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(table);
            var jsonResult = JsonConvert.SerializeObject(table);
            return jsonResult;
        }

    }
    public static class Maping
    {
        public static List<T> ConvertDataTableToList<T>(DataTable table) where T : new()
        {
            var list = new List<T>();

            foreach (DataRow row in table.Rows)
            {
                T obj = new T();

                foreach (DataColumn column in table.Columns)
                {
                    PropertyInfo prop = typeof(T).GetProperty(column.ColumnName);
                    if (prop != null && row[column] != DBNull.Value)
                        prop.SetValue(obj, Convert.ChangeType(row[column], prop.PropertyType));
                }

                list.Add(obj);
            }

            return list;
        }

    }
}
