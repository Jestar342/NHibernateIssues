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
    public class SkipCount : IDisposable
    {
        private const string ConnectionString = "Data Source=:memory:;Version=3;New=True;";
        private const string Mapping = @"<?xml version=""1.0"" encoding=""utf-8""?>
<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2"" assembly=""NHibernateIssues"">
   <class name=""NHibernateIssues.MyClass"" table=""MyClass"">
      <id name=""Id"" type=""int"" column=""id"">
         <generator class=""native""/>
      </id>
      <property name=""Foo"" column=""foo"" type=""string""/>
   </class>
</hibernate-mapping>";

        private readonly IDbConnection connection = new SQLiteConnection(ConnectionString);
        private readonly ISession session;

        public SkipCount()
        {
            var configuration = new Configuration();

            configuration
                .DataBaseIntegration(x =>
                {
                    x.ConnectionString = ConnectionString;
                    x.Driver<SQLite20Driver>();
                    x.Dialect<SQLiteDialect>();
                })
                .AddXmlString(Mapping);

            var sessionFactory = configuration.BuildSessionFactory();

            connection.Open();

            session = sessionFactory.OpenSession(connection);

            new SchemaExport(configuration).Execute(false, true, false, connection, Console.Out);
        }

        public void Dispose()
        {
            session?.Dispose();
            connection?.Dispose();
        }

        [Fact]
        public void GoesBoom()
        {
            var count = session.Query<MyClass>().Skip(100).Count();
            Assert.Equal(count, 0);
        }
    }

    public class MyClass
    {
        public virtual string Foo { get; set; }
        public virtual int Id { get; set; }
    }
}