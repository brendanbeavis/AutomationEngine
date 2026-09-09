using AutomationEngine.Data.Entities;
using AutomationEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class DatabaseViewer
    {
        [Inject]
        private IDbContextFactory<AutomationDbContext> DbContextFactory { get; set; } = default!;

        [Inject]
        private ILogger<DatabaseViewer> Logger { get; set; } = default!;

        private List<TableInfo> Tables { get; set; } = new();
        private TableInfo? SelectedTable { get; set; }
        private List<Dictionary<string, object?>> TableData { get; set; } = new();
        private string? ErrorMessage { get; set; }
        private bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadTablesAsync();
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                IsLoading = true;
                ErrorMessage = null;
                Tables.Clear();

                var dbSetProperties = dbContext.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType && 
                                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .OrderBy(p => p.Name);

                foreach (var prop in dbSetProperties)
                {
                    var entityType = prop.PropertyType.GetGenericArguments()[0];
                    var tableName = GetTableName(entityType);

                    var table = new TableInfo
                    {
                        PropertyName = prop.Name,
                        TableName = tableName,
                        EntityType = entityType,
                        Columns = GetColumnsInfo(entityType),
                        RowCount=0
                    };

                    // Get row count using EF Core's CountAsync
                    var dbSet = prop.GetValue(dbContext);
                    if (dbSet is IQueryable query)
                    {
                        try
                        {
                            var countAsyncMethod = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                                .GetMethods()
                                .FirstOrDefault(m => m.Name == "CountAsync" && m.GetGenericArguments().Length == 1)?
                                .MakeGenericMethod(entityType);

                            if (countAsyncMethod is not null)
                            {
                                var resultTask = countAsyncMethod.Invoke(null, new object[] { query, System.Threading.CancellationToken.None }) as System.Threading.Tasks.Task;
                                if (resultTask is not null)
                                {
                                    await resultTask;
                                    var resultProperty = resultTask.GetType().GetProperty("Result");
                                    table.RowCount = (int)(resultProperty?.GetValue(resultTask) ?? 0);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Failed to get row count for table {TableName}. Defaulting to 0", tableName);
                        }
                    }

                    Tables.Add(table);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading database tables");
                ErrorMessage = $"Error loading tables: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SelectTableAsync(TableInfo table)
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                SelectedTable = table;
                ErrorMessage = null;
                TableData.Clear();

                var dbSet = dbContext.GetType()
                    .GetProperty(table.PropertyName)?
                    .GetValue(dbContext) as IQueryable;

                if (dbSet is not null)
                {
                    // Use reflection to call ToListAsync on the IQueryable
                    var toListAsyncMethod = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "ToListAsync" && m.GetGenericArguments().Length == 1)?
                        .MakeGenericMethod(table.EntityType);

                    if (toListAsyncMethod is not null)
                    {
                        var resultTask = toListAsyncMethod.Invoke(null, new object[] { dbSet, System.Threading.CancellationToken.None }) as System.Threading.Tasks.Task;
                        if (resultTask is not null)
                        {
                            await resultTask;

                            // Get the result from the Task<List<T>>
                            var resultProperty = resultTask.GetType().GetProperty("Result");
                            var results = resultProperty?.GetValue(resultTask) as System.Collections.IList;

                            if (results is not null)
                            {
                                foreach (var item in results)
                                {
                                    var row = new Dictionary<string, object?>();
                                    foreach (var prop in table.Columns)
                                    {
                                        var value = item.GetType().GetProperty(prop.Name)?.GetValue(item);
                                        row[prop.Name] = value;
                                    }
                                    TableData.Add(row);
                                }
                            }
                        }
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading table data");
                ErrorMessage = $"Error loading table data: {ex.Message}";
            }
        }

        private string GetTableName(Type entityType)
        {
            var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
            return tableAttr?.Name ?? entityType.Name;
        }

        private List<ColumnInfo> GetColumnsInfo(Type entityType)
        {
            var columns = new List<ColumnInfo>();
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Skip navigation properties
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && 
                    prop.PropertyType != typeof(string) && 
                    prop.PropertyType != typeof(byte[]))
                    continue;

                var column = new ColumnInfo
                {
                    Name = prop.Name,
                    Type = GetFriendlyTypeName(prop.PropertyType),
                    IsNullable = IsNullable(prop.PropertyType)
                };

                columns.Add(column);
            }

            return columns.OrderBy(c => c.Name).ToList();
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            return type.Name switch
            {
                "Int32" => "int",
                "Int64" => "long",
                "Double" => "double",
                "Decimal" => "decimal",
                "Boolean" => "bool",
                "DateTime" => "datetime",
                "String" => "string",
                "Byte[]" => "byte[]",
                _ => type.Name
            };
        }

        private bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private string FormatValue(object? value)
        {
            if (value is null)
                return "NULL";

            if (value is DateTime dt)
                return dt.ToString("g");

            if (value is byte[] bytes)
                return $"[{bytes.Length} bytes]";

            if (value is bool b)
                return b ? "true" : "false";

            return value.ToString() ?? "NULL";
        }

        private string GetTableIconStyle(bool isSelected)
        {
            return isSelected ? "color: var(--cp-accent-cyan)" : "color: rgba(230,247,255,0.4)";
        }

        public class TableInfo
        {
            public string PropertyName { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public Type EntityType { get; set; } = typeof(object);
            public List<ColumnInfo> Columns { get; set; } = new();
            public int RowCount { get; set; }
        }

        public class ColumnInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool IsNullable { get; set; }
        }
    }
}
