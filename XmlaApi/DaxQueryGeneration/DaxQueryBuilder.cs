using XmlaApi.Models;

namespace XmlaApi.DaxQueryGeneration
{
    public class DaxQueryBuilder
    { 

        
        public string GenerateDaxQuery(DaxAPI request)
        {
            // Ensure there is at least one GroupBy field to derive the table name
            if (request.GroupBy == null || request.GroupBy.Count == 0)
            {
                throw new ArgumentException("At least one GroupBy field is required to generate the DAX query.");
            }

            // Extract the table name from the first GroupBy field (assuming all groupBy fields are from the same table)
            var tableName = request.GroupBy[0].Id.Split('[')[0];  // Assumes all GroupBy fields are from the same table

            // Start building the DAX query using SUMMARIZE
            var daxQuery = $"EVALUATE SUMMARIZE(";

            // Add the table name 
            daxQuery += tableName + ", ";

            // Add GroupBy fields (Dimensions)
            foreach (var groupBy in request.GroupBy)
            {
                daxQuery += $"{groupBy.Id}, ";
            }

            // Remove the last comma and space
            daxQuery = daxQuery.TrimEnd(',', ' ') + ", ";

            // Add Aggregate fields (Measures) with valid syntax
            foreach (var aggregate in request.Aggregate)
            {
                daxQuery += $"\"{aggregate.Name}\", {aggregate.AggregationType.ToUpper()}({aggregate.Id}), ";
            }

            // Remove the last comma and space
            daxQuery = daxQuery.TrimEnd(',', ' ') + ")";

            return daxQuery;
        }

    }
}
