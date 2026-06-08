using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace MingleWPF
{
    public enum LoginMessage
    {
        NoUserWithUsername,
        IncorrectPassword,
        SuccessfulLogin,
        SystemError
    }

    public enum SignUpMessage
    {
        UserWithSameUsername,
        InvalidShortPassword,
        SuccessfulSignUp,
        SystemError
    }

    public class ProjectModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string RootPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class DatabaseHandler
    {
        public static int CurrentUserId { get; set; }

        private const string ConnectionString = "Data Source=MingleDatabase.db";

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Email TEXT NOT NULL,
                        Password TEXT NOT NULL
                    );";

                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                string createProjectsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Projects (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        ProjectName TEXT NOT NULL,
                        RootPath TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );";

                using (var command = new SqliteCommand(createProjectsTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                string checkUserQuery = "SELECT COUNT(*) FROM Users";
                using (var checkCommand = new SqliteCommand(checkUserQuery, connection))
                {
                    long count = (long)checkCommand.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertQuery = "INSERT INTO Users (Username, Email, Password) VALUES (@Username, @Email, @Password)";
                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Username", "engin");
                            insertCommand.Parameters.AddWithValue("@Email", "enginsahin@comu.edu.tr");
                            insertCommand.Parameters.AddWithValue("@Password", HashPassword("engin123"));
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static void CreateNewProject(int userId, string projectName, string rootPath)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Projects (UserId, ProjectName, RootPath) VALUES (@UserId, @ProjectName, @RootPath)";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ProjectName", projectName);
                    command.Parameters.AddWithValue("@RootPath", rootPath);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static LoginMessage Login(string username, string password)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string selectQuery = "SELECT Id, Password FROM Users WHERE Username = @Username";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string storedHash = reader.GetString(1);
                            string inputHash = HashPassword(password);

                            if (storedHash == inputHash)
                            {
                                CurrentUserId = userId;
                                return LoginMessage.SuccessfulLogin;
                            }
                            else
                                return LoginMessage.IncorrectPassword;
                        }
                        else
                        {
                            return LoginMessage.NoUserWithUsername;
                        }
                    }
                }
            }

            return LoginMessage.SystemError;
        }

        public static SignUpMessage SignUp(string username, string email, string password)
        {
            if (password.Length < 8)
                return SignUpMessage.InvalidShortPassword;

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string selectQuery = "SELECT Username FROM Users WHERE Username = @Username";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    var result = command.ExecuteScalar();
                    if (result != null)
                        return SignUpMessage.UserWithSameUsername;
                }

                string insertQuery = "INSERT INTO Users (Username, Email, Password) VALUES (@Username, @Email, @Password)";
                using (var insertCommand = new SqliteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Username", username);
                    insertCommand.Parameters.AddWithValue("@Email", email);
                    insertCommand.Parameters.AddWithValue("@Password", HashPassword(password));
                    insertCommand.ExecuteNonQuery();
                }

                return SignUpMessage.SuccessfulSignUp;
            }

            return SignUpMessage.SystemError;
        }

        public static List<ProjectModel> GetProjectsByUserId(int userId)
        {
            List<ProjectModel> projects = new List<ProjectModel>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Id, ProjectName, RootPath, CreatedAt FROM Projects WHERE UserId = @UserId ORDER BY CreatedAt DESC";

                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projects.Add(new ProjectModel
                            {
                                Id = reader.GetInt32(0),
                                ProjectName = reader.GetString(1),
                                RootPath = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }
            return projects;
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}