using InRule.Repository;
using InRule.Repository.EndPoints;
using InRule.Repository.ValueLists;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace IntegrationTrainingSamples.Helpers
{
    internal static class AuthoringInCodeExtensions
    {
        internal static void UpdateDbConnectionString(this RuleApplicationDef ruleApp, string dbConnectionName, string newConnectionString)
        {
            if (ruleApp.EndPoints.Contains(dbConnectionName) && ruleApp.EndPoints[dbConnectionName] is DatabaseConnection dbConnection)
                dbConnection.ConnectionString = newConnectionString;
        }

        internal static void UpdateRestRootUrl(this RuleApplicationDef ruleApp, string restServiceName, string newRootUrl)
        {
            if (ruleApp.EndPoints.Contains(restServiceName) && ruleApp.EndPoints[restServiceName] is RestServiceDef restService)
                restService.RootUrl = newRootUrl;
        }

        internal static void UpdateRestOperationHeader(this RuleApplicationDef ruleApp, string restOperationName, string headerName, string newHeaderValue)
        {
            if (ruleApp.DataElements.Contains(restOperationName) && ruleApp.DataElements[restOperationName] is RestOperationDef restOperation)
            {
                var relevantHeader = restOperation.Headers.FirstOrDefault(h => h.Name == headerName);
                if (relevantHeader != null)
                    relevantHeader.Value = newHeaderValue;
            }
        }
        internal static void UpdateFieldDefaultValue(this RuleApplicationDef ruleApp, string entityName, string fieldName, string newDefaultValue)
        {
            if (ruleApp.Entities.Contains(entityName) && ruleApp.Entities[entityName].Fields.Contains(fieldName))
                ruleApp.Entities[entityName].Fields[fieldName].DefaultValue = newDefaultValue;
        }

        // For newData, the Inline Value List's "Value" column will come from the dictionary's Key,
        //  and the optional "DisplayName" column will come from the Dictionary item's Value (which can be null)
        internal static void UpdateInlineValueList(this RuleApplicationDef ruleApp, string inlineValueListName, Dictionary<string, string> newData)
        {
            if (ruleApp.DataElements.Contains(inlineValueListName) && ruleApp.DataElements[inlineValueListName] is InlineValueListDef valueList)
            {
                var data = valueList.Items;
                data.Clear();

                if (newData == null || newData.Count == 0)
                    return;

                foreach (var item in newData)
                {
                    data.Add(new ValueListItemDef(item.Key, item.Value));
                }
            }
        }

        internal static void UpdateInlineTable(this RuleApplicationDef ruleApp, string inlineTableName, object[,] newData)
        {
            if (ruleApp.DataElements.Contains(inlineTableName) && ruleApp.DataElements[inlineTableName] is TableDef inlineTable)
            {
                DataTable table = inlineTable.TableSettings.InlineDataTable;
                table.Rows.Clear();

                if (newData == null || newData.Length == 0)
                    return;

                for (int rowIndex = 0; rowIndex < newData.GetLength(0); rowIndex++)
                {
                    var newRow = table.NewRow();
                    for (int columnIndex = 0; columnIndex < newRow.ItemArray.Length; columnIndex++)
                    {
                        newRow[columnIndex] = newData[rowIndex, columnIndex];
                    }
                    table.Rows.Add(newRow);
                }
            }
        }
    }
}