using ApiRefactor.Models;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ApiRefactor.Tests;

public class SqlConnectionTests
{
    [Fact]
    public void GetSqlConnection_ShouldReturnSqliteConnection()
    {
        // Act
        var connection = SqlConnection.GetSqlConnection();

        // Assert
        connection.ShouldNotBeNull();
        connection.ShouldBeOfType<SqliteConnection>();
    }

    [Fact]
    public void GetSqlConnection_ShouldHaveCorrectConnectionString()
    {
        // Act
        var connection = SqlConnection.GetSqlConnection();

        // Assert
        connection.ConnectionString.ShouldContain("waves.db");
        connection.ConnectionString.ShouldContain("App_Data");
    }

    [Fact]
    public void GetSqlConnection_ShouldReturnNewInstanceEachTime()
    {
        // Act
        var connection1 = SqlConnection.GetSqlConnection();
        var connection2 = SqlConnection.GetSqlConnection();

        // Assert
        connection1.ShouldNotBeSameAs(connection2);
    }

    [Fact]
    public void GetSqlConnection_ShouldReturnConnectionInClosedState()
    {
        // Act
        var connection = SqlConnection.GetSqlConnection();

        // Assert
        connection.State.ShouldBe(System.Data.ConnectionState.Closed);
    }
}