﻿using System;
using System.Collections.Generic;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryBuilder
    {
        private readonly TypeRegistry typeRegistry;
        private readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
        private readonly List<string> whereParts = new List<string>();
        private Projection projection;
        private Type sourceType;
        private string sourceName;

        public QueryBuilder(TypeRegistry typeRegistry)
        {
            this.typeRegistry = typeRegistry;
        }

        public Ordering[] Orderings { get; set; }
        public string TableSectionName { get; set; }
        public int? Take { get; set; }

        public void SetProjection(Projection newProjection)
        {
            if (projection != null)
                throw new InvalidOperationException("only one select clause supported");
            projection = newProjection;
        }

        public void AddWherePart(string formatString, params object[] args)
        {
            whereParts.Add(string.Format(formatString, args));
        }

        public string AddParameter(object value)
        {
            var parameterName = "p" + parameters.Count;
            parameters.Add(parameterName, value);
            return parameterName;
        }

        public BuiltQuery Build()
        {
            var resultBuilder = new StringBuilder();
            resultBuilder.Append("ВЫБРАТЬ ");
            if (Take.HasValue)
            {
                resultBuilder.Append("ПЕРВЫЕ ");
                resultBuilder.Append(Take.Value);
                resultBuilder.Append(" ");
            }
            var selection = projection == null
                ? ConfigurationName.Get(sourceType).HasReference ? "src.Ссылка" : "*"
                : projection.GetSelection();
            resultBuilder.Append(selection);
            resultBuilder.Append(" ИЗ ");
            resultBuilder.Append(sourceName);
            if (TableSectionName != null)
            {
                resultBuilder.Append('.');
                resultBuilder.Append(TableSectionName);
            }
            resultBuilder.Append(" КАК src");
            if (whereParts.Count > 0)
            {
                resultBuilder.Append(" ГДЕ ");
                if (whereParts.Count == 1)
                    resultBuilder.Append(whereParts[0]);
                else
                {
                    resultBuilder.Append("(");
                    resultBuilder.Append(whereParts.JoinStrings(" И "));
                    resultBuilder.Append(")");
                }
            }
            if (Orderings != null)
            {
                resultBuilder.Append(" УПОРЯДОЧИТЬ ПО ");
                for (var i = 0; i < Orderings.Length; i++)
                {
                    if (i != 0)
                        resultBuilder.Append(',');
                    var ordering = Orderings[i];
                    resultBuilder.Append(ordering.Field.Expression);
                    if (!ordering.IsAsc)
                        resultBuilder.Append(" УБЫВ");
                }
            }
            return new BuiltQuery(sourceType, resultBuilder.ToString(), parameters, projection);
        }

        public void SetSource(Type type, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                sourceName = name;
                sourceType = typeRegistry.GetTypeOrNull(sourceName);
            }
            else
            {
                sourceName = ConfigurationName.Get(type).Fullname;
                sourceType = type;
            }
        }
    }
}