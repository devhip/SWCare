using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.Data;

public static class DbExceptionExtensions
{
    /// <summary>SQL Server 2601 / 2627: duplicate key in a unique index or constraint.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
