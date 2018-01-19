using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using Xunit;

namespace NHibernateIssues
{
    public class NestedComponents : IDisposable
    {
        const string ConnectionString = "Data Source=:memory:;Version=3;New=True;";
        const string Mapping = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2"" assembly=""NHibernateIssues"">
  <class name=""NHibernateIssues.TopLevelClassWithComponent"">
    <id name=""Id"" type=""int"" column=""id"">
      <generator class=""native""/>
    </id>

    <component name=""Component"" class=""NHibernateIssues.AbstractComponent"">
      <component name=""Component"" class=""NHibernateIssues.SecondLevelClassWithComponent"">
        <property name=""Foo"" />
      </component>
    </component>
  </class>
</hibernate-mapping>";

        readonly IDbConnection connection = new SQLiteConnection(ConnectionString);
        readonly ISession session;

        public NestedComponents()
        {
            var configuration = new Configuration();
            configuration
                .DataBaseIntegration(x =>
                {
                    x.ConnectionString = ConnectionString;
                    x.Driver<SQLite20Driver>();
                    x.Dialect<SQLiteDialect>();
                    x.LogSqlInConsole = true;
                    x.LogFormattedSql = true;
                })
                .AddXmlString(Mapping);

            var sessionFactory = configuration.BuildSessionFactory();

            connection.Open();

            session = sessionFactory.OpenSession(connection);

            new SchemaExport(configuration).Execute(false, true, false, connection, Console.Out);
        }

        [Fact]
        public void Pops()
        {
            session.Save(new TopLevelClassWithComponent {Component = new DerivativeComponent { Component = new SecondLevelClassWithComponent { Foo = "bar"}}});
            session.Flush();
            session.Clear();

            var where = session.Query<TopLevelClassWithComponent>()
                .Where(x => x.Component.Component.Foo.Contains("bar"));

            Assert.NotEmpty(where);
        }

        public void Dispose()
        {
            connection?.Dispose();
            session?.Dispose();
        }
    }

    public class TopLevelClassWithComponent
    {
        public virtual int Id { get; set; }
        public virtual AbstractComponent Component { get; set; }
    }

    
    public class AbstractComponent
    {
        public virtual int Id { get; set; }

        public virtual SecondLevelClassWithComponent Component { get; set; }
    }

    public class SecondLevelClassWithComponent
    {
        public virtual string Foo { get; set; }
    }

    public class DerivativeComponent : AbstractComponent
    {
        
    }
}