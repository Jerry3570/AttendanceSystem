using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();

// SQLite database initialization
string connectionString = "Data Source=attendance.db";
using (var connection = new SqliteConnection(connectionString))
{
connection.Open();
var command = connection.CreateCommand();
command.CommandText = @"
        CREATE TABLE IF NOT EXISTS Attendance (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            UserName TEXT NOT NULL,
            SignInTime TEXT NOT NULL,
            SignOutTime TEXT
        );
    ";
command.ExecuteNonQuery();
}

// Sign-in API 
app.MapPost("/signin", async (HttpContext context) =>
{
var requestBody = await context.Request.ReadFromJsonAsync<SignInRequest>();
if (requestBody == null || string.IsNullOrEmpty(requestBody.UserName))
return Results.BadRequest(new { message = "UserName is required" });

using var connection = new SqliteConnection(connectionString);
connection.Open();
var command = connection.CreateCommand();
command.CommandText = "INSERT INTO Attendance (UserName, SignInTime) VALUES (@name, @time)";
command.Parameters.AddWithValue("@name", requestBody.UserName);
command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
await command.ExecuteNonQueryAsync();

return Results.Json(new { message = "Sign-in successful" });
});

// Sign-out API
app.MapPost("/signout", async (HttpContext context) =>
{
var requestBody = await context.Request.ReadFromJsonAsync<SignInRequest>();
if (requestBody == null || string.IsNullOrEmpty(requestBody.UserName))
return Results.BadRequest(new { message = "UserName is required" });

using var connection = new SqliteConnection(connectionString);
connection.Open();
var command = connection.CreateCommand();
command.CommandText = "UPDATE Attendance SET SignOutTime = @time WHERE UserName = @name AND SignOutTime IS NULL";
command.Parameters.AddWithValue("@name", requestBody.UserName);
command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
int rowsAffected = await command.ExecuteNonQueryAsync();

if (rowsAffected == 0)
return Results.BadRequest(new { message = "No active sign-in record found." });

return Results.Json(new { message = "Sign-out successful" });
});

// Clear Records API
app.MapDelete("/clear", async () =>
{
using var connection = new SqliteConnection(connectionString);
connection.Open();
var command = connection.CreateCommand();
command.CommandText = "DELETE FROM Attendance";
await command.ExecuteNonQueryAsync();

return Results.Json(new { message = "All records cleared" });
});

// Get Records API
app.MapGet("/records", async () =>
{
using var connection = new SqliteConnection(connectionString);
connection.Open();
var command = connection.CreateCommand();
command.CommandText = "SELECT UserName, SignInTime, SignOutTime FROM Attendance ORDER BY SignInTime DESC";
using var reader = command.ExecuteReader();
var records = new List<object>();
while (reader.Read())
{
records.Add(new
{
userName = reader.GetString(0),
signInTime = reader.GetString(1),
signOutTime = reader.IsDBNull(2) ? "Not Signed Out" : reader.GetString(2)
});
}
return Results.Json(records);
});

app.Run();

// Define the request model for sign-in/sign-out
public class SignInRequest
{
    public string UserName { get; set; }
}