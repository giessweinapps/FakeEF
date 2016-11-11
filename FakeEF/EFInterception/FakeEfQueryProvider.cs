using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using FakeEF.Data;

namespace FakeEF.EFInterception
{
    public class FakeEfQueryProvider<T> : IQueryProvider where T : class, new()
    {
        private readonly StubDbSet<T> stubDbSet;
        private readonly DbContext dbContext;

        public FakeEfQueryProvider(StubDbSet<T> stubDbSet, DbContext dbContext)
        {
            this.stubDbSet = stubDbSet;
            this.dbContext = dbContext;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return stubDbSet.CurrentData.AsQueryable().Provider.CreateQuery(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var method = expression as MethodCallExpression;
            if (method != null && method.Method.Name == "SelectMany")
            {
                foreach (var arg in method.Arguments.Skip(1))
                {
                    var unaryExpression = arg as UnaryExpression;
                    var operand = unaryExpression?.Operand as LambdaExpression;
                    var member = operand?.Body as MemberExpression;

                    if (member?.Member != null)
                    {
                        stubDbSet.AddInclude(member.Member.Name);
                    }
                }
            }
            var result = stubDbSet as IQueryable<TElement>;
            if (result != null)
                return result;
            return stubDbSet.CurrentData.AsQueryable().Provider.CreateQuery<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return stubDbSet.CurrentData.AsQueryable().Provider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return stubDbSet.CurrentData.AsQueryable().Provider.Execute<TResult>(expression);
        }
    }
}