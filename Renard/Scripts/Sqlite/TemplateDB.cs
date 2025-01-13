using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Renard.Sqlite;

[Serializable]
public abstract class TemplateDB
{
    [Serializable]
    public abstract class DataColumn
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id = string.Empty;

        /// <summary>
        /// 備考
        /// </summary>
        public string Other = string.Empty;

        public void Set(DataRow dr)
        {
            if (dr == null) return;

            Id = dr.Keys.Contains("Id") ? (string)dr["Id"] : string.Empty;
            Other = dr.Keys.Contains("Other") ? (string)dr["Other"] : string.Empty;

            OnSet(dr);
        }

        protected abstract void OnSet(DataRow dr);
    }

    protected static string CreateSelectQuery(string tableName, string[] idList)
    {
        if (string.IsNullOrEmpty(tableName))
            return string.Empty;

        var where = string.Empty;
        var order = "ORDER BY Id";

        if (idList != null && idList.Length > 0)
        {
            var whereEdit = string.Empty;
            for (int i = 0; i < idList.Length; i++)
            {
                if (string.IsNullOrEmpty(idList[i]))
                    continue;

                if (!string.IsNullOrEmpty(whereEdit))
                {
                    whereEdit += ", ";
                }

                whereEdit += $"'{idList[i]}'";
            }

            if (!string.IsNullOrEmpty(whereEdit))
            {
                if (string.IsNullOrEmpty(where))
                {
                    where = $"WHERE Id IN({whereEdit})";
                }
                else
                {
                    where += $" AND Id IN({whereEdit})";
                }
            }
        }

        return $"SELECT * FROM {tableName} {where} {order};";
    }

    protected static async UniTask<T[]> OnExecuteQueryCoroutine<T>(CancellationToken cancellationToken, string directoryPath, string dbFileName, string query) where T : DataColumn, new()
    {
        try
        {
            var dataTable = await GetData(cancellationToken, directoryPath, dbFileName, query);

            var list = new List<T>();
            if (dataTable != null)
            {
                foreach (DataRow dr in dataTable.Rows)
                {
                    var data = new T();
                    data.Set(dr);
                    list.Add(data);
                }
            }
            return list.ToArray();
        }
        catch
        {
            return null;
        }
    }

    protected static async UniTask<DataTable> GetData(CancellationToken token, string directoryPath, string dbFileName, string query)
    {
        var sqlite = new SqliteHandler(directoryPath, dbFileName);
        await UniTask.SwitchToMainThread();
        return sqlite.ExecuteQuery(query);
    }
}
