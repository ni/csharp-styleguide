using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public class DatabaseColumnsShouldBeNullableAnalyzerTests : NIDiagnosticAnalyzerTests<DatabaseColumnsShouldBeNullableAnalyzer>
    {
        private const string Setup = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore
{
    public class DbContext
    {
    }

    public class DbSet<T> : IEnumerable<T>, IEnumerable
        where T : class
    {
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
    }
}

namespace System.ComponentModel.DataAnnotations.Schema
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotMappedAttribute : Attribute
    {
    }
}

class MyDbContext : DbContext
{
    public DbSet<MyDataModel> Data { get; set; }
}";

        [Theory]
        [InlineData("Id")]
        [InlineData("MyDataModelId")]
        [InlineData("MyPk", "[Key]")]
        public void NI0017_PrimaryKey_NoDiagnostic(string propertyName, string? attribute = null)
        {
            var test = new AutoTestFile(Setup + $@"
class MyDataModel
{{
    {attribute}
    public int {propertyName} {{ get; set; }}
}}");

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("DateTime?")]
        [InlineData("Nullable<DateTime>")]
        [InlineData("IEnumerable<DateTime>")]
        [InlineData("int", "[NotMapped]")]
        public void NI0017_AcceptableTypes_NoDiagnostic(string type, string? attribute = null)
        {
            var test = new AutoTestFile(Setup + $@"
class MyDataModel
{{
    public int Id {{ get; set; }}
    {attribute ?? string.Empty}
    public {type} Data {{ get; set; }}
}}");

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("int", "Int32")]
        [InlineData("DateTime")]
        public void NI0017_ValueTypes_Diagnostic(string type, string? typeDiagnostic = null)
        {
            var test = new AutoTestFile(
                Setup + $@"
class MyDataModel
{{
    public int Id {{ get; set; }}
    public {type} <?>Data {{ get; set; }}
}}",
                GetNI0017ValueTypeRule(typeDiagnostic ?? type, "Data", "MyDataModel"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI0017_CustomIEnumerableDateTimeMember_Diagnostic()
        {
            var test = new AutoTestFile(
                Setup + @"
class CustomEnumerable : IEnumerable<DateTime>
{
    public IEnumerator<DateTime> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}

class MyDataModel
{
    public int Id { get; set; }
    public CustomEnumerable <?>Data { get; set; }
}",
                GetNI0017UnknownIEnumerableTypeRule("CustomEnumerable", "Data", "MyDataModel"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI0017_RecursesThroughTypes_Diagnostic()
        {
            var test = new AutoTestFile(
                Setup + @"
class MyOtherModel
{
    public int Id { get; set; }
    public DateTime <?>Data { get; set; }
}

class MyDataModel
{
    public int Id { get; set; }
    public MyOtherModel Data { get; set; }
}",
                GetNI0017ValueTypeRule("DateTime", "Data", "MyOtherModel"));

            VerifyDiagnostics(test);
        }

        private Rule GetNI0017ValueTypeRule(string type, string property, string @class)
        {
            return new Rule(DatabaseColumnsShouldBeNullableAnalyzer.ValueTypeRule, type, property, @class);
        }

        private Rule GetNI0017UnknownIEnumerableTypeRule(string type, string property, string @class)
        {
            return new Rule(DatabaseColumnsShouldBeNullableAnalyzer.IEnumerableColumnRule, type, property, @class);
        }
    }
}
