﻿using System.Diagnostics.CodeAnalysis;

namespace InlineIL.Fody.Processing
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    internal static class KnownNames
    {
        public static class Short
        {
            public const string IlType = "IL";
            public const string IlEmitType = "IL/Emit";
            public const string TypeRefType = "TypeRef";
            public const string MethodRefType = "MethodRef";
            public const string FieldRefType = "FieldRef";
            public const string StandAloneMethodSigType = "StandAloneMethodSig";

            public const string PushMethod = "Push";
            public const string PushInRefMethod = "PushInRef";
            public const string PushOutRefMethod = "PushOutRef";
            public const string PopMethod = "Pop";
            public const string UnreachableMethod = "Unreachable";
            public const string ReturnMethod = "Return";
            public const string ReturnRefMethod = "ReturnRef";
            public const string ReturnPointerMethod = "ReturnPointer";
            public const string MarkLabelMethod = "MarkLabel";
            public const string DeclareLocalsMethod = "DeclareLocals";
            public const string EnsureLocalMethod = "EnsureLocal";
        }

        public static class Full
        {
            private const string _nsPrefix = "InlineIL.";

            public const string IlType = _nsPrefix + Short.IlType;
            public const string IlEmitType = _nsPrefix + Short.IlEmitType;
            public const string TypeRefType = _nsPrefix + Short.TypeRefType;
            public const string MethodRefType = _nsPrefix + Short.MethodRefType;
            public const string FieldRefType = _nsPrefix + Short.FieldRefType;
        }
    }
}
