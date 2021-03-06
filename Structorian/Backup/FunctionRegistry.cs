using System;
using System.Collections.Generic;

namespace Structorian.Engine
{
    class FunctionRegistry<T, U, P> where T : IEvaluateContext where U : class
    {
        protected delegate U FunctionDelegate(T context, P[] parameters);

        private readonly Dictionary<String, FunctionDelegate> _functions = new Dictionary<string, FunctionDelegate>(StringComparer.InvariantCultureIgnoreCase);

        protected void Register(string name, FunctionDelegate functionDelegate)
        {
            _functions[name] = functionDelegate;
        }

        public U Evaluate(string function, P[] parameters, IEvaluateContext context)
        {
            FunctionDelegate evalDelegate;
            if (!_functions.TryGetValue(function, out evalDelegate))
                return null;
            if (context != null && !(context is T))
                throw new Exception("Invalid context type");

            return evalDelegate((T) context, parameters);
        }
    }
}
