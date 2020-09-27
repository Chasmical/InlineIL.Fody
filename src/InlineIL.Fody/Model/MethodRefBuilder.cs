﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using InlineIL.Fody.Extensions;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace InlineIL.Fody.Model
{
    internal class MethodRefBuilder
    {
        private readonly ModuleDefinition _module;
        private MethodReference _method;

        private MethodRefBuilder(ModuleDefinition module, TypeReference typeRef, MethodReference method)
        {
            _module = module;

            method = method.MapToScope(typeRef.Scope, module);
            _method = _module.ImportReference(_module.ImportReference(method).MakeGeneric(typeRef));
        }

        private MethodRefBuilder(ModuleDefinition module, MethodReference method)
        {
            _module = module;
            _method = method;
        }

        public static MethodRefBuilder MethodByName(ModuleDefinition module, TypeReference typeRef, string methodName)
            => new MethodRefBuilder(module, typeRef, FindMethod(typeRef, methodName, null, null));

        public static MethodRefBuilder MethodByNameAndSignature(ModuleDefinition module, TypeReference typeRef, string methodName, int? genericArity, IReadOnlyList<TypeRefBuilder> paramTypes)
            => new MethodRefBuilder(module, typeRef, FindMethod(typeRef, methodName, genericArity, paramTypes ?? throw new ArgumentNullException(nameof(paramTypes))));

        public static MethodRefBuilder MethodFromDelegateReference(ModuleDefinition module, MethodReference methodRef)
        {
            if (methodRef.Name.StartsWith("<"))
                throw new WeavingException("A compiler-generated method is referenced by the delegate");

            return new MethodRefBuilder(module, methodRef);
        }

        private static MethodReference FindMethod(TypeReference typeRef, string methodName, int? genericArity, IReadOnlyList<TypeRefBuilder>? paramTypes)
        {
            var typeDef = typeRef.ResolveRequiredType();

            var methods = typeDef.Methods.Where(m => m.Name == methodName);

            if (genericArity != null)
            {
                methods = genericArity == 0
                    ? methods.Where(m => !m.HasGenericParameters)
                    : methods.Where(m => m.HasGenericParameters && m.GenericParameters.Count == genericArity);
            }

            if (paramTypes != null)
                methods = methods.Where(m => SignatureMatches(m, paramTypes));

            var methodList = methods.ToList();

            switch (methodList.Count)
            {
                case 1:
                    return methodList.Single();

                case 0:
                    throw paramTypes == null
                        ? new WeavingException($"Method '{methodName}' not found in type {typeDef.FullName}")
                        : new WeavingException($"Method {methodName}({string.Join(", ", paramTypes.Select(p => p.GetDisplayName()))}) not found in type {typeDef.FullName}");

                default:
                    throw paramTypes == null
                        ? new WeavingException($"Ambiguous method '{methodName}' in type {typeDef.FullName}")
                        : new WeavingException($"Ambiguous method {methodName}({string.Join(", ", paramTypes.Select(p => p.GetDisplayName()))}) in type {typeDef.FullName}");
            }
        }

        private static bool SignatureMatches(MethodReference method, IReadOnlyList<TypeRefBuilder> paramTypes)
        {
            if (method.Parameters.Count != paramTypes.Count)
                return false;

            for (var i = 0; i < paramTypes.Count; ++i)
            {
                var paramType = paramTypes[i].TryBuild(method);
                if (paramType == null)
                    return false;

                if (method.Parameters[i].ParameterType.FullName != paramType.FullName)
                    return false;
            }

            return true;
        }

        public static MethodRefBuilder PropertyGet(ModuleDefinition module, TypeReference typeRef, string propertyName)
        {
            var property = FindProperty(typeRef, propertyName);

            if (property.GetMethod == null)
                throw new WeavingException($"Property '{propertyName}' in type {typeRef.FullName} has no getter");

            return new MethodRefBuilder(module, typeRef, property.GetMethod);
        }

        public static MethodRefBuilder PropertySet(ModuleDefinition module, TypeReference typeRef, string propertyName)
        {
            var property = FindProperty(typeRef, propertyName);

            if (property.SetMethod == null)
                throw new WeavingException($"Property '{propertyName}' in type {typeRef.FullName} has no setter");

            return new MethodRefBuilder(module, typeRef, property.SetMethod);
        }

        private static PropertyDefinition FindProperty(TypeReference typeRef, string propertyName)
        {
            var typeDef = typeRef.ResolveRequiredType();

            var properties = typeDef.Properties.Where(p => p.Name == propertyName).ToList();

            return properties.Count switch
            {
                1 => properties.Single(),
                0 => throw new WeavingException($"Property '{propertyName}' not found in type {typeDef.FullName}"),
                _ => throw new WeavingException($"Ambiguous property '{propertyName}' in type {typeDef.FullName}")
            };
        }

        public static MethodRefBuilder EventAdd(ModuleDefinition module, TypeReference typeRef, string eventName)
        {
            var property = FindEvent(typeRef, eventName);

            if (property.AddMethod == null)
                throw new WeavingException($"Event '{eventName}' in type {typeRef.FullName} has no add method");

            return new MethodRefBuilder(module, typeRef, property.AddMethod);
        }

        public static MethodRefBuilder EventRemove(ModuleDefinition module, TypeReference typeRef, string eventName)
        {
            var property = FindEvent(typeRef, eventName);

            if (property.RemoveMethod == null)
                throw new WeavingException($"Event '{eventName}' in type {typeRef.FullName} has no remove method");

            return new MethodRefBuilder(module, typeRef, property.RemoveMethod);
        }

        public static MethodRefBuilder EventRaise(ModuleDefinition module, TypeReference typeRef, string eventName)
        {
            var property = FindEvent(typeRef, eventName);

            if (property.InvokeMethod == null)
                throw new WeavingException($"Event '{eventName}' in type {typeRef.FullName} has no raise method");

            return new MethodRefBuilder(module, typeRef, property.InvokeMethod);
        }

        private static EventDefinition FindEvent(TypeReference typeRef, string eventName)
        {
            var typeDef = typeRef.ResolveRequiredType();

            var events = typeDef.Events.Where(e => e.Name == eventName).ToList();

            return events.Count switch
            {
                1 => events.Single(),
                0 => throw new WeavingException($"Event '{eventName}' not found in type {typeDef.FullName}"),
                _ => throw new WeavingException($"Ambiguous event '{eventName}' in type {typeDef.FullName}")
            };
        }

        public static MethodRefBuilder Constructor(ModuleDefinition module, TypeReference typeRef, IReadOnlyList<TypeRefBuilder> paramTypes)
        {
            var typeDef = typeRef.ResolveRequiredType();

            var constructors = typeDef.GetConstructors()
                                      .Where(i => !i.IsStatic && i.Name == ".ctor" && SignatureMatches(i, paramTypes))
                                      .ToList();

            if (constructors.Count == 1)
                return new MethodRefBuilder(module, typeRef, constructors.Single());

            if (paramTypes.Count == 0)
                throw new WeavingException($"Type {typeDef.FullName} has no default constructor");

            throw new WeavingException($"Type {typeDef.FullName} has no constructor with signature ({string.Join(", ", paramTypes.Select(p => p.GetDisplayName()))})");
        }

        public static MethodRefBuilder TypeInitializer(ModuleDefinition module, TypeReference typeRef)
        {
            var typeDef = typeRef.ResolveRequiredType();

            var initializers = typeDef.GetConstructors()
                                      .Where(i => i.IsStatic && i.Name == ".cctor" && i.Parameters.Count == 0)
                                      .ToList();

            if (initializers.Count == 1)
                return new MethodRefBuilder(module, typeRef, initializers.Single());

            throw new WeavingException($"Type {typeDef.FullName} has no type initializer");
        }

        public static MethodRefBuilder Operator(ModuleDefinition module, TypeReference typeRef, UnaryOperator op)
        {
            var typeDef = typeRef.ResolveRequiredType();
            var memberName = $"op_{op}";

            var operators = typeDef.Methods
                                   .Where(i => i.IsStatic && i.IsSpecialName && i.Name == memberName && i.Parameters.Count == 1 && i.Parameters[0].ParameterType.FullName == typeDef.FullName)
                                   .ToList();

            return operators.Count switch
            {
                1 => new MethodRefBuilder(module, typeRef, operators.Single()),
                0 => throw new WeavingException($"Operator '{memberName}' not found in type {typeDef.FullName}"),
                _ => throw new WeavingException($"Ambiguous operator '{memberName}' in type {typeDef.FullName}")
            };
        }

        public static MethodRefBuilder Operator(ModuleDefinition module, TypeReference typeRef, BinaryOperator op, TypeRefBuilder leftOperandType, TypeRefBuilder rightOperandType)
        {
            var typeDef = typeRef.ResolveRequiredType();
            var memberName = $"op_{op}";
            var signature = new[] { leftOperandType, rightOperandType };

            var operators = typeDef.Methods
                                   .Where(i => i.IsStatic && i.IsSpecialName && i.Name == memberName && SignatureMatches(i, signature))
                                   .ToList();

            return operators.Count switch
            {
                1 => new MethodRefBuilder(module, typeRef, operators.Single()),
                0 => throw new WeavingException($"Operator '{memberName}' not found in type {typeDef.FullName}"),
                _ => throw new WeavingException($"Ambiguous operator '{memberName}' in type {typeDef.FullName}")
            };
        }

        public MethodReference Build()
            => _method;

        public void MakeGenericMethod(TypeReference[] genericArgs)
        {
            if (!_method.HasGenericParameters)
                throw new WeavingException($"Not a generic method: {_method.FullName}");

            if (genericArgs.Length == 0)
                throw new WeavingException("No generic arguments supplied");

            if (_method.GenericParameters.Count != genericArgs.Length)
                throw new WeavingException($"Incorrect number of generic arguments supplied for method {_method.FullName} - expected {_method.GenericParameters.Count}, but got {genericArgs.Length}");

            var genericInstance = new GenericInstanceMethod(_method);
            genericInstance.GenericArguments.AddRange(genericArgs);

            _method = _module.ImportReference(genericInstance);
        }

        public void SetOptionalParameters(TypeReference[] optionalParamTypes)
        {
            if (_method.CallingConvention != MethodCallingConvention.VarArg)
                throw new WeavingException($"Not a vararg method: {_method.FullName}");

            if (_method.Parameters.Any(p => p.ParameterType.IsSentinel))
                throw new WeavingException("Optional parameters for vararg call have already been supplied");

            if (optionalParamTypes.Length == 0)
                throw new WeavingException("No optional parameter type supplied for vararg method call");

            var methodRef = _method.Clone();
            methodRef.CallingConvention = MethodCallingConvention.VarArg;

            for (var i = 0; i < optionalParamTypes.Length; ++i)
            {
                var paramType = optionalParamTypes[i];
                if (i == 0)
                    paramType = paramType.MakeSentinelType();

                methodRef.Parameters.Add(new ParameterDefinition(paramType));
            }

            _method = _module.ImportReference(methodRef);
        }

        public override string ToString() => _method.ToString();
    }
}
