// using Microsoft.AnalysisServices.Tabular;
// using TabularModelDeployer.Api.Models;

// namespace TabularModelDeployer.Api.Services;

// public class TabularDeploymentService
// {
//     private readonly IConfiguration _config;

//     public TabularDeploymentService(IConfiguration config)
//     {
//         _config = config;
//     }

//     public string DeployModel(DeploymentRequest request)
//     {
//         // 🔒 Service Principal from appsettings.json
//         var tenantId = _config["PowerBI:TenantId"];
//         var clientId = _config["PowerBI:ClientId"];
//         var clientSecret = _config["PowerBI:ClientSecret"];

//         // 🔄 Dynamic values from endpoint
//         var workspaceUrl =
//             $"powerbi://api.powerbi.com/v1.0/myorg/{request.WorkspaceName}";

//         var lakehouseServer = request.LakehouseServer;
//         var lakehouseDb = request.LakehouseDatabase;

//         string conn =
//             $"Provider=MSOLAP;Data Source={workspaceUrl};" +
//             $"User ID=app:{clientId}@{tenantId};Password={clientSecret};";

//         var server = new Server();
//         server.Connect(conn);

//         var schema = request.ModelSchema;

//         var db = server.Databases.FindByName(schema.Model_Name);

//         // ---------------- DATABASE CREATION ----------------
//         if (db == null)
//         {
//             db = new Database
//             {
//                 Name = schema.Model_Name,
//                 ID = schema.Model_Name,
//                 CompatibilityLevel = 1500  // ✅ Correct place
//             };

//             db.Model = new Model
//             {
//                 Name = schema.Model_Name
//             };

//             server.Databases.Add(db);
//         }

//         var model = db.Model;

//         // Clear old objects
//         model.Tables.Clear();
//         model.Relationships.Clear();
//         model.SaveChanges();

//         // ---------------- TABLE CREATION ----------------
//         foreach (var t in schema.Tables)
//         {
//             var table = new Table
//             {
//                 Name = t.Name
//             };

//             // 1️⃣ Add Columns
//             foreach (var c in t.Columns)
//             {
//                 var column = new DataColumn
//                 {
//                     Name = c.Name,
//                     SourceColumn = c.Name,
//                     DataType = c.Type.ToLower() switch
//                     {
//                         "double" => DataType.Double,
//                         "date" => DataType.DateTime,
//                         _ => DataType.String
//                     }
//                 };

//                 if (c.IsHidden == true)
//                     column.IsHidden = true;

//                 table.Columns.Add(column);
//             }

//             // 2️⃣ Create Partition
//             var partition = new Partition
//             {
//                 Name = "MainPartition",
//                 Mode = t.Is_Physical ? ModeType.DirectQuery : ModeType.Import
//             };

//             var msource = new MPartitionSource();

//             if (t.Is_Physical)
//             {
//                 msource.Expression = $@"
// let
//     Source = Sql.Database(""{lakehouseServer}"", ""{lakehouseDb}""),
//     Nav = Source{{[Schema=""dbo"",Item=""{t.Name}""]}}[Data]
// in
//     Nav";
//             }
//             else
//             {
//                 msource.Expression = @"
// let
//     Source = #table(
//         {""ORDER_AMOUNT""},
//         {
//             {100},
//             {200},
//             {300}
//         }
//     )
// in
//     Source";
//             }

//             partition.Source = msource;
//             table.Partitions.Add(partition);

//             // 3️⃣ Add table to model FIRST
//             model.Tables.Add(table);

//             // 4️⃣ Add Measures AFTER table is attached to model
//             if (t.Measures != null)
//             {
//                 foreach (var m in t.Measures)
//                 {
//                     var measure = new Measure
//                     {
//                         Name = m.Name,
//                         Expression = m.Expression
//                     };

//                     table.Measures.Add(measure);
//                 }
//             }
//         }

//         // ---------------- RELATIONSHIPS ----------------
//         foreach (var r in schema.Relationships)
//         {
//             var fromColumn = (DataColumn)
//                 model.Tables[r.From_Table].Columns[r.From_Col];

//             var toColumn = (DataColumn)
//                 model.Tables[r.To_Table].Columns[r.To_Col];

//             var relationship = new SingleColumnRelationship
//             {
//                 Name = r.Name,
//                 FromColumn = fromColumn,
//                 ToColumn = toColumn
//             };

//             model.Relationships.Add(relationship);
//         }

//         // Final Save
//         model.SaveChanges();
//         server.Disconnect();

//         return "🔥 Model deployed successfully with DirectQuery + Measures!";
//     }
// }


using Microsoft.AnalysisServices.Tabular;
using TabularModelDeployer.Api.Models;

namespace TabularModelDeployer.Api.Services;

public class TabularDeploymentService
{
    private readonly IConfiguration _config;

    public TabularDeploymentService(IConfiguration config)
    {
        _config = config;
    }

    public string DeployModel(DeploymentRequest request)
    {
        // 🔒 Service Principal from appsettings.json
        var tenantId = _config["PowerBI:TenantId"];
        var clientId = _config["PowerBI:ClientId"];
        var clientSecret = _config["PowerBI:ClientSecret"];

        // 🔄 Dynamic values from endpoint
        var workspaceUrl =
            $"powerbi://api.powerbi.com/v1.0/myorg/{request.WorkspaceName}";

        var lakehouseServer = request.LakehouseServer;
        var lakehouseDb = request.LakehouseDatabase;

        string conn =
            $"Provider=MSOLAP;Data Source={workspaceUrl};" +
            $"User ID=app:{clientId}@{tenantId};Password={clientSecret};";

        var server = new Server();
        server.Connect(conn);

        var schema = request.ModelSchema;

        var db = server.Databases.FindByName(schema.Model_Name);

        // ---------------- DATABASE CREATION ----------------
        if (db == null)
        {
            db = new Database
            {
                Name = schema.Model_Name,
                ID = schema.Model_Name,
                CompatibilityLevel = 1500
            };

            db.Model = new Model
            {
                Name = schema.Model_Name
            };

            server.Databases.Add(db);
        }

        var model = db.Model;

        // Clear old objects
        model.Tables.Clear();
        model.Relationships.Clear();
        model.SaveChanges();

        // ---------------- TABLE CREATION ----------------
        foreach (var t in schema.Tables)
        {
            var table = new Table
            {
                Name = t.Name
            };

            bool isMeasureOnlyTable =
                (t.Columns == null || t.Columns.Count == 0) &&
                (t.Measures != null && t.Measures.Count > 0);

            // 1️⃣ Add Columns
            if (t.Columns != null)
            {
                foreach (var c in t.Columns)
                {
                    var column = new DataColumn
                    {
                        Name = c.Name,
                        SourceColumn = c.Name,
                        DataType = c.Type.ToLower() switch
                        {
                            "double" => DataType.Double,
                            "date" => DataType.DateTime,
                            _ => DataType.String
                        }
                    };

                    if (c.IsHidden == true)
                        column.IsHidden = true;

                    table.Columns.Add(column);
                }
            }

            // 🔥 If measure-only table, inject DummyColumn (hidden)
            if (isMeasureOnlyTable)
            {
                var dummyColumn = new DataColumn
                {
                    Name = "DummyColumn",
                    DataType = DataType.String,
                    IsHidden = true
                };

                table.Columns.Add(dummyColumn);
            }

            // 2️⃣ Create Partition
            var partition = new Partition
            {
                Name = "MainPartition",
                Mode = t.Is_Physical ? ModeType.DirectQuery : ModeType.Import
            };

            var msource = new MPartitionSource();

            if (t.Is_Physical)
            {
                msource.Expression = $@"
let
    Source = Sql.Database(""{lakehouseServer}"", ""{lakehouseDb}""),
    Nav = Source{{[Schema=""dbo"",Item=""{t.Name}""]}}[Data]
in
    Nav";
            }
            else
            {
                msource.Expression = @"
let
    Source = #table(
        {""ORDER_AMOUNT""},
        {
            {100},
            {200},
            {300}
        }
    )
in
    Source";
            }

            partition.Source = msource;
            table.Partitions.Add(partition);

            // 3️⃣ Add table to model FIRST
            model.Tables.Add(table);

            // 4️⃣ Add Measures AFTER table is attached to model
            if (t.Measures != null)
            {
                foreach (var m in t.Measures)
                {
                    var measure = new Measure
                    {
                        Name = m.Name,
                        Expression = m.Expression
                    };

                    table.Measures.Add(measure);
                }
            }
        }

        // ---------------- RELATIONSHIPS ----------------
        foreach (var r in schema.Relationships)
        {
            var fromColumn = (DataColumn)
                model.Tables[r.From_Table].Columns[r.From_Col];

            var toColumn = (DataColumn)
                model.Tables[r.To_Table].Columns[r.To_Col];

            var relationship = new SingleColumnRelationship
            {
                Name = r.Name,
                FromColumn = fromColumn,
                ToColumn = toColumn
            };

            model.Relationships.Add(relationship);
        }

        // 🔥 SAVE ONCE (Model must validate with dummy column)
        model.SaveChanges();

        // 🔥 REMOVE DUMMY COLUMNS FROM MEASURE-ONLY TABLES
        foreach (var table in model.Tables)
        {
            var dummy = table.Columns.Find("DummyColumn");

            if (dummy != null)
            {
                table.Columns.Remove(dummy);
            }
        }

        // 🔥 FINAL SAVE (Table becomes pure Measure Table)
        model.SaveChanges();

        server.Disconnect();

        return "🔥 Model deployed successfully with DirectQuery + Measures!";
    }
}
