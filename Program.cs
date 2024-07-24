using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string connectionString = "Server=localhost;Database=data_base_name;User ID=root;Password=123;";

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                // Fetch all table names
                MySqlCommand getTablesCmd = new MySqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'data_base_name';", conn);
                MySqlDataAdapter adapter = new MySqlDataAdapter(getTablesCmd);
                DataTable tables = new DataTable();
                adapter.Fill(tables);

                // List of tables to exclude
                HashSet<string> tablesToExclude = new HashSet<string> { "businessunitmemberservice", "conversationdetails", "keyword", "membergroup", "membergroupmember", "memberlocation", "memberlogin", "membernotes", "memberpermission", "notificationeventmembergroup", "notificationeventrole", "notificationmessage", "serilog", "servicerequestjobtype", "servicerequestnote", "serviceresponsemember", "team", "teammember", "timesheet", "tac_driver_overtime_report", "tac_job_details", "tac_job_with_previous_data", "tac_jobs", "tac_member_shift_report", "tac_notes", "tac_sr_logs", "tac_time_cards", "__efmigrationshistory" };

                // Store foreign key relationships
                List<string> relationships = new List<string>();

                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["table_name"].ToString();

                    // Skip excluded tables
                    if (tablesToExclude.Contains(tableName))
                    {
                        continue;
                    }

                    // Fetch columns and their types
                    MySqlCommand getColumnsCmd = new MySqlCommand($@"
                        SELECT COLUMN_NAME, COLUMN_TYPE 
                        FROM information_schema.columns 
                        WHERE table_name = '{tableName}' 
                        AND table_schema = 'tac_prod_database';", conn);

                    MySqlDataReader columnReader = getColumnsCmd.ExecuteReader();

                    // Output the table definition
                    Console.WriteLine($"Table {tableName} {{");
                    while (columnReader.Read())
                    {
                        string columnName = columnReader["COLUMN_NAME"].ToString();
                        string columnType = columnReader["COLUMN_TYPE"].ToString();

                        // Convert MySQL types to dbdiagram.io types
                        string dbType = ConvertToDbDiagramType(columnType);
                        Console.WriteLine($"  {columnName} {dbType}");
                    }
                    Console.WriteLine("}\n");
                    columnReader.Close();

                    // Fetch foreign key relationships
                    MySqlCommand getForeignKeysCmd = new MySqlCommand($@"
                        SELECT 
                            kcu.column_name AS foreign_column,
                            kcu.referenced_table_name AS referenced_table,
                            kcu.referenced_column_name AS referenced_column
                        FROM 
                            information_schema.key_column_usage AS kcu
                        WHERE 
                            kcu.table_name = '{tableName}' 
                            AND kcu.table_schema = 'tac_prod_database' 
                            AND kcu.referenced_table_name IS NOT NULL;", conn);

                    MySqlDataReader fkReader = getForeignKeysCmd.ExecuteReader();
                    while (fkReader.Read())
                    {
                        string foreignColumn = fkReader["foreign_column"].ToString();
                        string referencedTable = fkReader["referenced_table"].ToString();
                        string referencedColumn = fkReader["referenced_column"].ToString();

                        // Format the relationship
                        relationships.Add($"Ref: {tableName}.{foreignColumn} > {referencedTable}.{referencedColumn} // many-to-one");
                    }
                    fkReader.Close();
                }

                // Output all relationships
                foreach (var relationship in relationships)
                {
                    Console.WriteLine(relationship);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    static string ConvertToDbDiagramType(string columnType)
    {
        // Convert MySQL data types to dbdiagram.io data types
        if (columnType.StartsWith("int")) return "integer";
        if (columnType.StartsWith("varchar")) return "varchar";
        if (columnType.StartsWith("text")) return "text";
        if (columnType.StartsWith("timestamp")) return "timestamp";
        if (columnType.StartsWith("datetime")) return "datetime";
        if (columnType.StartsWith("decimal")) return "decimal";
        if (columnType.StartsWith("float")) return "float";

        // Add more conversions as needed
        return columnType; // Return the line as is if no conversion is needed
    }
}