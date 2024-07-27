﻿using System;
using System.Reflection;
using InlineIL.Tests.Support;
using JetBrains.Annotations;
using Xunit;

namespace InlineIL.Tests.Weaving;

public class FieldRefTests() : ClassTestsBase("FieldRefTestCases")
{
    [Fact]
    public void should_handle_field_references()
    {
        var instance = GetInstance();
        instance.IntField = 42;
        var result = (int)instance.ReturnIntField();
        result.ShouldEqual(42);
    }

    [Fact]
    public void should_handle_fields_with_types_from_referenced_assemblies()
    {
        var instance = GetUnverifiableInstance();
        instance.ReadFieldFromReferencedAssemblyType();
    }

    [Fact]
    public void should_handle_pointer_fields_with_types_from_referenced_assemblies()
    {
        var instance = GetUnverifiableInstance();
        instance.ReadPointerFieldFromReferencedAssemblyType();
    }

    [Fact]
    public void should_handle_generic_fields_with_types_from_referenced_assemblies()
    {
        var instance = GetUnverifiableInstance();
        instance.ReadGenericPointerFieldFromReferencedAssemblyType();
    }

    [Fact]
    public void should_handle_generic_type_fields_from_referenced_assemblies()
    {
        var instance = GetUnverifiableInstance();
        instance.ReadGenericTypeFieldFromReferencedAssemblyType();
    }

    [Fact]
    public void should_reference_field_in_different_ways()
    {
        var result = (int[])GetInstance().ReturnStaticIntFieldInDifferentWays();
        result.ShouldAll(i => i == result[0]);
    }

    [Fact]
    public void should_report_null_field()
    {
        ShouldHaveError("NullField").ShouldContain("ldnull");
        ShouldHaveError("NullFieldRef").ShouldContain("ldnull");
    }

    [Fact]
    public void should_report_unknown_field()
    {
        ShouldHaveError("UnknownField").ShouldContain("Field 'Nope' not found");
    }

    [Fact]
    public void should_report_unconsumed_reference()
    {
        ShouldHaveError("UnusedInstance");
    }

    [Fact]
    public void should_handle_field_token_load()
    {
        var handle = (RuntimeFieldHandle)GetInstance().ReturnFieldHandle();
        FieldInfo.GetFieldFromHandle(handle).Name.ShouldEqual("IntField");
    }

    [Fact]
    public void should_get_static_field_from_generic_type()
    {
        var result = (int)GetInstance().GetValueFromGenericType();
        result.ShouldEqual(10);
    }

    [Fact]
    public void should_get_static_generic_field_from_generic_type()
    {
        var result = (int)GetInstance().GetValueFromGenericType2();
        result.ShouldEqual(10);
    }

    [Fact]
    public void should_get_field_from_imported_generic_type()
    {
        var result = (int)GetInstance().GetValueFromImportedGenericType();
        result.ShouldEqual(10);
    }

    [Fact]
    public void should_get_generic_field_from_imported_generic_type()
    {
        var result = (int)GetInstance().GetValueFromImportedGenericType2<int>(10);
        result.ShouldEqual(10);
    }
}

[UsedImplicitly]
public class FieldRefTestsStandard : FieldRefTests
{
    public FieldRefTestsStandard()
        => NetStandard = true;
}
