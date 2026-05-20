using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;

namespace LatexStudio.LicenseGenerator;

public record LicenseRecord(
    int Id,
    string ClientName,
    string DeviceId,
    string Price,
    string IssueDate,
    string ExpiryDate,
    bool IsPaid);

public static class LicenseDatabase
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LatexStudio",
        "licenses.db");

    static LicenseDatabase()
    {
        var dir = Path.GetDirectoryName(DbPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Licenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ClientName TEXT,
                DeviceId TEXT,
                Price TEXT,
                IssueDate TEXT,
                ExpiryDate TEXT,
                IsPaid INTEGER
            )";
        command.ExecuteNonQuery();
    }

    public static void Save(LicenseRecord record)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Licenses (ClientName, DeviceId, Price, IssueDate, ExpiryDate, IsPaid)
            VALUES ($client, $device, $price, $issue, $expiry, $paid)";
        command.Parameters.AddWithValue("$client", record.ClientName);
        command.Parameters.AddWithValue("$device", record.DeviceId);
        command.Parameters.AddWithValue("$price", record.Price);
        command.Parameters.AddWithValue("$issue", record.IssueDate);
        command.Parameters.AddWithValue("$expiry", record.ExpiryDate);
        command.Parameters.AddWithValue("$paid", record.IsPaid ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static List<LicenseRecord> GetAll()
    {
        var list = new List<LicenseRecord>();
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Licenses ORDER BY Id DESC";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new LicenseRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetInt32(6) == 1));
        }
        return list;
    }

    public static void UpdatePaymentStatus(int id, bool isPaid)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Licenses SET IsPaid = $paid WHERE Id = $id";
        command.Parameters.AddWithValue("$paid", isPaid ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}
