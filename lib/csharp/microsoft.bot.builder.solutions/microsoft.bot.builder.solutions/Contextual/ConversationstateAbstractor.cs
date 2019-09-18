using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class ConversationstateAbstractor
    {
        public ConversationstateAbstractor(IStatePropertyAccessor<dynamic> accessor, List<string> targetProperties)
        {
            Accessor = accessor;
            TargetProperties = targetProperties;
        }

        private enum PartType
        {
            Method,
            Field,
            Property,
        }

        private IStatePropertyAccessor<dynamic> Accessor { get; set; }

        private List<string> TargetProperties { get; set; }

        private IList<string> ValueTupleElementAlias { get; set; } = null;

        internal async Task<List<dynamic>> AbstractTargetPropertiesAsync(ITurnContext turnContext)
        {
            List<dynamic> result = new List<dynamic>();
            var state = await Accessor.GetAsync(turnContext);

            foreach (var targetProperty in TargetProperties)
            {
                dynamic obj = state;
                foreach (string part in targetProperty.Split('.'))
                {
                    if (IsEmptyObject(obj))
                    {
                        return null;
                    }

                    dynamic subObj = null;

                    // This part is a method.
                    if (part.Contains("("))
                    {
                        int leftBracket = part.IndexOf("(");
                        var methodName = part.Substring(0, leftBracket);

                        // ToDo: handle parameters.

                        subObj = GetPartValue(obj, methodName, PartType.Method);
                    }

                    // This part is a filed or a property
                    else
                    {
                        // It's a ValueTuple
                        // We can only get field by 'ItemX' at runtime.
                        if (ValueTupleElementAlias != null)
                        {
                            int index = ScanValueTupleElementAlias(part);
                            subObj = GetPartValue(obj, string.Format("Item{0}", index), PartType.Field);
                        }
                        else
                        {
                            // Try property.
                            object tempObj = GetPartValue(obj, part, PartType.Property);
                            if (!IsEmptyObject(tempObj))
                            {
                                subObj = tempObj;
                            }

                            // Try field.
                            tempObj = GetPartValue(obj, part, PartType.Field);
                            if (!IsEmptyObject(tempObj))
                            {
                                subObj = tempObj;
                            }
                        }
                    }

                    obj = subObj;
                }

                if (!IsEmptyObject(obj))
                {
                    result.Add(obj);
                }
                else
                {
                    return null;
                }
            }

            return result;
        }

        private object GetPartValue(object obj, string name, PartType type)
        {
            object subObj = null;
            switch (type)
            {
                // Now we only support method without parameter.
                case PartType.Method:
                    MethodInfo mInfo = obj.GetType().GetMethod(name, new Type[0]);
                    if (mInfo != null)
                    {
                        subObj = mInfo.Invoke(obj, null);
                    }

                    // If it's a linq method. Should find in extension methods.
                    else if (obj is IEnumerable)
                    {
                        var linqMethods = GetExtensionMethods(typeof(Enumerable).Assembly);
                        var linqMethod = linqMethods
                            .Where(x => x.Name == name)
                            .Where(x => x.GetParameters().Length == 1)
                            .First();

                        if (linqMethod != null)
                        {
                            subObj = linqMethod.MakeGenericMethod(obj.GetType().GetGenericArguments()[0]).Invoke(null, new object[] { obj });
                        }
                    }

                    // If this method returns a ValueTuple. Need to cache its alias.
                    if (!IsEmptyObject(subObj) && subObj.GetType().Name.Contains("ValueTuple"))
                    {
                        ValueTupleElementAlias = mInfo.ReturnParameter.GetCustomAttribute<TupleElementNamesAttribute>().TransformNames;
                    }

                    return subObj;

                case PartType.Field:
                    FieldInfo fInfo = obj.GetType().GetField(name);
                    if (fInfo != null)
                    {
                        subObj = fInfo.GetValue(obj);
                    }

                    return subObj;

                case PartType.Property:
                    PropertyInfo pInfo = obj.GetType().GetProperty(name);
                    if (pInfo != null)
                    {
                        subObj = pInfo.GetValue(obj, null);
                    }

                    return subObj;

                default:
                    return null;
            }
        }

        private IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly)
        {
            var query = from type in assembly.GetTypes()
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        select method;
            return query;
        }

        private int ScanValueTupleElementAlias(string part)
        {
            if (ValueTupleElementAlias.Contains(part))
            {
                for (int index = 0; index < ValueTupleElementAlias.Count; index++)
                {
                    if (ValueTupleElementAlias[index] == part)
                    {
                        ValueTupleElementAlias = null;
                        return index + 1;
                    }
                }
            }

            ValueTupleElementAlias = null;

            // not found this field.
            return -1;
        }

        private bool IsEmptyObject(object obj)
        {
            return obj == null;
        }

    }
}
