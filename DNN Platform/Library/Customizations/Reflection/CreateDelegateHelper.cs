#region Usings

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#endregion

namespace DotNetNuke.Customizations.Reflection
{
    public static class CreateDelegateHelper
    {
        public static Delegate CreateAssignValueAction(Type inputType, string inputVariableName, IDictionary<string, object> assignDictionary)
        {
            ParameterExpression parameterExpression = Expression.Parameter(inputType, inputVariableName);

            List<BinaryExpression> assignExpressions = new List<BinaryExpression>();
            foreach (KeyValuePair<string, object> keyValuePair in assignDictionary)
            {
                MemberExpression memberExpression = Expression.Property(parameterExpression, keyValuePair.Key);
                ConstantExpression constant = Expression.Constant(keyValuePair.Value);
                BinaryExpression assign = Expression.Assign(memberExpression, constant);
                assignExpressions.Add(assign);
            }

            BlockExpression body = Expression.Block(assignExpressions);
            Type retType = typeof(Action<>).MakeGenericType(inputType);
            LambdaExpression lambdaExpression = Expression.Lambda(retType, body, parameterExpression);

            return lambdaExpression.Compile();
        }
    }
}
