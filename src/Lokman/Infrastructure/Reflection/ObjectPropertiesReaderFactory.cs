using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Lokman
{
    public class ObjectPropertiesReaderFactory
    {
        /// <summary>
        /// Emits <see cref=" Action{IDictionary{string, object}, object}"/> delegate for reading object properties into <seealso cref="Dictionary{string, object}"/>
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Lazy<Action<IDictionary<string, object>, object>>> _objectReadersCache = new ConcurrentDictionary<Type, Lazy<Action<IDictionary<string, object>, object>>>();
        private static readonly Type[] _readerArgs = new[] { typeof(IDictionary<string, object>), typeof(object) };
        private static readonly MethodInfo _dictAddMethodInfo = typeof(IDictionary<string, object>).GetMethod(nameof(IDictionary.Add))!;

        public Action<IDictionary<string, object>, object> GetReader(Type type)
            => _objectReadersCache.GetOrAdd(type, t => new Lazy<Action<IDictionary<string, object>, object>>(() => CreateReaderDelegate(t))).Value;

        private static Action<IDictionary<string, object>, object> CreateReaderDelegate(Type type)
        {
            var dm = new DynamicMethod($"TypeReader_{Guid.NewGuid():N}", null, _readerArgs, restrictedSkipVisibility: true);
            var il = dm.GetILGenerator();
            /* Example of generated IL:
            ldarg.1
            castclass AnonType
            stloc.0

            ldarg.0
            ldstr "ClassExample"
            ldloc.0
            callvirt instance string AnonType::get_ClassExample()
            callvirt instance void class [mscorlib]System.Collections.Generic.IDictionary`2<string, object>::Add(!0, !1)

            ldarg.0
            ldstr "ValueExample"
            ldloc.0
            callvirt instance int32 AnonType::get_ValueExample()
            box [mscorlib]System.Int32
            callvirt instance void class [mscorlib]System.Collections.Generic.IDictionary`2<string, object>::Add(!0, !1)

            ret
            */
            il.DeclareLocal(type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Stloc_0);

            // Indexed properties are not useful for grabbing
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(prop => prop.GetIndexParameters().Length == 0 && prop.GetMethod != null);

            foreach (var p in properties)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, p.Name);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Callvirt, p.GetGetMethod()!);
                if (p.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, p.PropertyType);
                il.Emit(OpCodes.Callvirt, _dictAddMethodInfo);
            }
            il.Emit(OpCodes.Ret);
            return (Action<IDictionary<string, object>, object>)dm.CreateDelegate(typeof(Action<IDictionary<string, object>, object>));
        }
    }
}
