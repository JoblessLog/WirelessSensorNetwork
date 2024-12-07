using System.Data;
using System.Data.SqlClient;

namespace wsn_keboo.Data;

public class ModifyDb : DbConnection
{
    SqlDataAdapter? adapter;
    SqlCommand? command;
    public ModifyDb() { }

    public DataTable GetAllNodeInfo()
    {
        DataTable dataTable = new DataTable();
        string query = @"select * from NodeInfo
                                ORDER BY id DESC;";
        using (SqlConnection conn = DbConnection.GetConnection())
        {
            conn.Open();
            adapter = new SqlDataAdapter(query, conn);
            adapter.Fill(dataTable);
            conn.Close(); 
        }
        return dataTable;
    }
    public bool insert(NodeInfo info)
    {
        SqlConnection conn = DbConnection.GetConnection();
        string query = @"INSERT INTO NodeInfo (ID,[Time],[Temperature],[RSSI],[SNR]) 
                            VALUES 
                            ('" + info.Id + "'," +
                                "'" + info.Time + "'," +
                                "'" + info.Temp + "'," +
                                "'" + info.Rssi + "'," +
                                "'" + info.Snr + "')";
        try
        {
            conn.Open();
            command = new SqlCommand(query, conn);
            command.ExecuteNonQuery();
        }
        catch { return false; }
        finally { conn.Close(); }
        return true;
    }
    
    public bool delete(string id)
    {
        SqlConnection conn = DbConnection.GetConnection();
        string query = "DELETE FROM NodeInfo WHERE ID IN (SELECT '" + id + "')";
        try
        {
            conn.Open();
            command = new SqlCommand(query, conn);
            command.ExecuteNonQuery();
        }
        catch { return false; }
        finally { conn.Close(); }
        return true;
    }
}
