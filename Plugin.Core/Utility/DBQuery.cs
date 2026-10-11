using System.Collections.Generic;

namespace Plugin.Core.Utility
{
    public class DBQuery
    {
        private readonly List<string> Tables;
        private readonly List<object> Values;

        public DBQuery()
        {
            Tables = new List<string>();
            Values = new List<object>();
        }

        public void AddQuery(string table, object value)
        {
            int index = Tables.IndexOf(table);
            if (index >= 0)
            {
                // Atualiza o valor para a coluna já existente
                Values[index] = value;
            }
            else
            {
                // Adiciona uma nova coluna e valor
                Tables.Add(table);
                Values.Add(value);
            }
        }

        public string[] GetTables()
        {
            return Tables.ToArray();
        }

        public object[] GetValues()
        {
            return Values.ToArray();
        }
    }
}
