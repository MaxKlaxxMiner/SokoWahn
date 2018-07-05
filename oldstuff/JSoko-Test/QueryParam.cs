using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JSoko
{
  [AttributeUsage(AttributeTargets.Field)]
  public sealed class QueryParam : Attribute
  {
    readonly string name;
    readonly bool ignore;
    readonly object alternate;

    public QueryParam(string name = null, bool ignore = false, object alternate = null)
    {
      this.name = name;
      this.ignore = ignore;
      this.alternate = alternate;
    }

    public static IEnumerable<KeyValuePair<string, string>> ParseQueryElements(string queryString)
    {
      if (string.IsNullOrEmpty(queryString)) yield break;
      string tmp = queryString;
      int p = tmp.IndexOf('?');
      if (p < 0) p = tmp.FirstOrDefault() == '&' ? 1 : 0; else p++;

      while (p < tmp.Length)
      {
        int p2 = tmp.IndexOf('=', p);
        int p3 = tmp.IndexOf('&', p);

        if (p2 <= p || (p2 > p3 && p3 > 0))
        {
          throw new NotImplementedException("todo: value-less param");
        }

        var name = tmp.Substring(p, p2 - p);
        p2++;
        if (p3 < 0) p3 = tmp.Length;
        var value = tmp.Substring(p2, p3 - p2);
        p = p3 + 1;

        yield return new KeyValuePair<string, string>(name, value);
      }

    }

    static class ParseModule<T>
    {
      // ReSharper disable once StaticMemberInGenericType
      static readonly Dictionary<string, Action<string, object>> ParseMethods = new Dictionary<string, Action<string, object>>();

      static int SafeConvertInt32(object val) { try { return Convert.ToInt32(val); } catch { return 0; } }
      static long SafeConvertInt64(object val) { try { return Convert.ToInt64(val); } catch { return 0; } }
      static string SafeConvertString(object val) { try { return Convert.ToString(val); } catch { return null; } }

      static ParseModule()
      {
        try
        {
          foreach (var field in typeof(T).GetFields())
          {
            string name = field.Name;
            object alternate = null;
            if (field.IsDefined(typeof(QueryParam)))
            {
              var qParam = field.GetCustomAttributes<QueryParam>().First();
              if (qParam.name != null) name = qParam.name;
              if (qParam.ignore) continue;
              alternate = qParam.alternate;
            }

            #region # // --- int.Parse() ---
            if (field.FieldType == typeof(int))
            {
              if (alternate != null)
              {
                int alternateValue = SafeConvertInt32(alternate);
                ParseMethods.Add(name, (value, target) =>
                {
                  int result;
                  if (!int.TryParse(value, out result)) result = alternateValue;
                  field.SetValue(target, result);
                });
              }
              else
              {
                ParseMethods.Add(name, (value, target) => field.SetValue(target, int.Parse(value)));
              }
              continue;
            }
            #endregion

            #region # // --- long.Parse() ---
            if (field.FieldType == typeof(long))
            {
              if (alternate != null)
              {
                long alternateValue = SafeConvertInt64(alternate);
                ParseMethods.Add(name, (value, target) =>
                {
                  long result;
                  if (!long.TryParse(value, out result)) result = alternateValue;
                  field.SetValue(target, result);
                });
              }
              else
              {
                ParseMethods.Add(name, (value, target) => field.SetValue(target, long.Parse(value)));
              }
              continue;
            }
            #endregion

            #region # // --- string ---
            if (field.FieldType == typeof(string))
            {
              if (alternate != null)
              {
                string alternateValue = SafeConvertString(alternate);
                ParseMethods.Add(name, (value, target) => field.SetValue(target, value ?? alternateValue));
              }
              else
              {
                ParseMethods.Add(name, (value, target) => field.SetValue(target, value));
              }
              continue;
            }
            #endregion

            throw new ArgumentException("type not found: " + field.FieldType);
          }
        }
        catch
        {
          // ignored
        }
      }

      public static T Parse(KeyValuePair<string, string>[] values)
      {
        if (typeof(T).IsClass)
        {
          var result = Activator.CreateInstance<T>();

          for (int i = 0; i < values.Length; i++)
          {
            Action<string, object> method;
            if (ParseMethods.TryGetValue(values[i].Key, out method))
            {
              method(values[i].Value, result);
            }
          }

          return result;
        }
        else
        {
          var result = (object)default(T);

          for (int i = 0; i < values.Length; i++)
          {
            Action<string, object> method;
            if (ParseMethods.TryGetValue(values[i].Key, out method))
            {
              method(values[i].Value, result);
            }
          }

          return (T)result;
        }
      }
    }

    public static T Parse<T>(string queryString)
    {
      return ParseModule<T>.Parse(ParseQueryElements(queryString).ToArray());
    }

  }
}
