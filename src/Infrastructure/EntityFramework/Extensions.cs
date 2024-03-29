﻿using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;

namespace Fuxion.EntityFramework;

public static class Extensions
{
	public static void ClearData(this DbContext me, bool usePartialExcludedTableName = false, bool autoExludeMigrationHistory = true, params string[] excludedTable)
	{
		using (var connection = new SqlConnection(me.Database.Connection.ConnectionString))
		{
			// Open connection
			connection.Open();
			// Get tables
			var tables = (from DataRow row in connection.GetSchema("Tables").Rows select row[2].ToString()).ToList();
			// Compute exlusions
			if (autoExludeMigrationHistory && !excludedTable.Contains("__MigrationHistory"))
			{
				var list = excludedTable.ToList();
				list.Add("__MigrationHistory");
				excludedTable = list.ToArray();
			}
			var exclusions = usePartialExcludedTableName ? tables.Where(t => excludedTable.Any(t.Contains)) : tables.Where(excludedTable.Contains);
			// Remove exlusions
			tables.RemoveAll(exclusions.Contains);
			// Deactivate db consistency check
			foreach (var table in tables)
			{
				var com = connection.CreateCommand();
				com.CommandText = $"ALTER TABLE [{table}] NOCHECK CONSTRAINT ALL";
				com.ExecuteNonQuery();
			}
			// Remove all data from tables
			foreach (var table in tables)
			{
				var com = connection.CreateCommand();
				com.CommandText = $"DELETE FROM [{table}]";
				com.ExecuteNonQuery();
			}
			// Activate db consistency check
			foreach (var table in tables)
			{
				var com = connection.CreateCommand();
				com.CommandText = $"ALTER TABLE [{table}] CHECK CONSTRAINT ALL";
				com.ExecuteNonQuery();
			}
			// Close connection
			connection.Close();
		}
	}

	#region Sequences
	public static int CreateSequence(this DbContext me, string name, int startWith = 1, int increment = 1, int minValue = 1, int maxValue = int.MaxValue, bool cycle = false)
	{
		var con = new SqlConnection(me.Database.Connection.ConnectionString);
		con.Open();
		var com = new SqlCommand($@"CREATE SEQUENCE [{name}]
				AS int
				START WITH {startWith}
				INCREMENT BY {increment}
				MINVALUE {minValue}
				MAXVALUE {maxValue}
				{(cycle ? " CYCLE" : "")}", con);
		var res = Convert.ToInt32(com.ExecuteScalar());
		con.Close();
		return res;
	}
	public static int DeleteSequence(this DbContext me, string name)
	{
		var con = new SqlConnection(me.Database.Connection.ConnectionString);
		con.Open();
		var com = new SqlCommand($"DROP SEQUENCE [{name}]", con);
		var res = Convert.ToInt32(com.ExecuteScalar());
		con.Close();
		return res;
	}
	public static int GetSequenceValue(this DbContext me, string name, bool increment = true)
	{
		var con = new SqlConnection(me.Database.Connection.ConnectionString);
		con.Open();
		SqlCommand com;
		if (increment)
			com = new($"SELECT NEXT VALUE FOR [{name}]", con);
		else
			com = new($"SELECT current_value FROM sys.sequences WHERE name = '{name}'", con);
		var res = Convert.ToInt32(com.ExecuteScalar());
		con.Close();
		return res;
	}
	public static void SetSequenceValue(this DbContext me, string name, int value)
	{
		var con = new SqlConnection(me.Database.Connection.ConnectionString);
		con.Open();
		var com = new SqlCommand($"ALTER SEQUENCE [{name}] RESTART WITH {value}", con);
		com.ExecuteNonQuery();
		con.Close();
	}
	#endregion
}