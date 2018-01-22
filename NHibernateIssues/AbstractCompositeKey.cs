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
    public class AbstractCompositeKey : IDisposable
    {
        const string ConnectionString = "Data Source=:memory:;Version=3;New=True;";

        const string Mapping = @"<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2"">
  <class name=""NHibernateIssues.AbstractClass, NHibernateIssues"" table=""Derivatives"">
    <composite-id>
      <key-many-to-one name=""IdOne"" class=""NHibernateIssues.KeyClass, NHibernateIssues"" foreign-key=""FK_IdOne"" lazy=""false"" />
      <key-many-to-one name=""IdTwo"" class=""NHibernateIssues.KeyClass, NHibernateIssues"" foreign-key=""FK_IdTwo"" lazy=""false"" />
    </composite-id>
    <discriminator type=""String"">
      <column name=""Type"" />
    </discriminator>
    <subclass name=""NHibernateIssues.DerivativeOne, NHibernateIssues"" discriminator-value=""1"" />
    <subclass name=""NHibernateIssues.DerivativeTwo, NHibernateIssues"" discriminator-value=""2"" />
  </class>
  <class name=""NHibernateIssues.KeyClass, NHibernateIssues"" table=""Keys"">
    <id name=""Id"" type=""System.Guid, mscorlib"">
        <column name=""[Id]""/>
        <generator class=""assigned""/>
    </id>
  </class>
</hibernate-mapping>";

        readonly IDbConnection connection = new SQLiteConnection(ConnectionString);
        readonly ISession session;

        public AbstractCompositeKey()
        {
            var configuration = new Configuration();
            configuration
                .DataBaseIntegration(
                    x =>
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

            var idOne = new KeyClass { Id = Guid.NewGuid() };
            var idTwo = new KeyClass { Id = Guid.NewGuid() };

            session.Save(idOne);
            session.Save(idTwo);

            session.Save(
                new DerivativeOne
                {
                    IdOne = idOne,
                    IdTwo = idTwo
                });

            session.Flush();
            session.Clear();
        }

        public void Dispose()
        {
            session?.Dispose();
            connection?.Dispose();
        }

        [Fact]
        public void Linq()
        {
            var things = session.Query<AbstractClass>().ToArray();

            Assert.Single(things);
        }

        [Fact]
        public void Criteria()
        {
            var thing = session.CreateCriteria<AbstractClass>().UniqueResult<AbstractClass>();

            Assert.NotNull(thing);
        }

        [Fact]
        public void QueryOver()
        {
            var thing = session.QueryOver<AbstractClass>().SingleOrDefault();

            Assert.NotNull(thing);
        }

    }

    public abstract class AbstractClass : IEquatable<AbstractClass>
    {
        public virtual KeyClass IdOne { get; set; }
        public virtual KeyClass IdTwo { get; set; }

        public virtual bool Equals(AbstractClass other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(IdOne, other.IdOne)
                   && Equals(IdTwo, other.IdTwo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AbstractClass) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((IdOne != null ? IdOne.GetHashCode() : 0) * 397) ^ (IdTwo != null ? IdTwo.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AbstractClass left, AbstractClass right) => Equals(left, right);

        public static bool operator !=(AbstractClass left, AbstractClass right) => !Equals(left, right);
    }

    public class DerivativeOne : AbstractClass
    { }

    public class DerivativeTwo : AbstractClass
    { }

    public class KeyClass
    {
        public virtual Guid Id { get; set; }
    }
}