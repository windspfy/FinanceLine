using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FinanceLine.Helper
{
    class CommonHelper
    {
        internal static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        internal static List<object> ObjectToList(object o)
        {
            if (o == null) return null;
            List<object> ret = new List<object>();
            var enumerator = ((IEnumerable)o).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ret.Add(enumerator.Current);
            }
            return ret;
        }

        internal static bool DataTableIsNullOrEmpty(DataTable t)
        {
            if (t == null) return true;
            if (t.Rows.Count < 1) return true;
            return false;
        }

        internal static T DataTableToObject<T>(DataTable t) where T : new()
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            if (t.Rows.Count < 1) throw new ArgumentException("No rows in DataTable");
            foreach (DataRow r in t.Rows)
            {
                return DataRowToObject<T>(r);
            }
            return default(T);
        }

        internal static T DataRowToObject<T>(DataRow r) where T : new()
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            T item = new T();
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            foreach (var property in properties)
            {
                property.SetValue(item, r[property.Name], null);
            }
            return item;
        }

        internal static List<dynamic> DataTableToListDynamic(DataTable dt)
        {
            List<dynamic> ret = new List<dynamic>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

        internal static dynamic DataTableToDynamic(DataTable dt)
        {
            dynamic ret = new ExpandoObject();
            if (dt == null || dt.Rows.Count < 1) return ret;
            if (dt.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }

        internal static List<Dictionary<string, object>> DataTableToListDictionary(DataTable dt)
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                Dictionary<string, object> currDict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    currDict.Add(col.ColumnName, curr[col]);
                }

                ret.Add(currDict);
            }

            return ret;
        }

        internal static Dictionary<string, object> DataTableToDictionary(DataTable dt)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (dt == null || dt.Rows.Count < 1) return ret;
            if (dt.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    ret.Add(col.ColumnName, curr[col]);
                }

                return ret;
            }

            return ret;
        }

        internal static bool IsExtendedCharacters(string data)
        {
            if (String.IsNullOrEmpty(data)) return false;
            foreach (char c in data)
            {
                if ((int)c > 256) return true;
            }
            return false;
        }
    }

    public class Expression
    {
        #region Constructor

        /// <summary>
        /// A structure in the form of term-operator-term that defines a boolean operation within a WHERE clause.
        /// </summary>
        public Expression()
        {
        }

        /// <summary>
        /// A structure in the form of term-operator-term that defines a boolean operation within a WHERE clause.
        /// </summary>
        /// <param name="left">The left term of the expression; can either be a string term or a nested Expression.</param>
        /// <param name="oper">The operator.</param>
        /// <param name="right">The right term of the expression; can either be an object for comparison or a nested Expression.</param>
        public Expression(object left, Operators oper, object right)
        {
            LeftTerm = left;
            Operator = oper;
            RightTerm = right;
        }

        #endregion

        #region Public-Members

        /// <summary>
        /// The left term of the expression; can either be a string term or a nested Expression.
        /// </summary>
        public object LeftTerm;

        /// <summary>
        /// The boolean operator.
        /// </summary>
        public Operators Operator;

        /// <summary>
        /// The right term of the expression; can either be an object for comparison or a nested Expression.
        /// </summary>
        public object RightTerm;

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Converts an Expression to a string that is compatible for use in a WHERE clause.
        /// </summary>
        /// <returns>String containing human-readable version of the Expression.</returns>
        public string ToWhereClause()
        {
            string clause = "";

            if (LeftTerm == null) return null;

            clause += "(";

            if (LeftTerm is Expression)
            {
                clause += ((Expression)LeftTerm).ToWhereClause() + " ";
            }
            else
            {
                if (!(LeftTerm is string))
                {
                    Console.WriteLine("ToWhereClause LeftTerm is not string (" + LeftTerm.GetType() + ")");
                    return null;
                }

                if (Operator != Operators.Contains
                    && Operator != Operators.ContainsNot)
                {
                    //
                    // These operators will add the left term
                    //
                    clause += SanitizeString(LeftTerm.ToString()) + " ";
                }
            }

            switch (Operator)
            {
                #region Process-By-Operators

                case Operators.And:
                    if (RightTerm == null) return null;
                    clause += "AND ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.Or:
                    if (RightTerm == null) return null;
                    clause += "OR ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.Equals:
                    if (RightTerm == null) return null;
                    clause += "= ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.NotEquals:
                    if (RightTerm == null) return null;
                    clause += "<> ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.In:
                    if (RightTerm == null) return null;
                    int inAdded = 0;
                    if (!CommonHelper.IsList(RightTerm)) return null;
                    List<object> inTempList = CommonHelper.ObjectToList(RightTerm);
                    clause += " IN ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        clause += "(";
                        foreach (object currObj in inTempList)
                        {
                            if (inAdded == 0)
                            {
                                if (currObj is DateTime || currObj is DateTime?)
                                {
                                    clause += "'" + DbTimestamp(currObj) + "'";
                                }
                                else
                                {
                                    clause += "'" + SanitizeString(currObj.ToString()) + "'";
                                }
                                inAdded++;
                            }
                            else
                            {
                                if (currObj is DateTime || currObj is DateTime?)
                                {
                                    clause += "'" + DbTimestamp(currObj) + "'";
                                }
                                else
                                {
                                    clause += ",'" + SanitizeString(currObj.ToString()) + "'";
                                }
                                inAdded++;
                            }
                        }
                        clause += ")";
                    }
                    break;

                case Operators.NotIn:
                    if (RightTerm == null) return null;
                    int notInAdded = 0;
                    if (!CommonHelper.IsList(RightTerm)) return null;
                    List<object> notInTempList = CommonHelper.ObjectToList(RightTerm);
                    clause += " NOT IN ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        clause += "(";
                        foreach (object currObj in notInTempList)
                        {
                            if (notInAdded == 0)
                            {
                                if (currObj is DateTime || currObj is DateTime?)
                                {
                                    clause += "'" + DbTimestamp(currObj) + "'";
                                }
                                else
                                {
                                    clause += "'" + SanitizeString(currObj.ToString()) + "'";
                                }
                                notInAdded++;
                            }
                            else
                            {
                                if (currObj is DateTime || currObj is DateTime?)
                                {
                                    clause += "'" + DbTimestamp(currObj) + "'";
                                }
                                else
                                {
                                    clause += ",'" + SanitizeString(currObj.ToString()) + "'";
                                }
                                notInAdded++;
                            }
                        }
                        clause += ")";
                    }
                    break;

                case Operators.Contains:
                    if (RightTerm == null) return null;
                    if (RightTerm is string)
                    {
                        clause +=
                            "(" + SanitizeString(LeftTerm.ToString()) + " LIKE '" + SanitizeString(RightTerm.ToString()) + "%'" +
                            "OR " + SanitizeString(LeftTerm.ToString()) + " LIKE '%" + SanitizeString(RightTerm.ToString()) + "%'" +
                            "OR " + SanitizeString(LeftTerm.ToString()) + " LIKE '%" + SanitizeString(RightTerm.ToString()) + "')";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                case Operators.ContainsNot:
                    if (RightTerm == null) return null;
                    if (RightTerm is string)
                    {
                        clause +=
                            "(" + SanitizeString(LeftTerm.ToString()) + " NOT LIKE '" + SanitizeString(RightTerm.ToString()) + "%'" +
                            "OR " + SanitizeString(LeftTerm.ToString()) + " NOT LIKE '%" + SanitizeString(RightTerm.ToString()) + "%'" +
                            "OR " + SanitizeString(LeftTerm.ToString()) + " NOT LIKE '%" + SanitizeString(RightTerm.ToString()) + "')";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                case Operators.GreaterThan:
                    if (RightTerm == null) return null;
                    clause += "> ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.GreaterThanOrEqualTo:
                    if (RightTerm == null) return null;
                    clause += ">= ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.LessThan:
                    if (RightTerm == null) return null;
                    clause += "< ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.LessThanOrEqualTo:
                    if (RightTerm == null) return null;
                    clause += "<= ";
                    if (RightTerm is Expression)
                    {
                        clause += ((Expression)RightTerm).ToWhereClause();
                    }
                    else
                    {
                        if (RightTerm is DateTime || RightTerm is DateTime?)
                        {
                            clause += "'" + DbTimestamp(RightTerm) + "'";
                        }
                        else
                        {
                            clause += "'" + SanitizeString(RightTerm.ToString()) + "'";
                        }
                    }
                    break;

                case Operators.IsNull:
                    clause += " IS NULL";
                    break;

                case Operators.IsNotNull:
                    clause += " IS NOT NULL";
                    break;

                    #endregion
            }

            clause += ")";

            return clause;
        }

        /// <summary>
        /// Display Expression in a human-readable string.
        /// </summary>
        /// <returns>String containing human-readable version of the Expression.</returns>
        public override string ToString()
        {
            string ret = "";
            ret += "(";

            if (LeftTerm is Expression) ret += ((Expression)LeftTerm).ToString();
            else ret += LeftTerm.ToString();

            ret += " " + Operator.ToString() + " ";

            if (RightTerm is Expression) ret += ((Expression)RightTerm).ToString();
            else ret += RightTerm.ToString();

            ret += ")";
            return ret;
        }

        /// <summary>
        /// Prepends the Expression in prepend to the Expression original using an AND clause.
        /// </summary>
        /// <param name="prepend">The Expression to prepend.</param>
        /// <param name="original">The original Expression.</param>
        /// <returns>A new Expression.</returns>
        public static Expression PrependAndClause(Expression prepend, Expression original)
        {
            if (prepend == null) throw new ArgumentNullException(nameof(prepend));
            if (original == null) throw new ArgumentNullException(nameof(original));
            Expression ret = new Expression
            {
                LeftTerm = prepend,
                Operator = Operators.And,
                RightTerm = original
            };
            return ret;
        }

        /// <summary>
        /// Prepends the Expression in prepend to the Expression original using an OR clause.
        /// </summary>
        /// <param name="prepend">The Expression to prepend.</param>
        /// <param name="original">The original Expression.</param>
        /// <returns>A new Expression.</returns>
        public static Expression PrependOrClause(Expression prepend, Expression original)
        {
            if (prepend == null) throw new ArgumentNullException(nameof(prepend));
            if (original == null) throw new ArgumentNullException(nameof(original));
            Expression ret = new Expression
            {
                LeftTerm = prepend,
                Operator = Operators.Or,
                RightTerm = original
            };
            return ret;
        }

        /// <summary>
        /// Convert a List of Expression objects to a nested Expression containing AND between each Expression in the list. 
        /// </summary>
        /// <param name="exprList">List of Expression objects.</param>
        /// <returns>A nested Expression.</returns>
        public static Expression ListToNestedAndExpression(List<Expression> exprList)
        {
            if (exprList == null) throw new ArgumentNullException(nameof(exprList));
            if (exprList.Count < 1) return null;

            int evaluated = 0;
            Expression ret = null;
            Expression left = null;
            List<Expression> remainder = new List<Expression>();

            if (exprList.Count == 1)
            {
                foreach (Expression curr in exprList)
                {
                    ret = curr;
                    break;
                }

                return ret;
            }
            else
            {
                foreach (Expression curr in exprList)
                {
                    if (evaluated == 0)
                    {
                        left = new Expression();
                        left.LeftTerm = curr.LeftTerm;
                        left.Operator = curr.Operator;
                        left.RightTerm = curr.RightTerm;
                        evaluated++;
                    }
                    else
                    {
                        remainder.Add(curr);
                        evaluated++;
                    }
                }

                ret = new Expression();
                ret.LeftTerm = left;
                ret.Operator = Operators.And;
                Expression right = ListToNestedAndExpression(remainder);
                ret.RightTerm = right;

                return ret;
            }
        }

        /// <summary>
        /// Convert a List of Expression objects to a nested Expression containing OR between each Expression in the list. 
        /// </summary>
        /// <param name="exprList">List of Expression objects.</param>
        /// <returns>A nested Expression.</returns>
        public static Expression ListToNestedOrExpression(List<Expression> exprList)
        {
            if (exprList == null) throw new ArgumentNullException(nameof(exprList));
            if (exprList.Count < 1) return null;

            int evaluated = 0;
            Expression ret = null;
            Expression left = null;
            List<Expression> remainder = new List<Expression>();

            if (exprList.Count == 1)
            {
                foreach (Expression curr in exprList)
                {
                    ret = curr;
                    break;
                }

                return ret;
            }
            else
            {
                foreach (Expression curr in exprList)
                {
                    if (evaluated == 0)
                    {
                        left = new Expression();
                        left.LeftTerm = curr.LeftTerm;
                        left.Operator = curr.Operator;
                        left.RightTerm = curr.RightTerm;
                        evaluated++;
                    }
                    else
                    {
                        remainder.Add(curr);
                        evaluated++;
                    }
                }

                ret = new Expression();
                ret.LeftTerm = left;
                ret.Operator = Operators.Or;
                Expression right = ListToNestedOrExpression(remainder);
                ret.RightTerm = right;

                return ret;
            }
        }

        /// <summary>
        /// Prepends a new Expression using the supplied left term, operator, and right term using an AND clause.
        /// </summary>
        /// <param name="left">The left term of the expression; can either be a string term or a nested Expression.</param>
        /// <param name="oper">The operator.</param>
        /// <param name="right">The right term of the expression; can either be an object for comparison or a nested Expression.</param>
        public void PrependAnd(object left, Operators oper, object right)
        {
            Expression e = new Expression(left, oper, right);
            PrependAnd(e);
        }

        /// <summary>
        /// Prepends the Expression with the supplied Expression using an AND clause.
        /// </summary>
        /// <param name="prepend">The Expression to prepend.</param> 
        public void PrependAnd(Expression prepend)
        {
            if (prepend == null) throw new ArgumentNullException(nameof(prepend));

            Expression orig = new Expression(this.LeftTerm, this.Operator, this.RightTerm);
            Expression e = PrependAndClause(prepend, orig);
            LeftTerm = e.LeftTerm;
            Operator = e.Operator;
            RightTerm = e.RightTerm;

            return;
        }

        /// <summary>
        /// Prepends a new Expression using the supplied left term, operator, and right term using an OR clause.
        /// </summary>
        /// <param name="left">The left term of the expression; can either be a string term or a nested Expression.</param>
        /// <param name="oper">The operator.</param>
        /// <param name="right">The right term of the expression; can either be an object for comparison or a nested Expression.</param>
        public void PrependOr(object left, Operators oper, object right)
        {
            Expression e = new Expression(left, oper, right);
            PrependOr(e);
        }

        /// <summary>
        /// Prepends the Expression with the supplied Expression using an OR clause.
        /// </summary>
        /// <param name="prepend">The Expression to prepend.</param> 
        public void PrependOr(Expression prepend)
        {
            if (prepend == null) throw new ArgumentNullException(nameof(prepend));

            Expression orig = new Expression(this.LeftTerm, this.Operator, this.RightTerm);
            Expression e = PrependOrClause(prepend, orig);
            LeftTerm = e.LeftTerm;
            Operator = e.Operator;
            RightTerm = e.RightTerm;

            return;
        }

        #endregion

        #region Private-Methods

        private string SanitizeString(string s)
        {
            if (String.IsNullOrEmpty(s)) return String.Empty;
            string ret = "";
            int doubleDash = 0;
            int openComment = 0;
            int closeComment = 0;

            //
            // null, below ASCII range, above ASCII range
            //
            for (int i = 0; i < s.Length; i++)
            {
                if (
                    ((int)(s[i]) == 0) || // null
                    ((int)(s[i]) < 32)
                    )
                {
                    continue;
                }
                else
                {
                    ret += s[i];
                }
            }

            //
            // double dash
            //
            doubleDash = 0;
            while (true)
            {
                doubleDash = ret.IndexOf("--");
                if (doubleDash < 0)
                {
                    break;
                }
                else
                {
                    ret = ret.Remove(doubleDash, 2);
                }
            }

            //
            // open comment
            // 
            openComment = 0;
            while (true)
            {
                openComment = ret.IndexOf("/*");
                if (openComment < 0) break;
                else
                {
                    ret = ret.Remove(openComment, 2);
                }
            }

            //
            // close comment
            //
            closeComment = 0;
            while (true)
            {
                closeComment = ret.IndexOf("*/");
                if (closeComment < 0) break;
                else
                {
                    ret = ret.Remove(closeComment, 2);
                }
            }

            //
            // in-string replacement
            //
            ret = ret.Replace("'", "''");
            return ret;
        }

        private string DbTimestamp(object ts)
        {
            DateTime dt = DateTime.Now;
            if (ts == null) return null;
            if (ts is DateTime?) dt = Convert.ToDateTime(ts);
            else if (ts is DateTime) dt = (DateTime)ts;
            return dt.ToString("MM/dd/yyyy hh:mm:ss.fffffff tt");
        }

        #endregion
    }
}
